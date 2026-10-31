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
            // Amber Dunes monsters: Frost Slime (Lv 1) and Ice Goblin (Lv 2) give EXP 14L and gold L..2L+1, times the same role factor as the stat block (MonsterDef).
            { "duneScarab", new MonsterLootDef { Type = "duneScarab", Exp = 235, GoldMin = 17, GoldMax = 34, ItemLevel = 21 } },
            { "sandRaider", new MonsterLootDef { Type = "sandRaider", Exp = 385, GoldMin = 28, GoldMax = 56, ItemLevel = 24 } },
            { "amberWisp", new MonsterLootDef { Type = "amberWisp", Exp = 455, GoldMin = 33, GoldMax = 66, ItemLevel = 26 } },
            // Emberfen Marsh and Sunken Reliquary monsters: the same line. Bosses take the king's factor on it (x6.67) and his sure drops.
            { "bogSlime", new MonsterLootDef { Type = "bogSlime", Exp = 275, GoldMin = 20, GoldMax = 40, ItemLevel = 28 } },
            { "mireToad", new MonsterLootDef { Type = "mireToad", Exp = 434, GoldMin = 31, GoldMax = 63, ItemLevel = 31 } },
            { "marshStalker", new MonsterLootDef { Type = "marshStalker", Exp = 620, GoldMin = 44, GoldMax = 90, ItemLevel = 34 } },
            { "ruinWisp", new MonsterLootDef { Type = "ruinWisp", Exp = 545, GoldMin = 39, GoldMax = 79, ItemLevel = 34 } },
            { "glacierWarden", new MonsterLootDef { Type = "glacierWarden", Exp = 1865, GoldMin = 133, GoldMax = 273, ItemLevel = 20, IsBoss = true, HpPotion = 1, MpPotion = 1, UpgradeStone = 1, StoneCount = 2 } },
            { "mirefangHydra", new MonsterLootDef { Type = "mirefangHydra", Exp = 3270, GoldMin = 233, GoldMax = 473, ItemLevel = 35, IsBoss = true, HpPotion = 1, MpPotion = 1, UpgradeStone = 1, StoneCount = 2 } },
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
