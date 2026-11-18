using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// The fixed look of a character (docs/character-appearance-layered-spec.md): hairstyle, hair colour and outfit colour (indices into
    /// Data/hero-layered/palettes.json) and the worn headgear. Saved with the character (SaveData.appearance, server-api state.appearance) and
    /// carried in NetAppearance. -1 colour = not chosen: Resolved() then picks one from a hash of the character's name (or net id), the same on
    /// every client and after every map change. stored = false (old saves, swarm bots) means nothing was chosen at all.
    /// Engine-free so the zone server and the dotnet tests share it.
    /// </summary>
    [Serializable]
    public class CharacterAppearanceData
    {
        public const int HairColorCount = 8, OutfitColorCount = 6; // palettes.json "hair" / "outfit" lengths (a test checks the file)
        public const int MaxHairStyle = 255, MaxHeadgear = 255;    // one byte each on the wire
        public const byte WireNone = 255;                          // NetAppearance colour byte: not chosen

        [NonSerialized] public string classId; // SaveData.classId already holds it: not written twice
        public bool stored;                    // false = a save from before 19/11/2026 or a character that never chose: treat as Unset
        public int hairStyle;                  // head layer style (HeroHeadLayer / layered hair of the class); 0 = the class default
        public int hairColorIndex = -1;        // palettes.json hair index, -1 = not chosen
        public int outfitColorIndex = -1;      // palettes.json outfit index, -1 = not chosen
        public int equippedHeadgearId;         // 0 = bare head; set by the item / drop code later, never by the client

        public bool HasHairColor { get { return hairColorIndex >= 0; } }
        public bool HasOutfitColor { get { return outfitColorIndex >= 0; } }

        public static CharacterAppearanceData Unset(string classId) { return new CharacterAppearanceData { classId = classId }; }

        public CharacterAppearanceData Clone()
        {
            return new CharacterAppearanceData { classId = classId, stored = stored, hairStyle = hairStyle, hairColorIndex = hairColorIndex, outfitColorIndex = outfitColorIndex, equippedHeadgearId = equippedHeadgearId };
        }

        /// <summary>A save's appearance made safe: null or never stored gives Unset; out-of-range numbers fall back to "not chosen" / defaults.</summary>
        public static CharacterAppearanceData Sanitize(CharacterAppearanceData raw, string classId)
        {
            if (raw == null || !raw.stored) return Unset(classId);
            return new CharacterAppearanceData
            {
                classId = classId, stored = true,
                hairStyle = raw.hairStyle < 0 || raw.hairStyle > MaxHairStyle ? 0 : raw.hairStyle,
                hairColorIndex = InRange(raw.hairColorIndex, HairColorCount),
                outfitColorIndex = InRange(raw.outfitColorIndex, OutfitColorCount),
                equippedHeadgearId = raw.equippedHeadgearId < 0 || raw.equippedHeadgearId > MaxHeadgear ? 0 : raw.equippedHeadgearId,
            };
        }

        static int InRange(int i, int count) { return i >= 0 && i < count ? i : -1; }

        /// <summary>A copy with every colour not chosen filled in from the seed (SeedFor). Chosen values and the headgear stay.</summary>
        public CharacterAppearanceData Resolved(string seed)
        {
            CharacterAppearanceData r = Clone();
            if (r.hairColorIndex < 0) r.hairColorIndex = PickHair(seed);
            if (r.outfitColorIndex < 0) r.outfitColorIndex = PickOutfit(seed);
            return r;
        }

        /// <summary>The seed of the deterministic pick: the character name (case-insensitive; stable across maps and reconnects), else the net id.</summary>
        public static string SeedFor(string name, int netId)
        {
            return !string.IsNullOrEmpty(name) ? "n:" + name.Trim().ToLowerInvariant() : "id:" + netId;
        }

        public static int PickHair(string seed) { return (int)(Hash(seed) % HairColorCount); }
        public static int PickOutfit(string seed) { return (int)((Hash(seed) / HairColorCount) % OutfitColorCount); }

        /// <summary>A small extra per-hero number from the seed in [0, 1): crowd animation phase / scale (HeroRenderer). Independent of the colour picks.</summary>
        public static float Unit(string seed, int salt)
        {
            uint h = Hash(seed + "#" + salt);
            return (h & 0xFFFFFF) / (float)0x1000000;
        }

        /// <summary>FNV-1a over UTF-16 code units: string.GetHashCode differs between processes in .NET, this does not.</summary>
        public static uint Hash(string s)
        {
            uint h = 2166136261u;
            if (s != null) foreach (char c in s) { h ^= c; h *= 16777619u; }
            h ^= h >> 15; h *= 2246822519u; h ^= h >> 13; // final mix: similar names ("bot1", "bot2") still spread over the palette
            return h;
        }

        // ---- wire (NetAppearance) ----

        public static byte ColorToWire(int index) { return index < 0 || index > 254 ? WireNone : (byte)index; }
        public static int ColorFromWire(byte b, int count) { return b == WireNone || b >= count ? -1 : b; }

        /// <summary>The look of another hero from its NetAppearance bytes (stored = something was chosen).</summary>
        public static CharacterAppearanceData FromWire(string classId, byte hairStyle, byte hairColor, byte outfitColor, byte headgear)
        {
            var a = new CharacterAppearanceData
            {
                classId = classId, hairStyle = hairStyle, equippedHeadgearId = headgear,
                hairColorIndex = ColorFromWire(hairColor, HairColorCount), outfitColorIndex = ColorFromWire(outfitColor, OutfitColorCount),
            };
            a.stored = a.HasHairColor || a.HasOutfitColor || hairStyle != 0 || headgear != 0;
            return a;
        }

        // ---- InvOp.Appearance text: "style,hairColor,outfitColor" (the client never sends a headgear) ----

        public string ToOpText() { return hairStyle + "," + hairColorIndex + "," + outfitColorIndex; }

        /// <summary>Parses an InvOp.Appearance request onto a copy of `current` (headgear kept). False for garbage or out-of-range numbers.</summary>
        public static bool TryApplyOpText(string text, CharacterAppearanceData current, out CharacterAppearanceData result)
        {
            result = null;
            if (string.IsNullOrEmpty(text) || text.Length > 16) return false;
            string[] p = text.Split(',');
            int s, h, o;
            if (p.Length != 3 || !int.TryParse(p[0], out s) || !int.TryParse(p[1], out h) || !int.TryParse(p[2], out o)) return false;
            if (s < 0 || s > MaxHairStyle || h < -1 || h >= HairColorCount || o < -1 || o >= OutfitColorCount) return false;
            result = (current ?? Unset(null)).Clone();
            result.stored = true;
            result.hairStyle = s; result.hairColorIndex = h; result.outfitColorIndex = o;
            return true;
        }

        public bool SameLook(CharacterAppearanceData o)
        {
            return o != null && stored == o.stored && hairStyle == o.hairStyle && hairColorIndex == o.hairColorIndex && outfitColorIndex == o.outfitColorIndex && equippedHeadgearId == o.equippedHeadgearId;
        }

        // ---- migration from the device-only CharacterLookPrefs (style + dye preset) ----

        /// <summary>Index of the palette colour nearest to rgb (0xRRGGBB, squared distance weighted 3:4:2 like the eye); -1 for an empty palette.</summary>
        public static int NearestColor(int rgb, int[] palette)
        {
            int best = -1;
            long bestD = long.MaxValue;
            for (int i = 0; palette != null && i < palette.Length; i++)
            {
                int dr = ((rgb >> 16) & 255) - ((palette[i] >> 16) & 255), dg = ((rgb >> 8) & 255) - ((palette[i] >> 8) & 255), db = (rgb & 255) - (palette[i] & 255);
                long d = 3L * dr * dr + 4L * dg * dg + 2L * db * db;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>The stored look built from an old device choice: the style as is, the dye preset's colour (or none: -1) mapped to the nearest palette hair colour.</summary>
        public static CharacterAppearanceData FromLegacyLook(CharacterAppearanceData current, int style, int dyeRgbOrMinus1, int[] hairPalette)
        {
            CharacterAppearanceData a = (current ?? Unset(null)).Clone();
            a.stored = true;
            a.hairStyle = style < 0 || style > MaxHairStyle ? 0 : style;
            a.hairColorIndex = dyeRgbOrMinus1 < 0 ? a.hairColorIndex : NearestColor(dyeRgbOrMinus1, hairPalette);
            return a;
        }
    }
}
