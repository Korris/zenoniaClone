using System;
using ZenoniaProto.Rpg;

namespace ZenoniaProto.EditorTools
{
    /// <summary>
    /// Pure-logic checks of the RPG core against numbers computed from the web modules with node (expectations in comments).
    /// Called from FrostSmokeTest. (No Unity Test Framework assembly yet: see plans/261029-1720-unity-rpg-systems/plan.md.)
    /// </summary>
    public static class RpgLogicCheck
    {
        public static void Run(Action<bool, string> expect)
        {
            // EXP curve + stats (web leveling-system.js)
            expect(Leveling.ExpToNext(1) == 50 && Leveling.ExpToNext(5) == 559 && Leveling.ExpToNext(29) == 7808, "rpg: exp curve 50 / 559 / 7808");
            BaseStats w2 = Leveling.StatsForLevel(2, "warrior"), a10 = Leveling.StatsForLevel(10, "assassin");
            expect(w2.MaxHp == 112 && w2.MaxMp == 45 && w2.Atk == 15 && w2.Def == 5, "rpg: warrior lv2 stats (atk 14.5 rounds up like JS)");
            expect(a10.MaxHp == 178 && a10.MaxMp == 95 && a10.Atk == 37 && a10.Def == 9, "rpg: assassin lv10 stats");
            ExpResult r = Leveling.ApplyExpGain(1, 0, 50 + 141);
            expect(r.Level == 3 && r.Exp == 0 && r.LevelsGained == 2, "rpg: multi level-up");

            // Enhance (web weapon-enhance-stats.js): 4@1=1 4@15=8 12@15=17 10@7=7 2@3=1 15@10=10, offhand 3@15 half = 6
            expect(EnhanceStats.Bonus(4, 1) == 1 && EnhanceStats.Bonus(4, 15) == 8 && EnhanceStats.Bonus(12, 15) == 17 && EnhanceStats.Bonus(10, 7) == 7, "rpg: enhance bonus parity");
            expect(EnhanceStats.Bonus(2, 3) == 1 && EnhanceStats.Bonus(15, 10) == 10 && EnhanceStats.Bonus(3, 15, 0.5) == 6, "rpg: enhance scaled floor + offhand half");
            expect(Math.Abs(EnhanceConfig.AtkPct[15] - 1.44) < 1e-9 && Math.Abs(EnhanceConfig.AtkPct[7] - 0.38) < 1e-9, "rpg: cumulative ATK % table");

            // Generation (seeded rand() = 0 / 0.5 sequences from the web)
            Item sword = ItemFactory.Generate(() => 0.0, 1, "weapon", "common", "warrior");
            expect(sword.uid == "sword-0" && sword.stats.atk == 4 && sword.name == "Sword" && sword.classId == "warrior", "rpg: generateItem(0) = web sword-0");
            Item gloves = ItemFactory.Generate(() => 0.5, 5, "gloves", "rare", "mechanic");
            expect(gloves.templateId == "leatherGloves" && gloves.stats.atk == 4 && Math.Abs(gloves.stats.critChance - 0.084f) < 1e-5f && gloves.name == "Superior Cloth Gloves", "rpg: gloves stats / name parity");
            var a = new SeededRng(7); var b = new SeededRng(7);
            expect(ItemFactory.Generate(a.AsRand(), 3).uid == ItemFactory.Generate(b.AsRand(), 3).uid, "rpg: seeded generation deterministic");

            // Enhance flow + class lock + loot + save
            PlayerState p = PlayerState.NewGame("warrior");
            Item weapon = p.GetEquipped("weapon");
            int atk0 = p.Atk;
            p.UpgradeStone = 1;
            EnhanceResult e = EnhanceSystem.Attempt(p, weapon, null, () => 0.0);
            expect(e.Ok && e.Success && weapon.enhance == 1 && p.UpgradeStone == 0 && p.Atk == atk0 + 1, "rpg: enhance +1 spends a stone and raises ATK");
            expect(!EnhanceSystem.Attempt(p, weapon, null, () => 0.0).Ok, "rpg: enhance without stones refused");
            Item wiz = ItemFactory.Generate(() => 0.0, 1, "weapon", "common", "wizard");
            expect(!p.Equip(wiz).Ok, "rpg: class lock blocks wizard weapon on warrior");
            MonsterLootDef king = LootTable.For("frostfangKing");
            bool gear = LootTable.RollAll(king, new SeededRng(1).AsRand()).Exists(d => d.Kind == LootKind.Equipment);
            expect(gear, "rpg: boss always drops equipment");

            // Presentation helpers (icons / stat lines / enhance preview) used by the HUD, inventory and ground loot
            Item gl = ItemFactory.Generate(() => 0.5, 25, "gloves", "rare", "mechanic");
            expect(ItemInfo.IconName(weapon) == "weapon-warrior-0" && ItemInfo.IconName(wiz) == "weapon-wizard-0" && ItemInfo.IconName(gl) == "gloves-2", "rpg: icon names (class gear + armor tier)");
            var lines = ItemInfo.Lines(ItemFactory.Generate(() => 0.0, 1, "weapon", "epic", "warrior"), weapon);
            expect(lines.Count == 1 && lines[0].Label == "ATK" && lines[0].Delta > 0 && ItemInfo.FormatDelta(lines[0]).StartsWith("(+"), "rpg: stat compare vs equipped");
            expect(ItemInfo.EnhancePreview(weapon).StartsWith("+1 -> +2  100%") && ItemInfo.EnhancePreview(gl) == "", "rpg: enhance preview (weapon only)");
            p.Gold = 50; p.AddToInventory(wiz);
            PlayerState back = SaveSerializer.Restore(SaveSerializer.Parse(SaveSerializer.ToJson(SaveSerializer.Capture(p, true, 10f, 20f, false))));
            expect(back.Gold == 50 && back.Inventory.Count == 1 && back.GetEquipped("weapon").enhance == 1 && back.Atk == p.Atk, "rpg: save round-trip");
            expect(SaveSerializer.Parse("{\"level\":3}") != null && SaveSerializer.Parse("not json") == null && SaveSerializer.Parse("{\"version\":9,\"level\":3}") == null, "rpg: old save tolerated, garbage / future version rejected");
        }
    }
}
