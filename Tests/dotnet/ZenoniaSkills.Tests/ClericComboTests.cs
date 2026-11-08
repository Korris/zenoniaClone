using Xunit;
using ZenoniaProto;
using ZenoniaProto.Rpg;

/// <summary>
/// Cleric basic attack redone from the board docs/reference/cleric-basic-attack-board.png (2026-11-08): only the picture changed. Hit 3 (Halo Lance)
/// is told apart by its sprite name (the lance row of the "cleric-v2" FX set) and must fly, hit and time exactly like the light arrow of hits 1-2;
/// the board's labels (0.26 s, ~4 tiles; finisher 0.46 s, small AoE) are these numbers.
/// </summary>
public class ClericComboTests
{
    static readonly ComboHit[] Hits = ComboConfigs.ClericHits;

    [Fact]
    public void FourHitsWithTheBoardTiming()
    {
        Assert.Equal(4, Hits.Length);
        for (int i = 0; i < 3; i++) Assert.Equal(260f, Hits[i].TotalMs);
        Assert.Equal(460f, Hits[3].TotalMs);
        Assert.Equal(26f, Hits[3].Bolt.ExplodeRadius);
    }

    [Fact]
    public void HaloLanceOnlyChangesThePicture()
    {
        BoltDef arrow = Hits[0].Bolt, lance = Hits[2].Bolt;
        Assert.Same(arrow, Hits[1].Bolt);
        Assert.Equal(ComboConfigs.HaloLanceSprite, lance.Sprite);
        Assert.NotEqual(arrow.Sprite, lance.Sprite);
        Assert.Equal(arrow.Speed, lance.Speed);
        Assert.Equal(arrow.Range, lance.Range);
        Assert.Equal(arrow.W, lance.W);
        Assert.Equal(arrow.H, lance.H);
        Assert.Equal(arrow.Scale, lance.Scale);
        Assert.Equal(arrow.ExplodeRadius, lance.ExplodeRadius);
        Assert.Equal(BoltStyle.HolyArrow, lance.Style);
    }

    static readonly float[][] Facings = { new[] { 0f, -1f }, new[] { 0f, 1f }, new[] { -1f, 0f }, new[] { 1f, 0f } };

    // Fix 2026-11-08: the bolts leave the staff crystal (hit 2: the shield), not a point 12 in front of the body that put the lance and orb
    // over her face; each still ends where the old shot did (same reach in front of the hero, server and client alike).
    [Fact]
    public void BoltsLeaveTheStaffOrShieldAndEndWhereTheOldShotDid()
    {
        for (int step = 1; step <= 4; step++)
            for (int facing = 0; facing < 4; facing++)
            {
                float[] f = Facings[facing];
                float ox, oy, cut, tx, ty, bx, by;
                Assert.True(RangedAimRules.ClericBoltOrigin(step, facing, 100f, 200f, f[0], f[1], out ox, out oy, out cut));
                Assert.True(RangedAimRules.ClericTip(step, facing, out tx, out ty));
                Assert.Equal(100f + tx, ox, 3);
                Assert.Equal(200f + ty - RangedAimRules.BoltDrawLift, oy, 3); // drawn at the tip (ComboProjectile.VisualLift)
                RangedAimRules.BoltOrigin(false, 100f, 200f, f[0], f[1], out bx, out by);
                float oldEnd = bx * f[0] + by * f[1] + Hits[step - 1].Bolt.Range;
                float newEnd = ox * f[0] + oy * f[1] + Hits[step - 1].Bolt.Range - cut;
                Assert.Equal(oldEnd, newEnd, 3);
                // on the figure: within 16 units sideways, from the feet to above the hair (hit 3 staff raised overhead)
                Assert.InRange(tx, -16f, 16f);
                Assert.InRange(ty, -2f, 26f);
            }
        float x, y, c;
        Assert.False(RangedAimRules.ClericBoltOrigin(5, 0, 100f, 200f, 0f, -1f, out x, out y, out c));
        Assert.Equal(0f, c);
    }

    [Fact]
    public void HaloLanceAndOrbStartOverTheHeadAndSideArrowsOutInFront()
    {
        float tx, ty;
        for (int facing = 0; facing < 4; facing++)
        {
            RangedAimRules.ClericTip(3, facing, out tx, out ty);
            Assert.True(ty > 20f); // the raised staff: over the hair, not the face (hair top ~18 units)
        }
        RangedAimRules.ClericTip(1, 3, out tx, out ty);
        Assert.True(tx > 10f); // thrust right: the crystal is out in front
        RangedAimRules.ClericTip(1, 2, out tx, out ty);
        Assert.True(tx < -10f);
    }

    [Fact]
    public void FiredUpOrFromTheRaisedStaffDownTheBoltStartsBehindHer()
    {
        for (int step = 1; step <= 4; step++)
        {
            Assert.True(RangedAimRules.ClericStartsBehind(step, 1)); // facing up the staff and shield are behind her
            Assert.False(RangedAimRules.ClericStartsBehind(step, 2)); // sideways the bolt leaves in front, away from the body
            Assert.False(RangedAimRules.ClericStartsBehind(step, 3));
        }
        Assert.True(RangedAimRules.ClericStartsBehind(3, 0)); // the lance comes down from over the head
        Assert.True(RangedAimRules.ClericStartsBehind(4, 0));
        Assert.False(RangedAimRules.ClericStartsBehind(1, 0)); // thrust down: the crystal is at her feet
        Assert.False(RangedAimRules.ClericStartsBehind(2, 0)); // the shield is in front of her
        Assert.False(RangedAimRules.ClericStartsBehind(5, 1));
        float tx, ty;
        for (int step = 1; step <= 4; step++)
            for (int facing = 0; facing < 4; facing++)
                if (RangedAimRules.ClericTip(step, facing, out tx, out ty)) Assert.True(ty < RangedAimRules.ClericBehindTop); // every start is inside the band
    }
}
