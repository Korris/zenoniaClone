using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Short messages (RpgHud.Toast(text, color): pickups, warnings) are a plain dark strip below the centre of the screen. Show(title, sub, icon) is the full
    /// top-centre banner in the win-toast frame with an optional icon slot, a title and a sub line. Messages queue (max 4) and each is shown at least ~1 s,
    /// fading in / out; a long queue shortens the current one. Runs on unscaled time so it keeps working while a dialogue pauses the sim.
    /// With UiScreens.UseNew(UiScreen.Hud) both kinds are the compact HUD's small dark pill ("Obtained: Iron Sword", hud-compact-mobile-v1.png):
    /// on mobile at the bottom centre, just above the chat lines; on PC at the top centre under the boss bar (AreaTop). Same API, same queue.
    /// </summary>
    public sealed partial class NotificationToast : MonoBehaviour
    {
        struct Entry { public string Title, Sub, Plate; public Sprite Icon; public Color TitleColor; } // Plate: an RO pill other than the default (ShowStyled)

        const float FadeIn = 0.15f, FadeOut = 0.45f, HoldFull = 2.8f, HoldCompact = 2.2f, MinHold = 1.0f;
        const int MaxQueue = 4;
        static readonly Color StripColor = new Color(0.03f, 0.06f, 0.1f, 0.75f);
        const float StripW = 420f, StripH = 28f, StripBelowCentre = 165f; // clear of the touch buttons along the bottom edge
        const float PillMaxW = 360f, PillMinW = 120f, PillLineH = 25f, PillFullH = 38f, PillPad = 22f; // 3.9 % of the screen height; the pill's round ends (and its stars) take half its height
        /// <summary>Mobile: bottom edge of the pill above the bottom of the safe area (the four chat lines end 106 units up on the mockup).</summary>
        const float PillAboveBottom = 124f;
        static NotificationToast instance;

        /// <summary>
        /// A notice of higher priority holds the toast area (WantedNotices: WARNING, a boss returning ...): the toast on screen is cut to its minimum
        /// and the queued ones wait until this is false again, so the two never overlap. Priority order: plans/.../phase-11 (Wanted notices > toasts).
        /// </summary>
        public static bool Suspended;

        /// <summary>Top of the toast area in units below the safe area's top: under the boss bar (RpgHud.ToastTop) and under the online target bar.</summary>
        public static float AreaTop { get { return Mathf.Max(RpgHud.ToastTop, TargetMarker.Bottom + 6f); } }

        readonly Queue<Entry> queue = new Queue<Entry>();
        RectTransform root, iconSlot, pill;
        bool paperShown;
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
            bool paper = UiScreens.UseNew(UiScreen.Hud);
            title.font = sub.font = paper ? PaperTheme.BoldFont : UiKit.Font;
            title.fontStyle = sub.fontStyle = paper ? FontStyle.Normal : FontStyle.Bold;
            bool ro = !paper && RoHud.Active; // RO Soft HUD: the ChatGPT-painted ivory toasts (NotificationToast.RoSoft.cs)
            paperShown = paper;
            roShown = ro;
            ShowRoParts(ro);
            if (ro) BeginRo(e); else if (paper) BeginPaper(e); else BeginOld(e);
            age = 0f;
            hold = e.Sub != null ? HoldFull : HoldCompact;
            root.gameObject.SetActive(true);
        }

        /// <summary>The pill: one centred line, or title + sub line, with the icon on its left end; as wide as its text.</summary>
        void BeginPaper(Entry e)
        {
            bool twoLines = !string.IsNullOrEmpty(e.Sub), hasIcon = e.Icon != null;
            float h = twoLines ? PillFullH : PillLineH, iconSize = h - 7f;
            title.text = e.Title; title.color = e.TitleColor;
            sub.text = twoLines ? e.Sub : "";
            title.fontSize = sub.fontSize = PaperTheme.BodySize;
            float tx = hasIcon ? PillPad * 0.5f + iconSize + 5f : PillPad;
            float tw = Mathf.Min(PillMaxW - tx - PillPad, Mathf.Max(title.preferredWidth, twoLines ? sub.preferredWidth : 0f) + 2f);
            float w = Mathf.Max(PillMinW, tx + tw + PillPad);
            if (w > tx + tw + PillPad) { tx += (w - tx - tw - PillPad) * 0.5f; } // a short text sits in the middle of the smallest pill
            root.sizeDelta = new Vector2(w, h);
            bool bottom = !HudLayout.IsWeb;
            Vector2 anchor = new Vector2(0.5f, bottom ? 0f : 1f);
            root.anchorMin = anchor; root.anchorMax = anchor; root.pivot = anchor;
            PlacePaper();
            frame.sprite = null; // the pill is its own child (the old frame is not shown)
            frame.enabled = false;
            if (pill == null)
            {
                pill = UiKit.Fill(root, "Pill");
                pill.SetAsFirstSibling();
                HudCompact.Capsule(pill, PaperPiece.HcToastPill, HudCompact.Dark, HudCompact.Rim, PillLineH * 0.5f);
            }
            pill.gameObject.SetActive(true);
            iconSlot.gameObject.SetActive(false);
            icon.transform.SetParent(root, false); // no slot frame on the pill: the icon sits straight on its left end
            icon.rectTransform.anchoredPosition = new Vector2(hasIcon ? tx - iconSize - 5f : 0f, -(h - iconSize) * 0.5f);
            icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            icon.sprite = e.Icon;
            icon.enabled = hasIcon;
            title.alignment = sub.alignment = TextAnchor.MiddleLeft;
            title.rectTransform.anchoredPosition = new Vector2(tx, -(twoLines ? 3f : 0f));
            title.rectTransform.sizeDelta = new Vector2(tw, twoLines ? 17f : h);
            sub.rectTransform.anchoredPosition = new Vector2(tx, -19f);
            sub.rectTransform.sizeDelta = new Vector2(tw, 16f);
        }

        /// <summary>Mobile: above the chat lines. PC: under the boss bar / target bar, which may appear while the pill shows.</summary>
        void PlacePaper()
        {
            float y = HudLayout.IsWeb ? -AreaTop : PillAboveBottom + (Screen.safeArea.yMin / Screen.height) * ((RectTransform)transform).rect.height;
            if (!Mathf.Approximately(root.anchoredPosition.y, y)) root.anchoredPosition = new Vector2(0f, y);
        }

        void BeginOld(Entry e)
        {
            if (icon.transform.parent != iconSlot) // back from the compact look
            {
                pill.gameObject.SetActive(false);
                frame.enabled = true;
                icon.transform.SetParent(iconSlot, false);
                icon.rectTransform.anchoredPosition = new Vector2(8f, -8f);
                icon.rectTransform.sizeDelta = new Vector2(38f, 38f);
                icon.enabled = true;
                sub.fontSize = 16;
            }
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
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (hold >= 0f)
            {
                age += dt;
                if (paperShown) PlacePaper();
                else if (roShown) PlaceRo();
                // a waiting message shortens the current one (but never below MinHold)
                float limit = Suspended ? MinHold : queue.Count > 0 ? Mathf.Max(MinHold, hold - queue.Count * 0.5f) : hold;
                group.alpha = Mathf.Min(Mathf.Clamp01(age / FadeIn), Mathf.Clamp01((limit + FadeOut - age) / FadeOut));
                if (age >= limit + FadeOut) { hold = -1f; root.gameObject.SetActive(false); }
            }
            if (hold < 0f && queue.Count > 0 && !Suspended) Begin(queue.Dequeue());
        }
    }
}
