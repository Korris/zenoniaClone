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
            // World Map 2.0 step 1 (13/11/2026): Exp/gold/ItemLevel rescaled to each monster's new level with Exp=14L, gold=L..2L+1 (MonsterDef has the matching Hp/Atk/Def curve); Wanted bosses
            // (rimeclawTyrant/glacialColossus/mireHydraQueen) keep their old numbers on purpose, since MonsterDef.Wanted now freezes their basis stats.
            { "frostSlime", new MonsterLootDef { Type = "frostSlime", Exp = 154, GoldMin = 11, GoldMax = 23, ItemLevel = 11 } },
            { "iceGoblin", new MonsterLootDef { Type = "iceGoblin", Exp = 224, GoldMin = 16, GoldMax = 33, ItemLevel = 16 } },
            { "spectralWolf", new MonsterLootDef { Type = "spectralWolf", Exp = 234, GoldMin = 9, GoldMax = 30, ItemLevel = 18 } },
            { "frostfangKing", new MonsterLootDef { Type = "frostfangKing", Exp = 1400, GoldMin = 100, GoldMax = 221, ItemLevel = 15, IsBoss = true, HpPotion = 1, MpPotion = 1, UpgradeStone = 1, StoneCount = 2 } },
            // Amber Dunes monsters: Frost Slime (Lv 1) and Ice Goblin (Lv 2) give EXP 14L and gold L..2L+1, times the same role factor as the stat block (MonsterDef).
            { "duneScarab", new MonsterLootDef { Type = "duneScarab", Exp = 235, GoldMin = 17, GoldMax = 34, ItemLevel = 21 } },
            { "sandRaider", new MonsterLootDef { Type = "sandRaider", Exp = 417, GoldMin = 30, GoldMax = 61, ItemLevel = 26 } },
            { "amberWisp", new MonsterLootDef { Type = "amberWisp", Exp = 525, GoldMin = 38, GoldMax = 76, ItemLevel = 30 } },
            // Emberfen Marsh and Sunken Reliquary monsters: the same line. Bosses take the king's factor on it (x6.67) and his sure drops.
            { "bogSlime", new MonsterLootDef { Type = "bogSlime", Exp = 304, GoldMin = 22, GoldMax = 44, ItemLevel = 31 } },
            { "mireToad", new MonsterLootDef { Type = "mireToad", Exp = 462, GoldMin = 33, GoldMax = 67, ItemLevel = 33 } },
            { "marshStalker", new MonsterLootDef { Type = "marshStalker", Exp = 638, GoldMin = 45, GoldMax = 93, ItemLevel = 35 } },
            { "ruinWisp", new MonsterLootDef { Type = "ruinWisp", Exp = 609, GoldMin = 44, GoldMax = 88, ItemLevel = 38 } },
            { "glacierWarden", new MonsterLootDef { Type = "glacierWarden", Exp = 1865, GoldMin = 133, GoldMax = 273, ItemLevel = 20, IsBoss = true, HpPotion = 1, MpPotion = 1, UpgradeStone = 1, StoneCount = 2 } },
            { "mirefangHydra", new MonsterLootDef { Type = "mirefangHydra", Exp = 3737, GoldMin = 266, GoldMax = 540, ItemLevel = 40, IsBoss = true, HpPotion = 1, MpPotion = 1, UpgradeStone = 1, StoneCount = 2 } },
            // Daily Wanted Bosses (MonsterDef.Wanted): the drops of the map boss of the level band (the fixed daily reward is paid by WantedBossCredit on top); only the gear level differs.
            // Kept at their pre-step-1 numbers on purpose (frostfangKing/mirefangHydra above moved, these did not).
            { "rimeclawTyrant", new MonsterLootDef { Type = "rimeclawTyrant", Exp = 280, GoldMin = 20, GoldMax = 50, ItemLevel = 10, IsBoss = true, HpPotion = 1, MpPotion = 1, UpgradeStone = 1, StoneCount = 2 } },
            { "glacialColossus", new MonsterLootDef { Type = "glacialColossus", Exp = 1865, GoldMin = 133, GoldMax = 273, ItemLevel = 25, IsBoss = true, HpPotion = 1, MpPotion = 1, UpgradeStone = 1, StoneCount = 2 } },
            { "mireHydraQueen", new MonsterLootDef { Type = "mireHydraQueen", Exp = 3270, GoldMin = 233, GoldMax = 473, ItemLevel = 40, IsBoss = true, HpPotion = 1, MpPotion = 1, UpgradeStone = 1, StoneCount = 2 } },
            // Willowbrook meadow monsters: EXP 14L x role factor, gold L..2L+1 x role factor. World Map 2.1: Willowbrook Lv 1-5 (slime 1, rabbit 3, boar 5); mantis 6 and treant 8 are Bramblewood Forest (Lv 6-10).
            { "hornRabbit", new MonsterLootDef { Type = "hornRabbit", Exp = 39, GoldMin = 2, GoldMax = 6, ItemLevel = 3 } },
            { "thornBoar", new MonsterLootDef { Type = "thornBoar", Exp = 84, GoldMin = 6, GoldMax = 13, ItemLevel = 5 } },
            { "meadowSlime", new MonsterLootDef { Type = "meadowSlime", Exp = 12, GoldMin = 1, GoldMax = 3, ItemLevel = 1 } },
            { "meadowMantis", new MonsterLootDef { Type = "meadowMantis", Exp = 96, GoldMin = 7, GoldMax = 14, ItemLevel = 6 } },
            { "brambleTreant", new MonsterLootDef { Type = "brambleTreant", Exp = 179, GoldMin = 13, GoldMax = 27, ItemLevel = 8 } },
            // Frost Hollow ice monsters (Lv 13-15; step 1 band is now Lv 11-15): the same line.
            { "iceWraith", new MonsterLootDef { Type = "iceWraith", Exp = 182, GoldMin = 13, GoldMax = 27, ItemLevel = 13 } },
            { "frostbiteYeti", new MonsterLootDef { Type = "frostbiteYeti", Exp = 196, GoldMin = 14, GoldMax = 29, ItemLevel = 14 } },
            { "crystalGolem", new MonsterLootDef { Type = "crystalGolem", Exp = 210, GoldMin = 15, GoldMax = 31, ItemLevel = 15 } },
            // Frost Hollow lower tiers (Lv 11-13; step 1 band is now Lv 11-15): the same line.
            { "snowFox", new MonsterLootDef { Type = "snowFox", Exp = 152, GoldMin = 12, GoldMax = 21, ItemLevel = 12 } },
            { "iceImp", new MonsterLootDef { Type = "iceImp", Exp = 168, GoldMin = 12, GoldMax = 25, ItemLevel = 12 } },
            { "frostBear", new MonsterLootDef { Type = "frostBear", Exp = 203, GoldMin = 16, GoldMax = 32, ItemLevel = 13 } },
            // World Map 2.1 new monsters: Bramblewood Forest (Lv 6-10) + Rootburrow boss, Sunbreak Foothills (Lv 16-20). Same line (EXP 14L, gold L..2L+1) x role factor; bosses x6.67 with the sure drops.
            { "forestWolf", new MonsterLootDef { Type = "forestWolf", Exp = 91, GoldMin = 5, GoldMax = 12, ItemLevel = 7 } },
            { "thornSprite", new MonsterLootDef { Type = "thornSprite", Exp = 126, GoldMin = 9, GoldMax = 19, ItemLevel = 9 } },
            { "thornfangAlpha", new MonsterLootDef { Type = "thornfangAlpha", Exp = 934, GoldMin = 67, GoldMax = 140, ItemLevel = 10, IsBoss = true, HpPotion = 1, MpPotion = 1, UpgradeStone = 1, StoneCount = 2 } },
            { "thornheartTreant", new MonsterLootDef { Type = "thornheartTreant", Exp = 934, GoldMin = 67, GoldMax = 140, ItemLevel = 10, IsBoss = true, HpPotion = 1, MpPotion = 1, UpgradeStone = 1, StoneCount = 2 } },
            { "highlandIbex", new MonsterLootDef { Type = "highlandIbex", Exp = 224, GoldMin = 16, GoldMax = 33, ItemLevel = 16 } },
            { "dustscaleLizard", new MonsterLootDef { Type = "dustscaleLizard", Exp = 238, GoldMin = 17, GoldMax = 35, ItemLevel = 17 } },
            { "cliffHarpy", new MonsterLootDef { Type = "cliffHarpy", Exp = 252, GoldMin = 18, GoldMax = 37, ItemLevel = 18 } },
            { "stonebackGuardian", new MonsterLootDef { Type = "stonebackGuardian", Exp = 448, GoldMin = 32, GoldMax = 66, ItemLevel = 20 } },
            { "ridgehornBehemoth", new MonsterLootDef { Type = "ridgehornBehemoth", Exp = 1865, GoldMin = 133, GoldMax = 273, ItemLevel = 20, IsBoss = true, HpPotion = 1, MpPotion = 1, UpgradeStone = 1, StoneCount = 2 } },
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
