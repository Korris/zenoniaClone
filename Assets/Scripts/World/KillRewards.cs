using System;
using System.Collections.Generic;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// What a kill pays out (decided rules): EXP is split by damage share among the heroes that hit the monster and are still alive in the zone,
    /// the top damage dealer owns the drops for 3 s (online only), and every contributor is told when it was the boss. Offline the player keeps
    /// the whole reward even when a debug party member made the kill (party members have no PlayerRpg).
    /// </summary>
    public static class KillRewards
    {
        static readonly List<HeroController> Winners = new List<HeroController>(8);
        static readonly List<int> Damage = new List<int>(8);

        public static void Resolve(Zone zone, Monster m)
        {
            MonsterLootDef def = LootTable.For(m.Type);
            if (def == null) return;
            Collect(zone, m);
            int topIdx = -1;
            long total = 0;
            for (int i = 0; i < Winners.Count; i++)
            {
                total += Damage[i];
                // most damage wins; a tie goes to the lower NetId so the result never depends on dictionary order
                if (topIdx < 0 || Damage[i] > Damage[topIdx] || (Damage[i] == Damage[topIdx] && Winners[i].NetId < Winners[topIdx].NetId)) topIdx = i;
            }
            HeroController top = topIdx >= 0 ? Winners[topIdx] : null;

            int[] exp = Split(def.Exp, Damage, total, topIdx);
            for (int i = 0; i < Winners.Count; i++)
                if (exp[i] > 0 || Winners.Count == 1) Winners[i].Owner.GainExp(exp[i], m.Feet);

            ushort ownerId = 0;
            float freeAt = 0f;
            if (!NetMode.IsOffline && top != null) { ownerId = top.NetId; freeAt = LootOwnership.FreeAtMs(zone.TimeMs, false); }
            LootDrop.SpawnAll(zone.Root, LootTable.RollAll(def, RpgRandom.Default), m.Feet, ownerId, freeAt);

            if (m.IsBoss) for (int i = 0; i < Winners.Count; i++) Winners[i].Owner.RaiseBossKilled();
        }

        /// <summary>Contributors that can receive rewards: alive, still in the zone, with an RPG owner. Offline falls back to the local player.</summary>
        static void Collect(Zone zone, Monster m)
        {
            Winners.Clear(); Damage.Clear();
            foreach (KeyValuePair<ushort, int> kv in m.DamageBy)
            {
                HeroController h = zone.Heroes.ById(kv.Key);
                if (h == null || h.IsDead || h.Owner == null) continue;
                Winners.Add(h); Damage.Add(kv.Value);
            }
            if (Winners.Count > 0 || !NetMode.IsOffline) return;
            HeroController local = LocalPlayer.Hero;
            if (local != null && local.Owner != null) { Winners.Add(local); Damage.Add(1); }
        }

        /// <summary>Whole-number EXP per winner proportional to damage; rounding leftovers go to the top damage dealer so the total is exact.</summary>
        public static int[] Split(int totalExp, IList<int> damage, long totalDamage, int topIndex)
        {
            var result = new int[damage.Count];
            if (damage.Count == 0 || totalDamage <= 0) return result;
            int given = 0;
            for (int i = 0; i < damage.Count; i++) { result[i] = (int)Math.Floor((double)totalExp * damage[i] / totalDamage); given += result[i]; }
            if (topIndex >= 0 && topIndex < result.Length) result[topIndex] += totalExp - given;
            return result;
        }
    }
}
