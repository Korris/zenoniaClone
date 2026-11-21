using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Progress of a chest opening (1.2 s) or a warp channel (3 s), DungeonInteract: a navy-outlined white label over a 9-slice bar of the RO Soft kit
    /// (ChatGPT pieces hud-bar-track + bar-gold for a chest / hud-bar-teal for a warp), centred above the Talk button. Box model per docs/ui-layout-rules.md:
    /// bar 240 x 16, label 22 high, gap 4; the fill never drops under its two rounded ends (MinFill) so the 9-slice never squashes.
    /// </summary>
    public static class DungeonChannelBar
    {
        const float BarW = 240f, BarH = 16f, LabelH = 22f, Gap = 4f, Bottom = 128f, MinFill = 16f;
        static RectTransform root, fill;
        static Image fillImage;
        static Text label;
        static Sprite gold, teal;

        static bool Build()
        {
            if (root != null) return true;
            if (UiScreens.Root == null) return false;
            Transform canvas = UiKit.Canvas(UiScreens.Root).transform;
            root = UiWinKit.Anchored(canvas, "DungeonChannelBar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 0f, Bottom, BarW, BarH + Gap + LabelH);
            label = RoHud.Label(UiKit.Rect(root, "Label", 0f, 0f, BarW, LabelH), "", 16, Color.white, TextAnchor.MiddleCenter, true, true);
            RectTransform track = UiKit.Rect(root, "Track", 0f, LabelH + Gap, BarW, BarH);
            RoHud.Art(track, "hud-bar-track");
            fill = UiKit.Rect(track, "Fill", 0f, 0f, BarW, BarH);
            fillImage = RoHud.Art(fill, "bar-gold");
            gold = RoHud.Piece("bar-gold"); teal = RoHud.Piece("hud-bar-teal");
            root.gameObject.SetActive(false);
            return true;
        }

        public static void Show(string text, float progress, bool chest)
        {
            if (!Build()) return;
            if (!root.gameObject.activeSelf) { root.gameObject.SetActive(true); root.SetAsLastSibling(); }
            label.text = text;
            Sprite s = chest ? gold : teal;
            if (s != null && fillImage.sprite != s) fillImage.sprite = s;
            fill.sizeDelta = new Vector2(Mathf.Lerp(MinFill, BarW, progress), BarH);
            fill.gameObject.SetActive(progress > 0.01f);
        }

        public static void Hide() { if (root != null) root.gameObject.SetActive(false); }
    }
}
