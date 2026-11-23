using System;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using W = ZenoniaProto.InventoryRoWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// Boxes of the Soul Gem screens. Same CSS-like rules as the bag (docs/ui-layout-rules.md section 1): every box has its padding / gap / min-height as a constant here,
    /// content is centred both ways, corners come from the 9-slice art (never from code), and a pill / button takes its height from the art so the ends stay round.
    /// </summary>
    public static class GemUiWidgets
    {
        public const float ChipH = 30f, ChipPadX = 12f, ChipGap = 4f, ChipIcon = 20f;   // filter chip: icon + text, padding 12, gap 4
        public const float ButtonH = 48f, ButtonPadX = 18f;                              // main button: 48 high, padding 18
        public const float RowH = 60f, RowPadL = 12f, RowGap = 8f, RowIcon = 44f;        // gear row: icon 44 + name + sockets
        public const int TextNormal = 16, TextSmall = 14, TextTitle = 20;

        public static readonly Color Ink = W.Ink, InkSoft = W.InkSoft, Good = RoSoftTheme.GoodText, Bad = RoSoftTheme.BadText, OnButton = new Color(1f, 0.97f, 0.93f);

        public static Text Txt(Transform parent, string name, float x, float y, float w, float h, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            Text t = W.Txt(parent, name, x, y, w, h, text, size, color, align);
            t.horizontalOverflow = HorizontalWrapMode.Wrap; // long names wrap inside their box; nothing leaves it
            t.resizeTextForBestFit = true; t.resizeTextMinSize = Mathf.Min(size, Mathf.Max(12, size - 2)); t.resizeTextMaxSize = size; // shrinks a little for a long name, never below 12 (14 is the design size of small text)
            return t;
        }

        /// <summary>The panel behind a group ("panel-inner" or "panel-detail").</summary>
        public static Image Panel(Transform parent, string name, float x, float y, float w, float h, bool detail = false)
        {
            return GemUiArt.Img(parent, name, x, y, w, h, GemUiArt.Frame(detail ? "panel-detail" : "panel-inner"));
        }

        /// <summary>A heading line inside a panel: serif-less bold ink text with a thin rule under it.</summary>
        public static Text Heading(Transform parent, string name, float x, float y, float w, string text)
        {
            return Txt(parent, name, x, y, w, 28f, text, TextTitle, Ink);
        }

        /// <summary>Art button (btn-gold / btn-teal / btn-red / btn-blue / btn-disabled): label centred both ways, `enabled` swaps the disabled piece in.</summary>
        public sealed class PieceButton
        {
            public readonly RectTransform Root;
            readonly Image skin;
            readonly Text label;
            readonly string normal;
            readonly Button button;

            public PieceButton(Transform parent, string name, float x, float y, float w, float h, string piece, string text, Action onClick)
            {
                normal = piece;
                Root = UiKit.Rect(parent, name, x, y, w, h);
                skin = Root.gameObject.AddComponent<Image>();
                GemUiArt.SetPiece(skin, piece);
                button = UiWinKit.MakeButton(skin, onClick);
                label = Txt(Root, "Label", ButtonPadX * 0.5f, 0f, w - ButtonPadX, h - 4f, text, 18, OnButton, TextAnchor.MiddleCenter);
                var rim = label.gameObject.AddComponent<Outline>(); // thin brown rim: cream text stays readable on the yellow / teal art
                rim.effectColor = new Color(0.45f, 0.28f, 0.1f, 0.85f); rim.effectDistance = new Vector2(1.2f, -1.2f);
            }

            public void Set(string text, bool enabled)
            {
                label.text = text;
                GemUiArt.SetPiece(skin, enabled ? normal : GemUiArt.Frame("btn-disabled"));
                button.interactable = enabled;
                label.color = enabled ? OnButton : InkSoft;
            }
        }

        /// <summary>Filter chip with an icon on the left of the text: pill art normal / selected, content centred.</summary>
        public sealed class Chip
        {
            public readonly RectTransform Root;
            readonly Image skin, icon;
            readonly Text label;

            public Chip(Transform parent, string name, float x, float y, float w, string iconPiece, string text, Action onClick)
            {
                bool compact = w < 90f; // narrow chips (5 in a 360 box): padding 6, icon 16, gap 2, so the text still fits
                float padX = compact ? 6f : ChipPadX, iconS = compact ? 16f : ChipIcon, gap = compact ? 2f : ChipGap;
                Root = UiKit.Rect(parent, name, x, y, w, ChipH);
                skin = Root.gameObject.AddComponent<Image>();
                var row = Root.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.padding = new RectOffset((int)padX, (int)padX, 2, 2);
                row.spacing = gap;
                row.childAlignment = TextAnchor.MiddleCenter;
                row.childControlWidth = row.childControlHeight = true;
                row.childForceExpandWidth = row.childForceExpandHeight = false;
                icon = UiKit.Rect(Root, "Icon", 0f, 0f, iconS, iconS).gameObject.AddComponent<Image>();
                icon.raycastTarget = false;
                GemUiArt.SetPiece(icon, iconPiece);
                Fixed(icon.gameObject, iconS, iconS);
                float textW = w - 2f * padX - iconS - gap;
                label = Txt(Root, "Label", 0f, 0f, textW, ChipH, text, compact ? TextSmall : TextSmall + 1, Ink, TextAnchor.MiddleCenter);
                Fixed(label.gameObject, textW, ChipH - 4f);
                UiWinKit.MakeButton(skin, onClick);
                Set(false);
            }

            public void Set(bool on) { GemUiArt.SetPiece(skin, GemUiArt.Frame(on ? "filter-pill-selected" : "filter-pill")); }
        }

        public static void Fixed(GameObject go, float w, float h)
        {
            LayoutElement e = go.AddComponent<LayoutElement>();
            e.minWidth = e.preferredWidth = w;
            e.minHeight = e.preferredHeight = h;
        }

        /// <summary>Row of the five filter chips (All / ATK / DEF / HP / Utility) starting at (x, y); `onPick` gets the filter.</summary>
        public static Chip[] FilterRow(Transform parent, float x, float y, float width, Action<GemFilter> onPick)
        {
            GemFilter[] all = { GemFilter.All, GemFilter.Atk, GemFilter.Def, GemFilter.Hp, GemFilter.Utility };
            string[] names = { "All", "ATK", "DEF", "HP", "Utility" };
            float gap = 6f, w = (width - gap * 4f) / 5f;
            var chips = new Chip[5];
            for (int i = 0; i < 5; i++) { GemFilter f = all[i]; chips[i] = new Chip(parent, "Filter" + names[i], x + i * (w + gap), y, w, GemUiArt.FilterIcon(f), names[i], () => onPick(f)); }
            return chips;
        }

        public static void SelectChip(Chip[] chips, GemFilter f) { for (int i = 0; i < chips.Length; i++) chips[i].Set(i == (int)f); }

        public static string Name(GemKind kind, CardDef c) { return c.Name + " " + kind + " Gem"; }
    }
}
