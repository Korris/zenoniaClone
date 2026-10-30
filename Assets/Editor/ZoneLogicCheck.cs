using System;
using System.Reflection;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto.EditorTools
{
    /// <summary>
    /// Zone / multi-hero checks (called from FrostSmokeTest): the pure rules (boss respawn clock, loot priority, EXP split) and a small live
    /// world with two simulated heroes (nearest-hero targeting, sticky aggro + retarget on death, dormancy, damage attribution, loot owner,
    /// boss timer online vs offline). Monsters are stepped by hand through reflection because edit mode has no Update loop.
    /// </summary>
    public static class ZoneLogicCheck
    {
        const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

        public static void RunPure(Action<bool, string> expect)
        {
            var t = new BossRespawnTimer();
            expect(!t.Active && !t.Tick(1000f), "zone: boss timer idle until started");
            t.Start(100f);
            expect(!t.Tick(60f) && t.Active && t.Tick(60f) && !t.Active && !t.Tick(1000f), "zone: boss timer fires once after its delay");
            expect(Mathf.Approximately(BossRespawnTimer.DefaultRespawnMs, 30f * 60f * 1000f), "zone: boss respawns after 30 min");

            float freeAt = LootOwnership.FreeAtMs(1000f, false);
            expect(Mathf.Approximately(freeAt, 4000f) && Mathf.Approximately(LootOwnership.FreeAtMs(1000f, true), 6000f), "zone: loot priority 3 s (+2 s with a party)");
            expect(LootOwnership.CanPick(0, 0f, 5, 0f, false), "zone: unowned loot is free");
            expect(LootOwnership.CanPick(7, freeAt, 7, 1500f, false) && !LootOwnership.CanPick(7, freeAt, 8, 1500f, false), "zone: owner only during priority");
            expect(LootOwnership.CanPick(7, freeAt, 8, 4000f, false), "zone: anyone after the priority window");
            expect(LootOwnership.CanPick(7, 6000f, 8, 4000f, true) && !LootOwnership.CanPick(7, 6000f, 9, 4000f, false), "zone: owner's party mates share the window");

            int[] split = KillRewards.Split(14, new[] { 22, 14 }, 36, 0);
            expect(split[0] == 9 && split[1] == 5 && split[0] + split[1] == 14, "zone: EXP split by damage share, remainder to the top dealer (9 + 5 = 14)");
            expect(KillRewards.Split(280, new[] { 5 }, 5, 0)[0] == 280 && KillRewards.Split(10, new int[0], 0, -1).Length == 0, "zone: single attacker gets everything");
        }

        public static void RunWorld(Action<bool, string> expect, HeroController player, GameObject root)
        {
            Zone zone = Zone.Main;
            expect(zone != null && zone.Heroes.ById(player.NetId) == player && player.NetId != 0, "zone: local hero registered with a NetId");
            player.God = true;
            HeroController other = MakeHero(root, HeroClass.Wizard);
            expect(other.NetId != 0 && other.NetId != player.NetId && other.Owner != null, "zone: second (headless) hero gets its own NetId");
            try
            {
                CheckTargeting(expect, zone, player, other);
                CheckDormancy(expect, zone, player, other);
                CheckAttribution(expect, zone, player, other);
                CheckOnlineRules(expect, zone, player, other);
            }
            finally { NetMode.Current = RunMode.Offline; player.God = false; }
        }

        static HeroController MakeHero(GameObject root, HeroClass id)
        {
            var go = new GameObject("TestHero_" + id);
            go.transform.SetParent(root.transform, false);
            HeroController h = go.AddComponent<HeroController>();
            h.InitHeadless(HeroClassDef.Get(id), PlayerState.NewGame(RpgSession.ClassId(id)));
            h.Owner = new PlayerRpg(h.Rpg);
            return h;
        }

        static Monster Fresh(Zone zone, int skip = 0)
        {
            foreach (Monster m in zone.Monsters) if (m.Type == "frostSlime" && !m.IsDying && skip-- <= 0) return m; // slimes only: the EXP / HP numbers below assume 36 HP and 14 EXP
            return null;
        }

        /// <summary>Kills a hero through the real damage path (edit mode never ticks the invulnerability frames, so clear them first).</summary>
        static void Kill(HeroController h)
        {
            typeof(HeroController).GetField("invuln", Priv).SetValue(h, 0f);
            h.TakeDamage(99999);
        }

        static void Step(Monster m) { typeof(Monster).GetMethod("Step", Priv).Invoke(m, new object[] { 0.016f, 16f }); }
        static T Field<T>(Monster m, string name) { return (T)typeof(Monster).GetField(name, Priv).GetValue(m); }

        static void CheckTargeting(Action<bool, string> expect, Zone zone, HeroController a, HeroController b)
        {
            Monster m = Fresh(zone);
            Vector2 at = m.Feet;
            a.Teleport(at + new Vector2(0f, 150f), 0);
            b.Teleport(at + new Vector2(0f, 60f), 0);
            expect(zone.Heroes.NearestAlive(at, 320f) == b, "zone: NearestAlive picks the closer hero");
            Step(m);
            expect(Field<HeroController>(m, "target") == b, "monster: targets the nearest living hero");
            m.TakeHit(1, Vector2.zero, false, false, true, b); // b hurts it: sticky aggro, Chase
            a.Teleport(at + new Vector2(0f, 20f), 0);          // a is now closer, but b still owns the fight
            Step(m);
            expect(Field<HeroController>(m, "target") == b, "monster: aggro target is sticky while it lives");
            Kill(b);
            Step(m);
            expect(b.IsDead && Field<HeroController>(m, "target") == a, "monster: retargets to the nearest living hero when the target dies");
            b.Teleport(at + new Vector2(0f, 60f), 0);
            typeof(HeroController).GetField("dead", Priv).SetValue(b, false); // revive for the next checks
        }

        static void CheckDormancy(Action<bool, string> expect, Zone zone, HeroController a, HeroController b)
        {
            Monster m = Fresh(zone, 1);
            Vector2 at = m.Feet;
            a.Teleport(at + new Vector2(Monster.ActiveRadius + 80f, 0f), 0);
            b.Teleport(at + new Vector2(0f, -(Monster.ActiveRadius + 80f)), 0);
            float timer = Field<float>(m, "timer");
            Step(m);
            expect(Mathf.Approximately(Field<float>(m, "timer"), timer) && m.Feet == at, "monster: dormant with no hero within ActiveRadius");
            b.Teleport(at + new Vector2(0f, -100f), 0);
            Step(m);
            expect(!Mathf.Approximately(Field<float>(m, "timer"), timer), "monster: wakes when a hero comes within ActiveRadius");
        }

        static void CheckAttribution(Action<bool, string> expect, Zone zone, HeroController a, HeroController b)
        {
            a.Teleport(Vector2.zero, 0); b.Teleport(Vector2.zero, 0);
            Monster m = Fresh(zone, 2);
            int ea = a.Rpg.Exp, eb = b.Rpg.Exp, drops = zone.Loot.Count;
            m.TakeHit(22, Vector2.zero, false, false, true, a);
            m.TakeHit(20, Vector2.zero, false, false, true, b); // only 14 HP were left: overkill is not credited
            expect(m.IsDying && m.DamageBy[a.NetId] == 22 && m.DamageBy[b.NetId] == 14, "attribution: damage table per hero, overkill capped (22 / 14)");
            expect(a.Rpg.Exp - ea == 9 && b.Rpg.Exp - eb == 5, "attribution: EXP split by damage share (" + (a.Rpg.Exp - ea) + " / " + (b.Rpg.Exp - eb) + " of 14)");
            bool unowned = true;
            for (int i = drops; i < zone.Loot.Count; i++) unowned &= zone.Loot[i].OwnerId == 0;
            expect(zone.Loot.Count > drops && unowned, "attribution: offline drops have no owner");

            Monster n = Fresh(zone, 2);
            a.Rpg.Exp = 0; b.Rpg.Exp = 0; ea = 0; eb = 0; // no level-up in the middle of the measurement
            n.TakeHit(10, Vector2.zero, false, false, true, b);
            Kill(b);
            n.TakeHit(99999, Vector2.zero, false, false, true, a);
            expect(a.Rpg.Exp - ea == 14 && b.Rpg.Exp == eb, "attribution: a dead contributor gets no EXP, the killer keeps the rest");
            typeof(HeroController).GetField("dead", Priv).SetValue(b, false);
        }

        static void CheckOnlineRules(Action<bool, string> expect, Zone zone, HeroController a, HeroController b)
        {
            NetMode.Current = RunMode.Host; // online rules (loot owner, boss timer) without the headless view skipping
            Monster m = Fresh(zone, 2);
            int drops = zone.Loot.Count;
            m.TakeHit(10, Vector2.zero, false, false, true, b);
            m.TakeHit(99999, Vector2.zero, false, false, true, a);
            bool owned = zone.Loot.Count > drops;
            for (int i = drops; i < zone.Loot.Count; i++)
                owned &= zone.Loot[i].OwnerId == a.NetId && Mathf.Abs(zone.Loot[i].FreeAtMs - (zone.TimeMs + LootOwnership.PriorityMs)) < 1f;
            expect(owned, "online: loot owned by the top damage dealer for 3 s");

            Monster boss = Monster.Boss;
            boss.TakeHit(999999, Vector2.zero, false, false, true, a);
            expect(Monster.Boss == null && zone.BossTimer.Active, "online: boss death starts the respawn timer");
            zone.Advance(BossRespawnTimer.DefaultRespawnMs / 1000f + 1f);
            expect(Monster.Boss != null && !zone.BossTimer.Active, "online: the king is back after the timer");

            NetMode.Current = RunMode.Offline;
            Monster.Boss.TakeHit(999999, Vector2.zero, false, false, true, a);
            expect(Monster.Boss == null && !zone.BossTimer.Active, "offline: no respawn timer (the save flag rules)");
        }
    }
}
