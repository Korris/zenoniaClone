using System.Collections.Generic;
using UnityEngine;
using Xunit;
using ZenoniaProto.Rpg;

// The whole save travels as ONE string in RpgState (NetProtocol.MaxSaveJsonLength = 60000): LiteNetLib cuts a longer one silently. Build the worst character the rules allow and keep it well under.
public class CardSaveSizeTests
{
    const int MaxSaveJsonLength = 60000; // NetProtocol.MaxSaveJsonLength (not compiled into this project)
    const int Budget = 45000;            // headroom for quests, dungeon chests and later fields

    [Fact]
    public void TheWorstCardCharacterStaysUnderTheBudget()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Inventory.Clear();
        string longest = CardTable.All[0].Id;
        foreach (CardDef c in CardTable.All) if (c.Id.Length > longest.Length) longest = c.Id;
        var twoLong = new List<string> { longest + ":p", longest + ":g" }; // the longest ids, 2 per item, on every item (the rules allow fewer: this over-counts)
        for (int i = 0; i < PlayerState.InventorySize; i++)
        {
            Item it = ItemFactory.Generate(() => 0.37, 10 + i % 80, Slots.Generated[i % Slots.Generated.Length], "epic", "warrior");
            it.uid = it.templateId + "-zzzzzz" + i; it.enhance = 10; it.sockets = new List<string>(twoLong);
            p.AddToInventory(it);
        }
        for (int s = 0; s < Slots.Generated.Length; s++)
        {
            Item it = ItemFactory.Generate(() => 0.37, 50, Slots.Generated[s], "epic", "warrior");
            it.sockets = new List<string>(twoLong);
            p.SetEquippedRaw(Slots.Generated[s], it);
        }
        foreach (CardDef c in CardTable.All) { p.Cards.Add(c.Id, 999); p.Cards.SetLocked(c.Id, 999); }
        int rows = 0;
        foreach (CardDef c in CardTable.All)
            foreach (GemKind k in new[] { GemKind.Power, GemKind.Guard })
                foreach (bool locked in new[] { false, true })
                    if (rows < CardCollection.MaxGemRows && p.Cards.AddGem(c.Id, k, locked, 999)) rows++;
        Assert.Equal(CardCollection.MaxGemRows, rows);
        p.Cards.AlbumClaimed = 15; p.Cards.NpcFirstWins = 0xFFFF;
        p.Cards.Daily = new CardDailyData { day = 99999, npcWins = 99999, pvpRewards = 99999, wagers = 99999 };
        p.Cards.Rank = new CardRankData { season = 99999, mmr = 99999, lp = 99999, wins = 99999, losses = 99999, draws = 99999, best = 99999 };
        string json = JsonUtility.ToJson(SaveSerializer.Capture(p, true, 1234.5f, 678.25f, true, 0, "frostHollow", new List<string> { "frostHollow", "amberDunes", "willowbrookMeadows" }));
        System.Console.WriteLine("[CardSaveSizeTests] worst card save JSON: " + json.Length + " chars");
        Assert.True(json.Length < Budget, "save JSON " + json.Length + " chars, budget " + Budget + ", hard cap " + MaxSaveJsonLength);
    }
}
