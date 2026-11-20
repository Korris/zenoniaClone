using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Soft unit collision (2026-11-20, RO3 style): foot cores, sliding instead of shoving, eased separation.</summary>
public class BodyCollisionRulesTests
{
    [Fact]
    public void FootRadiusFollowsBodyWidthWithinTheClassBand()
    {
        // Mire Hydra Queen: painted 102.5 units, hit box 43 -> body half width ~35.9 -> foot ~17.9 (inside the wanted 15-20 band, not swelling with the necks).
        float queen = BodyCollisionRules.FootRadius(43f, 102.5f, true, true);
        Assert.InRange(queen, 17f, 19f);
        Assert.Equal(BodyCollisionRules.WantedFootMax, BodyCollisionRules.FootRadius(43f, 400f, true, true)); // capped
        Assert.InRange(BodyCollisionRules.FootRadius(22f, 35f, true, false), 9f, 10f);                        // Frostfang King: 0.8 x hit half width 12.25
        Assert.Equal(BodyCollisionRules.BossFootMin, BodyCollisionRules.FootRadius(12f, 20f, true, false));   // a small boss: floor
        Assert.Equal(BodyCollisionRules.NormalFootMin, BodyCollisionRules.FootRadius(6f, 10f, false, false));
    }

    [Fact]
    public void NormalMonstersAreSofterThanBosses()
    {
        Assert.True(BodyCollisionRules.ContactDistance(6f, false) < BodyCollisionRules.ContactDistance(6f, true));
        Assert.Equal(6f + BodyCollisionRules.HeroRadius, BodyCollisionRules.ContactDistance(6f, true), 3);
    }

    [Fact]
    public void GroundDistanceCountsYDouble()
    {
        Assert.Equal(10f, BodyCollisionRules.GroundDist(10f, 0f), 3);
        Assert.Equal(10f, BodyCollisionRules.GroundDist(0f, 5f), 3);
    }

    [Fact]
    public void MoveIntoTheCoreIsBlockedButSlidingAndLeavingAreNot()
    {
        float mx = -5f, my = 0f; // hero 22 right of a contact-20 core walks 5 left
        BodyCollisionRules.ClipMove(22f, 0f, 20f, ref mx, ref my);
        Assert.Equal(0f, mx, 3);
        mx = -5f; my = 3f;       // diagonal: the sideways part stays (slides around)
        BodyCollisionRules.ClipMove(22f, 0f, 20f, ref mx, ref my);
        Assert.Equal(0f, mx, 3); Assert.Equal(3f, my, 3);
        mx = 5f; my = 0f;        // overlapping and walking out: free
        BodyCollisionRules.ClipMove(10f, 0f, 20f, ref mx, ref my);
        Assert.Equal(5f, mx, 3);
        mx = -1f; my = 0f;       // far away: untouched
        BodyCollisionRules.ClipMove(40f, 0f, 20f, ref mx, ref my);
        Assert.Equal(-1f, mx, 3);
    }

    [Fact]
    public void OverlapEasesOutOverAFifthOfASecondWithoutJumping()
    {
        float x = 12f; // 8 units deep in a contact-20 core
        float sx, sy;
        Assert.True(BodyCollisionRules.Separate(x, 0f, 20f, 1f / 30f, out sx, out sy));
        Assert.True(sx > 0f && sx < 8f); // first step is partial
        for (int i = 0; i < 30; i++) if (BodyCollisionRules.Separate(x, 0f, 20f, 1f / 30f, out sx, out sy)) x += sx;
        Assert.InRange(x, 19.5f, 20.001f); // out within a second, never past the edge
        Assert.False(BodyCollisionRules.Separate(25f, 0f, 20f, 1f / 30f, out sx, out sy));
        Assert.True(BodyCollisionRules.Separate(0f, 0f, 20f, 1f / 30f, out sx, out sy));
        Assert.True(sy < 0f); // on the same point: steps south, in front of the body
    }

    [Fact]
    public void ABlockedMonsterStillReachesTheHeroItTouches()
    {
        Assert.True(BodyCollisionRules.TouchReach(22f, 0f, 22.4f, BodyCollisionRules.TouchStrikeSlack));
        Assert.False(BodyCollisionRules.TouchReach(30f, 0f, 22.4f, BodyCollisionRules.TouchStrikeSlack));
    }
}
