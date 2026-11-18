using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Hair and outfit colours of the layered hero (Data/hero-layered/palettes.json; index = CharacterAppearanceData.hairColorIndex / outfitColorIndex).
    /// Each colour is the base tone; HeroRecolor.shader builds the shadow and highlight from it. Missing file = one neutral grey, so nothing breaks.
    /// </summary>
    public static class HeroPalettes
    {
        [Serializable] class PaletteFile { public string[] hair; public string[] outfit; }

        static Color32[] hair, outfit;
        static readonly Color32 Fallback = new Color32(153, 153, 153, 255);

        static void Load()
        {
            if (hair != null) return;
            hair = new Color32[0];
            outfit = new Color32[0];
            TextAsset asset = Resources.Load<TextAsset>("Data/hero-layered/palettes");
            if (asset == null) { Debug.LogWarning("[ZenoniaProto] Data/hero-layered/palettes.json missing"); return; }
            try
            {
                PaletteFile f = JsonUtility.FromJson<PaletteFile>(asset.text);
                if (f != null && f.hair != null) hair = Array.ConvertAll(f.hair, Parse);
                if (f != null && f.outfit != null) outfit = Array.ConvertAll(f.outfit, Parse);
            }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] palettes.json: " + e.Message); }
        }

        static Color32 Parse(string hex)
        {
            Color c;
            return hex != null && ColorUtility.TryParseHtmlString(hex, out c) ? (Color32)c : Fallback;
        }

        public static int HairCount { get { Load(); return hair.Length; } }
        public static int OutfitCount { get { Load(); return outfit.Length; } }

        /// <summary>Hair colour i (wraps; grey when the palette is empty).</summary>
        public static Color32 Hair(int i) { Load(); return Pick(hair, i); }
        public static Color32 Outfit(int i) { Load(); return Pick(outfit, i); }

        static Color32 Pick(Color32[] p, int i) { return p.Length == 0 ? Fallback : p[((i % p.Length) + p.Length) % p.Length]; }

        /// <summary>Hair palette as 0xRRGGBB (CharacterAppearanceData.NearestColor).</summary>
        public static int[] HairRgb()
        {
            Load();
            var r = new int[hair.Length];
            for (int i = 0; i < hair.Length; i++) r[i] = Rgb(hair[i]);
            return r;
        }

        public static int Rgb(Color32 c) { return (c.r << 16) | (c.g << 8) | c.b; }

        /// <summary>0xRRGGBB of an old CharacterLookPrefs dye preset of a class (Data/classes/&lt;class&gt;/hair-dyes.json), -1 for "own colour" or an unknown preset.</summary>
        public static int LegacyDyeRgb(HeroClassDef cls, int dye)
        {
            if (cls == null || dye < 0 || string.IsNullOrEmpty(cls.SheetFolder)) return -1;
            int cut = cls.SheetFolder.IndexOf("/body/", StringComparison.Ordinal);
            if (cut < 0) return -1;
            Color32?[] styles;
            Color32[] presets;
            HeroHairLayers.LoadDyes(cls.SheetFolder.Substring(0, cut), out styles, out presets);
            return dye < presets.Length ? Rgb(presets[dye]) : -1;
        }
    }
}
