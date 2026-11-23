using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

// Phase 03: sell to an NPC, Lock Card, Album claim: the pure rules behind InvOp.CardSell / CardLock / AlbumClaim (PlayerRpg.ApplyInvAction calls exactly these).
public class CardSellAlbumClaimTests
{
    static PlayerState Hero() { return PlayerState.NewGame("warrior"); }

    [Fact]
    public void SellPaysTheRarityPriceAndTakesTheCopies()
    {
        PlayerState p = Hero();
        p.Cards.Add("frostSlime", 3); // R = 200 G
        int gold = p.Gold;
        Assert.True(CardOps.Sell(p, "frostSlime", 2).Ok);
        Assert.Equal(gold + 400, p.Gold);
        Assert.Equal(1, p.Cards.Count("frostSlime"));
        Assert.True(p.Cards.InAlbum("frostSlime")); // the Album keeps it
    }

    [Fact]
    public void SellPricesFollowTheRarityTable()
    {
        Assert.Equal(50, CardFees.Sell(CardTable.Get("meadowSlime")));
        Assert.Equal(200, CardFees.Sell(CardTable.Get("frostSlime")));
        Assert.Equal(800, CardFees.Sell(CardTable.Get("duneScarab")));
        Assert.Equal(2500, CardFees.Sell(CardTable.Get("frostfangKing")));
    }

    [Fact]
    public void LockedAndDeckCopiesCannotBeSold()
    {
        PlayerState p = Hero();
        p.Cards.Add("thornBoar", 3);
        Assert.True(p.Cards.SetLocked("thornBoar", 2));
        int gold = p.Gold;
        Assert.False(CardOps.Sell(p, "thornBoar", 2).Ok); // 1 spare only
        Assert.True(CardOps.Sell(p, "thornBoar", 1).Ok);
        Assert.False(CardOps.Sell(p, "thornBoar", 1).Ok);
        Assert.Equal(gold + 50, p.Gold);
        Assert.Equal(2, p.Cards.Count("thornBoar"));
        p.Cards.SetLocked("thornBoar", 0);
        p.Cards.Deck.Add("thornBoar");
        Assert.False(CardOps.Sell(p, "thornBoar", 2).Ok); // one is in the deck
        Assert.True(CardOps.Sell(p, "thornBoar", 1).Ok);
    }

    [Fact]
    public void SellRefusesBadInput()
    {
        PlayerState p = Hero();
        p.Cards.Add("hornRabbit", 1);
        int gold = p.Gold;
        Assert.False(CardOps.Sell(p, "nosuchcard", 1).Ok);
        Assert.False(CardOps.Sell(p, "meadowSlime", 1).Ok); // not owned
        Assert.False(CardOps.Sell(p, "hornRabbit", 0).Ok);
        Assert.False(CardOps.Sell(p, "", 1).Ok);
        Assert.Equal(gold, p.Gold);
        Assert.Equal(1, p.Cards.Count("hornRabbit"));
    }

    [Fact]
    public void SellIsRefusedWhenGoldWouldPassTheCap()
    {
        PlayerState p = Hero();
        p.Cards.Add("frostfangKing", 1);
        p.Gold = PlayerState.GoldCap - 100;
        Assert.False(CardOps.Sell(p, "frostfangKing", 1).Ok);
        Assert.Equal(1, p.Cards.Count("frostfangKing"));
    }

    [Fact]
    public void LockCardLocksEveryCopyOutsideTheDeckAndUnlocks()
    {
        PlayerState p = Hero();
        p.Cards.Add("forestWolf", 4);
        p.Cards.Deck.Add("forestWolf");
        Assert.True(CardOps.SetLock(p, "forestWolf", true).Ok);
        Assert.Equal(3, p.Cards.Locked("forestWolf"));
        Assert.Equal(0, p.Cards.Spare("forestWolf"));
        Assert.True(CardOps.SetLock(p, "forestWolf", false).Ok);
        Assert.Equal(0, p.Cards.Locked("forestWolf"));
        Assert.False(CardOps.SetLock(p, "meadowSlime", true).Ok); // not owned
        Assert.False(CardOps.SetLock(p, "nosuchcard", true).Ok);
    }

    [Fact]
    public void AlbumClaimThroughTheOpPaysOnceAndChecksTheMilestone()
    {
        PlayerState p = Hero();
        foreach (CardDef c in CardTable.All.Take(9)) p.Cards.Add(c.Id, 1);
        Assert.False(AlbumRules.Claim(p, 0).Ok);   // 9 of 10
        p.Cards.Add(CardTable.All[9].Id, 1);
        int gold = p.Gold;
        Assert.True(AlbumRules.Claim(p, 0).Ok);
        Assert.Equal(gold + AlbumRules.Gold10, p.Gold);
        Assert.False(AlbumRules.Claim(p, 0).Ok);   // once
        Assert.Equal(gold + AlbumRules.Gold10, p.Gold);
        Assert.False(AlbumRules.Claim(p, 1).Ok);   // 20 not reached
        Assert.False(AlbumRules.Claim(p, 9).Ok);   // no such milestone
    }

    [Fact]
    public void SoldCardsStillCountForTheAlbumMilestone()
    {
        PlayerState p = Hero();
        foreach (CardDef c in CardTable.All.Take(10)) { p.Cards.Add(c.Id, 1); CardOps.Sell(p, c.Id, 1); }
        Assert.Equal(0, p.Cards.OwnedKinds);
        Assert.True(AlbumRules.Claim(p, 0).Ok);
    }
}
