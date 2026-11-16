using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>Small text labels of the style board: red flag ("New"), flame ("Hot"), dark flag ("Lv70"), green flag ("Đã gán"), purple ribbon ("Max").</summary>
    public enum PaperLabel { Red, Hot, Dark, Green, Purple }

    /// <summary>Small round marks that sit on a corner of a button, slot or card.</summary>
    public enum PaperMark
    {
        Alert,     // red "!": something new to look at
        AlertGold, // gold "!": a hint / quest marker
        Check,     // green tick: done, assigned
        Lock,      // steel padlock: not available yet
        Dot,       // plain red dot: unread, without a number
    }

    public static partial class PaperKit
    {
        static readonly string[] LabelPieces = { PaperPiece.LabelRed, PaperPiece.LabelHot, PaperPiece.LabelDark, PaperPiece.LabelGreen, PaperPiece.LabelPurple };
        static readonly Color[] LabelFills = { PaperTheme.BadgeRed, PaperTheme.LabelOrange, PaperTheme.Slate, PaperTheme.LabelGreen, PaperTheme.Purple };
        static readonly string[] MarkPieces = { PaperPiece.MarkAlert, PaperPiece.MarkAlertGold, PaperPiece.MarkCheck, PaperPiece.MarkLock, PaperPiece.MarkDot };
        static readonly Color[] MarkFills = { PaperTheme.BadgeRed, PaperTheme.Gold, PaperTheme.LabelGreen, PaperTheme.Steel, PaperTheme.BadgeRed };
        static readonly string[] MarkGlyphs = { "!", "!", "v", "", "" }; // only on the flat fallback: the cut marks carry their own symbol

        /// <summary>
        /// Small label with a caption drawn by code (so it can be translated): "New", "Hot", "Lv70", "Đã gán", "Max".
        /// The cut labels keep their shape inside the rect: flags are about 2.1 : 1, the flame 1.45 : 1, the purple ribbon 2.8 : 1.
        /// </summary>
        public static Text RibbonLabel(Transform parent, string name, float x, float y, float w, float h, string text, PaperLabel tone)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            Image img = PaperArt.Draw(rt, LabelPieces[(int)tone], LabelFills[(int)tone], Color.clear, 4f);
            Text t = PaperTheme.Caption(rt, text, PaperTheme.BarSize, PaperTheme.TextLight);
            if (img.sprite == PaperArt.Round()) return t;
            img.preserveAspect = true;
            if (tone == PaperLabel.Hot) t.rectTransform.offsetMax = new Vector2(0f, -h * 0.3f);          // the body of the flame is its lower part
            else if (tone != PaperLabel.Purple) t.rectTransform.offsetMax = new Vector2(-w * 0.12f, 0f); // flags are notched on the right
            else t.rectTransform.offsetMin = new Vector2(0f, h * 0.2f);                                  // the ribbon band sits above its tails
            return t;
        }

        /// <summary>A round mark centred on (x, y) from the parent's top-left (usually a corner of the widget it sits on).</summary>
        public static GameObject Mark(Transform parent, PaperMark mark, float x, float y, float size = 18f)
        {
            RectTransform rt = UiKit.Rect(parent, mark.ToString(), x - size * 0.5f, y - size * 0.5f, size, size);
            Image img = PaperArt.Draw(rt, MarkPieces[(int)mark], MarkFills[(int)mark], PaperTheme.TextLight, size * 0.5f);
            string glyph = MarkGlyphs[(int)mark];
            if (img.sprite == PaperArt.Round() && glyph != "") PaperTheme.Caption(rt, glyph, Mathf.RoundToInt(size * 0.72f), PaperTheme.TextLight);
            return rt.gameObject;
        }
    }

    /// <summary>
    /// Round red count badge ("12" free skill points, "4" unread): the number is drawn by code. Set(n) hides it at 0, shows "99+" above 99
    /// and rebuilds the text only when n changed.
    /// </summary>
    public sealed class PaperBadge
    {
        public readonly RectTransform Root;
        readonly Text text;
        int shown = int.MinValue;

        /// <summary>Centred on (x, y) from the parent's top-left (usually a corner of the button it sits on).</summary>
        public PaperBadge(Transform parent, float x, float y, float size = 20f)
        {
            Root = UiKit.Rect(parent, "Badge", x - size * 0.5f, y - size * 0.5f, size, size);
            PaperArt.Draw(Root, PaperPiece.BadgeCount, PaperTheme.BadgeRed, PaperTheme.TextLight, size * 0.5f);
            text = PaperTheme.Caption(Root, "", Mathf.Max(PaperTheme.BarSize, Mathf.RoundToInt(size * 0.55f)), PaperTheme.TextLight);
            Set(0);
        }

        public void Set(int count)
        {
            if (count == shown) return;
            shown = count;
            Root.gameObject.SetActive(count > 0);
            if (count > 0) text.text = count > 99 ? "99+" : count.ToString();
        }
    }
}
