using System;
using System.Collections.Generic;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// What a kill pays out (decided rules): EXP is split by damage share among the heroes that hit the monster and are still alive in the zone, a party
    /// sharing what its members earned (PartyKillShare), the top damage dealer owns the drops for 3 s (online only), and every contributor is told when it was the boss. Offline the player keeps
    /// the whole reward even when a debug party member made the kill (party members have no PlayerRpg).
    /// </summary>
    public static class KillRewards
    {
        static readonly List<HeroController> Winners = new List<HeroController>(8);
        static readonly List<int> Damage = new List<int>(8);

        public static void Resolve(Zone zone, Monster m)
        {
            if (m.WasCaptured) return; // a caught pet pays nothing
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
            PartyKillShare.Pay(zone, m, Winners, exp);

            ushort ownerId = 0;
            float freeAt = 0f;
            if (!NetMode.IsOffline && top != null) { ownerId = top.NetId; freeAt = LootOwnership.FreeAtMs(zone.TimeMs, false); }
            LootDrop.SpawnAll(zone.Root, LootTable.RollAll(def, RpgRandom.Default), m.Feet, ownerId, freeAt);

            for (int i = 0; i < Winners.Count; i++) Winners[i].Owner.QuestKill(m.Type); // every hero that hit it, each on its own quests
            if (m.IsBoss && !m.IsWanted) for (int i = 0; i < Winners.Count; i++) Winners[i].Owner.RaiseBossKilled(); // map boss flags: the event boss opens no gate
            if (m.IsBoss && !m.IsWanted) // pet rewards: food, the 5th valid map boss of the week pays Pet Crystals, 10 % a Pet Ball (>= 1 % of its max HP by the hero itself; pet hits are not in DamageBy)
                for (int i = 0; i < Winners.Count; i++) if (AccountRewards.IsValidBossShare(Damage[i], m.MaxHp)) Winners[i].Owner.AccountMapBossKilled(RpgRandom.Default() < PetRules.BossBallChance);
            if (!m.IsBoss) for (int i = 0; i < Winners.Count; i++) if (RpgRandom.Default() < PetRules.MonsterFoodChance) Winners[i].Owner.AccountFoodDrop(); // Pet Food, 8 % per contributor
            RollCards(m); // monster card: every contributor with a valid share rolls its own
            if (m.IsBoss) PetCaptureService.OpenCorpse(zone, m, Winners, Damage); // 30 / 45 s to throw a ball at it
            if (m.IsWanted) WantedBossCredit.Pay(zone, m, Winners); // daily Wanted Boss: no-op unless this is today's boss, online
            if (m.IsWanted) DropWantedHats(zone, m);
        }

        /// <summary>Monster card drop (CardDrops): each winner whose damage reached the valid share (>= 1 % of the monster's max HP, like the pet boss rewards) rolls once; the card goes
        /// straight into its collection (nothing lies on the ground, so NetLootCodec is untouched). A Wanted Boss only drops its card when it is today's boss.</summary>
        static void RollCards(Monster m)
        {
            if (!CardDrops.Enabled || CardTable.Get(m.Type) == null) return;
            if (m.IsWanted && WantedBoss.Today(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).BossType != m.Type) return;
            for (int i = 0; i < Winners.Count; i++)
            {
                if (!AccountRewards.IsValidBossShare(Damage[i], m.MaxHp)) continue;
                string card = CardDrops.Roll(m.Type, RpgRandom.Default);
                if (card != null) Winners[i].Owner.CardDrop(card);
            }
        }

        /// <summary>Wanted Boss (MVP) hats: every contributor rolls its own (HeadgearCatalog.RollWantedDrops: the boss's crown, the rare ultimate wizard hat);
        /// each hat lands at the boss owned by that hero for the usual head start online, so nobody else can grab it first.</summary>
        static void DropWantedHats(Zone zone, Monster m)
        {
            for (int i = 0; i < Winners.Count; i++)
            {
                List<Item> hats = HeadgearCatalog.RollWantedDrops(m.Type, RpgRandom.Default);
                if (hats.Count == 0) continue;
                var drops = new List<LootEntry>(hats.Count);
                foreach (Item hat in hats) drops.Add(new LootEntry { Kind = LootKind.Equipment, Item = hat });
                bool owned = !NetMode.IsOffline;
                LootDrop.SpawnAll(zone.Root, drops, m.Feet, owned ? Winners[i].NetId : (ushort)0, owned ? LootOwnership.FreeAtMs(zone.TimeMs, false) : 0f);
            }
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
