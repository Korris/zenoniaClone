using Xunit;
using ZenoniaProto.Rpg;

/// <summary>The shader light FX of Ice Storm must land its meteors on the damage waves, and the ground light must follow the requested curve.</summary>
public class LightSkillTimingTests
{
    [Fact]
    public void MeteorsLandOnTheDamageWaves()
    {
        Assert.Equal(new[] { 650, 1050, 1450, 1850 }, new[] { LightSkillTiming.WaveMs(0), LightSkillTiming.WaveMs(1), LightSkillTiming.WaveMs(2), LightSkillTiming.WaveMs(3) });
        Assert.Equal(3, LightSkillTiming.SpearsPerWave);
        for (int i = 0; i < LegacySkillNumbers.IceStormSpears; i++)
        {
            int wave = i / 3, land = LightSkillTiming.LandMs(i);
            Assert.Equal((int)(LegacySkillNumbers.IceStormCastMs + wave * LegacySkillNumbers.IceStormWaveMs), land);
            Assert.Equal(1f, LightSkillTiming.MeteorProgress(i, land));         // on the ground exactly when the wave damages
            Assert.True(LightSkillTiming.MeteorProgress(i, land - 1) < 1f);
            Assert.Equal(0f, LightSkillTiming.MeteorProgress(i, LightSkillTiming.MeteorStartMs(i)));
        }
        Assert.Equal(1850 + 850, LightSkillTiming.EndMs());
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
