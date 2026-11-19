using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using ZenoniaProto.Rpg;

// Character creation picks the look (docs/character-appearance-layered-spec.md, step E): CharacterAppearanceData.ForNewCharacter / RandomNew,
// the look the server stores at creation reaching the zone's PlayerState, and the offline new save keeping it.
public class CharacterCreateAppearanceTests
{
    [Fact]
    public void ForNewCharacterStoresBothColoursBareHeaded()
    {
        CharacterAppearanceData a = CharacterAppearanceData.ForNewCharacter("cleric", 0, 7, 4);
        Assert.NotNull(a);
        Assert.True(a.stored);
        Assert.Equal(0, a.hairStyle);
        Assert.Equal(7, a.hairColorIndex);
        Assert.Equal(4, a.outfitColorIndex);
        Assert.Equal(0, a.equippedHeadgearId);
        Assert.Equal("cleric", a.classId);
    }

    [Theory]
    [InlineData(0, 8, 0)]    // past the 8 hair colours
    [InlineData(0, -1, 0)]   // creation always picks a hair colour
    [InlineData(0, 0, 6)]    // past the 6 outfit colours
    [InlineData(0, 0, -1)]
    [InlineData(-1, 0, 0)]   // negative style
    [InlineData(256, 0, 0)]  // style above one byte (InvOp / NetAppearance)
    public void ForNewCharacterRefusesOutOfRange(int style, int hair, int outfit)
    {
        Assert.Null(CharacterAppearanceData.ForNewCharacter("warrior", style, hair, outfit));
    }

    [Fact]
    public void RandomStaysInRangeAndReachesEveryColour()
    {
        var rng = new Random(1234);
        var hairSeen = new bool[CharacterAppearanceData.HairColorCount];
        var outfitSeen = new bool[CharacterAppearanceData.OutfitColorCount];
        for (int i = 0; i < 400; i++)
        {
            CharacterAppearanceData a = CharacterAppearanceData.RandomNew("assassin", 3, rng);
            Assert.True(a.stored);
            Assert.InRange(a.hairStyle, 0, 2);
            Assert.Equal(0, a.equippedHeadgearId);
            hairSeen[a.hairColorIndex] = true;
            outfitSeen[a.outfitColorIndex] = true;
        }
        Assert.All(hairSeen, Assert.True);
        Assert.All(outfitSeen, Assert.True);
    }

    [Fact]
    public void RandomWithOneOrNoStyleKeepsStyleZero()
    {
        var rng = new Random(7);
        for (int i = 0; i < 50; i++)
        {
            Assert.Equal(0, CharacterAppearanceData.RandomNew("wizard", 1, rng).hairStyle);
            Assert.Equal(0, CharacterAppearanceData.RandomNew("wizard", 0, rng).hairStyle);
        }
    }

    /// <summary>server-api POST /characters stores state.appearance like this; the zone parses that state with SaveSerializer on redeem.</summary>
    [Fact]
    public void LookStoredByTheServerAtCreationReachesThePlayerState()
    {
        string json = SaveSerializer.ToJson(SaveSerializer.Capture(PlayerState.NewGame("mechanic"), false, 0f, 0f, false, 1));
        json = Regex.Replace(json, "\"appearance\"\\s*:\\s*\\{[^}]*\\}", "\"appearance\":{\"stored\":true,\"hairStyle\":0,\"hairColorIndex\":3,\"outfitColorIndex\":5,\"equippedHeadgearId\":0}");
        PlayerState p = SaveSerializer.Restore(SaveSerializer.Parse(json));
        Assert.True(p.Appearance.SameLook(CharacterAppearanceData.ForNewCharacter("mechanic", 0, 3, 5)));
    }

    /// <summary>Offline: GameSession.Spawn puts the picked look on the new PlayerState (Sanitize) and writes the save at once.</summary>
    [Fact]
    public void OfflineNewHeroSaveKeepsThePickedLook()
    {
        CharacterAppearanceData picked = CharacterAppearanceData.ForNewCharacter("warrior", 0, 6, 5);
        PlayerState p = PlayerState.NewGame("warrior");
        p.Appearance = CharacterAppearanceData.Sanitize(picked, p.ClassId);
        string json = SaveSerializer.ToJson(SaveSerializer.Capture(p, false, 0f, 0f, false, 1));
        PlayerState back = SaveSerializer.Restore(SaveSerializer.Parse(json));
        Assert.True(back.Appearance.SameLook(picked));
        Assert.Equal(6, back.Appearance.Resolved("n:anyone").hairColorIndex); // a chosen colour never falls back to the name hash
    }

    [Fact]
    public void PaletteFileNamesEveryColour()
    {
        string text = File.ReadAllText(Path.Combine("..", "..", "..", "..", "..", "..", "Assets", "Resources", "Data", "hero-layered", "palettes.json"));
        Assert.Equal(CharacterAppearanceData.HairColorCount, Regex.Matches(Names(text, "hairNames"), "\"[^\"]+\"").Count);
        Assert.Equal(CharacterAppearanceData.OutfitColorCount, Regex.Matches(Names(text, "outfitNames"), "\"[^\"]+\"").Count);
    }

    static string Names(string json, string key)
    {
        int at = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
        Assert.True(at >= 0, key);
        int open = json.IndexOf('[', at), end = json.IndexOf(']', open);
        return json.Substring(open, end - open);
    }
}
