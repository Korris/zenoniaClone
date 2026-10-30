using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// Client side replay of server events. Batches arrive stamped with the server time of the tick that produced them and are played when the
    /// render time (server now - 150 ms) reaches that stamp, so a swing, its hit numbers and the monster's death line up with the interpolated
    /// positions. Everything here is presentation: FX, sound, damage numbers, toasts -- no state is decided from events.
    /// </summary>
    public sealed class NetEventPlayer
    {
        struct Stamped { public double T; public NetEvent Ev; }
        readonly Queue<Stamped> queue = new Queue<Stamped>(64);
        readonly NetStats stats;
        readonly ushort myHeroId;

        public NetEventPlayer(ushort localHeroId, NetStats netStats) { myHeroId = localHeroId; stats = netStats; }

        public int Pending { get { return queue.Count; } }

        public void Enqueue(uint serverMs, List<NetEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                queue.Enqueue(new Stamped { T = serverMs, Ev = events[i] });
                if (events[i].Type == NetEventType.BossDied) stats.Mark("bossDiedRecvUtcMs", NetStats.UtcMs.ToString()); // arrival time: the harness compares it with the server's
            }
            stats.Add("eventsReceived", events.Count);
        }

        public void Play(double renderTime)
        {
            while (queue.Count > 0 && queue.Peek().T <= renderTime)
            {
                Stamped s = queue.Dequeue();
                try { PlayOne(s.Ev, s.T); }
                catch (System.Exception e) { Debug.LogException(e); } // one bad event must not stall the stream
            }
        }

        static HeroController HeroOf(ushort id) { return Zone.Main != null ? Zone.Main.Ids.Get(id) as HeroController : null; }
        static Monster MonsterOf(ushort id) { return Zone.Main != null ? Zone.Main.Ids.Get(id) as Monster : null; }

        void PlayOne(NetEvent e, double t)
        {
            bool fx = NetMode.RendersFx; // an unattended -nographics bot client replays nothing visual
            var at = new Vector2(NetQuantize.Pos(e.X), NetQuantize.Pos(e.Y));
            switch (e.Type)
            {
                case NetEventType.AttackStep:
                    HeroController swinger = HeroOf(e.Id);
                    if (swinger != null && swinger.IsPredicted) break; // the local hero swung the moment the key went down
                    if (fx && swinger != null) swinger.PlaySwingFx(e.Step & 0x7F, (e.Step & 0x80) != 0, e.Facing);
                    break;
                case NetEventType.BoltFired:
                    HeroController shooter = HeroOf(e.Other);
                    if (shooter != null && shooter.AdoptPredictedBolt(e.Id)) break; // already flying: it only needed the server's id
                    if (fx && shooter != null) shooter.PlayBoltFx(e.Step, at, NetQuantize.Angle(e.Angle), (e.Flags & 1) != 0, e.Id);
                    break;
                case NetEventType.BoltEnd:
                    if (fx) ComboProjectile.EndRemote(e.Id, at, (e.Flags & 1) != 0, (e.Flags & 2) != 0);
                    break;
                case NetEventType.MonsterHit:
                    Monster m = MonsterOf(e.Id);
                    stats.Add("monsterHitsSeen", 1);
                    if (m == null) break;
                    bool crit = (e.Flags & HitFlags.Crit) != 0, finisher = (e.Flags & HitFlags.Finisher) != 0;
                    HeroController src = HeroOf(e.Other);
                    bool others = src != LocalPlayer.Hero; // only your own hits get the big yellow number; no shake / hit-stop for the others either
                    m.ProxyHit(e.Dmg, e.HpLeft, new Vector2(e.KnockX * 2f, e.KnockY * 2f), crit, finisher, (e.Flags & HitFlags.Quiet) != 0, others);
                    if (fx && src != null && src == LocalPlayer.Hero && !src.Class.Ranged) HeroFx.MeleeLanded(src.Class, crit, finisher);
                    if (e.HpLeft == 0) stats.Add("monstersKilledSeen", 1);
                    break;
                case NetEventType.HeroHit:
                    HeroController hit = HeroOf(e.Id);
                    if (hit != null) hit.ProxyPlayHurt(e.Dmg);
                    break;
                case NetEventType.SkillCast:
                    HeroController caster = HeroOf(e.Id);
                    stats.Add("skillCastsSeen", 1);
                    if (caster != null && caster.IsPredicted && caster.PredictsSkillFx) break; // started locally
                    if (fx && caster != null) caster.PlaySkillFx(at, e.Other);
                    break;
                case NetEventType.ExpGain:
                    HeroController me = LocalPlayer.Hero;
                    stats.Add("expGained", e.Value);
                    if (fx && me != null) DamageNumber.Spawn(me.Position.x + 6f, me.Position.y + 20f, "+" + e.Value + " EXP", new Color(0.75f, 0.6f, 1f), false);
                    break;
                case NetEventType.LevelUp:
                    HeroController lv = HeroOf(e.Id);
                    stats.Add("levelUpsSeen", 1);
                    if (fx && lv != null) HeroFx.LevelUp(lv.Position, e.Step, lv == LocalPlayer.Hero);
                    break;
                case NetEventType.PotionUsed:
                    HeroController dr = HeroOf(e.Id);
                    if (fx && dr != null) HeroFx.Potion(dr.Position, e.Step == 1, e.Step == 1 ? "HP +30%" : "MP +30%");
                    break;
                case NetEventType.AppearanceChanged:
                    HeroController look = HeroOf(e.Id);
                    if (fx && look != null && look != LocalPlayer.Hero) look.ProxyLook(e.Look);
                    break;
                case NetEventType.Toast:
                    stats.Add("toastsSeen", 1);
                    if (fx) RpgHud.Toast(e.Text ?? "", new Color32((byte)(e.Value >> 24), (byte)(e.Value >> 16), (byte)(e.Value >> 8), (byte)e.Value));
                    break;
                case NetEventType.BossSpawned:
                    stats.Add("bossSpawnedSeen", 1);
                    stats.Mark("bossSpawnedSeenUtcMs", NetStats.UtcMs.ToString());
                    break;
                case NetEventType.BossDied:
                    stats.Add("bossDiedSeen", 1);
                    stats.Mark("bossDiedPlayedUtcMs", NetStats.UtcMs.ToString());
                    if (fx) RpgHud.Toast("The Frostfang King has fallen!", new Color(1f, 0.9f, 0.5f));
                    break;
            }
        }
    }
}
