using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using ZenoniaProto;
using ZenoniaProto.Rpg;

/// <summary>Taurus V4 phase 06, the engine-free parts: the Elder Root Guardian's skill shapes and timing, its Awakening, the skill poses, the new monsters' stats and loot, the Matriarch card, the T-M05 quest chain.</summary>
public class TaurusShrineSystemsTests
{
    readonly ITestOutputHelper log;
    public TaurusShrineSystemsTests(ITestOutputHelper log) { this.log = log; }

    static WantedSkillDef Sweep { get { return WantedSkillTable.ElderRootGuardian[0]; } }
    static WantedSkillDef Pulse { get { return WantedSkillTable.ElderRootGuardian[1]; } }
    static WantedSkillDef Awaken { get { return WantedSkillTable.ElderRootGuardian[2]; } }
    const float Tile = 16f;

    // --- shapes + timing ----------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void TheElderHasSweepPulseAndAnAwakeningThatOnlyJoinsBelowThirtyPercent()
    {
        Assert.Same(WantedSkillTable.ElderRootGuardian, WantedSkillTable.For("tauElderRootGuardian"));
        Assert.Equal(3, WantedSkillTable.ElderRootGuardian.Length);
        Assert.Equal(new[] { 0, 1, 2 }, WantedSkillTable.ElderRootGuardian.Select(s => s.Id).ToArray());
        Assert.Equal(WantedAreaShape.Cone, Sweep.Shape); Assert.Equal(120f, Sweep.AngleDeg); Assert.Equal(1200, Sweep.TelegraphMs); Assert.InRange(Sweep.CooldownMs, 9000, 12000);
        Assert.Equal(WantedAreaShape.Circle, Pulse.Shape); Assert.Equal(1500, Pulse.TelegraphMs); Assert.InRange(Pulse.CooldownMs, 14000, 18000);
        Assert.True(Awaken.Enrage); Assert.True(Awaken.Transition); Assert.Equal(1000, Awaken.TelegraphMs); Assert.Equal(0f, Awaken.DamageMult);
        Assert.False(Sweep.Enrage); Assert.False(Pulse.Enrage); Assert.False(Sweep.Transition);
        Assert.All(WantedSkillTable.ElderRootGuardian, s => Assert.Equal(0, s.ChargeMs));
        Assert.Equal(new[] { "-sweep", "-pulse", "-awaken" }, WantedSkillTable.ElderRootGuardian.Select(s => s.PoseSheetSuffix).ToArray());
        // Awakening is only ever picked below 30 % HP, and then once (its cooldown is an hour)
        var clock = new WantedSkillClock();
        clock.OnSkillUsed(Sweep); clock.OnSkillUsed(Pulse); clock.Tick(30000f);
        for (float roll = 0f; roll < 1f; roll += 0.1f) Assert.NotEqual(2, WantedSkillPicker.Pick(WantedSkillTable.ElderRootGuardian, clock, 0.5f, roll));
        Assert.Equal(0, WantedSkillPicker.Pick(new[] { Awaken }, clock, 0.29f, 0f));
        Assert.Equal(-1, WantedSkillPicker.Pick(new[] { Awaken }, clock, 0.31f, 0f));
        clock.OnSkillUsed(Awaken, 0.87f);
        Assert.True(clock.CooldownLeft[2] > 3000000f);
    }

    [Fact]
    public void TheConeHitsInFrontAndNotBehindAndTheCircleHitsAllRoundButNotBeyond()
    {
        var east = new WantedShapeInstance { OriginX = 200f, OriginY = 200f, DirDeg = 0f };
        float r = Sweep.RadiusTiles * Tile;
        Assert.True(WantedSkillArea.Hits(Sweep, east, 200f + r * 0.6f, 200f));                       // straight ahead
        Assert.True(WantedSkillArea.Hits(Sweep, east, 200f + r * 0.6f * (float)Math.Cos(0.9), 200f + r * 0.6f * (float)Math.Sin(0.9))); // 51 degrees off: inside 60
        Assert.False(WantedSkillArea.Hits(Sweep, east, 200f + r * 0.6f * (float)Math.Cos(1.3), 200f + r * 0.6f * (float)Math.Sin(1.3))); // 74 degrees: outside 120 / 2
        Assert.False(WantedSkillArea.Hits(Sweep, east, 200f - r * 0.4f, 200f));                      // behind the boss: safe
        Assert.False(WantedSkillArea.Hits(Sweep, east, 200f, 200f + r * 0.5f));                      // beside him: safe
        Assert.False(WantedSkillArea.Hits(Sweep, east, 200f + r + 8f, 200f));                        // out of reach
        var spot = new WantedShapeInstance { OriginX = 100f, OriginY = 100f };
        float pr = Pulse.RadiusTiles * Tile;
        foreach (double a in new[] { 0.0, 1.6, 3.2, 4.8 }) Assert.True(WantedSkillArea.Hits(Pulse, spot, 100f + pr * 0.8f * (float)Math.Cos(a), 100f + pr * 0.8f * (float)Math.Sin(a)));
        Assert.False(WantedSkillArea.Hits(Pulse, spot, 100f + pr + 6f, 100f));
        // two AoEs to dodge, both bigger than the boss body but small against the 30 x 28 arena (>= 2H of room round the pulse)
        Assert.True(pr * 2f < 28f * Tile * 0.45f);
    }

    [Fact]
    public void AwakeningShortensTheGapsByTenToFifteenPercentAndHurtsNobody()
    {
        const float rate = 0.87f; // MonsterDef.TauElderRootGuardian.EnrageRate (TaurusShrineMapTests pins it)
        var calm = new WantedSkillClock(); var awake = new WantedSkillClock();
        calm.OnSkillUsed(Sweep); awake.OnSkillUsed(Sweep, rate);
        Assert.InRange(awake.GapLeft / calm.GapLeft, 0.85f, 0.90f);
        Assert.InRange(awake.CooldownLeft[0] / calm.CooldownLeft[0], 0.85f, 0.90f);
        awake.Reset();
        Assert.Equal(0f, awake.GapLeft); Assert.All(awake.CooldownLeft, c => Assert.Equal(0f, c));
        Assert.True(Sweep.DamageMult <= 1.6f && Pulse.DamageMult <= 1.5f); // "not much stronger": the first boss
    }

    [Fact]
    public void ThePoseFollowsTheSpecWarningThenImpactThenHold()
    {
        Assert.Equal(0, WantedSkillPose.Frame(true, 0f, 0f));      // F1 first half of the warning
        Assert.Equal(0, WantedSkillPose.Frame(true, 0.49f, 0f));
        Assert.Equal(1, WantedSkillPose.Frame(true, 0.5f, 0f));    // F2 second half
        Assert.Equal(1, WantedSkillPose.Frame(true, 1f, 0f));
        Assert.Equal(2, WantedSkillPose.Frame(false, 1f, 0f));     // F3 impact / launch the moment Execute starts
        Assert.Equal(2, WantedSkillPose.Frame(false, 1f, WantedSkillPose.ImpactHoldMs - 1f));
        Assert.Equal(3, WantedSkillPose.Frame(false, 1f, WantedSkillPose.ImpactHoldMs)); // F4 held through the dash / recovery
        Assert.InRange(WantedSkillPose.ImpactHoldMs, 200f, 300f);  // "giu F3 khoang 0,2-0,3 giay" for Awakening
        Assert.Equal("-charge", WantedSkillTable.BramblebackMatriarch[0].PoseSheetSuffix);
        Assert.Equal("-stomp", WantedSkillTable.BramblebackMatriarch[1].PoseSheetSuffix);
    }

    // --- monsters, loot, cards ---------------------------------------------------------------------------------------------------------------

    [Fact]
    public void TheSoloLv10HeroNeedsNinetyToOneHundredFiftySecondsForTheElder()
    {
        TaurusMeadowMonsterSpecTests.Spec boss = TaurusMeadowMonsterSpecTests.Read("tauElderRootGuardian");
        Assert.Equal(10, boss.Level);
        foreach (string cls in new[] { "warrior", "wizard", "cleric" })
        {
            double sec = boss.Hp / TaurusMeadowMonsterSpecTests.Dps(cls, 10, boss.Def); // 0.75 uptime already stands for the dodging
            log.WriteLine("Elder vs " + cls + " Lv10: " + sec.ToString("F0") + " s (HP " + boss.Hp + ")");
            Assert.InRange(sec, 90.0, 150.0);
        }
        Assert.True(boss.Hp > 3f * TaurusMeadowMonsterSpecTests.Read("tauBramblebackMatriarch").Hp); // above the field boss
    }

    [Fact]
    public void TheNewMonstersFollowTheBibleShapes()
    {
        var spo = TaurusMeadowMonsterSpecTests.Read("tauAmberSporeling"); var gua = TaurusMeadowMonsterSpecTests.Read("tauRuneBarkGuardian"); var elder = TaurusMeadowMonsterSpecTests.Read("tauElderRootGuardian");
        Assert.True(spo.Ranged); Assert.Equal(9, spo.Level); Assert.False(gua.Ranged); Assert.Equal(10, gua.Level);
        Assert.True(gua.Speed < 20f && gua.Windup >= 700f);                         // "can chien cham, HP cao"
        Assert.True(gua.Hp > TaurusMeadowMonsterSpecTests.Read("tauAncientBarkling").Hp); // tankier than the meadow's toughest
        Assert.True(spo.Hp < gua.Hp);
        Assert.True(elder.Speed < gua.Speed);                                       // "extremely heavy and slow"
        Assert.Equal(500f, elder.Windup);                                           // Root Slam wind-up 0.5 s
    }

    [Fact]
    public void LootAndCardsOfTheShrine()
    {
        foreach (string id in new[] { "tauAmberSporeling", "tauRuneBarkGuardian", "tauElderRootGuardian" }) { Assert.NotNull(LootTable.For(id)); Assert.NotNull(CardTable.Get(id)); }
        Assert.True(LootTable.For("tauElderRootGuardian").IsBoss);
        Assert.Equal(0.03, CardDrops.Chance(CardTable.Get("tauElderRootGuardian")));   // boss card 3 % (phase 06)
        Assert.Equal(0.02, CardDrops.Chance(CardTable.Get("tauBramblebackMatriarch"))); // field boss card 2 %
        Assert.Equal(12, CardTable.All.Count(CardTable.IsLive));                        // the Matriarch card is outside the 12 of the Album
        CardCollection cards = PlayerState.NewGame("warrior").Cards;
        cards.Add("tauBramblebackMatriarch", 1);
        Assert.Equal(0, cards.AlbumCount);                                              // owning it adds nothing to the Album count
    }

    // --- quests ------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void TheShrineChainFollowsTheRootsQuestInThreeSteps()
    {
        string prev = TaurusMeadowQuests.RootsId;
        foreach (string id in new[] { TaurusShrineQuests.EnterId, TaurusShrineQuests.AmberId, TaurusShrineQuests.GuardianId })
        {
            QuestDef q = QuestCatalog.Get(id);
            Assert.NotNull(q); Assert.Contains(q, QuestCatalog.Live);
            Assert.Equal("elderRowan", q.GiverNpcId); Assert.Equal(prev, q.Requires);
            Assert.True(q.RewardGold > 0); Assert.NotEmpty(q.OfferLines); Assert.NotEmpty(q.ReadyLines); Assert.NotEmpty(q.ActiveLines);
            prev = id;
        }
        Assert.Equal(DungeonPoints.TauShrineGateId, QuestCatalog.Get(TaurusShrineQuests.EnterId).WaypointId);
        Assert.Equal(DungeonPoints.TauMemoryAmberId, QuestCatalog.Get(TaurusShrineQuests.AmberId).WaypointId);
        QuestDef boss = QuestCatalog.Get(TaurusShrineQuests.GuardianId);
        Assert.Equal("tauElderRootGuardian", boss.MonsterType); Assert.Equal(1, boss.Count);
        Assert.NotNull(DungeonPoints.Waypoint(boss.Requires == TaurusShrineQuests.AmberId ? DungeonPoints.TauMemoryAmberId : null));
    }

    [Fact]
    public void ShrineCrystalsWarpOnlyToEachOtherAndOutToTheMeadows()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        Assert.True(p.UnlockWaypoint(DungeonPoints.TauShrineGateId)); Assert.True(p.UnlockWaypoint(DungeonPoints.TauShrineDeepId));
        Assert.Null(DungeonPoints.WarpRefusal(p, DungeonPoints.TauShrineGateId, DungeonPoints.TauShrineDeepId, 99999f)); // the checkpoint: back to the boss hall in one touch
        Assert.Null(DungeonPoints.WarpRefusal(p, DungeonPoints.TauShrineDeepId, DungeonPoints.ShrineExitId, 99999f));
        Assert.NotNull(DungeonPoints.WarpRefusal(p, DungeonPoints.TauShrineGateId, DungeonPoints.TauShrineDeepId, 1000f)); // not in combat
        Assert.NotNull(DungeonPoints.WarpRefusal(p, DungeonPoints.TauShrineGateId, DungeonPoints.TauMemoryAmberId, 99999f)); // never TO the amber
        string map; int tx, ty;
        Assert.True(DungeonPoints.Target(DungeonPoints.ShrineExitId, out map, out tx, out ty));
        Assert.Equal("tauCloverMeadows", map); Assert.Equal(TauCloverArrival.X, tx); Assert.Equal(TauCloverArrival.Y, ty);
        Assert.Equal(DungeonPoints.ShrineExitId, DungeonPoints.ExitFor(DungeonPoints.Waypoint(DungeonPoints.TauShrineGateId)));
        Assert.Equal(DungeonPoints.ExitId, DungeonPoints.ExitFor(DungeonPoints.Waypoint("rootburrow1")));
        Assert.False(DungeonPoints.SameDungeon(DungeonPoints.Waypoint("rootburrow1"), DungeonPoints.Waypoint(DungeonPoints.TauShrineGateId)));
        Assert.True(DungeonPoints.SameDungeon(DungeonPoints.Waypoint(DungeonPoints.TauShrineDeepId), DungeonPoints.Waypoint(DungeonPoints.TauShrineGateId)));
    }

    static class TauCloverArrival { public const int X = 57, Y = 6; } // TauCloverMeadowsMap.DoorArrivalTx / Ty (the Rpg assembly cannot see the map class; a Net test pins the real portals to these)
}
