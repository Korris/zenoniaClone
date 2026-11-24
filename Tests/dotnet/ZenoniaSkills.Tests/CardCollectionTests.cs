using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

// Card collection, Album, milestone claims and the way they ride in the save (v6).
public class CardCollectionTests
{
    static PlayerState Hero() { return PlayerState.NewGame("warrior"); }

    /// <summary>Save -> JSON -> parse -> restore, the way a character is stored and loaded.</summary>
    static PlayerState RoundTrip(PlayerState p)
    {
        SaveData d = SaveSerializer.Parse(SaveSerializer.ToJson(SaveSerializer.Capture(p, false, 0f, 0f, false, 1)));
        Assert.NotNull(d);
        return SaveSerializer.Restore(d);
    }

    static void FillAlbum(PlayerState p, int n) { for (int i = 0; i < n; i++) p.AddCard(CardTable.All[CardTable.LiveStart + i].Id); } // the Taurus cards, in table order

    [Fact]
    public void AddCountsCopiesAndMarksTheAlbumOnce()
    {
        var p = Hero();
        Assert.True(p.AddCard("tauCloverSlime"));   // first time in the Album
        Assert.False(p.AddCard("tauCloverSlime"));
        Assert.Equal(2, p.Cards.Count("tauCloverSlime"));
        Assert.Equal(1, p.Cards.AlbumCount);
        Assert.True(p.AddCard("meadowSlime"));      // a pre-Taurus card is owned but hidden from the Album: it does not count
        Assert.Equal(1, p.Cards.AlbumCount);
        Assert.False(p.AddCard("notACard"));
        Assert.Equal(0, p.Cards.Count("notACard"));
    }

    [Fact]
    public void StackStopsAt999()
    {
        var p = Hero();
        p.Cards.Add("meadowSlime", 998); p.Cards.Add("meadowSlime", 5);
        Assert.Equal(CardTable.MaxStack, p.Cards.Count("meadowSlime"));
    }

    [Fact]
    public void RemovingCardsKeepsTheAlbum()
    {
        var p = Hero();
        p.Cards.Add("thornBoar", 2);
        Assert.False(p.Cards.Remove("thornBoar", 3));   // not enough: nothing changes
        Assert.Equal(2, p.Cards.Count("thornBoar"));
        Assert.True(p.Cards.Remove("thornBoar", 2));
        Assert.Equal(0, p.Cards.Count("thornBoar"));
        Assert.True(p.Cards.InAlbum("thornBoar"));
        Assert.True(RoundTrip(p).Cards.InAlbum("thornBoar"));
    }

    [Fact]
    public void GemStacksSplitByKindAndLockAndStopAtTheRowLimit()
    {
        var c = Hero().Cards;
        Assert.True(c.AddGem("snowFox", GemKind.Power, false, 2));
        Assert.True(c.AddGem("snowFox", GemKind.Guard, false));
        Assert.True(c.AddGem("snowFox", GemKind.Power, true));
        Assert.Equal(2, c.GemCount("snowFox", GemKind.Power, false));
        Assert.Equal(3, c.Gems.Count);
        Assert.True(c.RemoveGem("snowFox", GemKind.Power, false, 2));
        Assert.False(c.RemoveGem("snowFox", GemKind.Power, false));
        Assert.False(c.AddGem("nope", GemKind.Power, false));
        var big = Hero().Cards;
        for (int i = 0; i < CardTable.Count; i++) foreach (GemKind k in new[] { GemKind.Power, GemKind.Guard }) { big.AddGem(CardTable.All[i].Id, k, false); big.AddGem(CardTable.All[i].Id, k, true); }
        Assert.Equal(CardCollection.MaxGemRows, big.Gems.Count);       // every card x kind x locked
        Assert.True(big.AddGem("meadowSlime", GemKind.Power, false)); // an existing stack still grows
    }

    [Fact]
    public void EveryCardFieldSurvivesSaveAndLoad()
    {
        var p = Hero();
        p.Cards.Add("tauCloverSlime", 3); p.Cards.Add("tauElderRootGuardian"); p.Cards.Add("tauRootling", 2);
        p.Cards.Remove("tauRootling", 2);                       // gone from the bag, still in the Album
        p.Cards.AddGem("snowFox", GemKind.Guard, true, 2);
        p.Cards.Deck.AddRange(new[] { "tauCloverSlime", "tauCloverSlime", "tauElderRootGuardian" });
        p.Cards.Daily = new CardDailyData { day = 20000, npcWins = 2, pvpRewards = 3, wagers = 1 };
        p.Cards.NpcFirstWins = 5;
        p.Cards.Rank = new CardRankData { season = 2, mmr = 1500, lp = 40, wins = 7, losses = 3, draws = 1, best = 1610, prevBest = 820, rewards = 6 };

        PlayerState q = RoundTrip(p);
        Assert.Equal(3, q.Cards.Count("tauCloverSlime"));
        Assert.Equal(1, q.Cards.Count("tauElderRootGuardian"));
        Assert.Equal(0, q.Cards.Count("tauRootling"));
        Assert.True(q.Cards.InAlbum("tauRootling"));
        Assert.Equal(3, q.Cards.AlbumCount);
        Assert.Equal(2, q.Cards.GemCount("snowFox", GemKind.Guard, true));
        Assert.Equal(new[] { "tauCloverSlime", "tauCloverSlime", "tauElderRootGuardian" }, q.Cards.Deck.ToArray());
        Assert.Equal(20000, q.Cards.Daily.day); Assert.Equal(2, q.Cards.Daily.npcWins); Assert.Equal(3, q.Cards.Daily.pvpRewards); Assert.Equal(1, q.Cards.Daily.wagers);
        Assert.Equal(5, q.Cards.NpcFirstWins);
        Assert.Equal(2, q.Cards.Rank.season); Assert.Equal(1500, q.Cards.Rank.mmr); Assert.Equal(40, q.Cards.Rank.lp);
        Assert.Equal(7, q.Cards.Rank.wins); Assert.Equal(3, q.Cards.Rank.losses); Assert.Equal(1, q.Cards.Rank.draws); Assert.Equal(1610, q.Cards.Rank.best); Assert.Equal(820, q.Cards.Rank.prevBest); Assert.Equal(6, q.Cards.Rank.rewards);
    }

    [Fact]
    public void TheFirstTimeHintFlagIsMarkedOnceSavedAndLoaded()
    {
        var p = Hero();
        Assert.Equal(0, p.Cards.Help);
        Assert.False(CardOps.MarkHelp(p, 8).Ok);                    // not a known hint bit (bit 1 = PvP risk is known since the Orin tutorial)
        Assert.False(CardOps.MarkHelp(p, 0).Ok);
        Assert.Equal(0, p.Cards.Help);
        Assert.True(CardOps.MarkHelp(p, CardCollection.HelpTutorial).Ok);
        Assert.True(CardOps.MarkHelp(p, CardCollection.HelpTutorial).Ok); // repeating is harmless
        Assert.Equal(CardCollection.HelpTutorial, RoundTrip(p).Cards.Help);
        SaveData d = SaveSerializer.Capture(Hero(), false, 0f, 0f, false, 1);
        d.cardHelp = 0x7F;                                             // a forged value keeps only the known bits
        Assert.Equal(CardCollection.HelpMask, SaveSerializer.Restore(d).Cards.Help);
    }

    [Fact]
    public void SocketsSurviveSaveAndLoadAndJunkIsDropped()
    {
        var p = Hero();
        Item w = ItemFactory.CreateStarterItem(Slots.Weapon, "warrior");
        w.uid = "w1"; w.sockets = new List<string> { "meadowSlime:p", "notACard:g", "snowFox:x", "frostBear:g", "thornBoar:p" };
        p.Inventory.Add(w);
        Item q = RoundTrip(p).Inventory.Single(i => i.uid == "w1");
        Assert.Equal(new[] { "meadowSlime:p", "frostBear:g" }, q.sockets.ToArray()); // two valid gems, junk skipped, at most two
    }

    [Fact]
    public void AnOldSaveWithoutCardFieldsLoadsEmpty()
    {
        SaveData d = SaveSerializer.Capture(Hero(), false, 0f, 0f, false, 1);
        d.version = 5;
        string json = SaveSerializer.ToJson(d);
        // strip every card field the way a v5 file lacks them
        foreach (string key in new[] { "cards", "gems", "album", "albumClaimed", "cardDeck", "cardDaily", "cardNpcFirstWins", "cardRank" })
            Assert.Contains("\"" + key + "\"", json);
        var node = System.Text.Json.Nodes.JsonNode.Parse(json).AsObject();
        foreach (string key in new[] { "cards", "gems", "album", "albumClaimed", "cardDeck", "cardDaily", "cardNpcFirstWins", "cardRank" }) node.Remove(key);
        SaveData old = SaveSerializer.Parse(node.ToJsonString());
        Assert.NotNull(old);
        PlayerState p = SaveSerializer.Restore(old);
        Assert.Equal(0, p.Cards.OwnedKinds); Assert.Equal(0, p.Cards.AlbumCount); Assert.Empty(p.Cards.Gems); Assert.Equal(0, p.Cards.AlbumClaimed);
        Assert.Equal(SaveSerializer.SaveVersion, old.version);
    }

    [Fact]
    public void LoadDropsUnknownAndForgedData()
    {
        SaveData d = SaveSerializer.Capture(Hero(), false, 0f, 0f, false, 1);
        d.cards = new List<CardEntry> { new CardEntry { id = "ghost", n = 5 }, new CardEntry { id = "tauCloverSlime", n = 5000 }, new CardEntry { id = "thornBoar", n = -2 } };
        d.gems = new List<GemEntry> { new GemEntry { id = "ghost", kind = 0, n = 1 }, new GemEntry { id = "snowFox", kind = 7, n = 1 } };
        d.album = new List<int> { -1, 99, 43 };
        d.albumClaimed = 0xFF;                       // claims with no cards behind them
        d.cardDeck = new List<string> { "tauCloverSlime", "tauElderRootGuardian", "tauCloverSlime" };
        d.cardDaily = null; d.cardRank = null;
        PlayerState p = SaveSerializer.Restore(SaveSerializer.Parse(SaveSerializer.ToJson(d)));
        Assert.Equal(CardTable.MaxStack, p.Cards.Count("tauCloverSlime"));
        Assert.Equal(0, p.Cards.Count("thornBoar"));
        Assert.Empty(p.Cards.Gems);
        Assert.Equal(2, p.Cards.AlbumCount);          // index 43 = tauMossMushroom (from the list) + meadowSlime (owned)
        Assert.Equal(0, p.Cards.AlbumClaimed);
        Assert.Equal(new[] { "tauCloverSlime", "tauCloverSlime" }.Length, p.Cards.Deck.Count);  // frostfangKing is not owned, so out of the deck
        Assert.DoesNotContain("tauElderRootGuardian", p.Cards.Deck);
        Assert.Equal(0, p.Cards.Daily.day); Assert.Equal(0, p.Cards.Rank.mmr);
    }

    [Fact]
    public void AlbumMilestonesPayOnceAndOnlyWhenReached()
    {
        var p = Hero();
        int gold = p.Gold;
        Assert.False(AlbumRules.Claim(p, 0).Ok);       // 0 cards
        FillAlbum(p, 2);
        Assert.False(AlbumRules.Claim(p, 0).Ok);
        FillAlbum(p, 3);
        ActionResult r = AlbumRules.Claim(p, 0);
        Assert.True(r.Ok);
        Assert.Equal(gold + AlbumRules.Gold[0], p.Gold);
        Assert.False(AlbumRules.Claim(p, 0).Ok);       // once only
        Assert.Equal(gold + AlbumRules.Gold[0], p.Gold);
        Assert.False(AlbumRules.Claim(p, 1).Ok);       // 6 needs 6
        Assert.False(AlbumRules.Claim(p, 4).Ok);
        Assert.False(AlbumRules.Claim(p, -1).Ok);
        FillAlbum(p, CardTable.LiveCount);
        for (int m = 1; m < 4; m++) { Assert.True(AlbumRules.Claim(p, m).Ok); Assert.True(AlbumRules.Claimed(p.Cards, m)); }
        Assert.Equal(gold + AlbumRules.Gold[0] + AlbumRules.Gold[1] + AlbumRules.Gold[2] + AlbumRules.Gold[3], p.Gold);
        Assert.Equal(0xF, RoundTrip(p).Cards.AlbumClaimed);
    }

    [Fact]
    public void SellingEveryCopyNeverTakesTheAlbumOrAClaimAway()
    {
        var p = Hero();
        FillAlbum(p, 3);
        Assert.True(AlbumRules.Claim(p, 0).Ok);
        foreach (CardDef c in CardTable.All.Skip(CardTable.LiveStart).Take(3)) p.Cards.Remove(c.Id);
        PlayerState q = RoundTrip(p);
        Assert.Equal(3, q.Cards.AlbumCount);
        Assert.True(AlbumRules.Claimed(q.Cards, 0));
    }
}
