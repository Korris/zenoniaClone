using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    public enum LootKind { Coin, HpPotion, MpPotion, UpgradeStone, Equipment }

    public struct LootEntry
    {
        public LootKind Kind; public int Value; public Item Item;
    }

    /// <summary>Per-monster reward block (web monster-definitions.js: exp, gold, itemLevel, drops).</summary>
    public sealed class MonsterLootDef
    {
        public string Type;
        public int Exp, GoldMin, GoldMax, ItemLevel;
        public double HpPotion = 0.15, MpPotion = 0.10, UpgradeStone = 0.05;
        public int StoneCount = 1;
        public bool IsBoss;
    }

    /// <summary>Drop rolls on monster death (web systems/loot-system.js rollDrops + equipment-system rollEquipmentDrop). Pure, RNG injected.</summary>
    public static class LootTable
    {
        // Frost Hollow monsters (web monster-definitions.js).
        static readonly Dictionary<string, MonsterLootDef> Table = new Dictionary<string, MonsterLootDef>
        {
            { "frostSlime", new MonsterLootDef { Type = "frostSlime", Exp = 14, GoldMin = 1, GoldMax = 3, ItemLevel = 1 } },
            { "iceGoblin", new MonsterLootDef { Type = "iceGoblin", Exp = 28, GoldMin = 2, GoldMax = 5, ItemLevel = 2 } },
            { "spectralWolf", new MonsterLootDef { Type = "spectralWolf", Exp = 26, GoldMin = 1, GoldMax = 4, ItemLevel = 2 } },
            { "frostfangKing", new MonsterLootDef { Type = "frostfangKing", Exp = 280, GoldMin = 20, GoldMax = 50, ItemLevel = 3, IsBoss = true, HpPotion = 1, MpPotion = 1, UpgradeStone = 1, StoneCount = 2 } },
        };

        public static MonsterLootDef For(string type) { MonsterLootDef d; return type != null && Table.TryGetValue(type, out d) ? d : null; }

        /// <summary>Coin, potions and stone rolls in the web's rand() order (stone appended last).</summary>
        public static List<LootEntry> RollDrops(MonsterLootDef def, Rand rand)
        {
            var drops = new List<LootEntry>();
            int gold = def.GoldMin + (int)Math.Floor(rand() * (def.GoldMax - def.GoldMin + 1));
            if (gold > 0) drops.Add(new LootEntry { Kind = LootKind.Coin, Value = gold });
            if (rand() < def.HpPotion) drops.Add(new LootEntry { Kind = LootKind.HpPotion, Value = 1 });
            if (rand() < def.MpPotion) drops.Add(new LootEntry { Kind = LootKind.MpPotion, Value = 1 });
            if (rand() < def.UpgradeStone) drops.Add(new LootEntry { Kind = LootKind.UpgradeStone, Value = def.StoneCount });
            return drops;
        }

        /// <summary>Everything a kill drops: consumables + (maybe) one equipment item.</summary>
        public static List<LootEntry> RollAll(MonsterLootDef def, Rand rand)
        {
            List<LootEntry> drops = RollDrops(def, rand);
            Item gear = ItemFactory.RollEquipmentDrop(def.ItemLevel, def.IsBoss, rand);
            if (gear != null) drops.Add(new LootEntry { Kind = LootKind.Equipment, Item = gear });
            return drops;
        }
    }
}
