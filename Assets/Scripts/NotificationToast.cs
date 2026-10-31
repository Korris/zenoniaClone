using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Short messages (RpgHud.Toast(text, color): pickups, warnings) are a plain dark strip below the centre of the screen. Show(title, sub, icon) is the full
    /// top-centre banner in the win-toast frame with an optional icon slot, a title and a sub line. Messages queue (max 4) and each is shown at least ~1 s,
    /// fading in / out; a long queue shortens the current one. Runs on unscaled time so it keeps working while a dialogue pauses the sim.
    /// </summary>
    public sealed class NotificationToast : MonoBehaviour
    {
        struct Entry { public string Title, Sub; public Sprite Icon; public Color TitleColor; }

        const float FadeIn = 0.15f, FadeOut = 0.45f, HoldFull = 2.8f, HoldCompact = 2.2f, MinHold = 1.0f;
        const int MaxQueue = 4;
        static readonly Color StripColor = new Color(0.03f, 0.06f, 0.1f, 0.75f);
        const float StripW = 420f, StripH = 28f, StripBelowCentre = 165f; // clear of the touch buttons along the bottom edge
        static NotificationToast instance;

        readonly Queue<Entry> queue = new Queue<Entry>();
        RectTransform root, iconSlot;
        Image frame, icon;
        Sprite frameSprite;
        Text title, sub;
        CanvasGroup group;
        float age, hold = -1f;

        public static void Create(Transform canvas)
        {
            instance = UiKit.Fill(canvas, "NotificationToast").gameObject.AddComponent<NotificationToast>(); // full-screen layer: children anchor to the real screen
            instance.Build();
        }

        void OnDestroy() { if (instance == this) instance = null; }

        /// <summary>Compact one-line banner (pickups, warnings).</summary>
        public static void ShowLine(string text, Color color) { Enqueue(new Entry { Title = text, TitleColor = color }); }

        /// <summary>Full banner: icon (optional), title and a sub line ("Item Obtained!" / "Small HP Potion x1").</summary>
        public static void Show(string title, string subLine, Sprite iconSprite)
        {
            Enqueue(new Entry { Title = title, Sub = subLine ?? "", Icon = iconSprite, TitleColor = UiWinKit.Gold });
        }

        static void Enqueue(Entry e)
        {
            if (instance == null || string.IsNullOrEmpty(e.Title)) return;
            if (instance.queue.Count >= MaxQueue) instance.queue.Dequeue();
            instance.queue.Enqueue(e);
        }

        void Build()
        {
            root = UiWinKit.Anchored(transform, "Banner", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 0f, -50f, 360f, 92f);
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; group.interactable = false; group.alpha = 0f;
            frame = UiWinKit.Sliced(root, "toast", 0.57f);
            frameSprite = frame.sprite;
            iconSlot = UiKit.Rect(root, "IconSlot", 26f, 19f, 54f, 54f);
            UiWinKit.Sliced(iconSlot, "slot", 0.34f);
            icon = UiKit.Icon(iconSlot, "Icon", 8f, 8f, 38f);
            title = UiKit.Label(root, "Title", 90f, 16f, 250f, 28f, "", 20, UiWinKit.Gold, TextAnchor.MiddleLeft, true);
            sub = UiKit.Label(root, "Sub", 90f, 46f, 250f, 26f, "", 16, Color.white, TextAnchor.MiddleLeft, true);
            title.horizontalOverflow = sub.horizontalOverflow = HorizontalWrapMode.Overflow;
            root.gameObject.SetActive(false);
        }

        void Begin(Entry e)
        {
            bool full = e.Sub != null; // Show(...) always carries a sub line (possibly empty); ShowLine does not
            bool hasIcon = full && e.Icon != null;
            float w = full ? 360f : StripW, h = full ? 92f : StripH;
            root.sizeDelta = new Vector2(w, h);
            Vector2 anchor = full ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
            root.anchorMin = anchor; root.anchorMax = anchor; root.pivot = anchor;
            root.anchoredPosition = new Vector2(0f, full ? -50f : -StripBelowCentre);
            frame.sprite = full ? frameSprite : null;
            frame.type = full ? Image.Type.Sliced : Image.Type.Simple;
            frame.color = full ? Color.white : StripColor;
            frame.pixelsPerUnitMultiplier = 630f / w; // corners keep a constant on-screen size relative to the width
            iconSlot.gameObject.SetActive(hasIcon);
            icon.sprite = e.Icon;
            float tx = hasIcon ? 90f : 24f, tw = w - tx - 24f;
            title.text = e.Title; title.color = e.TitleColor;
            sub.text = full ? e.Sub : "";
            title.alignment = sub.alignment = hasIcon ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            title.fontSize = full ? 20 : 14;
            bool twoLines = full && !string.IsNullOrEmpty(e.Sub);
            title.rectTransform.anchoredPosition = new Vector2(tx, -(twoLines ? 16f : 0f));
            title.rectTransform.sizeDelta = new Vector2(tw, twoLines ? 28f : h);
            sub.rectTransform.anchoredPosition = new Vector2(tx, -46f);
            sub.rectTransform.sizeDelta = new Vector2(tw, 26f);
            age = 0f;
            hold = full ? HoldFull : HoldCompact;
            root.gameObject.SetActive(true);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (hold >= 0f)
            {
                age += dt;
                // a waiting message shortens the current one (but never below MinHold)
                float limit = queue.Count > 0 ? Mathf.Max(MinHold, hold - queue.Count * 0.5f) : hold;
                group.alpha = Mathf.Min(Mathf.Clamp01(age / FadeIn), Mathf.Clamp01((limit + FadeOut - age) / FadeOut));
                if (age >= limit + FadeOut) { hold = -1f; root.gameObject.SetActive(false); }
            }
            if (hold < 0f && queue.Count > 0) Begin(queue.Dequeue());
        }
    }
}
