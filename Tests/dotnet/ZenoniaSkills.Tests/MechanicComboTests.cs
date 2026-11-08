using Xunit;
using ZenoniaProto;
using ZenoniaProto.Rpg;

/// <summary>
/// Mechanic basic attack (2026-11-08, approved board): every bolt leaves the shooting gun's muzzle (hit 3 between both pistols, hit 4 the cannon)
/// and flies its range minus how far that muzzle is ahead of the body point, so it ends where a body shot would; hit 3 is one big bullet instead of
/// a bullet plus a tracer.
/// </summary>
public class MechanicComboTests
{
    static readonly float[][] Facings = { new[] { 0f, -1f }, new[] { 0f, 1f }, new[] { -1f, 0f }, new[] { 1f, 0f } };

    [Fact]
    public void MechanicBoltsStartAtTheBodyCentreInEveryFacing()
    {
        foreach (float[] f in Facings)
        {
            float ox, oy;
            RangedAimRules.BoltOrigin(true, 100f, 200f, f[0], f[1], out ox, out oy);
            Assert.Equal(100f, ox);
            Assert.Equal(200f + RangedAimRules.BoltLift, oy);
        }
    }

    [Fact]
    public void PistolBoltsLeaveTheGunMuzzleAndEndWhereABodyShotWould()
    {
        for (int step = 1; step <= 4; step++)
            for (int facing = 0; facing < 4; facing++)
            {
                float[] f = Facings[facing];
                float ox, oy, cut, mx, my;
                Assert.True(RangedAimRules.MechanicBoltOrigin(step, facing, 100f, 200f, f[0], f[1], out ox, out oy, out cut));
                Assert.True(RangedAimRules.MechanicMuzzle(step, facing, out mx, out my));
                // drawn at the muzzle: the sim point sits BoltDrawLift under it (ComboProjectile.VisualLift)
                Assert.Equal(100f + mx, ox, 3);
                Assert.Equal(200f + my - RangedAimRules.BoltDrawLift, oy, 3);
                // the end point along the facing equals a body shot's: body point + range == muzzle + (range - cut)
                float bodyAlong = 100f * f[0] + (200f + RangedAimRules.BoltLift) * f[1];
                float muzzleAlong = ox * f[0] + oy * f[1];
                Assert.Equal(bodyAlong, muzzleAlong - cut, 3);
                // pistols: within 16 units sideways, a torso to a head high; the cannon reaches further (its muzzle under the feet facing down, over the head facing up)
                Assert.InRange(mx, step < 4 ? -16f : -20f, step < 4 ? 16f : 20f);
                Assert.InRange(my, step < 4 ? 8f : -6f, step < 4 ? 20f : 30f);
            }
    }

    [Fact]
    public void SideShotsLeaveFromTheFacingSideAndTheCannonFurtherOut()
    {
        float ox, oy, cut;
        for (int step = 1; step <= 3; step++)
        {
            RangedAimRules.MechanicBoltOrigin(step, 3, 100f, 200f, 1f, 0f, out ox, out oy, out cut);
            Assert.True(ox > 108f && cut > 8f); // facing right: the gun is out in front
            RangedAimRules.MechanicBoltOrigin(step, 2, 100f, 200f, -1f, 0f, out ox, out oy, out cut);
            Assert.True(ox < 92f && cut > 8f);
        }
        // hit 4 leaves the cannon muzzle, further out than the pistols facing right
        float px, py, pcut;
        Assert.True(RangedAimRules.MechanicBoltOrigin(4, 3, 100f, 200f, 1f, 0f, out ox, out oy, out cut));
        RangedAimRules.MechanicBoltOrigin(1, 3, 100f, 200f, 1f, 0f, out px, out py, out pcut);
        Assert.True(cut > pcut);
        // no fifth step, no muzzle: body point, nothing cut
        Assert.False(RangedAimRules.MechanicBoltOrigin(5, 3, 100f, 200f, 1f, 0f, out ox, out oy, out cut));
        Assert.Equal(100f, ox);
        Assert.Equal(200f + RangedAimRules.BoltLift, oy);
        Assert.Equal(0f, cut);
    }

    [Fact]
    public void OtherClassesKeepTheMuzzleInFront()
    {
        float ox, oy;
        RangedAimRules.BoltOrigin(false, 100f, 200f, 1f, 0f, out ox, out oy);
        Assert.Equal(112f, ox);
        Assert.Equal(204f, oy);
        RangedAimRules.BoltOrigin(false, 100f, 200f, 0f, -1f, out ox, out oy);
        Assert.Equal(100f, ox);
        Assert.Equal(192f, oy);
    }

    [Fact]
    public void BoltsStillReachAsFarAheadAsFromTheOldMuzzle()
    {
        // Before 2026-11-08: bullets flew 70 and the cannon shell 65 from 12 units ahead of the hero; every shot now starts at the body
        // (HeroController.FireBolt: BoltOrigin(fromBody: Mechanic)), so each range carries those 12. Since the board repaint the pistol bolts leave the gun
        // muzzle and fly Range minus the muzzle's lead (RangedAimRules.MechanicBoltOrigin), still ending Range from the body point.
        ComboHit[] hits = ComboConfigs.MechanicHits;
        Assert.Equal(70f + RangedAimRules.MuzzleAhead, hits[0].Bolt.Range);
        Assert.Equal(70f + RangedAimRules.MuzzleAhead, hits[1].Bolt.Range);
        Assert.Equal(70f + RangedAimRules.MuzzleAhead, hits[2].Bolt.Range);
        Assert.Equal(65f + RangedAimRules.MuzzleAhead, hits[3].Bolt.Range);
        // The cannon's blast radius and speed are unchanged by the move.
        Assert.Equal(24f, hits[3].Bolt.ExplodeRadius);
        Assert.Equal(240f, hits[3].Bolt.Speed);
    }

    [Fact]
    public void HitThreeIsOneBigBulletWithThePairsDamage()
    {
        ComboHit[] hits = ComboConfigs.MechanicHits;
        BoltDef normal = hits[0].Bolt, big = hits[2].Bolt;
        Assert.Equal(4, hits.Length);
        Assert.NotSame(normal, big);
        // Damage: the old hit 3 rolled 1.05 once (the second pistol's tracer had no hit box), the single big bullet carries all of it.
        Assert.Equal(1.05f, hits[2].Mult);
        Assert.Equal(1, hits[2].Stabs);
        Assert.Equal(0f, big.ExplodeRadius);
        Assert.Equal(BoltStyle.Plasma, big.Style);
        // About 1.6-2x the normal bullet on screen, the hit box scaled with it.
        Assert.InRange(big.Scale / normal.Scale, 1.6f, 2f);
        Assert.InRange(big.W / normal.W, 1.5f, 2f);
        Assert.InRange(big.H / normal.H, 1.5f, 2f);
        Assert.Equal(normal.Speed, big.Speed);
        Assert.Equal(normal.Range, big.Range);
    }
}
