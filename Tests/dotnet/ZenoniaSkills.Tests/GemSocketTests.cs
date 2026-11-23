using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

// Phase 04: card -> Soul Gem, socketing, removing, the trade / discard lock, sockets per slot and the fees.
public class GemSocketTests
{
    static PlayerState Hero(int gold = 100000)
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Gold = gold;
        return p;
    }

    static Item Gear(PlayerState p, string slot, string uid)
    {
        Item it = ItemFactory.Generate(() => 0.4, 10, slot, "rare", "warrior");
        it.uid = uid;
        p.AddToInventory(it);
        return it;
    }

    static string IdOf(CardRarity r, bool boss = false) { return CardTable.All.First(c => c.Rarity == r && c.IsBoss == boss).Id; }

    // ---- fees ----

    [Fact]
    public void FeesFollowTheDesignTable()
    {
        int[] convert = { 500, 2000, 8000, 25000 }, remove = { 200, 800, 3000, 10000 };
        for (int i = 0; i < 4; i++)
        {
            CardDef c = CardTable.All.First(x => (int)x.Rarity == i);
            Assert.Equal(convert[i], CardFees.Convert(c));
            Assert.Equal(remove[i], CardFees.Remove(c));
        }
    }

    // ---- convert ----

    [Fact]
    public void ConvertNormalCardCostsFiveHundredAndOneCardAndGivesAnUnlockedGem()
    {
        PlayerState p = Hero(1000);
        string id = IdOf(CardRarity.N);
        p.Cards.Add(id, 2);
        ActionResult r = CardOps.Convert(p, id, GemKind.Power);
        Assert.True(r.Ok, r.Text);
        Assert.Equal(500, p.Gold);
        Assert.Equal(1, p.Cards.Count(id));
        Assert.Equal(1, p.Cards.GemCount(id, GemKind.Power, false));
        Assert.Equal(0, p.Cards.GemCount(id, GemKind.Power, true));
    }

    [Fact]
    public void ConvertKeepsTheCardInTheAlbum()
    {
        PlayerState p = Hero();
        string id = IdOf(CardRarity.R);
        p.Cards.Add(id, 1);
        Assert.True(CardOps.Convert(p, id, GemKind.Guard).Ok);
        Assert.Equal(0, p.Cards.Count(id));
        Assert.True(p.Cards.InAlbum(id));
        Assert.Equal(1, p.Cards.GemCount(id, GemKind.Guard, false));
    }

    [Fact]
    public void ConvertRefusals()
    {
        PlayerState p = Hero(499);
        string id = IdOf(CardRarity.N);
        Assert.False(CardOps.Convert(p, "ghost", GemKind.Power).Ok);
        Assert.False(CardOps.Convert(p, id, GemKind.Power).Ok);          // not owned
        p.Cards.Add(id, 1);
        Assert.False(CardOps.Convert(p, id, GemKind.Power).Ok);          // 499 G < 500
        Assert.Equal(1, p.Cards.Count(id)); Assert.Equal(499, p.Gold);   // nothing was taken
        p.Gold = 5000;
        p.Cards.SetLocked(id, 1);
        Assert.False(CardOps.Convert(p, id, GemKind.Power).Ok);          // locked copy
        p.Cards.SetLocked(id, 0);
        p.Cards.Deck.Add(id);
        Assert.False(CardOps.Convert(p, id, GemKind.Power).Ok);          // the only copy is in the deck
        Assert.Equal(5000, p.Gold);
    }

    [Fact]
    public void ConvertIsRefusedBeforeTakingAnythingWhenTheGemCannotBeStored()
    {
        PlayerState p = Hero();
        string id = IdOf(CardRarity.N);
        p.Cards.Add(id, 3);
        p.Cards.AddGem(id, GemKind.Power, false, 999);                   // full stack
        ActionResult r = CardOps.Convert(p, id, GemKind.Power);
        Assert.False(r.Ok);
        Assert.Equal(3, p.Cards.Count(id)); Assert.Equal(100000, p.Gold); Assert.Equal(999, p.Cards.GemCount(id, GemKind.Power, false));
        // row limit (160 = every card x kind x locked, so it only guards forged saves): all 160 combinations fit
        PlayerState q = Hero();
        int rows = 0;
        foreach (CardDef c in CardTable.All)
            foreach (GemKind k in new[] { GemKind.Power, GemKind.Guard })
                foreach (bool locked in new[] { false, true })
                    if (q.Cards.AddGem(c.Id, k, locked)) rows++;
        Assert.Equal(CardCollection.MaxGemRows, rows);
        Assert.Equal(CardTable.Count * 4, CardCollection.MaxGemRows);
    }

    // ---- socket ----

    [Fact]
    public void SocketMovesTheGemIntoTheItemForFree()
    {
        PlayerState p = Hero();
        string id = IdOf(CardRarity.N);
        p.Cards.AddGem(id, GemKind.Power, false);
        Item w = Gear(p, Slots.Weapon, "w1");
        ActionResult r = CardOps.Socket(p, "w1", id, GemKind.Power);
        Assert.True(r.Ok, r.Text);
        Assert.Equal(new[] { id + ":p" }, w.sockets.ToArray());
        Assert.Equal(0, p.Cards.GemCount(id, GemKind.Power, false));
        Assert.Equal(100000, p.Gold);
    }

    [Fact]
    public void SocketLimitsAreWeaponTwoArmorTwoNecklaceOneOthersNone()
    {
        Assert.Equal(2, GemSockets.SlotCount(Slots.Weapon));
        Assert.Equal(2, GemSockets.SlotCount(Slots.Armor));
        Assert.Equal(1, GemSockets.SlotCount(Slots.Helmet));
        foreach (string s in new[] { Slots.Gloves, Slots.Boots, Slots.Offhand, Slots.Headgear, Slots.Pet }) Assert.Equal(0, GemSockets.SlotCount(s));

        PlayerState p = Hero();
        CardDef a = CardTable.All[0], b = CardTable.All[1], c = CardTable.All[2];
        foreach (CardDef d in new[] { a, b, c }) p.Cards.AddGem(d.Id, GemKind.Power, false);
        Gear(p, Slots.Helmet, "n1"); Gear(p, Slots.Gloves, "g1"); Gear(p, Slots.Armor, "a1");
        Assert.True(CardOps.Socket(p, "n1", a.Id, GemKind.Power).Ok);
        Assert.False(CardOps.Socket(p, "n1", b.Id, GemKind.Power).Ok);   // necklace is full after one
        Assert.False(CardOps.Socket(p, "g1", b.Id, GemKind.Power).Ok);   // gloves have no socket
        Assert.True(CardOps.Socket(p, "a1", b.Id, GemKind.Power).Ok);
        Assert.True(CardOps.Socket(p, "a1", c.Id, GemKind.Power).Ok);
        p.Cards.AddGem(a.Id, GemKind.Power, false);
        Assert.False(CardOps.Socket(p, "a1", a.Id, GemKind.Power).Ok);   // armor is full after two
        Assert.Equal(1, p.Cards.GemCount(a.Id, GemKind.Power, false));   // the refused gem stayed
    }

    [Fact]
    public void SocketRefusals()
    {
        PlayerState p = Hero();
        string id = IdOf(CardRarity.N);
        Gear(p, Slots.Weapon, "w1");
        Assert.False(CardOps.Socket(p, "nope", id, GemKind.Power).Ok);   // no such item
        Assert.False(CardOps.Socket(p, "w1", id, GemKind.Power).Ok);     // gem not owned
        Assert.False(CardOps.Socket(p, "w1", "ghost", GemKind.Power).Ok);
        p.Cards.AddGem(id, GemKind.Power, false, 2);
        Assert.True(CardOps.Socket(p, "w1", id, GemKind.Power).Ok);
        Assert.False(CardOps.Socket(p, "w1", id, GemKind.Power).Ok);     // the same gem twice on one item
        Assert.Equal(1, p.Cards.GemCount(id, GemKind.Power, false));
    }

    [Fact]
    public void SocketUsesALockedGemBeforeAFreeOne()
    {
        PlayerState p = Hero();
        string id = IdOf(CardRarity.N);
        p.Cards.AddGem(id, GemKind.Power, true); p.Cards.AddGem(id, GemKind.Power, false);
        Gear(p, Slots.Weapon, "w1");
        Assert.True(CardOps.Socket(p, "w1", id, GemKind.Power).Ok);
        Assert.Equal(0, p.Cards.GemCount(id, GemKind.Power, true));
        Assert.Equal(1, p.Cards.GemCount(id, GemKind.Power, false));
    }

    // ---- remove ----

    [Fact]
    public void RemoveCostsTheRarityFeeAndReturnsTheGemLocked()
    {
        PlayerState p = Hero(1000);
        string id = IdOf(CardRarity.N);
        p.Cards.AddGem(id, GemKind.Guard, false);
        Gear(p, Slots.Armor, "a1");
        Assert.True(CardOps.Socket(p, "a1", id, GemKind.Guard).Ok);
        ActionResult r = CardOps.Remove(p, "a1", 0);
        Assert.True(r.Ok, r.Text);
        Assert.Equal(800, p.Gold);                                        // N: 200
        Assert.Equal(1, p.Cards.GemCount(id, GemKind.Guard, true));       // trade-locked for good
        Assert.Equal(0, p.Cards.GemCount(id, GemKind.Guard, false));
        Assert.Empty(p.FindItem("a1").sockets);
    }

    [Fact]
    public void RemoveRefusalsKeepTheGemInTheItem()
    {
        PlayerState p = Hero(100);
        string id = IdOf(CardRarity.N);
        p.Cards.AddGem(id, GemKind.Power, false);
        Gear(p, Slots.Weapon, "w1");
        CardOps.Socket(p, "w1", id, GemKind.Power);
        Assert.False(CardOps.Remove(p, "w1", 0).Ok);                      // 100 G < 200
        Assert.False(CardOps.Remove(p, "w1", 1).Ok);                      // empty socket
        Assert.False(CardOps.Remove(p, "w1", -1).Ok);
        Assert.False(CardOps.Remove(p, "nope", 0).Ok);
        Assert.Single(p.FindItem("w1").sockets);
        p.Gold = 5000;
        p.Cards.AddGem(id, GemKind.Power, true, 999);                     // the locked stack is full: no room to keep the gem
        Assert.False(CardOps.Remove(p, "w1", 0).Ok);
        Assert.Single(p.FindItem("w1").sockets); Assert.Equal(5000, p.Gold);
    }

    [Fact]
    public void RemoveFeeOfARarerGemIsHigher()
    {
        PlayerState p = Hero(20000);
        string id = IdOf(CardRarity.SSR, true);
        p.Cards.AddGem(id, GemKind.Power, false);
        Gear(p, Slots.Weapon, "w1");
        CardOps.Socket(p, "w1", id, GemKind.Power);
        Assert.True(CardOps.Remove(p, "w1", 0).Ok);
        Assert.Equal(10000, p.Gold);
    }

    // ---- the lock: no trade, no discard ----

    [Fact]
    public void AnItemWithAGemCannotBeDiscardedUntilTheGemIsRemoved()
    {
        PlayerState p = Hero();
        string id = IdOf(CardRarity.N);
        p.Cards.AddGem(id, GemKind.Power, false);
        Gear(p, Slots.Weapon, "w1");
        CardOps.Socket(p, "w1", id, GemKind.Power);
        Assert.False(p.Discard("w1").Ok);
        Assert.NotNull(p.FindItem("w1"));
        Assert.True(CardOps.Remove(p, "w1", 0).Ok);
        Assert.True(p.Discard("w1").Ok);
        Assert.Null(p.FindItem("w1"));
        Assert.False(p.Discard("w1").Ok);
    }

    // ---- sockets survive the save, per slot ----

    [Fact]
    public void SocketedGemsSurviveSaveAndLoad()
    {
        PlayerState p = Hero();
        string id = IdOf(CardRarity.R);
        p.Cards.AddGem(id, GemKind.Power, false);
        Gear(p, Slots.Armor, "a1");
        CardOps.Socket(p, "a1", id, GemKind.Power);
        SaveData d = SaveSerializer.Parse(SaveSerializer.ToJson(SaveSerializer.Capture(p, false, 0f, 0f, false, 1)));
        PlayerState q = SaveSerializer.Restore(d);
        Assert.Equal(new[] { id + ":p" }, q.FindItem("a1").sockets.ToArray());
    }

    [Fact]
    public void LoadCutsSocketsToWhatTheSlotAllows()
    {
        List<string> two = new List<string> { "meadowSlime:p", "frostBear:g" };
        Assert.Equal(2, ItemFactory.SanitizeSockets(two, Slots.Weapon).Count);
        Assert.Single(ItemFactory.SanitizeSockets(two, Slots.Helmet));
        foreach (string s in new[] { Slots.Gloves, Slots.Boots, Slots.Offhand, Slots.Headgear, Slots.Pet }) Assert.Empty(ItemFactory.SanitizeSockets(two, s));
    }

    [Fact]
    public void GemCodesRoundTripForTheNetwork()
    {
        foreach (CardDef c in CardTable.All)
            foreach (GemKind k in new[] { GemKind.Power, GemKind.Guard })
            {
                string id; GemKind kind;
                Assert.True(GemSockets.TryDecode(GemSockets.Code(c.Id, k), out id, out kind));
                Assert.Equal(c.Id, id); Assert.Equal(k, kind);
            }
        string x; GemKind y;
        Assert.False(GemSockets.TryDecode(200, out x, out y));
    }
}

/// <summary>Flips the static rollback switch, so it runs alone (no other test class in parallel).</summary>
[CollectionDefinition("CardOps switch", DisableParallelization = true)]
public class CardOpsSwitchCollection { }

[Collection("CardOps switch")]
public class GemSwitchTests
{
    [Fact]
    public void ConvertSocketAndRemoveCanBeSwitchedOff()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Gold = 100000;
        string id = CardTable.All[0].Id;
        p.Cards.Add(id, 1);
        CardOps.GemsEnabled = false;
        try
        {
            Assert.False(CardOps.Convert(p, id, GemKind.Power).Ok);
            Assert.False(CardOps.Socket(p, p.GetEquipped(Slots.Weapon).uid, id, GemKind.Power).Ok);
            Assert.False(CardOps.Remove(p, p.GetEquipped(Slots.Weapon).uid, 0).Ok);
        }
        finally { CardOps.GemsEnabled = true; }
        Assert.Equal(1, p.Cards.Count(id));
        Assert.Equal(100000, p.Gold);
    }

    [Fact]
    public void RemoveByGemTakesThatGemOnceAndARepeatFindsNothing()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Gold = 100000;
        Item w = p.GetEquipped(Slots.Weapon);
        CardDef a = CardTable.Get("meadowSlime"), b = CardTable.Get("hornRabbit");
        p.Cards.AddGem(a.Id, GemKind.Power, false); p.Cards.AddGem(b.Id, GemKind.Power, false);
        Assert.True(CardOps.Socket(p, w.uid, a.Id, GemKind.Power).Ok);
        Assert.True(CardOps.Socket(p, w.uid, b.Id, GemKind.Power).Ok);
        int gold = p.Gold;
        Assert.True(CardOps.RemoveGem(p, w.uid, a.Id, GemKind.Power).Ok);
        int afterFirst = p.Gold;
        Assert.Equal(gold - CardFees.Remove(a), afterFirst);
        Assert.False(CardOps.RemoveGem(p, w.uid, a.Id, GemKind.Power).Ok);   // the repeat finds no gem ...
        Assert.Equal(afterFirst, p.Gold);                                    // ... and costs nothing
        Assert.Equal(new[] { "hornRabbit:p" }, w.sockets.ToArray());         // the neighbour stays
        Assert.False(CardOps.RemoveGem(p, w.uid, a.Id, GemKind.Guard).Ok);   // another kind is another gem
    }
}
