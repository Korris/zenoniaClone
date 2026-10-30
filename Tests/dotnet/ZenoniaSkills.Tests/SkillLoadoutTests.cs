using Xunit;
using ZenoniaProto.Rpg;

public class SkillLoadoutTests
{
    [Fact]
    public void NewGameLearnsFirstSkillAndPlacesItInSlotOne()
    {
        PlayerState p = PlayerState.NewGame("wizard");
        Assert.Equal(1, p.Skills[0]);
        Assert.Equal(0, p.SkillPointsFree);
        Assert.Equal(0, p.Loadout[0]);
        Assert.Equal(-1, p.Loadout[1]);
    }

    [Fact]
    public void LearningAutoFillsFirstEmptySlotOnlyOnFirstRank()
    {
        var p = new PlayerState("warrior") { Level = 20 };
        for (int i = 0; i < 3; i++) Assert.Equal(LearnResult.Ok, p.LearnSkill(0));
        Assert.Equal(LearnResult.PrereqMissing, p.LearnSkill(2));
        Assert.Equal(LearnResult.Ok, p.LearnSkill(1));
        Assert.Equal(0, p.Loadout.SlotOf(0));
        Assert.Equal(1, p.Loadout.SlotOf(1));
        Assert.Equal(20 - 4, p.SkillPointsFree);
        p.Loadout.Unassign(0);
        p.LearnSkill(0); // rank-up must not re-place the skill
        Assert.Equal(-1, p.Loadout.SlotOf(0));
    }

    [Fact]
    public void AssignRejectsUnlearnedAndKeepsSkillsUnique()
    {
        var r = new SkillRanks("warrior");
        r.Learn(SkillTable.Get("warrior", 0), 5);
        var l = new SkillLoadout();
        Assert.False(l.Assign(0, 1, r));
        Assert.False(l.Assign(8, 0, r));
        Assert.False(l.Assign(-1, 0, r));
        Assert.True(l.Assign(3, 0, r));
        Assert.True(l.Assign(5, 0, r)); // moves
        Assert.Equal(-1, l[3]); Assert.Equal(0, l[5]);
        Assert.Equal(5, l.AutoFill(0)); // already placed
        Assert.Equal(0, l.AutoFill(1));
    }

    [Fact]
    public void AutoFillStopsWhenFull()
    {
        var l = new SkillLoadout();
        for (int i = 0; i < 8; i++) Assert.Equal(i, l.AutoFill(i));
        Assert.Equal(-1, l.AutoFill(8));
        l.Clear();
        Assert.Equal(-1, l[0]);
    }

    [Fact]
    public void LoadDropsUnlearnedDuplicateAndOutOfRangeSlots()
    {
        var r = new SkillRanks("warrior");
        r.Load(new[] { 3, 1, 0, 0, 0, 0, 0, 0 }, 10);
        var l = new SkillLoadout();
        l.Load(new[] { 1, 1, 5, 99, -7, 0 }, r);
        Assert.Equal(new[] { 1, -1, -1, -1, -1, 0, -1, -1 }, l.ToList());
        l.Load(null, r);
        Assert.Equal(-1, l[0]);
    }

    [Fact]
    public void ListsRoundTrip()
    {
        var r = new SkillRanks("assassin");
        r.Load(new[] { 4, 3, 2, 0, 0, 0, 0, 0 }, 20);
        var l = new SkillLoadout();
        l.Load(new[] { 2, 0, 1 }, r);
        var r2 = new SkillRanks("assassin"); r2.Load(r.ToList(), 20);
        var l2 = new SkillLoadout(); l2.Load(l.ToList(), r2);
        Assert.Equal(r.ToList(), r2.ToList());
        Assert.Equal(l.ToList(), l2.ToList());
    }
}

public class SkillPlaceAndRespecTests
{
    static PlayerState Leveled(int level, int gold)
    {
        var p = new PlayerState("warrior") { Level = level, Gold = gold };
        for (int i = 0; i < 3; i++) p.LearnSkill(0);
        p.LearnSkill(1);
        return p; // skill 0 in slot 0, skill 1 in slot 1
    }

    [Fact]
    public void PlaceSwapsWithTheOccupantWhenTheSkillWasAlreadyPlaced()
    {
        PlayerState p = Leveled(20, 0);
        Assert.True(p.Loadout.Place(1, 0, p.Skills));
        Assert.Equal(0, p.Loadout[1]);
        Assert.Equal(1, p.Loadout[0]);
    }

    [Fact]
    public void PlaceIntoEmptySlotMovesAndRejectsUnlearned()
    {
        PlayerState p = Leveled(20, 0);
        Assert.True(p.Loadout.Place(5, 1, p.Skills));
        Assert.Equal(-1, p.Loadout[1]);
        Assert.Equal(1, p.Loadout[5]);
        Assert.False(p.Loadout.Place(2, 3, p.Skills));
        Assert.False(p.Loadout.Place(8, 0, p.Skills));
    }

    [Fact]
    public void RespecCostScalesWithLevel()
    {
        Assert.Equal(50, SkillRespec.Cost(1));
        Assert.Equal(1000, SkillRespec.Cost(20));
        Assert.Equal(3000, SkillRespec.Cost(60));
    }

    [Fact]
    public void RespecChargesGoldRefundsPointsAndClearsMagazine()
    {
        PlayerState p = Leveled(20, 1500);
        ActionResult r = SkillRespec.Apply(p);
        Assert.True(r.Ok);
        Assert.Equal(500, p.Gold);
        Assert.Equal(20, p.SkillPointsFree);
        Assert.Equal(-1, p.Loadout[0]);
    }

    [Fact]
    public void RespecNeedsGoldAndSomethingToReset()
    {
        PlayerState poor = Leveled(20, 999);
        Assert.False(SkillRespec.Apply(poor).Ok);
        Assert.Equal(999, poor.Gold);
        Assert.Equal(4, poor.Skills.Sum);
        Assert.False(SkillRespec.Apply(new PlayerState("wizard") { Gold = 9999 }).Ok);
    }
}
