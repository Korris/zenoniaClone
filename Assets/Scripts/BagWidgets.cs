using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// UGUI building blocks of the bag window: cell (dark backing, thin rarity edge, gold glow when selected, padlock), art button and text anchored by its align side.
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
            public Image Glow, Back, Edge, Icon, Lock;
            public Text Count;

            public Slot(Transform parent, string name, float x, float y, float size, UnityAction onClick)
            {
                Root = UiKit.Rect(parent, name, x, y, size, size);
                Glow = Img(Root, "Glow", -10f, -10f, size + 20f, size + 20f, BagSprites.Glow(), Hex("#ffd576"));
                Back = Img(Root, "Back", 0f, 0f, size, size, BagSprites.SlotBack(), Color.white, true);
                Edge = Img(Root, "Edge", 0f, 0f, size, size, BagSprites.SlotEdge(), Color.white);
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

            /// <summary>Enhance effect of the item drawn in this cell; call after SetIcon, null = none.</summary>
            public void SetEnhance(Rpg.Item item) { ItemEnhanceSlotFx.Show(Icon, item); }
        }

        /// <summary>Gold-and-navy art button with a centred bold label (Gold = btn-gold, the rest btn-navy); Dim = 45% alpha, still tappable: the tap explains why.</summary>
        public sealed class BagButton
        {
            public readonly Text Label;
            public readonly RectTransform Root;
            readonly CanvasGroup group;

            public BagButton(Transform parent, string name, float x, float y, float w, float h, string text, BagSprites.Tone tone, UnityAction onClick)
            {
                Button b = UiWinKit.ArtButton(parent, name, tone == BagSprites.Tone.Gold ? "btn-gold" : "btn-navy", x, y, w, h, text, 16, tone == BagSprites.Tone.Gold ? new Color(0.25f, 0.13f, 0.03f) : new Color(0.96f, 0.97f, 1f), () => onClick());
                Root = (RectTransform)b.transform;
                Label = b.GetComponentInChildren<Text>();
                group = Root.gameObject.AddComponent<CanvasGroup>();
            }

            public void Set(bool visible, string text = null, bool dim = false)
            {
                Root.gameObject.SetActive(visible);
                if (text != null) Label.text = text;
                group.alpha = dim ? 0.45f : 1f;
            }
        }
    }
}
