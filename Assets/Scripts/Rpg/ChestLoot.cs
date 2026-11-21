using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// What a dungeon chest gives (ChatGPT V5, docs/reference/rootburrow-dark-25d/floors-purpose-chatgpt.md section 5): sure gold, then independent rolls for potions,
    /// upgrade stones and one piece of gear of the opener's class at the floor's level; a rare chest doubles the gold and every chance. Pure, RNG injected.
    /// The floor's own material (Amethyst Shard 2F, Aether Rune Fragment 3F) waits for a material item kind (follow-up).
    /// </summary>
    public static class ChestLoot
    {
        struct Tier { public int GoldMin, GoldMax, Potions, ItemLevel; public double PotionChance, StoneChance, GearChance; }

        static readonly Tier[] Tiers =
        {
            new Tier { GoldMin = 100, GoldMax = 180, Potions = 2, PotionChance = 0.70, StoneChance = 0.10, GearChance = 0.05, ItemLevel = 8 },   // 1F, Lv6-8
            new Tier { GoldMin = 200, GoldMax = 350, Potions = 3, PotionChance = 0.80, StoneChance = 0.20, GearChance = 0.08, ItemLevel = 11 },  // 2F, Lv9-11
            new Tier { GoldMin = 350, GoldMax = 550, Potions = 4, PotionChance = 0.90, StoneChance = 0.35, GearChance = 0.12, ItemLevel = 14 },  // 3F, Lv12-14
        };

        public static List<LootEntry> Roll(ChestDef chest, string classId, Rand rand)
        {
            Tier t = Tiers[Math.Max(1, Math.Min(Tiers.Length, chest.Floor)) - 1];
            double mul = chest.Rare ? 2.0 : 1.0;
            var loot = new List<LootEntry>();
            int gold = t.GoldMin + (int)Math.Floor(rand() * (t.GoldMax - t.GoldMin + 1));
            loot.Add(new LootEntry { Kind = LootKind.Coin, Value = (int)(gold * mul) });
            if (rand() < Math.Min(1.0, t.PotionChance * mul))
                for (int i = 0; i < t.Potions; i++) loot.Add(new LootEntry { Kind = i % 2 == 0 ? LootKind.HpPotion : LootKind.MpPotion, Value = 1 }); // HP first, then MP
            if (rand() < Math.Min(1.0, t.StoneChance * mul)) loot.Add(new LootEntry { Kind = LootKind.UpgradeStone, Value = chest.Rare ? 2 : 1 });
            if (rand() < Math.Min(1.0, t.GearChance * mul))
                loot.Add(new LootEntry { Kind = LootKind.Equipment, Item = ItemFactory.Generate(rand, t.ItemLevel, null, chest.Rare ? "rare" : null, classId) });
            return loot;
        }

        /// <summary>"Chest: +250 G, 2 HP potions, 1 MP potion, 1 Upgrade Stone, Iron Sword" for the toast / chat.</summary>
        public static string Describe(List<LootEntry> loot)
        {
            int gold = 0, hp = 0, mp = 0, stones = 0;
            var gear = new List<string>();
            foreach (LootEntry e in loot)
            {
                switch (e.Kind)
                {
                    case LootKind.Coin: gold += e.Value; break;
                    case LootKind.HpPotion: hp++; break;
                    case LootKind.MpPotion: mp++; break;
                    case LootKind.UpgradeStone: stones += Math.Max(1, e.Value); break;
                    case LootKind.Equipment: if (e.Item != null) gear.Add(EnhanceStats.DisplayName(e.Item)); break;
                }
            }
            var parts = new List<string>();
            if (gold > 0) parts.Add("+" + gold + " G");
            if (hp > 0) parts.Add(hp + " HP potion" + (hp > 1 ? "s" : ""));
            if (mp > 0) parts.Add(mp + " MP potion" + (mp > 1 ? "s" : ""));
            if (stones > 0) parts.Add(stones + " Upgrade Stone" + (stones > 1 ? "s" : ""));
            parts.AddRange(gear);
            return "Chest: " + string.Join(", ", parts);
        }
    }
}
