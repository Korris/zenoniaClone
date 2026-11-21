using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Rootburrow V5 (21/11/2026): dungeon chests (per-character refill, loot by floor), warp crystals (attune, warp rules) and the Elara quest chain.</summary>
public class DungeonPointsTests
{
    const long Now = 1_800_000_000_000L;
    static double Low() { return 0.0; }   // every roll succeeds, gold at its minimum
    static double High() { return 0.999; } // every roll fails, gold at its maximum

    [Fact]
    public void ChestPaysOnceThenWaitsItsCooldown()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        int gold = p.Gold;
        ActionResult r = p.OpenChest("rb1-a", Now, Low);
        Assert.True(r.Ok, r.Text);
        Assert.Equal(gold + 100, p.Gold);
        Assert.StartsWith("Chest: +100 G", r.Text);
        Assert.True(p.ChestEmpty("rb1-a", Now + 1));
        Assert.False(p.OpenChest("rb1-a", Now + 1000, Low).Ok);
        Assert.False(p.ChestEmpty("rb1-a", Now + DungeonPoints.ChestCooldownMs));
        Assert.True(p.OpenChest("rb1-a", Now + DungeonPoints.ChestCooldownMs, Low).Ok);
        Assert.False(p.ChestEmpty("rb1-b", Now)); // every chest is its own
    }

    [Fact]
    public void DeeperAndRareChestsPayMore()
    {
        var c1 = DungeonPoints.Chest("rb1-a"); var c3 = DungeonPoints.Chest("rb3-a"); var rare = DungeonPoints.Chest("rb3-c");
        int Gold(List<LootEntry> l) { int g = 0; foreach (var e in l) if (e.Kind == LootKind.Coin) g += e.Value; return g; }
        Assert.True(Gold(ChestLoot.Roll(c3, "wizard", High)) > Gold(ChestLoot.Roll(c1, "wizard", High)));
        Assert.Equal(2 * Gold(ChestLoot.Roll(c3, "wizard", Low)), Gold(ChestLoot.Roll(rare, "wizard", Low)));
        Assert.True(rare.Rare && rare.CooldownMs == DungeonPoints.RareChestCooldownMs);
        Assert.Single(ChestLoot.Roll(c1, "wizard", High)); // a failed roll on everything still pays the gold
    }

    [Fact]
    public void FullBagKeepsTheChestClosed()
    {
        PlayerState p = PlayerState.NewGame("cleric");
        while (p.Inventory.Count < PlayerState.InventorySize) p.Inventory.Add(ItemFactory.Generate(Low, 5, null, null, "cleric"));
        Assert.False(p.OpenChest("rb2-a", Now, Low).Ok);
        Assert.False(p.ChestEmpty("rb2-a", Now));
    }

    [Fact]
    public void WarpNeedsBothCrystalsAndNoRecentHit()
    {
        PlayerState p = PlayerState.NewGame("mechanic");
        Assert.NotNull(DungeonPoints.WarpRefusal(p, "rootburrow1", DungeonPoints.ExitId, 99999f)); // not attuned where it stands
        Assert.True(p.UnlockWaypoint("rootburrow1"));
        Assert.False(p.UnlockWaypoint("rootburrow1"));
        Assert.Null(DungeonPoints.WarpRefusal(p, "rootburrow1", DungeonPoints.ExitId, 99999f));
        Assert.NotNull(DungeonPoints.WarpRefusal(p, "rootburrow1", "rootburrow3", 99999f)); // 3F not attuned yet
        Assert.NotNull(DungeonPoints.WarpRefusal(p, "rootburrow1", "rootburrow1", 99999f));
        p.UnlockWaypoint("rootburrow3");
        Assert.Null(DungeonPoints.WarpRefusal(p, "rootburrow1", "rootburrow3", 99999f));
        Assert.NotNull(DungeonPoints.WarpRefusal(p, "rootburrow1", "rootburrow3", 1000f)); // hit a second ago
        string map; int tx, ty;
        Assert.True(DungeonPoints.Target("rootburrow3", out map, out tx, out ty));
        Assert.Equal("rootburrow3", map);
        Assert.True(DungeonPoints.Target(DungeonPoints.ExitId, out map, out tx, out ty));
        Assert.Equal("bramblewoodForest", map);
    }

    [Fact]
    public void ChainUnlocksStepByStepAndTheCrystalCountsForTheReachStep()
    {
        PlayerState p = PlayerState.NewGame("assassin");
        QuestDef a = RootburrowQuests.Hollow, b = RootburrowQuests.Amethyst, c = RootburrowQuests.Warden;
        Assert.Equal(a, p.Quests.TalkAbout(a.GiverNpcId));
        Assert.False(p.AcceptQuest(b.Id).Ok); // locked behind step 1
        p.UnlockWaypoint("rootburrow2");     // attuned before the quest was taken
        Assert.True(p.AcceptQuest(a.Id).Ok);
        Assert.Equal(QuestState.ReadyToTurnIn, p.Quests.StateOf(a));
        Assert.True(p.TurnInQuest(a.Id).Ok);
        Assert.Equal(b, p.Quests.TalkAbout(a.GiverNpcId));
        Assert.True(p.AcceptQuest(b.Id).Ok);
        for (int i = 0; i < b.Count; i++) p.Quests.CountKill("gloomWolf");
        Assert.True(p.TurnInQuest(b.Id).Ok);
        Assert.True(p.AcceptQuest(c.Id).Ok);
        p.Quests.CountKill("ambercapWarden");
        Assert.Equal(QuestState.ReadyToTurnIn, p.Quests.StateOf(c));
    }

    [Fact]
    public void SaveKeepsCooldownsAndCrystalsAndDropsGarbage()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        long now = PlayerState.UnixNowMs;
        p.OpenChest("rb2-c", now, Low);
        p.UnlockWaypoint("rootburrow2");
        SaveData d = SaveSerializer.Capture(p, false, 0f, 0f, false);
        d.chests.Add(new ChestCooldown { id = "nope", at = now + 5 });
        d.waypoints.Add("nope"); d.waypoints.Add("rootburrow2");
        PlayerState back = SaveSerializer.Restore(SaveSerializer.Parse(SaveSerializer.ToJson(d)));
        Assert.True(back.ChestEmpty("rb2-c", now + 1000));
        Assert.True(back.HasWaypoint("rootburrow2"));
        Assert.Single(back.Waypoints);
        Assert.Single(back.ChestCooldownsToList(now));
    }
}
