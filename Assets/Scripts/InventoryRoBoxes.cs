using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using W = ZenoniaProto.InventoryRoWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// The bag's tabs, pills and icon buttons laid out like CSS boxes (docs/ui-layout-rules.md): a ChatGPT 9-slice skin, then a LayoutGroup
    /// gives padding, icon-text gap and vertical centring; pills size to their text (ContentSizeFitter) with radius = half the height.
    /// Every number is a constant below, none is placed by hand at the call sites.
    /// </summary>
    public static class InventoryRoBoxes
    {
        // tab (icon over text): padding 8 top / 8 bottom / 6 sides, gap 2, icon 28, text 13
        public const float TabH = 62f, TabPadV = 8f, TabPadH = 6f, TabGap = 2f, TabIcon = 28f, TabTextH = 16f;
        public const int TabText = 13;
        // button (icon left of text): padding 10 / 10, gap 5, icon 20 (three buttons share a 320-wide row, English labels are long)
        public const float BtnPadV = 10f, BtnPadH = 10f, BtnGap = 5f, BtnIcon = 20f;
        // pill: padding 3 / 12, radius = half height (the pill art's whole height is mapped onto the pill)
        public const float PillPadV = 3f, PillPadH = 12f;
        /// <summary>Corner scale of the bag-pill-* tab skins: corners at half the art size, so a 62-high tab gets ~12 px rounding as in the mockup.</summary>
        const float TabCornerScale = 2f;

        public struct PillBox { public RectTransform Root; public Image Skin; public Text Label; }

        /// <summary>
        /// A pill sized to its text. (x, y) = top-left, or top-centre when `centreX`. `minWidth` keeps short words from making a dot.
        /// The pill art is scaled so its full height fits `h`: the ends stay half circles at every height.
        /// </summary>
        public static PillBox Pill(Transform parent, string name, float x, float y, float h, string piece, string text, int size, Color textColor, float minWidth = 0f, bool centreX = false)
        {
            PillBox p;
            p.Root = UiKit.Rect(parent, name, x, y, minWidth, h);
            if (centreX) { p.Root.pivot = new Vector2(0.5f, 1f); p.Root.anchoredPosition = new Vector2(x, -y); }
            p.Skin = p.Root.gameObject.AddComponent<Image>();
            p.Skin.raycastTarget = false;
            W.SetArt(p.Skin, piece);
            Sprite s = p.Skin.sprite;
            if (s != null) p.Skin.pixelsPerUnitMultiplier = s.rect.height / (2f * h); // 2 art px per canvas unit at multiplier 1
            var row = p.Root.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset((int)PillPadH, (int)PillPadH, (int)PillPadV, (int)PillPadV);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var fit = p.Root.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            p.Root.gameObject.AddComponent<LayoutElement>().minWidth = minWidth;
            p.Label = W.Txt(p.Root, "Label", 0f, 0f, 10f, h - 2f * PillPadV, text, size, textColor, TextAnchor.MiddleCenter);
            p.Label.gameObject.AddComponent<LayoutElement>().minHeight = h - 2f * PillPadV;
            row.childControlHeight = false; // the row height is the pill's: the label is centred in it
            p.Label.rectTransform.sizeDelta = new Vector2(10f, h - 2f * PillPadV);
            return p;
        }

        /// <summary>A category tab: icon over text, centred as one block (equal space above and below), mint when on.</summary>
        public sealed class Tab
        {
            public readonly Image Skin, Icon;
            public readonly Text Label;

            public Tab(Transform parent, string name, float x, float y, float w, string icon, string text, UnityAction onClick)
            {
                RectTransform root = UiKit.Rect(parent, name, x, y, w, TabH);
                Skin = root.gameObject.AddComponent<Image>();
                W.SetArt(Skin, "bag-pill-white");
                Skin.pixelsPerUnitMultiplier = TabCornerScale;
                var col = root.gameObject.AddComponent<VerticalLayoutGroup>();
                col.padding = new RectOffset((int)TabPadH, (int)TabPadH, (int)TabPadV, (int)TabPadV);
                col.spacing = TabGap;
                col.childAlignment = TextAnchor.MiddleCenter;
                col.childControlWidth = col.childControlHeight = true;
                col.childForceExpandWidth = col.childForceExpandHeight = false;
                Icon = W.Img(root, "Icon", 0f, 0f, TabIcon, TabIcon, icon);
                Fixed(Icon.gameObject, TabIcon, TabIcon);
                Label = W.Txt(root, "Label", 0f, 0f, w - 2f * TabPadH, TabTextH, text, TabText, W.Ink, TextAnchor.MiddleCenter);
                Fixed(Label.gameObject, w - 2f * TabPadH, TabTextH);
                Label.resizeTextForBestFit = true; // "Consumable" in a 68-wide tab: 13 shrinks to fit, never clipped
                Label.resizeTextMinSize = 9; Label.resizeTextMaxSize = TabText;
                Label.horizontalOverflow = HorizontalWrapMode.Wrap; Label.verticalOverflow = VerticalWrapMode.Truncate;
                W.Clickable(Skin, onClick);
            }

            public void Set(bool on)
            {
                W.SetArt(Skin, on ? "bag-pill-mint" : "bag-pill-white");
                Skin.pixelsPerUnitMultiplier = TabCornerScale;
                Icon.color = on ? Color.white : W.IconSlate;
                Label.color = on ? Color.white : W.Ink;
            }
        }

        /// <summary>Kit button (btn-mint / btn-sky / btn-coral ...) with an optional white icon, laid out as one centred row; Dim = 50 % alpha, still tappable.</summary>
        public sealed class IconButton
        {
            public readonly RectTransform Root;
            public readonly Text Label;
            readonly CanvasGroup group;
            readonly int baseSize;
            readonly bool hasIcon;

            public IconButton(Transform parent, string name, float x, float y, float w, float h, string piece, string icon, string text, int size, UnityAction onClick)
            {
                Image face = W.Img(parent, name, x, y, w, h, piece, true);
                Root = face.rectTransform;
                baseSize = size;
                hasIcon = icon != null;
                var row = Root.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.padding = new RectOffset((int)BtnPadH, (int)BtnPadH, (int)BtnPadV, (int)(BtnPadV + 2f)); // +2 below: the art's lower rim is thicker
                row.spacing = BtnGap;
                row.childAlignment = TextAnchor.MiddleCenter;
                row.childControlWidth = row.childControlHeight = true;
                row.childForceExpandWidth = row.childForceExpandHeight = false;
                if (hasIcon) Fixed(W.Img(Root, "Icon", 0f, 0f, BtnIcon, BtnIcon, icon).gameObject, BtnIcon, BtnIcon);
                Label = W.Txt(Root, "Label", 0f, 0f, 10f, h, text, size, Color.white, TextAnchor.MiddleCenter);
                Label.gameObject.AddComponent<Shadow>().effectColor = new Color(0.1f, 0.25f, 0.3f, 0.35f);
                Fit();
                group = Root.gameObject.AddComponent<CanvasGroup>();
                W.Clickable(face, onClick);
            }

            public void Set(bool visible, string text = null, bool dim = false)
            {
                Root.gameObject.SetActive(visible);
                if (text != null && text != Label.text) { Label.text = text; Fit(); }
                group.alpha = dim ? 0.5f : 1f;
            }

            /// <summary>A label too long for the button ("Unequip") steps its font down until icon + gap + text fit inside the padding.</summary>
            void Fit()
            {
                float room = Root.sizeDelta.x - 2f * BtnPadH - (hasIcon ? BtnIcon + BtnGap : 0f);
                Label.fontSize = baseSize;
                while (Label.fontSize > 9 && Label.preferredWidth > room) Label.fontSize--;
                LayoutRebuilder.MarkLayoutForRebuild(Root);
            }
        }

        static void Fixed(GameObject go, float w, float h)
        {
            LayoutElement e = go.AddComponent<LayoutElement>();
            e.minWidth = e.preferredWidth = w;
            e.minHeight = e.preferredHeight = h;
        }
    }
}
