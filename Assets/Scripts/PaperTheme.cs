using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>How a kit text is kept readable on what lies behind it.</summary>
    public enum PaperInk
    {
        Plain,    // dark text on paper: no effect
        Outlined, // light text on wood / a coloured button: dark brown outline
        World,    // text straight over the game world (HUD): black drop shadow
    }

    /// <summary>
    /// Tokens of the "Adventure Paper" UI (docs/reference/ui-redesign-261116/chatgpt/style-board-adventure-paper.png): colours measured on
    /// the style board, text sizes in canvas units (UiKit reference 960 x 640), the two fonts and the text builders every PaperKit widget uses.
    /// Titles = Baloo 2 ExtraBold, content = Nunito (both SIL OFL, full Vietnamese, Resources/Fonts/). Text is always drawn by code, never baked in art.
    /// </summary>
    public static class PaperTheme
    {
        // Palette row of the style board (the two labelled swatches keep their printed codes).
        public static readonly Color Brown = Hex("#4A2E1F"), Paper = Hex("#F6E6C8"), Cream = Hex("#FEF9E6"), Red = Hex("#D14735"), Blue = Hex("#3D7DFD"), Green = Hex("#64B255");
        public static readonly Color Navy = Hex("#2A4568"), Slate = Hex("#3C415C"), Sky = Hex("#95B9FD"), Gold = Hex("#FDC04E"), Purple = Hex("#B163FC");
        // Pieces of the board: wood rim, dark plates, text.
        public static readonly Color Wood = Hex("#8E5B43"), WoodDark = Hex("#54443D"), PaperEdge = Hex("#E2C79A"), Plate = Hex("#433F40"), PlateEdge = Hex("#2B2729");
        public static readonly Color Ink = Hex("#3E180B"), InkSoft = Hex("#7A5A45"), TextLight = Hex("#FBF5E1"), TitleGold = Hex("#F9E384"), Disabled = Hex("#5B5B61"), DisabledText = Hex("#A9A9AE"), Steel = Hex("#69768E");
        // Buttons: face / rim (darker lower edge of the board's buttons).
        public static readonly Color BtnGold = Hex("#FDBF29"), BtnGoldEdge = Hex("#AB5606"), BtnBlue = Hex("#1C61B4"), BtnBlueEdge = Hex("#083A7C"), BtnRed = Hex("#D64C43"), BtnRedEdge = Hex("#952624"), BtnPurpleEdge = Hex("#593981");
        // Bars and badges.
        public static readonly Color BarHp = Hex("#E63129"), BarMp = Hex("#25A0FA"), BarExp = Hex("#59AF41"), BadgeRed = Hex("#E63129"), LabelOrange = Hex("#DC4F02"), LabelGreen = Hex("#188231"), Glow = Hex("#FEF553");

        /// <summary>Rarity tiers of the style board (Thường, Tốt, Hiếm, Anh Hùng, Huyền Thoại): frame colour and the dark cell behind the icon.</summary>
        public const int RarityCount = 5;
        static readonly Color[] RarityEdge = { Hex("#A8A8A8"), Hex("#6CB448"), Hex("#3C90F0"), Hex("#C06CF0"), Hex("#F0A80C") };
        static readonly Color[] RarityBack = { Hex("#3A383E"), Hex("#253834"), Hex("#1C335D"), Hex("#4F2E5A"), Hex("#643309") };
        static readonly string[] RarityIds = { "common", "uncommon", "rare", "epic", "legendary" };
        static readonly string[] RarityArtNames = { "common", "good", "rare", "heroic", "legendary" }; // file names of the cut slots / cards

        /// <summary>Text sizes in canvas units; nothing smaller than BarSize is allowed (readability on phones).</summary>
        public const int TitleSize = 22, SectionSize = 15, BodySize = 13, BarSize = 11;

        static Font titleFont, bodyFont, boldFont;

        public static Color Hex(string hex) { Color c; ColorUtility.TryParseHtmlString(hex, out c); return c; }

        public static Color Rarity(int tier) { return RarityEdge[Mathf.Clamp(tier, 0, RarityCount - 1)]; }
        public static Color RarityCell(int tier) { return RarityBack[Mathf.Clamp(tier, 0, RarityCount - 1)]; }
        /// <summary>Tier index of an item rarity id (ItemDefs: common, uncommon, rare, epic; legendary is drawn but not dropped yet); 0 when unknown.</summary>
        public static int RarityTier(string id) { return Mathf.Max(0, System.Array.IndexOf(RarityIds, id)); }
        public static string RarityId(int tier) { return RarityIds[Mathf.Clamp(tier, 0, RarityCount - 1)]; }
        /// <summary>Art name of a tier: PaperPiece.SlotPrefix / CardPrefix + this.</summary>
        public static string RarityArt(int tier) { return RarityArtNames[Mathf.Clamp(tier, 0, RarityCount - 1)]; }

        public static Font TitleFont { get { return titleFont != null ? titleFont : titleFont = Load("Fonts/Baloo2/Baloo2-ExtraBold"); } }
        public static Font BodyFont { get { return bodyFont != null ? bodyFont : bodyFont = Load("Fonts/Nunito/Nunito-Regular"); } }
        public static Font BoldFont { get { return boldFont != null ? boldFont : boldFont = Load("Fonts/Nunito/Nunito-Bold"); } }

        static Font Load(string path)
        {
            Font f = Resources.Load<Font>(path);
            if (f == null) { Debug.LogWarning("[ZenoniaProto] missing UI font " + path); f = UiKit.Font; }
            return f;
        }

        /// <summary>Title text (Baloo 2 ExtraBold) at a top-left based position.</summary>
        public static Text Title(Transform parent, string name, float x, float y, float w, float h, string text, int size, Color color, PaperInk ink = PaperInk.Plain, TextAnchor align = TextAnchor.MiddleLeft)
        {
            return Make(UiKit.Rect(parent, name, x, y, w, h), TitleFont, text, size, color, ink, align);
        }

        /// <summary>Content text (Nunito, bold = Nunito Bold) at a top-left based position.</summary>
        public static Text Body(Transform parent, string name, float x, float y, float w, float h, string text, int size, Color color, PaperInk ink = PaperInk.Plain, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
        {
            return Make(UiKit.Rect(parent, name, x, y, w, h), bold ? BoldFont : BodyFont, text, size, color, ink, align);
        }

        /// <summary>Caption stretched over `parent` (button / tab / badge text), title font.</summary>
        public static Text Caption(Transform parent, string text, int size, Color color, PaperInk ink = PaperInk.Outlined)
        {
            Text t = Make(UiKit.Fill(parent, "Label"), TitleFont, text, size, color, ink, TextAnchor.MiddleCenter);
            t.horizontalOverflow = HorizontalWrapMode.Overflow; // captions are one line: too long shows as too long, never as two cramped lines
            return t;
        }

        static Text Make(RectTransform rt, Font font, string text, int size, Color color, PaperInk ink, TextAnchor align)
        {
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = Mathf.Max(size, BarSize);
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            if (ink == PaperInk.Outlined)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(Ink.r, Ink.g, Ink.b, 0.9f);
                o.effectDistance = new Vector2(1f, -1f);
            }
            else if (ink == PaperInk.World)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.85f);
                s.effectDistance = new Vector2(1f, -1f);
            }
            return t;
        }
    }
}
