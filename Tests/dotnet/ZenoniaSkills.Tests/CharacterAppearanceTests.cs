using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using ZenoniaProto.Rpg;

// CharacterAppearanceData (docs/character-appearance-layered-spec.md): save round trip, old saves, deterministic picks, wire + request text, legacy migration.
public class CharacterAppearanceTests
{
    static CharacterAppearanceData Chosen(int style, int hair, int outfit, int headgear = 0)
    {
        return new CharacterAppearanceData { stored = true, hairStyle = style, hairColorIndex = hair, outfitColorIndex = outfit, equippedHeadgearId = headgear };
    }

    [Fact]
    public void NewCharacterHasNothingChosen()
    {
        PlayerState p = PlayerState.NewGame("cleric");
        Assert.False(p.Appearance.stored);
        Assert.False(p.Appearance.HasHairColor);
        Assert.False(p.Appearance.HasOutfitColor);
        Assert.Equal(0, p.Appearance.equippedHeadgearId);
        Assert.Equal("cleric", p.Appearance.classId);
    }

    [Fact]
    public void SaveRoundTripKeepsTheLook()
    {
        PlayerState p = PlayerState.NewGame("wizard");
        p.Appearance = Chosen(3, 5, 2, 0);
        Assert.True(p.Equip(HeadgearCatalog.CreateItem(HeadgearCatalog.Get(7), () => 0.5)).Ok); // the headgear id follows the worn hat (HeadgearTests)
        string json = SaveSerializer.ToJson(SaveSerializer.Capture(p, false, 0f, 0f, false, 1));
        PlayerState back = SaveSerializer.Restore(SaveSerializer.Parse(json));
        Assert.True(back.Appearance.SameLook(Chosen(3, 5, 2, 7)));
        Assert.Equal("wizard", back.Appearance.classId);
    }

    [Fact]
    public void OldSaveWithoutAppearanceLoadsUnset()
    {
        string json = SaveSerializer.ToJson(SaveSerializer.Capture(PlayerState.NewGame("warrior"), false, 0f, 0f, false, 1));
        json = Regex.Replace(json, ",\\s*\"appearance\"\\s*:\\s*\\{[^}]*\\}", "");
        Assert.DoesNotContain("appearance", json);
        PlayerState back = SaveSerializer.Restore(SaveSerializer.Parse(json));
        Assert.False(back.Appearance.stored);
        Assert.Equal(-1, back.Appearance.hairColorIndex);
    }

    [Fact]
    public void NotStoredMeansUnsetEvenWithNumbers()
    {
        // Unity's JsonUtility fills a missing nested object with zeros: stored = false must still mean "nothing chosen".
        var raw = new CharacterAppearanceData { stored = false, hairStyle = 0, hairColorIndex = 0, outfitColorIndex = 0 };
        CharacterAppearanceData a = CharacterAppearanceData.Sanitize(raw, "warrior");
        Assert.False(a.HasHairColor);
        Assert.False(a.HasOutfitColor);
    }

    [Fact]
    public void SanitizeDropsOutOfRange()
    {
        CharacterAppearanceData a = CharacterAppearanceData.Sanitize(Chosen(999, 8, 6, 300), "assassin");
        Assert.Equal(0, a.hairStyle);
        Assert.Equal(-1, a.hairColorIndex);
        Assert.Equal(-1, a.outfitColorIndex);
        Assert.Equal(0, a.equippedHeadgearId);
        Assert.Equal(-1, CharacterAppearanceData.Sanitize(Chosen(0, -5, 1), "assassin").hairColorIndex);
    }

    [Fact]
    public void PicksAreDeterministicAndInRange()
    {
        for (int i = 0; i < 200; i++)
        {
            string seed = CharacterAppearanceData.SeedFor("Bot" + i, i);
            int h = CharacterAppearanceData.PickHair(seed), o = CharacterAppearanceData.PickOutfit(seed);
            Assert.InRange(h, 0, CharacterAppearanceData.HairColorCount - 1);
            Assert.InRange(o, 0, CharacterAppearanceData.OutfitColorCount - 1);
            Assert.Equal(h, CharacterAppearanceData.PickHair(seed));
            Assert.Equal(o, CharacterAppearanceData.PickOutfit(seed));
        }
        // Fixed values: the hash must never change between builds, or every player's crowd look would change after an update.
        Assert.Equal(3114816916u, CharacterAppearanceData.Hash("n:korri"));
    }

    [Fact]
    public void SeedIsTheNameIgnoringCaseElseTheNetId()
    {
        Assert.Equal(CharacterAppearanceData.SeedFor("Korri", 5), CharacterAppearanceData.SeedFor(" korri ", 99)); // map change: new net id, same name
        Assert.Equal("id:12", CharacterAppearanceData.SeedFor("", 12));
        Assert.Equal("id:12", CharacterAppearanceData.SeedFor(null, 12));
    }

    [Fact]
    public void PicksSpreadOverThePalette()
    {
        var hair = new int[CharacterAppearanceData.HairColorCount];
        var outfit = new int[CharacterAppearanceData.OutfitColorCount];
        for (int i = 0; i < 480; i++) { string s = CharacterAppearanceData.SeedFor("bot" + i, 0); hair[CharacterAppearanceData.PickHair(s)]++; outfit[CharacterAppearanceData.PickOutfit(s)]++; }
        foreach (int n in hair) Assert.InRange(n, 30, 90);   // 60 expected each
        foreach (int n in outfit) Assert.InRange(n, 50, 110); // 80 expected each
    }

    [Fact]
    public void ResolvedKeepsChosenAndFillsTheRest()
    {
        string seed = CharacterAppearanceData.SeedFor("Ann", 1);
        CharacterAppearanceData r = Chosen(2, 4, -1, 3).Resolved(seed);
        Assert.Equal(4, r.hairColorIndex);
        Assert.Equal(CharacterAppearanceData.PickOutfit(seed), r.outfitColorIndex);
        Assert.Equal(3, r.equippedHeadgearId);
        Assert.Equal(2, r.hairStyle);
    }

    [Fact]
    public void WireBytesRoundTrip()
    {
        Assert.Equal(CharacterAppearanceData.WireNone, CharacterAppearanceData.ColorToWire(-1));
        Assert.Equal(5, CharacterAppearanceData.ColorFromWire(CharacterAppearanceData.ColorToWire(5), 8));
        Assert.Equal(-1, CharacterAppearanceData.ColorFromWire(9, 8)); // a newer palette's index this build does not have
        CharacterAppearanceData a = CharacterAppearanceData.FromWire("mechanic", 2, 3, 255, 0);
        Assert.True(a.stored);
        Assert.Equal(3, a.hairColorIndex);
        Assert.False(a.HasOutfitColor);
        Assert.False(CharacterAppearanceData.FromWire("mechanic", 0, 255, 255, 0).stored); // a swarm bot: nothing chosen
    }

    [Fact]
    public void RequestTextParsesAndKeepsTheHeadgear()
    {
        CharacterAppearanceData cur = Chosen(0, -1, -1, 9);
        CharacterAppearanceData next;
        Assert.True(CharacterAppearanceData.TryApplyOpText(Chosen(4, 6, 1).ToOpText(), cur, out next));
        Assert.True(next.SameLook(Chosen(4, 6, 1, 9))); // the client never sets a headgear
        Assert.False(CharacterAppearanceData.TryApplyOpText("4,8,1", cur, out next));
        Assert.False(CharacterAppearanceData.TryApplyOpText("4,1,6", cur, out next));
        Assert.False(CharacterAppearanceData.TryApplyOpText("x,1,1", cur, out next));
        Assert.False(CharacterAppearanceData.TryApplyOpText("", cur, out next));
        Assert.True(CharacterAppearanceData.TryApplyOpText("0,-1,-1", cur, out next));
    }

    [Fact]
    public void LegacyLookMapsTheDyeToTheNearestPaletteColour()
    {
        int[] palette = { 0x7A4A2E, 0x2B2A33, 0xE8B850, 0xF2EEE6 };
        Assert.Equal(2, CharacterAppearanceData.NearestColor(0xE8C445, palette));
        CharacterAppearanceData a = CharacterAppearanceData.FromLegacyLook(null, 3, 0x34343C, palette);
        Assert.True(a.stored);
        Assert.Equal(3, a.hairStyle);
        Assert.Equal(1, a.hairColorIndex);
        CharacterAppearanceData own = CharacterAppearanceData.FromLegacyLook(null, 2, -1, palette); // dye -1 = the style's own colour: no colour chosen
        Assert.Equal(-1, own.hairColorIndex);
    }

    [Fact]
    public void PaletteFileHasTheCountsTheCodeUses()
    {
        string path = Path.Combine("..", "..", "..", "..", "..", "..", "Assets", "Resources", "Data", "hero-layered", "palettes.json");
        string text = File.ReadAllText(path);
        Assert.Equal(CharacterAppearanceData.HairColorCount, Regex.Matches(Section(text, "hair"), "#[0-9A-Fa-f]{6}").Count);
        Assert.Equal(CharacterAppearanceData.OutfitColorCount, Regex.Matches(Section(text, "outfit"), "#[0-9A-Fa-f]{6}").Count);
    }

    /// <summary>19/11/2026: "earth brown" (#A9744A) read as the leather belt / boots (#8A5A34, spec fixed colours) and became jade teal. No outfit colour may sit that close again.</summary>
    [Fact]
    public void NoOutfitColourLooksLikeTheLeatherBelt()
    {
        string path = Path.Combine("..", "..", "..", "..", "..", "..", "Assets", "Resources", "Data", "hero-layered", "palettes.json");
        int[] leather = { 0x8A5A34, 0x5E3A1E };
        foreach (Match m in Regex.Matches(Section(File.ReadAllText(path), "outfit"), "#([0-9A-Fa-f]{6})"))
        {
            int c = System.Convert.ToInt32(m.Groups[1].Value, 16);
            foreach (int l in leather)
            {
                int dr = ((c >> 16) & 255) - ((l >> 16) & 255), dg = ((c >> 8) & 255) - ((l >> 8) & 255), db = (c & 255) - (l & 255);
                Assert.True(3 * dr * dr + 4 * dg * dg + 2 * db * db > 9000, m.Value + " is too close to the belt leather"); // NearestColor's weights; the old earth brown scored 6555, the nearest kept colours 11 000+
            }
        }
    }

    static string Section(string json, string key)
    {
        int at = json.IndexOf("\"" + key + "\"", System.StringComparison.Ordinal);
        Assert.True(at >= 0, key);
        int end = json.IndexOf(']', at);
        return json.Substring(at, end - at);
    }
}
