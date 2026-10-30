using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// UGUI building blocks of the bag window, the web bag-widgets.js look: bagPanel, bagSlot (rarity border, corner brackets, gold glow
    /// when selected, padlock), bagButton (steel / blue / gold, dimmed when unusable) and web-positioned text (x, y = the web drawText anchor).
    /// </summary>
    public static class BagWidgets
    {
        public static Color Hex(string hex) { return LootSprites.Hex(hex); }

        public static Image Img(Transform parent, string name, float x, float y, float w, float h, Sprite sprite, Color color, bool raycast = false)
        {
            Image img = UiKit.Panel(UiKit.Rect(parent, name, x, y, w, h), color, raycast);
            img.sprite = sprite;
            img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            return img;
        }

        public static Image Panel(Transform parent, string name, float x, float y, float w, float h)
        {
            return Img(parent, name, x, y, w, h, BagSprites.Panel(), Color.white);
        }

        /// <summary>Text anchored like the web drawText: `align` left = x is the left edge, right = x is the right edge, centre = x is the middle.</summary>
        public static Text Text(Transform parent, string name, float x, float y, string text, int size, string color = "#ffffff",
            TextAnchor align = TextAnchor.UpperLeft, float width = 300f, bool bold = false)
        {
            float left = align == TextAnchor.UpperRight || align == TextAnchor.MiddleRight ? x - width
                : align == TextAnchor.UpperCenter || align == TextAnchor.MiddleCenter ? x - width * 0.5f : x;
            Text t = UiKit.Label(parent, name, left, y - 2f, width, size + 8f, text, size, Hex(color), align, bold);
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static Button Clickable(GameObject go, Graphic target, UnityAction onClick)
        {
            Button b = go.AddComponent<Button>();
            b.targetGraphic = target;
            b.transition = Selectable.Transition.None;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(() =>
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); // Space / Enter must never re-press it
                onClick();
            });
            return b;
        }

        /// <summary>One bag / equipment cell: glow, backing, rarity edge, brackets, icon, count and padlock.</summary>
        public sealed class Slot
        {
            public RectTransform Root;
            public Image Glow, Back, Edge, Brackets, Icon, Lock;
            public Text Count;

            public Slot(Transform parent, string name, float x, float y, float size, UnityAction onClick)
            {
                Root = UiKit.Rect(parent, name, x, y, size, size);
                Glow = Img(Root, "Glow", -10f, -10f, size + 20f, size + 20f, BagSprites.Glow(), Hex("#ffd576"));
                Back = Img(Root, "Back", 0f, 0f, size, size, BagSprites.SlotBack(), Color.white, true);
                Edge = Img(Root, "Edge", 0f, 0f, size, size, BagSprites.SlotEdge(), Color.white);
                Brackets = Img(Root, "Brackets", 0f, 0f, size, size, BagSprites.SlotBrackets(), Color.white);
                Icon = UiKit.Icon(Root, "Icon", 0f, 0f, size);
                Lock = Img(Root, "Lock", size * 0.25f, size * 0.25f, size * 0.5f, size * 0.5f, BagSprites.Lock(), Hex("#43596d"));
                Count = Text(Root, "Count", size - 10f, size - 17f, "", 14, "#ffffff", TextAnchor.UpperRight, size);
                if (onClick != null) Clickable(Root.gameObject, Back, onClick);
            }

            /// <summary>Selected = gold glow + light edge; `edge` = rarity colour otherwise.</summary>
            public void SetFrame(bool selected, Color edge, bool locked = false)
            {
                Glow.enabled = selected;
                Edge.color = selected ? Hex("#ffe7a5") : edge;
                Brackets.color = selected ? Hex("#fff2c4") : Hex("#b6c8d499");
                Lock.enabled = locked;
            }

            /// <summary>Icon centred in the cell at `scale` of the web 54px box (0 = hidden).</summary>
            public void SetIcon(Sprite sprite, float boxPx, Color tint)
            {
                Icon.sprite = sprite;
                Icon.enabled = sprite != null;
                Icon.color = tint;
                var rt = Icon.rectTransform;
                float size = Root.sizeDelta.x;
                rt.anchoredPosition = new Vector2((size - boxPx) * 0.5f, -(size - boxPx) * 0.5f);
                rt.sizeDelta = new Vector2(boxPx, boxPx);
            }
        }

        /// <summary>Web bagButton: tone gradient, centred bold label; Dim = 45% alpha (still clickable, the click explains why).</summary>
        public sealed class BagButton
        {
            public readonly Image Bg, Glow;
            public readonly Text Label;
            readonly CanvasGroup group;
            readonly BagSprites.Tone tone;

            public BagButton(Transform parent, string name, float x, float y, float w, float h, string text, BagSprites.Tone tone, UnityAction onClick)
            {
                this.tone = tone;
                RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
                group = rt.gameObject.AddComponent<CanvasGroup>();
                Glow = Img(rt, "Glow", -10f, -10f, w + 20f, h + 20f, BagSprites.Glow(), Hex("#58caff"));
                Glow.enabled = false;
                Bg = Img(rt, "Bg", 0f, 0f, w, h, BagSprites.Button(tone), Color.white, true);
                Label = UiKit.Label(UiKit.Fill(rt, "Label"), text, 16, Hex("#f4f7ff"), TextAnchor.MiddleCenter, true);
                Clickable(rt.gameObject, Bg, onClick);
            }

            public RectTransform Root { get { return (RectTransform)Bg.transform.parent; } }

            public void Set(bool visible, string text = null, bool dim = false)
            {
                Root.gameObject.SetActive(visible);
                if (text != null) Label.text = text;
                group.alpha = dim ? 0.45f : 1f;
            }

            public void SetSelected(bool selected)
            {
                Glow.enabled = selected;
                Bg.sprite = BagSprites.Button(selected ? BagSprites.Tone.Blue : tone, selected);
            }
        }
    }
}
