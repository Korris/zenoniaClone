using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>The shader light FX of Astral Cataclysm must land each meteor on its rain drop's damage step, and the ground light must follow the requested curve.</summary>
public class LightSkillTimingTests
{
    static SkillEffect Rain() { return SkillTable.ById("wizard.astral-cataclysm").Effects.First(e => e.Kind == EffectKind.Rain); }

    [Fact]
    public void MeteorsLandOnTheDamageSteps()
    {
        SkillEffect rain = Rain();
        SkillStep[] steps = SkillSchedule.Build(SkillTable.ById("wizard.astral-cataclysm").Effects).Where(s => s.EffectIndex == 0).ToArray();
        Assert.Equal(steps.Length, rain.Count);
        for (int i = 0; i < steps.Length; i++)
        {
            Assert.Equal(steps[i].AtMs, LightSkillTiming.LandMs(rain, steps[i].HitIndex));
            Assert.Equal(1f, LightSkillTiming.MeteorProgress(rain, i, steps[i].AtMs));     // on the ground exactly when the damage lands
            Assert.True(LightSkillTiming.MeteorProgress(rain, i, steps[i].AtMs - 1) < 1f || steps[i].AtMs == 0);
        }
        Assert.Equal(0, LightSkillTiming.MeteorStartMs(rain, 0));
        Assert.True(LightSkillTiming.EndMs(rain) > steps.Last().AtMs);
    }

    [Fact]
    public void GroundLightPeaksAt150MsAndEndsWithTheExplosion()
    {
        Assert.Equal(0f, LightSkillTiming.GroundLight(0f));
        Assert.Equal(1f, LightSkillTiming.GroundLight(LightSkillTiming.GroundPeakMs), 3);
        Assert.True(LightSkillTiming.GroundLight(75f) > 0.5f && LightSkillTiming.GroundLight(75f) < 1f);
        Assert.True(LightSkillTiming.GroundLight(300f) < LightSkillTiming.GroundLight(200f));
        Assert.Equal(0f, LightSkillTiming.GroundLight(LightSkillTiming.ExplosionMs));
        Assert.True(LightSkillTiming.GroundLight(LightSkillTiming.ExplosionMs - 10f) < 0.01f);
    }
}
