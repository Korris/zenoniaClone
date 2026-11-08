using System.Collections.Generic;
using UnityEngine;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>The 50-slot bag (was 20): what fits, what is refused, and that saves keep every slot.</summary>
public class BagSizeTests
{
    const int MaxSaveJsonLength = 60000; // NetProtocol.MaxSaveJsonLength: RpgState carries the whole save as one string (not compiled into this project)

    static int seed;
    static Item Gear(int i, string rarity = "rare")
    {
        Item it = ItemFactory.Generate(() => 0.37, 10 + i % 80, Slots.Generated[i % Slots.Generated.Length], rarity, "warrior");
        it.uid = it.templateId + "-bag" + (seed++);
        return it;
    }

    static PlayerState FullBag()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Inventory.Clear();
        for (int i = 0; i < PlayerState.InventorySize; i++) Assert.True(p.AddToInventory(Gear(i)));
        return p;
    }

    [Fact]
    public void TheBagHasFiftySlots() { Assert.Equal(50, PlayerState.InventorySize); }

    [Fact]
    public void FiftyItemsFitAndTheFiftyFirstIsRefused()
    {
        PlayerState p = FullBag();
        Assert.Equal(50, p.Inventory.Count);
        Assert.False(p.AddToInventory(Gear(99)));
        Assert.Equal(50, p.Inventory.Count);
        Assert.False(p.Unequip(Slots.Weapon).Ok); // nowhere to put the worn weapon
    }

    [Fact]
    public void AFullBagSurvivesASaveRoundTrip()
    {
        PlayerState p = FullBag();
        SaveData back = SaveSerializer.Parse(JsonUtility.ToJson(SaveSerializer.Capture(p, false, 0f, 0f, false)));
        Assert.NotNull(back);
        Assert.Equal(50, back.inventory.Count);
        PlayerState q = SaveSerializer.Restore(back);
        Assert.Equal(50, q.Inventory.Count);
        for (int i = 0; i < 50; i++) Assert.Equal(p.Inventory[i].uid, q.Inventory[i].uid);
    }

    [Fact]
    public void AnOldTwentySlotSaveLoadsWhole()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Inventory.Clear();
        for (int i = 0; i < 20; i++) p.AddToInventory(Gear(i));
        SaveData d = SaveSerializer.Capture(p, false, 0f, 0f, false);
        d.version = 2; // a save written before the bag grew
        SaveData back = SaveSerializer.Parse(JsonUtility.ToJson(d));
        Assert.Equal(20, back.inventory.Count);
        Assert.Equal(20, SaveSerializer.Restore(back).Inventory.Count);
    }

    [Fact]
    public void ATamperedSaveIsCutAtFifty()
    {
        PlayerState p = FullBag();
        SaveData d = SaveSerializer.Capture(p, false, 0f, 0f, false);
        for (int i = 0; i < 10; i++) d.inventory.Add(Gear(100 + i));
        Assert.Equal(50, SaveSerializer.Parse(JsonUtility.ToJson(d)).inventory.Count);
    }

    [Fact]
    public void AFullBagOfTheLongestItemsStillFitsTheRpgStateMessage()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Inventory.Clear();
        for (int i = 0; i < PlayerState.InventorySize; i++) { Item it = Gear(i, "epic"); it.enhance = 10; it.uid = it.templateId + "-zzzzzz" + i; p.AddToInventory(it); }
        for (int s = 0; s < Slots.Generated.Length; s++) p.SetEquippedRaw(Slots.Generated[s], Gear(s, "epic"));
        string json = JsonUtility.ToJson(SaveSerializer.Capture(p, true, 1234.5f, 678.25f, true, 0, "frostHollow", new List<string> { "frostHollow", "amberDunes", "willowbrookMeadows" }));
        System.Console.WriteLine("[BagSizeTests] full-bag save JSON: " + json.Length + " chars");
        Assert.True(json.Length < MaxSaveJsonLength / 2, "save JSON " + json.Length + " chars"); // half the cap: room for quests and future fields
    }
}
