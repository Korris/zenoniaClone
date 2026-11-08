using Xunit;
using ZenoniaProto;

/// <summary>
/// Wizard basic attack after the board of 2026-11-08 (Arcane Thrust, Frost Page, Twin Sigil, Astral Nova): only the pictures changed. Hit 2 now
/// carries its own BoltDef (sprite "frostShard", the v3 shard row) with the same numbers as the magic bolt, so the server and the damage are unchanged.
/// </summary>
public class WizardComboTests
{
    [Fact]
    public void ComboKeepsFourHitsAndItsTiming()
    {
        ComboHit[] h = ComboConfigs.WizardHits;
        Assert.Equal(4, h.Length);
        for (int i = 0; i < 3; i++) Assert.Equal(260f, h[i].TotalMs);
        Assert.Equal(470f, h[3].TotalMs);
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
        Assert.Same(bolt, ComboConfigs.WizardHits[2].Bolt); // hit 3 (Twin Sigil) fires the arcane bolt again
    }
}
