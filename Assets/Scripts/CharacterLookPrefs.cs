using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The hairstyle and hair colour a player picked for an online character, kept on this device only (the server has no look fields yet).
    /// PlayerPrefs key zenonia.look.&lt;characterId&gt; holds "style,dye". Dye is a preset index: -1 = the style's own colour, 0..MaxDye = hair-dyes.json presets
    /// (same meaning as HeroHairLayers.Dye). Only two small ints are stored, never a token. Anything unreadable falls back to the defaults.
    /// </summary>
    public static class CharacterLookPrefs
    {
        const string KeyPrefix = "zenonia.look.";
        public const int DefaultStyle = 0, DefaultDye = -1;
        /// <summary>Last valid preset index (9 presets). A bigger stored number is bad data and means "own colour".</summary>
        public const int MaxDye = 8;
        const int MaxStyle = 255; // InvOp.HairStyle carries one byte

        static string Key(string characterId) { return KeyPrefix + characterId; }

        /// <summary>"style,dye" for the given look, clamped like Parse does.</summary>
        public static string Format(int style, int dye)
        {
            style = ClampStyle(style);
            dye = ClampDye(dye);
            return style + "," + dye;
        }

        /// <summary>Reads "style,dye". Null, empty or garbage gives the defaults; out-of-range numbers are clamped.</summary>
        public static void Parse(string text, out int style, out int dye)
        {
            style = DefaultStyle;
            dye = DefaultDye;
            if (string.IsNullOrEmpty(text)) return;
            string[] parts = text.Split(',');
            int s, d;
            if (parts.Length != 2 || !int.TryParse(parts[0], out s) || !int.TryParse(parts[1], out d)) return;
            style = ClampStyle(s);
            dye = ClampDye(d);
        }

        public static bool Has(string characterId)
        {
            return !string.IsNullOrEmpty(characterId) && PlayerPrefs.HasKey(Key(characterId));
        }

        /// <summary>The saved look of a character, or the defaults when there is none (heroes made before this existed, or on another device).</summary>
        public static void Get(string characterId, out int style, out int dye)
        {
            if (string.IsNullOrEmpty(characterId)) { style = DefaultStyle; dye = DefaultDye; return; }
            Parse(PlayerPrefs.GetString(Key(characterId), ""), out style, out dye);
        }

        public static void Set(string characterId, int style, int dye)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            PlayerPrefs.SetString(Key(characterId), Format(style, dye));
            PlayerPrefs.Save();
        }

        /// <summary>Forgets a deleted character's look.</summary>
        public static void Remove(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            PlayerPrefs.DeleteKey(Key(characterId));
            PlayerPrefs.Save();
        }

        /// <summary>Puts the saved look on a hero's renderer. A character without one keeps whatever the renderer already shows.</summary>
        public static void ApplyTo(HeroRenderer view, string characterId)
        {
            if (view == null || !Has(characterId)) return;
            int style, dye;
            Get(characterId, out style, out dye);
            view.HairStyle = style;
            view.HairDye = dye;
        }

        static int ClampStyle(int style) { return style < 0 || style > MaxStyle ? DefaultStyle : style; }
        static int ClampDye(int dye) { return dye < DefaultDye || dye > MaxDye ? DefaultDye : dye; }
    }
}
