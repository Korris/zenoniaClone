using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Helpers for the gold-and-navy window pieces (UiArt.Win): anchored rects, sliced / plain sprite images and art buttons.
    /// Sizes are canvas units (UiKit reference 960 x 640); `scale` of a sliced piece = canvas units per native pixel, so corners keep their look.
    /// </summary>
    public static class UiWinKit
    {
        public static readonly Color Gold = new Color(1f, 0.82f, 0.36f), Cream = new Color(0.93f, 0.9f, 0.8f), Dim = new Color(0.62f, 0.66f, 0.74f);

        /// <summary>Rect with its anchor / pivot on one point of the parent (e.g. (0.5,0) = bottom centre); x / y are offsets in canvas units (y up).</summary>
        public static RectTransform Anchored(Transform parent, string name, Vector2 anchor, Vector2 pivot, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>Sliced piece: `scale` canvas units per native pixel (0 = draw unsliced, stretched).</summary>
        public static Image Sliced(RectTransform rt, string piece, float scale, bool raycast = false)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = UiArt.Win(piece);
            img.raycastTarget = raycast;
            if (img.sprite != null && scale > 0f && img.sprite.border != Vector4.zero) { img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f / scale; }
            return img;
        }

        /// <summary>Unsliced piece keeping its aspect (icons, portrait, arrow).</summary>
        public static Image Plain(RectTransform rt, string piece, bool raycast = false)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = UiArt.Win(piece);
            img.preserveAspect = true;
            img.raycastTarget = raycast;
            return img;
        }

        /// <summary>Makes `img` a button (tint on hover / press, no keyboard navigation so Space / Enter never re-press it).</summary>
        public static Button MakeButton(Image img, Action onClick)
        {
            img.raycastTarget = true;
            Button b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            b.colors = cb;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(() =>
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                onClick();
            });
            return b;
        }

        /// <summary>Sliced art button (btn-gold / btn-navy) with a centred bold caption; corners scale with the button height.</summary>
        public static Button ArtButton(Transform parent, string name, string piece, float x, float y, float w, float h, string text, int fontSize, Color textColor, Action onClick)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            Sprite s = UiArt.Win(piece);
            Image img = Sliced(rt, piece, h / (s != null ? s.rect.height : 100f));
            UiKit.Label(UiKit.Fill(rt, "Label"), text, fontSize, textColor, TextAnchor.MiddleCenter, true);
            return MakeButton(img, onClick);
        }

        /// <summary>Round gold close button (win-close) at a top-left based position.</summary>
        public static Button CloseButton(Transform parent, float x, float y, float size, Action onClick)
        {
            return MakeButton(Plain(UiKit.Rect(parent, "Close", x, y, size, size), "close"), onClick);
        }
    }
}
