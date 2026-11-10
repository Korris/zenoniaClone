using Xunit;
using ZenoniaProto;

/// <summary>
/// Wizard basic attack after the boards of 2026-11-08 and 2026-11-10 (Crystal Thrust, Frost Sweep, Glacier Slam, Blizzard Nova). Hits 2 and 3 carry
/// their own BoltDef (sprites "frostShard", "glacierSpikes": the set's shard / spikes rows) with the same numbers as the magic bolt. 2026-11-10: hits
/// 1-3 350 ms and hit 4 640 ms (the 6 / 8-frame bodies were a blur at 260 / 470), multipliers x1.35 so the damage per second is unchanged.
/// </summary>
public class WizardComboTests
{
    [Fact]
    public void ComboKeepsFourHitsAndItsTiming()
    {
        ComboHit[] h = ComboConfigs.WizardHits;
        Assert.Equal(4, h.Length);
        for (int i = 0; i < 3; i++) Assert.Equal(350f, h[i].TotalMs);
        Assert.Equal(640f, h[3].TotalMs);
        Assert.InRange(h[0].Mult / h[0].TotalMs, 0.9f / 260f * 0.97f, 0.9f / 260f * 1.03f); // damage per second as before 2026-11-10
        Assert.InRange(h[3].Mult / h[3].TotalMs, 1.6f / 470f * 0.97f, 1.6f / 470f * 1.03f);
        Assert.Equal(26f, h[3].Bolt.ExplodeRadius);
    }

    [Fact]
    public void FrostShardIsTheMagicBoltWithAnotherPicture()
    {
        BoltDef bolt = ComboConfigs.WizardHits[0].Bolt, shard = ComboConfigs.WizardHits[1].Bolt;
        Assert.Equal(ComboConfigs.FrostShardSprite, shard.Sprite);
        Assert.Equal(bolt.Speed, shard.Speed);
        Assert.Equal(bolt.Range, shard.Range);
        Assert.Equal(bolt.W, shard.W);
        Assert.Equal(bolt.H, shard.H);
        Assert.Equal(bolt.Scale, shard.Scale);
        Assert.Equal(bolt.ExplodeRadius, shard.ExplodeRadius);
        Assert.Equal(bolt.Style, shard.Style);
        BoltDef spikes = ComboConfigs.WizardHits[2].Bolt; // hit 3 (Glacier Slam): the spikes picture, the magic bolt's numbers
        Assert.Equal(ComboConfigs.GlacierSpikesSprite, spikes.Sprite);
        Assert.Equal(bolt.Speed, spikes.Speed);
        Assert.Equal(bolt.Range, spikes.Range);
        Assert.Equal(bolt.W, spikes.W);
        Assert.Equal(bolt.H, spikes.H);
        Assert.Equal(bolt.ExplodeRadius, spikes.ExplodeRadius);
        Assert.Equal(bolt.Style, spikes.Style);
    }
}
