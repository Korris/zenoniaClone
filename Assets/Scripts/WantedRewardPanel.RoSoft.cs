using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// "BOUNTY CLAIMED!" in the RO Soft HUD (mockup docs/reference/ro-soft-hud/wanted-notices-mockup-chatgpt-1.png panel 6): the ivory card,
    /// the red ribbon with laurels over its top, the crowned boss portrait between two laurel branches, "X defeated", the navy
    /// "Daily Bounty" pill, the four bounty cells, the mint OK and the sky "Wanted Board" buttons (labels centred in their padding).
    /// </summary>
    public sealed partial class WantedRewardPanel
    {
        const float RoW = 330f, RoH = 288f, RoBtnH = 38f;

        void BuildRo(RectTransform safe)
        {
            RectTransform panel = UiKit.Rect(safe, "RoPanel", 0f, 0f, RoW, RoH);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(0f, -20f);
            RoWantedWidgets.Plate(panel, "Card", 0f, 0f, RoW, RoH, "wanted-card", 1.4f * RoH, true);

            RoHud.Art(UiKit.Rect(panel, "LaurelL", RoW * 0.5f - 86f, 64f, 34f, 54f), "wanted-laurel");
            Image right = RoHud.Art(UiKit.Rect(panel, "LaurelR", RoW * 0.5f + 52f + 34f, 64f, 34f, 54f), "wanted-laurel");
            right.rectTransform.localScale = new Vector3(-1f, 1f, 1f); // the left branch mirrored about its top-left pivot
            face = RoWantedWidgets.Portrait(panel, "Face", (RoW - 84f) * 0.5f, 44f, 84f, true);
            nameText = RoHud.Label(UiKit.Rect(panel, "Name", 18f, 134f, RoW - 36f, 24f), "", 16, RoHud.Ink, TextAnchor.MiddleCenter);
            nameText.resizeTextForBestFit = true; nameText.resizeTextMinSize = 11; nameText.resizeTextMaxSize = 16;
            RectTransform tag;
            RoHud.Pill(panel, "Daily", new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), 0f, -172f, 24f, "hud-party-title", "wanted-sparkle", 12f, "Daily Bounty", 12, Color.white, out tag, 120f);
            roRewards = new RoWantedRewards(panel, (RoW - (4f * 46f + 3f * 8f)) * 0.5f, 188f, 46f, 8f);

            Button("Ok", 22f, 104f, "btn-mint", "OK", Close, panel);
            Button("Board", 136f, RoW - 158f, "btn-sky", "Wanted Board", () => { Close(); UiScreens.Open(UiScreen.Wanted); }, panel);

            RectTransform ribbon = UiKit.Rect(panel, "Ribbon", (RoW - 300f) * 0.5f, -58f, 300f, 300f * 120f / 251f);
            RoHud.Art(ribbon, "wanted-ribbon-red");
            RectTransform band = UiKit.Rect(ribbon, "Band", 56f, 22f, 300f - 112f, 46f); // the flat band between the laurels
            RoWantedWidgets.Centred(band, "BOUNTY CLAIMED!", 19, Color.white, 4f, 4f, true);
        }

        void Button(string name, float x, float w, string piece, string label, System.Action onTap, RectTransform panel)
        {
            Image b = RoWantedWidgets.Plate(panel, name, x, RoH - 20f - RoBtnH, w, RoBtnH, piece, 1.6f * RoBtnH, true);
            UiWinKit.MakeButton(b, onTap);
            RoWantedWidgets.Centred(b.rectTransform, label, 15, Color.white, 14f, 6f, true);
        }
    }
}
