using Xunit;
using ZenoniaProto.Rpg;

// Frost Hollow 72x48: saves written on the 24-row map hold y values measured from the old bottom edge. Parse moves them up by the rows the client's map has gained.
public class FrostHollowSaveMigrationTests
{
    static string Save(int version, string mapId, bool hasPosition, float y)
    {
        SaveData d = SaveSerializer.Capture(PlayerState.NewGame("warrior"), hasPosition, 100f, y, false, 1, mapId);
        d.version = version;
        return SaveSerializer.ToJson(d);
    }

    [Fact]
    public void OldFrostHollowSaveMovesUpByTheRowsGained()
    {
        Assert.Equal(34f + 24 * 16f, SaveSerializer.Parse(Save(2, "frostHollow", true, 34f), 48).y);
        Assert.Equal(34f + 12 * 16f, SaveSerializer.Parse(Save(2, "frostHollow", true, 34f), 36).y);
    }

    [Fact]
    public void A24RowMapNeverShifts()
    {
        Assert.Equal(34f, SaveSerializer.Parse(Save(2, "frostHollow", true, 34f), SaveSerializer.LegacyFrostHollowRows).y);
        Assert.Equal(34f, SaveSerializer.Parse(Save(2, "frostHollow", true, 34f)).y); // a caller that passes nothing never shifts
    }

    [Fact]
    public void OtherMapsAndPositionlessSavesStay()
    {
        Assert.Equal(34f, SaveSerializer.Parse(Save(2, "willowbrookMeadows", true, 34f), 48).y);
        Assert.Equal(34f, SaveSerializer.Parse(Save(2, "oakhavenVillage", true, 34f), 48).y);
        Assert.Equal(34f, SaveSerializer.Parse(Save(2, "frostHollow", false, 34f), 48).y);
    }

    [Fact]
    public void ShiftStopsOnceTheSaveVersionIsAtLeastThree()
    {
        Assert.Equal(34f, SaveSerializer.Parse(Save(3, "frostHollow", true, 34f), 48).y);
        SaveData once = SaveSerializer.Parse(Save(2, "frostHollow", true, 34f), 48);
        Assert.Equal(once.y, SaveSerializer.Parse(SaveSerializer.ToJson(once), 48).y); // re-parse does not shift twice
    }
}
