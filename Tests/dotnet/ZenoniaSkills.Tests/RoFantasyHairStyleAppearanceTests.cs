using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using ZenoniaProto.Rpg;

// RO Fantasy heroes (Tools/build-ro-fantasy-hero-game-assets.py): body per class, outfit fixed, 3 dyeable hairstyles picked at creation.
// The hairstyle index is part of CharacterAppearanceData: it must survive the offline save, the wire and the inventory op text.
public class RoFantasyHairStyleAppearanceTests
{
    static readonly string[] Classes = { "warrior", "wizard", "mechanic", "assassin", "cleric" };

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void PickedHairStyleSurvivesTheSave(int style)
    {
        CharacterAppearanceData picked = CharacterAppearanceData.ForNewCharacter("cleric", style, 3, 0);
        PlayerState p = PlayerState.NewGame("cleric");
        p.Appearance = CharacterAppearanceData.Sanitize(picked, p.ClassId);
        PlayerState back = SaveSerializer.Restore(SaveSerializer.Parse(SaveSerializer.ToJson(SaveSerializer.Capture(p, false, 0f, 0f, false, 1))));
        Assert.Equal(style, back.Appearance.hairStyle);
        Assert.True(back.Appearance.SameLook(picked));
        Assert.Equal(style, back.Appearance.Resolved("n:anyone").hairStyle);
    }

    [Fact]
    public void PickedHairStyleSurvivesTheWireAndTheOpText()
    {
        CharacterAppearanceData picked = CharacterAppearanceData.ForNewCharacter("assassin", 2, 7, 1);
        CharacterAppearanceData wire = CharacterAppearanceData.FromWire("assassin", (byte)picked.hairStyle, (byte)picked.hairColorIndex, (byte)picked.outfitColorIndex, 0);
        Assert.Equal(2, wire.hairStyle);
        Assert.Equal("2,7,1", picked.ToOpText());
    }

    [Fact]
    public void RandomLookUsesAllThreeStyles()
    {
        var rng = new System.Random(7);
        var seen = new bool[3];
        for (int i = 0; i < 200; i++)
        {
            CharacterAppearanceData a = CharacterAppearanceData.RandomNew("warrior", 3, rng);
            Assert.InRange(a.hairStyle, 0, 2);
            seen[a.hairStyle] = true;
        }
        Assert.All(seen, Assert.True);
    }

    /// <summary>Hairstyles each class set offers: 3, warrior 4 (24/11: 2 Starward styles + the Knight V3 hair + the Golden Paladin hair).</summary>
    static int StylesOf(string cls) => cls == "warrior" ? 4 : 3;

    /// <summary>The class sets the game loads: hairstyles 0..n-1 each (3, warrior 4), fixed outfit, a body of its own (not the common gender body).</summary>
    [Fact]
    public void EveryClassHasAnRoFantasySetWithItsHairStyles()
    {
        foreach (string cls in Classes)
        {
            string text = File.ReadAllText(Path.Combine("..", "..", "..", "..", "..", "..", "Assets", "Resources", "Data", "hero-layered", "ro-fantasy", cls, "class.json"));
            Assert.Matches("\"fixedOutfit\":\\s*true", text);
            Assert.Contains("\"body\": \"Data/hero-layered/ro-fantasy/" + cls + "/body\"", text);
            int n = StylesOf(cls);
            for (int s = 0; s < n; s++) Assert.Matches("\"style\":\\s*" + s + ",", text);
            Assert.Equal(n, Regex.Matches(text, "\"style\":").Count);
        }
    }
}
