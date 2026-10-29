using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Tiny UGUI builder for the runtime UI (no prefabs): one screen-space canvas scaled like the IMGUI HUD (reference 960 x 640, match height,
    /// so 1 UI unit == 1 IMGUI unit at 640p), plus helpers for panels, labels, bars, icon cells and buttons. Positions are top-left based.
    /// </summary>
    public static class UiKit
    {
        public const float RefWidth = 960f, RefHeight = 640f;
        static Font font;
        static Canvas canvas;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>The shared overlay canvas (created on first use, with an EventSystem for clicks).</summary>
        public static Canvas Canvas(Transform parent)
        {
            if (canvas != null) return canvas;
            var go = new GameObject("UiCanvas");
            go.transform.SetParent(parent, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(parent, false);
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            return canvas;
        }

        public static void ResetState() { canvas = null; }

        /// <summary>RectTransform anchored at the parent's top-left (pos = offset right / down), or centred when `center`.</summary>
        public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h, bool center = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Vector2 a = center ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f);
            rt.anchorMin = a; rt.anchorMax = a; rt.pivot = a;
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>Stretches a child over its parent (with an inset).</summary>
        public static RectTransform Fill(Transform parent, string name, float inset = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image Panel(RectTransform rt, Color color, bool raycast = false)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Text Label(RectTransform rt, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
        {
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            var sh = rt.gameObject.AddComponent<Shadow>(); // readable on bright snow
            sh.effectColor = new Color(0f, 0f, 0f, 0.75f);
            sh.effectDistance = new Vector2(1f, -1f);
            return t;
        }

        public static Text Label(Transform parent, string name, float x, float y, float w, float h, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
        {
            return Label(Rect(parent, name, x, y, w, h), text, size, color, align, bold);
        }

        public static Image Icon(Transform parent, string name, float x, float y, float size)
        {
            Image img = Rect(parent, name, x, y, size, size).gameObject.AddComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public static Button Button(Transform parent, string name, float x, float y, float w, float h, string text, UnityEngine.Events.UnityAction onClick, out Text label)
        {
            RectTransform rt = Rect(parent, name, x, y, w, h);
            Image bg = Panel(rt, new Color(0.18f, 0.27f, 0.4f, 0.95f), true);
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            ColorBlock cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            b.colors = cb;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(() =>
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); // Space (attack) must never re-press it
                onClick();
            });
            label = Label(Fill(rt, "Label", 2f), text, 13, Color.white, TextAnchor.MiddleCenter, true);
            return b;
        }

        /// <summary>Horizontal bar: dark back, coloured fill (Set(frac)) and a centred caption.</summary>
        public sealed class Bar
        {
            public readonly RectTransform Root, FillRt;
            public readonly Image FillImage;
            public readonly Text Caption;

            public Bar(Transform parent, string name, float x, float y, float w, float h, Color fill, int fontSize)
            {
                Root = Rect(parent, name, x, y, w, h);
                Panel(Root, new Color(0.03f, 0.06f, 0.1f, 0.85f));
                FillRt = Fill(Root, "Fill", 1.5f);
                FillImage = Panel(FillRt, fill);
                FillRt.anchorMax = new Vector2(1f, 1f);
                Caption = fontSize > 0 ? Label(Fill(Root, "Caption"), "", fontSize, Color.white, TextAnchor.MiddleCenter, true) : null;
            }

            public void Set(float frac)
            {
                frac = Mathf.Clamp01(frac);
                FillRt.anchorMax = new Vector2(frac, 1f);
                FillImage.enabled = frac > 0f;
            }
        }
    }
}
