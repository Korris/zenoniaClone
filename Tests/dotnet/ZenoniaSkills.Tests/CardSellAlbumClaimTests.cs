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
    public void ALockedKindAndCopiesInTheDeckCannotBeSold()
    {
        PlayerState p = Hero();
        p.Cards.Add("thornBoar", 3);
        Assert.True(p.Cards.SetLocked("thornBoar", true));
        int gold = p.Gold;
        Assert.False(CardOps.Sell(p, "thornBoar", 1).Ok); // locked: nothing is for sale
        Assert.Equal(gold, p.Gold);
        p.Cards.SetLocked("thornBoar", false);
        p.Cards.Deck.Add("thornBoar");
        Assert.False(CardOps.Sell(p, "thornBoar", 3).Ok); // one is in the deck
        Assert.True(CardOps.Sell(p, "thornBoar", 2).Ok);
        Assert.Equal(gold + 100, p.Gold);
        Assert.Equal(1, p.Cards.Count("thornBoar"));
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
    public void LockCardLocksTheKindAndUnlocks()
    {
        PlayerState p = Hero();
        p.Cards.Add("forestWolf", 4);
        p.Cards.Deck.Add("forestWolf");
        Assert.True(CardOps.SetLock(p, "forestWolf", true).Ok);
        Assert.True(p.Cards.IsLocked("forestWolf"));
        Assert.Equal(0, p.Cards.Spare("forestWolf"));
        Assert.True(CardOps.SetLock(p, "forestWolf", true).Ok); // idempotent
        Assert.True(CardOps.SetLock(p, "forestWolf", false).Ok);
        Assert.False(p.Cards.IsLocked("forestWolf"));
        Assert.Equal(3, p.Cards.Spare("forestWolf")); // 4 owned, 1 in the deck
        Assert.False(CardOps.SetLock(p, "meadowSlime", true).Ok); // not owned
        Assert.False(CardOps.SetLock(p, "nosuchcard", true).Ok);
    }

    [Fact]
    public void AlbumClaimIsRefusedWhenTheGoldWouldBeCutAndTheBitStaysFree()
    {
        PlayerState p = Hero();
        foreach (CardDef c in CardTable.All.Skip(CardTable.LiveStart).Take(3)) p.Cards.Add(c.Id, 1);
        p.Gold = PlayerState.GoldCap - 100;
        Assert.False(AlbumRules.Claim(p, 0).Ok);
        Assert.Equal(0, p.Cards.AlbumClaimed);
        Assert.Equal(PlayerState.GoldCap - 100, p.Gold);
        p.Gold = 0;
        Assert.True(AlbumRules.Claim(p, 0).Ok);
    }

    [Fact]
    public void AlbumClaimThroughTheOpPaysOnceAndChecksTheMilestone()
    {
        PlayerState p = Hero();
        foreach (CardDef c in CardTable.All.Skip(CardTable.LiveStart).Take(2)) p.Cards.Add(c.Id, 1);
        Assert.False(AlbumRules.Claim(p, 0).Ok);   // 2 of 3
        p.Cards.Add(CardTable.All[CardTable.LiveStart + 2].Id, 1);
        int gold = p.Gold;
        Assert.True(AlbumRules.Claim(p, 0).Ok);
        Assert.Equal(gold + AlbumRules.Gold[0], p.Gold);
        Assert.False(AlbumRules.Claim(p, 0).Ok);   // once
        Assert.Equal(gold + AlbumRules.Gold[0], p.Gold);
        Assert.False(AlbumRules.Claim(p, 1).Ok);   // 6 not reached
        Assert.False(AlbumRules.Claim(p, 9).Ok);   // no such milestone
    }

    [Fact]
    public void SoldCardsStillCountForTheAlbumMilestone()
    {
        PlayerState p = Hero();
        foreach (CardDef c in CardTable.All.Skip(CardTable.LiveStart).Take(3)) { p.Cards.Add(c.Id, 1); CardOps.Sell(p, c.Id, 1); }
        Assert.Equal(0, p.Cards.OwnedKinds);
        Assert.True(AlbumRules.Claim(p, 0).Ok);
    }

    [Fact]
    public void SellAllSpareTakesEveryCopyOutsideTheLockAndTheDeckAndPaysTheTotal()
    {
        PlayerState p = Hero();
        p.Cards.Add("meadowSlime", 12);
        p.Cards.Deck.Add("meadowSlime"); p.Cards.Deck.Add("meadowSlime");
        int gold = p.Gold, spare = p.Cards.Spare("meadowSlime");
        Assert.Equal(10, spare);
        // PlayerRpg maps Slot 0 of InvOp.CardSell to Spare(): the sale below is exactly that call
        ActionResult r = CardOps.Sell(p, "meadowSlime", spare);
        Assert.True(r.Ok);
        Assert.Equal(gold + 10 * CardFees.Sell(CardTable.Get("meadowSlime")), p.Gold);
        Assert.Equal(2, p.Cards.Count("meadowSlime")); // the 2 in the deck stay
        Assert.Equal(0, p.Cards.Spare("meadowSlime"));
        Assert.False(CardOps.Sell(p, "meadowSlime", p.Cards.Spare("meadowSlime")).Ok); // nothing left to sell: refused, not a no-op sale
    }

    // the three card ops as PlayerRpg.ApplyInvAction dispatches them (Slot -> copies / milestone / lock flag)
    [Fact]
    public void SlotZeroOfCardSellMeansEverySpareCopyAndASlotNumberMeansThatMany()
    {
        PlayerState p = Hero();
        p.Cards.Add("hornRabbit", 300); // more than a byte holds
        p.Cards.Deck.Add("hornRabbit");
        Assert.True(CardOps.SellBySlot(p, "hornRabbit", 5).Ok);
        Assert.Equal(295, p.Cards.Count("hornRabbit"));
        Assert.False(CardOps.SellBySlot(p, "hornRabbit", 295).Ok); // one is in the deck: refused as a whole
        Assert.Equal(295, p.Cards.Count("hornRabbit"));
        Assert.True(CardOps.SellBySlot(p, "hornRabbit", 0).Ok);
        Assert.Equal(1, p.Cards.Count("hornRabbit"));
        Assert.False(CardOps.SellBySlot(p, "hornRabbit", 0).Ok); // nothing spare: refused, not a free "sale"
    }

    [Fact]
    public void CardLockSlotOneLocksAndZeroUnlocks()
    {
        PlayerState p = Hero();
        p.Cards.Add("hornRabbit", 2);
        Assert.True(CardOps.LockBySlot(p, "hornRabbit", 1).Ok);
        Assert.True(p.Cards.IsLocked("hornRabbit"));
        Assert.False(CardOps.SellBySlot(p, "hornRabbit", 0).Ok);
        Assert.True(CardOps.LockBySlot(p, "hornRabbit", 0).Ok);
        Assert.False(p.Cards.IsLocked("hornRabbit"));
        Assert.True(CardOps.SellBySlot(p, "hornRabbit", 0).Ok);
    }

    [Fact]
    public void AlbumClaimSlotIsTheMilestoneIndex()
    {
        PlayerState p = Hero();
        foreach (CardDef c in CardTable.All.Skip(CardTable.LiveStart).Take(6)) p.Cards.Add(c.Id, 1);
        Assert.True(AlbumRules.Claim(p, 1).Ok);   // 6 cards: Card Hunter
        Assert.False(AlbumRules.Claim(p, 1).Ok);
        Assert.True(AlbumRules.Claim(p, 0).Ok);
        Assert.False(AlbumRules.Claim(p, 2).Ok);  // 9 not reached
    }
}
