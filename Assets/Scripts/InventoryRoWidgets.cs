using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Building blocks of the RO Soft Fantasy bag (InventoryPanel): images of the ChatGPT-painted pieces in Resources/Sprites/ui/ro-soft/
    /// (bag-* cut by Tools/cut-ui-paper-sheet.py with Tools/ui-ro-soft-sheets/ro3/ro4/ro5-inventory*.json, plus the older kit buttons),
    /// Nunito texts, item cells and icon buttons. Coordinates are UiKit's: top-left of the parent, y down. Nothing is painted in code.
    /// </summary>
    public static class InventoryRoWidgets
    {
        public static readonly Color Ink = RoSoftTheme.Hex("#2E4A63"), InkSoft = RoSoftTheme.Hex("#5F7488"), Good = RoSoftTheme.Hex("#2E9E57"), Bad = RoSoftTheme.Hex("#D2483F");
        public static readonly Color Orange = RoSoftTheme.Hex("#E2702A"), IconSlate = RoSoftTheme.Hex("#5B6B7C");

        public static Sprite Art(string piece) { return piece != null ? RoSoftSprites.Named(piece) : null; } // null = a plain tinted rect (hairlines)

        /// <summary>A piece as an image: 9-sliced when the piece has a border, else drawn whole (aspect kept).</summary>
        public static Image Img(Transform parent, string name, float x, float y, float w, float h, string piece, bool raycast = false)
        {
            Image img = UiKit.Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            img.raycastTarget = raycast;
            SetArt(img, piece);
            return img;
        }

        public static void SetArt(Image img, string piece)
        {
            Sprite s = Art(piece);
            img.sprite = s;
            bool sliced = s != null && s.border != Vector4.zero;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = !sliced;
            img.pixelsPerUnitMultiplier = 1f;
        }

        public static Text Txt(Transform parent, string name, float x, float y, float w, float h, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft, bool bold = true)
        {
            Text t = UiKit.Rect(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
            t.font = bold ? RoSoftTheme.Bold : RoSoftTheme.Body;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            return t;
        }

        /// <summary>White label with a soft dark rim, for numbers drawn over item art.</summary>
        public static void Rim(Text t)
        {
            Outline o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.12f, 0.16f, 0.24f, 0.85f);
            o.effectDistance = new Vector2(1.2f, -1.2f);
        }

        public static Button Clickable(Image img, UnityAction onClick)
        {
            Button b = UiWinKit.MakeButton(img, () => onClick());
            return b;
        }

        /// <summary>
        /// An item cell: lavender back (bag-slot), "+" back when empty (bag-slot-empty), gold glow piece when selected (bag-slot-selected),
        /// icon, corner number and optional caption pill under it (worn slots).
        /// </summary>
        public sealed class Cell
        {
            public readonly RectTransform Root;
            public readonly Image Glow, Back, Icon;
            public readonly Text Count;
            public Text Caption;

            public Cell(Transform parent, string name, float x, float y, float size, UnityAction onClick)
            {
                Root = UiKit.Rect(parent, name, x, y, size, size);
                float pad = size * 0.085f; // the selected piece is 168 px for a 144 px cell
                Glow = Img(Root, "Glow", -pad, -pad, size + 2f * pad, size + 2f * pad, "bag-slot-selected");
                Back = Img(Root, "Back", 0f, 0f, size, size, "bag-slot", onClick != null);
                Icon = UiKit.Rect(Root, "Icon", size * 0.08f, size * 0.08f, size * 0.84f, size * 0.84f).gameObject.AddComponent<Image>();
                Icon.preserveAspect = true;
                Icon.raycastTarget = false;
                Count = Txt(Root, "Count", 0f, size * 0.52f, size - 3f, size * 0.46f, "", Mathf.RoundToInt(size * 0.3f), Color.white, TextAnchor.LowerRight);
                Rim(Count);
                if (onClick != null) Clickable(Back, onClick);
            }

            /// <summary>Caption pill hanging over the cell's bottom edge (Weapon, Armor ...).</summary>
            public void AddCaption(string text, float minWidth)
            {
                float size = Root.sizeDelta.x;
                Caption = InventoryRoBoxes.Pill(Root, "CaptionPill", size * 0.5f, size - 10f, 22f, "bag-pill-white", text, 13, Ink, minWidth, true).Label;
            }

            /// <summary>`tint` = rarity colour (light tint of the lavender), `dim` = a cell past the bag's size.</summary>
            public void SetState(bool selected, bool empty, Color tint, bool dim = false)
            {
                Back.sprite = Art(empty ? "bag-slot-empty" : "bag-slot");
                Back.color = selected ? new Color(1f, 1f, 1f, 0f) : empty ? new Color(1f, 1f, 1f, dim ? 0.4f : 0.8f) : Color.Lerp(Color.white, new Color(tint.r, tint.g, tint.b, 1f), 0.2f); // alpha 0 still takes taps
                Glow.enabled = selected;
                Glow.color = new Color(1f, 1f, 1f, 0.85f); // a softer glow than the art
            }

            public void SetIcon(Sprite sprite, Color tint, float scale = 0.84f)
            {
                Icon.sprite = sprite;
                Icon.enabled = sprite != null;
                Icon.color = tint;
                float size = Root.sizeDelta.x, box = size * scale;
                Icon.rectTransform.anchoredPosition = new Vector2((size - box) * 0.5f, -(size - box) * 0.5f);
                Icon.rectTransform.sizeDelta = new Vector2(box, box);
            }
        }
    }
}
