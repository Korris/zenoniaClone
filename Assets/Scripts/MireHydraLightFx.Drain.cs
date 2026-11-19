using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Venom Drain (skill 4) of MireHydraLightFx, same shader / particle kit as the other Hydra skills (no sprite frames):
    ///  - warning (t &lt; 0): the usual WantedWarningDisc circle round the boss (BuildWarning), plus a purple sheet + moss core tether
    ///    (LightBeamScroll, rays scrolling hero -&gt; boss) from every POISONED hero inside the circle to the boss's body, with venom bubbles
    ///    flowing along it; at most MaxTethers drawn (your own hero first, then party, then nearest), as ChatGPT advised for 40 players;
    ///  - strike (t = 0, the server's damage instant): the tethers pulse wide and bright, gone at DrainPulseMs; a green heal column + bubble
    ///    fountain rises on the boss (HealFlash, HealFlashMs). Who is drawn is the client's guess from the replicated poison (PoisonMsLeft); the
    ///    server picks the real victims at the strike instant with the same rule.
    /// Every curve lives in WantedSkillLightTiming (tested).
    /// </summary>
    public sealed partial class MireHydraLightFx
    {
        const int MaxTethers = 8;
        const float TetherWidth = 7f, BossBodyLift = 30f, HeroChestLift = 9f;
        readonly List<HeroController> drainPicks = new List<HeroController>(MaxTethers);
        readonly Vector2[] drainEnds = new Vector2[MaxTethers];
        MeshRenderer[] tethers;
        MeshRenderer healColumn, healCore;
        bool healBurst;

        void TickDrain(float t)
        {
            if (t < 0f) PickDrainTargets(-t);               // frozen at the strike: the drained heroes lose their poison right then
            Vector2 boss = origin + new Vector2(0f, BossBodyLift);
            float alpha = WantedSkillLightTiming.TetherAlpha(skill, t), width = TetherWidth * WantedSkillLightTiming.TetherWidth(t);
            if (tethers == null) tethers = new MeshRenderer[MaxTethers * 2];
            for (int i = 0; i < MaxTethers; i++)
            {
                bool on = i < drainPicks.Count && alpha > 0f;
                if (on && t < 0f && drainPicks[i] != null) drainEnds[i] = drainPicks[i].Position + new Vector2(0f, HeroChestLift);
                for (int k = 0; k < 2; k++)
                {
                    MeshRenderer r = tethers[i * 2 + k];
                    if (!on) { if (r != null) r.enabled = false; continue; }
                    if (r == null)
                        r = tethers[i * 2 + k] = LightSkillAssets.Quad(transform, k == 0 ? "Tether" : "TetherCore", k == 0 ? MireHydraFxLook.StreamOuter : MireHydraFxLook.StreamCore, true, FeelConfig.FxOrder - 26 + k);
                    Vector2 d = boss - drainEnds[i];
                    r.enabled = true;
                    r.transform.position = new Vector3(drainEnds[i].x, drainEnds[i].y, 0f);  // bottom pivot on the hero: the rays scroll toward the boss
                    r.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
                    r.transform.localScale = new Vector3(k == 0 ? width : width * 0.4f, Mathf.Max(0.01f, d.magnitude), 1f);
                    LightSkillAssets.Set(r, k == 0 ? alpha * 0.85f : alpha);
                }
                if (on && t < 0f) FlowBubbles(drainEnds[i], boss);
            }
            TickHealFlash(t, boss);
        }

        /// <summary>Living heroes inside the circle still poisoned at the strike (`msToStrike` away): self, then party, then the nearest,
        /// at most MaxTethers.</summary>
        void PickDrainTargets(float msToStrike)
        {
            drainPicks.Clear();
            if (Zone.Main == null) return;
            var all = Zone.Main.Heroes.All;
            var shape = new WantedShapeInstance { OriginX = origin.x, OriginY = origin.y };
            HeroController me = LocalPlayer.Hero;
            var others = new List<HeroController>();
            for (int i = 0; i < all.Count; i++)
            {
                HeroController h = all[i];
                if (h == null || h.IsDead || h.PoisonMsLeft <= msToStrike ||!WantedSkillArea.Hits(skill, shape, h.Position.x, h.Position.y)) continue;
                if (h == me) drainPicks.Insert(0, h); else others.Add(h);
            }
            others.Sort((a, b) => Rank(a).CompareTo(Rank(b)));
            for (int i = 0; i < others.Count && drainPicks.Count < MaxTethers; i++) drainPicks.Add(others[i]);
        }

        /// <summary>Sort key: party members first (0 .. 1e6), then everyone else, each by distance to the boss.</summary>
        float Rank(HeroController h) { return (h.IsPartyMateOfLocal ? 0f : 1e6f) + (h.Position - origin).sqrMagnitude; }

        void FlowBubbles(Vector2 from, Vector2 to)
        {
            if (Random.value > 10f * GameClock.Dt) return;   // about 10 a second per tether
            Vector2 d = to - from;
            float life = 0.55f;
            WantedFxParticles.Emit(bubbles, from + d * Random.Range(0f, 0.3f), d / life * 0.7f, Random.Range(1.6f, 2.6f), WantedFxParticles.VenomColor(), life);
        }

        void TickHealFlash(float t, Vector2 boss)
        {
            float f = WantedSkillLightTiming.HealFlash(t);
            if (t < 0f) return;
            if (healColumn == null)
            {
                int order = GameConfig.OrderBase(origin.y) + 4;                       // y-sorted with the boss: heroes in front stay on top
                healColumn = LightSkillAssets.Quad(transform, "HealColumn", MireHydraFxLook.StreamCore, true, order);
                healCore = LightSkillAssets.Quad(transform, "HealCore", MireHydraFxLook.ColumnCore, true, order + 1);
                healColumn.transform.position = healCore.transform.position = new Vector3(origin.x, origin.y - 4f, 0f);
            }
            float rise = Mathf.Clamp01(t / 160f);
            healColumn.transform.localScale = new Vector3(34f, 80f * (0.5f + 0.5f * rise), 1f);
            healCore.transform.localScale = new Vector3(12f, 74f * (0.5f + 0.5f * rise), 1f);
            LightSkillAssets.Set(healColumn, f * 0.8f);
            LightSkillAssets.Set(healCore, f);
            if (healBurst) return;
            healBurst = true;
            for (int i = 0; i < 26; i++)
            {
                Vector2 p = origin + new Vector2(Random.Range(-14f, 14f), Random.Range(-2f, 6f));
                WantedFxParticles.Emit(bubbles, p, new Vector2(Random.Range(-6f, 6f), Random.Range(30f, 55f)), Random.Range(2f, 3.4f), MireHydraFxLook.Moss, Random.Range(0.6f, 0.9f));
            }
        }
    }
}
