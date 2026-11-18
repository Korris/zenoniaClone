using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Tokens of the "RO Soft Fantasy" look (docs/reference/ui-style-ro-soft-fantasy/README.md and 01-style-board.png): the eight named colours,
    /// a few derived text / line colours, the dark HUD plate and the fonts. Everything RO-styled reads its colours here, nowhere else.
    /// Menus: ivory #F7F8F5 at 96 % (never cold white), ink-blue text, flat mint primary / sky secondary / coral danger buttons.
    /// HUD in fights: dark blue-black plate at ~90 %, white text (no translucent white panels over a 40-80 player fight).
    /// </summary>
    public static class RoSoftTheme
    {
        // The eight colours of the style board.
        public static readonly Color CloudWhite = Hex("#F7F8F5"), MintMist = Hex("#EAF4F2"), SkyMist = Hex("#E4EDF7"), Ink = Hex("#32566B");
        public static readonly Color Mint = Hex("#48B6A5"), Sky = Hex("#78B7F0"), Coral = Hex("#E46C77"), Gold = Hex("#F4C66A");
        // Derived: secondary text, hairlines, readable gold / green / red text on ivory, cell fill.
        public static readonly Color InkSoft = Hex("#6A8496"), Line = Hex("#D3DFE7"), Cell = Hex("#F1F5F8"), CellEdge = Hex("#CBD9E3");
        public static readonly Color GoldText = Hex("#B9811C"), GoodText = Hex("#24937A"), BadText = Hex("#D0505E"), BlueText = Hex("#2F7EC4"), PurpleText = Hex("#8A58C4");
        public static readonly Color MintDark = Hex("#2F9585"), SkyDark = Hex("#4F94D6"), CoralDark = Hex("#C84F5C"), GoldDark = Hex("#D9A441");
        // HUD variant: blue-black plate, white text.
        public static readonly Color Night = Hex("#17222F"), NightEdge = Hex("#3C5268"), NightText = Color.white;

        /// <summary>Card (big window) opacity: README "96 %", the user asked for 95-98 %.</summary>
        public const float CardAlpha = 0.96f, NightAlpha = 0.9f;

        public static Font Body { get { return PaperTheme.BodyFont; } }  // Nunito Regular (full Vietnamese)
        public static Font Bold { get { return PaperTheme.BoldFont; } }  // Nunito Bold: titles, buttons, numbers

        public static Color Hex(string hex) { Color c; ColorUtility.TryParseHtmlString(hex, out c); return c; }

        static Color A(Color c, float a) { c.a = a; return c; }

        /// <summary>
        /// A text colour picked for the old dark navy windows, moved to one readable on ivory: white / cream -> ink, grey -> soft ink,
        /// gold / green / red / blue / purple keep their meaning in a darker shade. Already dark colours stay. Alpha is kept.
        /// </summary>
        public static Color OnLight(Color c)
        {
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            if (v < 0.5f) return c;                                    // dark already: readable on ivory
            if (s < 0.28f) return A(v > 0.78f ? Ink : InkSoft, c.a); // white, cream, steel grey
            if (h < 0.045f || h > 0.92f) return A(BadText, c.a);
            if (h < 0.18f) return A(GoldText, c.a);                    // orange, gold, yellow
            if (h < 0.46f) return A(GoodText, c.a);
            if (h < 0.7f) return A(BlueText, c.a);
            return A(PurpleText, c.a);
        }

        /// <summary>A plain (sprite-less) fill of the old windows on an ivory card: dark navy plates become sky mist, gold rules hairlines.</summary>
        public static Color FillOnLight(Color c)
        {
            if (c.a <= 0.01f) return c;
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            if (v < 0.42f) return A(SkyMist, Mathf.Max(c.a, 0.9f));       // navy plates, bar tracks
            if (s > 0.35f && h > 0.06f && h < 0.17f) return A(Line, 1f);  // gold divider rules
            return c;
        }
    }
}
