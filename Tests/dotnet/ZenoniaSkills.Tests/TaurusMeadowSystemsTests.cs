using System;
using System.Collections.Generic;
using Xunit;
using ZenoniaProto;
using ZenoniaProto.Rpg;

/// <summary>Taurus V4 phase 05, the engine-free parts: the Brambleback Matriarch's skills and charge timing, the field boss loot rules, the new ranged profiles, the Rowan quest chain and its touch points.</summary>
public class TaurusMeadowSystemsTests
{
    static WantedSkillDef Charge { get { return WantedSkillTable.BramblebackMatriarch[0]; } }
    static WantedSkillDef Stomp { get { return WantedSkillTable.BramblebackMatriarch[1]; } }
    const float H = 20.5f, Tile = 16f; // hero height in world units (164 px / 8), tile size

    // --- boss skills -------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void TheMatriarchHasTwoSkillsFromTheFieldBossSheet()
    {
        Assert.Same(WantedSkillTable.BramblebackMatriarch, WantedSkillTable.For("tauBramblebackMatriarch"));
        Assert.Equal(2, WantedSkillTable.BramblebackMatriarch.Length);
        Assert.DoesNotContain(WantedSkillTable.BramblebackMatriarch, s => s.Enrage); // Bramble Fury is only a look
        // Bramble Charge: rectangle 1.4H wide, 4H long, 1.1 s warning, 0.5 s dash, 12 s cooldown
        Assert.Equal(WantedAreaShape.Line, Charge.Shape);
        Assert.InRange(Charge.WidthTiles * Tile / H, 1.3f, 1.5f);
        Assert.InRange(Charge.LengthTiles * Tile / H, 3.8f, 4.2f);
        Assert.Equal(1100, Charge.TelegraphMs); Assert.Equal(500, Charge.ChargeMs); Assert.Equal(12000, Charge.CooldownMs);
        // Thorn Stomp: circle 1.8H round the boss, 1.4 s warning, 16 s cooldown
        Assert.Equal(WantedAreaShape.Circle, Stomp.Shape);
        Assert.InRange(Stomp.RadiusTiles * Tile / H, 1.7f, 1.9f);
        Assert.Equal(1400, Stomp.TelegraphMs); Assert.Equal(16000, Stomp.CooldownMs);
        Assert.False(Stomp.OnPlayers);
        Assert.Equal(0, Stomp.ChargeMs);
        Assert.Equal(new HashSet<int> { 0, 1 }, new HashSet<int>(new[] { Charge.Id, Stomp.Id }));
    }

    [Fact]
    public void TheChargeWarningHasTimeForAMobilePlayerAndNoOtherSkillIsACharge()
    {
        Assert.True(Charge.TelegraphMs >= 1000);
        foreach (WantedSkillDef[] table in new[] { WantedSkillTable.RimeclawTyrant, WantedSkillTable.GlacialColossus, WantedSkillTable.MireHydraQueen, WantedSkillTable.AmbercapWarden, WantedSkillTable.AetherglyphOwl })
            foreach (WantedSkillDef s in table) Assert.Equal(0, s.ChargeMs);
    }

    [Fact]
    public void TheDashCoversTheWholeWarnedLineInHalfASecond()
    {
        float full = Charge.LengthTiles * Tile;
        Assert.Equal(full / 0.5f, WantedChargeRules.SpeedUnitsPerSec(Charge), 2);
        Assert.Equal(0f, WantedChargeRules.Travelled(Charge, 0f));
        Assert.Equal(full * 0.5f, WantedChargeRules.Travelled(Charge, 250f), 2);
        Assert.Equal(full, WantedChargeRules.Travelled(Charge, 500f), 2);
        Assert.Equal(full, WantedChargeRules.Travelled(Charge, 5000f), 2); // never past the line
        Assert.Equal(0f, WantedChargeRules.SpeedUnitsPerSec(Stomp)); // not a charge
    }

    [Fact]
    public void AHeroInTheLockedLaneIsHitOnceTheBodyReachesThemAndASideStepAvoidsIt()
    {
        var lane = new WantedShapeInstance { OriginX = 100f, OriginY = 100f, DirDeg = 0f }; // dashing east
        float reach = 20f;
        Assert.False(WantedChargeRules.Reached(Charge, lane, 10f, reach, 140f, 100f));  // 40 ahead, body only 10 + 20 along
        Assert.True(WantedChargeRules.Reached(Charge, lane, 30f, reach, 140f, 100f));   // body front at 150: reached
        Assert.True(WantedChargeRules.Reached(Charge, lane, 80f, reach, 140f, 100f + 6f)); // still inside the 1.8-tile lane (edge margin included)
        Assert.False(WantedChargeRules.Reached(Charge, lane, 80f, reach, 140f, 100f + 30f)); // a side-step out of the lane
        Assert.False(WantedChargeRules.Reached(Charge, lane, 80f, reach, 100f + Charge.LengthTiles * Tile + 40f, 100f)); // beyond the end of the line
        Assert.False(WantedChargeRules.Reached(Charge, lane, 80f, reach, 60f, 100f));   // behind the start
    }

    [Fact]
    public void TheLockedDirectionIsWhateverTheShapeWasLockedToNotTheLiveHeroPosition()
    {
        var north = new WantedShapeInstance { OriginX = 50f, OriginY = 50f, DirDeg = 90f };
        Assert.True(WantedChargeRules.Reached(Charge, north, 60f, 20f, 50f, 110f));
        Assert.False(WantedChargeRules.Reached(Charge, north, 60f, 20f, 110f, 50f)); // the hero ran east: out of a north lane
    }

    // --- field boss loot + reward rules ----------------------------------------------------------------------------------------------------------

    static Rand Seeded(int seed) { var r = new Random(seed); return () => r.NextDouble(); }

    [Fact]
    public void EveryTaurusMeadowSpeciesAndTheBossHaveALootEntryOfTheirLevel()
    {
        string[] ids = { "tauHornRabbit", "tauReedFrog", "tauMossMushroom", "tauBrambleBoar", "tauLeafMantis", "tauRootling", "tauThornSprite", "tauAncientBarkling", "tauBramblebackMatriarch" };
        for (int i = 0; i < 8; i++)
        {
            MonsterLootDef d = LootTable.For(ids[i]);
            Assert.NotNull(d);
            Assert.InRange(d.Exp / (double)d.ItemLevel, 12.0, 25.0); // EXP follows the level: 14L x a role factor
            Assert.InRange(d.GoldMax, d.GoldMin, 99);
            Assert.False(d.PersonalLoot);
            Assert.Equal(-1.0, d.GearChance); // common monsters keep the normal 12 % equipment rule
        }
        Assert.NotNull(LootTable.For(ids[8]));
    }

    [Fact]
    public void TheFieldBossPaysThreeTimesTheLv9BoarExpAndTwoToThreeTimesItsGoldWithPersonalLoot()
    {
        MonsterLootDef boss = LootTable.For("tauBramblebackMatriarch"), boar = LootTable.For("tauBrambleBoar");
        double boar9 = 14 * 9 * 1.2; // the boar's role factor on the Lv9 line
        Assert.InRange(boss.Exp / boar9, 2.8, 3.2);
        double boar9Gold = (9 + 19) / 2.0 * 1.2, bossGold = (boss.GoldMin + boss.GoldMax) / 2.0;
        Assert.InRange(bossGold / boar9Gold, 1.8, 3.2);
        Assert.True(boss.PersonalLoot);
        Assert.Equal("uncommon", boss.GearRarity);
        Assert.True(boss.ItemLevel > boar.ItemLevel); // one step above the map: a notch better than the common drops
        Assert.InRange(boss.UpgradeStone, 0.3, 0.4);   // 35 % for 1 stone
        Assert.Equal(1, boss.StoneCount);
    }

    [Fact]
    public void FieldBossGearRollsAboutThirtyPercentOfUncommonAtItemLevelTen()
    {
        MonsterLootDef boss = LootTable.For("tauBramblebackMatriarch");
        Rand rand = Seeded(7);
        int gear = 0, stones = 0, n = 4000;
        for (int i = 0; i < n; i++)
            foreach (LootEntry e in LootTable.RollAll(boss, rand))
            {
                if (e.Kind == LootKind.Equipment && e.Item.rarity != null && e.Item.itemLevel == boss.ItemLevel && !e.Item.slot.StartsWith("head")) { gear++; Assert.Equal("uncommon", e.Item.rarity); }
                if (e.Kind == LootKind.UpgradeStone) stones++;
            }
        Assert.InRange(gear / (double)n, 0.24, 0.36);
        Assert.InRange(stones / (double)n, 0.30, 0.40);
        // a normal monster of the map never forces a rarity
        MonsterLootDef boar = LootTable.For("tauBrambleBoar");
        Assert.True(boar.GearChance < 0 && boar.GearRarity == null);
    }

    // --- ranged profiles ----------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void SporeAndThornSeedAreStraightSlowParticleShotsThatOutrangeNothing()
    {
        foreach (MonsterRangedProfile p in new[] { MonsterRangedProfile.SporeShot, MonsterRangedProfile.ThornSeed })
        {
            Assert.Null(p.Sprite);                     // drawn as particles (CLAUDE.md), no painted picture
            Assert.True(p.MaxTravel > p.Range);        // backing straight away does not dodge it
            Assert.True(p.ProjectileSpeed < 130f);     // a hero walks ~60-80 u/s: a sidestep is possible
            Assert.True(p.Range <= 100f);              // < 6.25 tiles
        }
        Assert.True(MonsterRangedProfile.SporeShot.Range < MonsterRangedProfile.ThornSeed.Range); // the forest sprite is the late-map shooter
        // the spore stops a hero beyond its range from being hit from nowhere: a straight shot with a clear line decides ShouldShoot
        Assert.True(MonsterRangedRules.ShouldShoot(MonsterRangedProfile.SporeShot, 0, 0, 50, 0, (x, y) => false));
        Assert.False(MonsterRangedRules.ShouldShoot(MonsterRangedProfile.SporeShot, 0, 0, 80, 0, (x, y) => false));
        Assert.False(MonsterRangedRules.ShouldShoot(MonsterRangedProfile.SporeShot, 0, 0, 50, 0, (x, y) => x > 20)); // a wall in between
    }

    // --- Rowan's chain + touch points ---------------------------------------------------------------------------------------------------------

    static readonly string[] ChainIds =
    {
        TaurusMeadowQuests.FrogsId, TaurusMeadowQuests.MushroomsId, TaurusMeadowQuests.RunestoneId,
        TaurusMeadowQuests.BoarsId, TaurusMeadowQuests.MantisId, TaurusMeadowQuests.RootlingsId, TaurusMeadowQuests.RootsId,
    };

    [Fact]
    public void SevenQuestsFormOneChainAfterTheMeadowTrialAndAllAreLive()
    {
        Assert.Equal(7, TaurusMeadowQuests.All.Length);
        string prev = TaurusQuests.TrialId;
        foreach (string id in ChainIds)
        {
            QuestDef q = QuestCatalog.Get(id);
            Assert.NotNull(q);
            Assert.Contains(q, QuestCatalog.Live);
            Assert.Equal("elderRowan", q.GiverNpcId);
            Assert.Equal(prev, q.Requires);
            Assert.True(q.RewardGold > 0);
            Assert.NotEmpty(q.OfferLines); Assert.NotEmpty(q.ReadyLines); Assert.NotEmpty(q.ActiveLines);
            prev = id;
        }
        var ids = new HashSet<string>();
        foreach (QuestDef q in QuestCatalog.Live) Assert.True(ids.Add(q.Id), "duplicate quest id " + q.Id);
    }

    [Fact]
    public void ObjectivesAreTheBibleOnesInOrder()
    {
        (string id, string type, int n)[] kills =
        {
            (TaurusMeadowQuests.FrogsId, "tauReedFrog", 4), (TaurusMeadowQuests.MushroomsId, "tauMossMushroom", 4),
            (TaurusMeadowQuests.BoarsId, "tauBrambleBoar", 5), (TaurusMeadowQuests.MantisId, "tauLeafMantis", 4), (TaurusMeadowQuests.RootlingsId, "tauRootling", 3),
        };
        foreach (var k in kills) { QuestDef q = QuestCatalog.Get(k.id); Assert.Equal(k.type, q.MonsterType); Assert.Equal(k.n, q.Count); Assert.Null(q.WaypointId); Assert.NotNull(MonsterDefsHaveIds(k.type)); }
        Assert.Equal(DungeonPoints.TauRunestoneId, QuestCatalog.Get(TaurusMeadowQuests.RunestoneId).WaypointId);
        Assert.Equal(DungeonPoints.TauAncientRootsId, QuestCatalog.Get(TaurusMeadowQuests.RootsId).WaypointId);
    }

    static string MonsterDefsHaveIds(string id) { return LootTable.For(id) != null && CardTable.Get(id) != null ? id : null; } // the species exists: loot table + card

    [Fact]
    public void TheTouchPointsAreQuestOnlyAndNeverAWarpEnd()
    {
        foreach (string id in new[] { DungeonPoints.TauRunestoneId, DungeonPoints.TauAncientRootsId })
        {
            WaypointDef w = DungeonPoints.Waypoint(id);
            Assert.NotNull(w);
            Assert.True(w.QuestOnly);
            Assert.Equal("tauCloverMeadows", w.MapId);
        }
        Assert.All(new[] { "rootburrow1", "rootburrow2", "rootburrow3" }, id => Assert.False(DungeonPoints.Waypoint(id).QuestOnly));
        PlayerState p = PlayerState.NewGame("warrior");
        Assert.True(p.UnlockWaypoint(DungeonPoints.TauRunestoneId));
        Assert.True(p.UnlockWaypoint("rootburrow1"));
        // standing at a quest stone you cannot warp anywhere, and nobody can warp TO one
        Assert.NotNull(DungeonPoints.WarpRefusal(p, DungeonPoints.TauRunestoneId, "rootburrow1", 99999f));
        Assert.NotNull(DungeonPoints.WarpRefusal(p, "rootburrow1", DungeonPoints.TauRunestoneId, 99999f));
        Assert.Null(DungeonPoints.WarpRefusal(p, "rootburrow1", DungeonPoints.ExitId, 99999f)); // the real warp still works
    }

    [Fact]
    public void TheWholeChainPlaysThroughAndTheStoneStepCountsWhenTheStoneIsTouched()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        string[] types = { "tauReedFrog", "tauMossMushroom", null, "tauBrambleBoar", "tauLeafMantis", "tauRootling", null };
        int[] counts = { 4, 4, 0, 5, 4, 3, 0 };
        // finish the meadow trial first (the chain needs it)
        Assert.True(p.AcceptQuest(TaurusQuests.TrialId).Ok);
        for (int i = 0; i < 5; i++) p.Quests.CountKill("tauCloverSlime");
        Assert.True(p.TurnInQuest(TaurusQuests.TrialId).Ok);
        for (int s = 0; s < ChainIds.Length; s++)
        {
            QuestDef q = QuestCatalog.Get(ChainIds[s]);
            Assert.True(p.Quests.Unlocked(q), q.Id + " unlocked");
            for (int later = s + 1; later < ChainIds.Length; later++) Assert.False(p.Quests.Unlocked(QuestCatalog.Get(ChainIds[later])), "later steps wait");
            Assert.True(p.AcceptQuest(q.Id).Ok, q.Id + " accepted");
            if (types[s] != null)
            {
                p.Quests.CountKill("tauHornRabbit"); // another species does not count
                Assert.Equal(QuestState.Active, p.Quests.StateOf(q));
                for (int i = 0; i < counts[s]; i++) p.Quests.CountKill(types[s]);
            }
            else Assert.True(p.UnlockWaypoint(q.WaypointId));
            Assert.Equal(QuestState.ReadyToTurnIn, p.Quests.StateOf(q));
            int gold = p.Gold;
            Assert.True(p.TurnInQuest(q.Id).Ok);
            Assert.Equal(gold + q.RewardGold, p.Gold);
            Assert.Equal(QuestState.Done, p.Quests.StateOf(q));
        }
    }

    [Fact]
    public void AStoneTouchedBeforeTheQuestIsTakenStillCounts()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        Assert.True(p.UnlockWaypoint(DungeonPoints.TauRunestoneId)); // walked past it early
        Assert.True(p.AcceptQuest(TaurusQuests.TrialId).Ok);
        for (int i = 0; i < 5; i++) p.Quests.CountKill("tauCloverSlime");
        p.TurnInQuest(TaurusQuests.TrialId);
        foreach (string id in new[] { TaurusMeadowQuests.FrogsId, TaurusMeadowQuests.MushroomsId })
        {
            QuestDef q = QuestCatalog.Get(id);
            p.AcceptQuest(id);
            for (int i = 0; i < q.Count; i++) p.Quests.CountKill(q.MonsterType);
            p.TurnInQuest(id);
        }
        Assert.True(p.AcceptQuest(TaurusMeadowQuests.RunestoneId).Ok);
        Assert.Equal(QuestState.ReadyToTurnIn, p.Quests.StateOf(QuestCatalog.Get(TaurusMeadowQuests.RunestoneId))); // PlayerState.Quests: attuned before taking it
    }
}
