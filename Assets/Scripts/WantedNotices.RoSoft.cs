using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// The Wanted banners in the RO Soft HUD (RoHud.Active; user 19/11 "design lại các thông báo có liên quan tới wanted"), built from the
    /// ChatGPT mockup docs/reference/ro-soft-hud/wanted-notices-mockup-chatgpt-1.png with sheet F (wanted-*) and the HUD kit, same triggers
    /// and priorities as the paper banners (WantedNotices.cs):
    ///   Login    ivory card + navy "Today's Wanted" ribbon: crowned portrait, name + Lv, map pin, the four bounty cells
    ///   Respawn  ivory banner with its portrait socket: face, "X has returned!"
    ///   Area     ivory pill: red crossed-swords shield, "A Wanted Boss lurks in this area", navy "32 m" pill with the arrow
    ///   Warning  soft coral hazard strip: alert triangle, "WARNING · WANTED BOSS"
    ///   Already  ivory pill: info icon, "Already claimed today - no bounty."
    /// Top centre under the menu row and the RO boss bar (RoHudBossBar.Bottom). Every text sits in its box's padding.
    /// </summary>
    public sealed partial class WantedNotices
    {
        const float RoTopMin = 96f; // under the menu row (MenuTop + plate + caption)
        RoWantedRewards roLoginRewards;

        RectTransform RoBanner(RectTransform safe, WantedNotice kind, float w, float h)
        {
            RectTransform rt = UiWinKit.Anchored(safe, kind + "NoticeRo", TopCentre, TopCentre, 0f, 0f, w, h);
            CanvasGroup g = rt.gameObject.AddComponent<CanvasGroup>();
            g.blocksRaycasts = false; g.interactable = false;
            banners[(int)kind] = rt; groups[(int)kind] = g;
            rt.gameObject.SetActive(false);
            NameTags.Occlude(rt); // IMGUI tags never draw over it
            return rt;
        }

        void BuildRoBanners(RectTransform safe)
        {
            Color ink = RoHud.Ink, coral = RoSoftTheme.Hex("#C8424C");

            // Login: card under a navy ribbon that covers the card's top 28: the content starts under it (round 2: 15-20 % bigger).
            const float LW = 430f, LH = 178f, CardTop = 24f, Body = CardTop + 32f;
            RectTransform login = RoBanner(safe, WantedNotice.Login, LW, LH);
            RoWantedWidgets.Plate(login, "Card", 0f, CardTop, LW, LH - CardTop, "wanted-card", 1.5f * (LH - CardTop)); // corners ~19 units
            loginFace = RoWantedWidgets.Portrait(login, "Face", 16f, Body - 6f, 100f, true);
            loginName = RoHud.Label(UiKit.Rect(login, "Name", 130f, Body, LW - 144f, 24f), "", 18, ink, TextAnchor.MiddleLeft);
            loginName.supportRichText = true;
            RoHud.Art(UiKit.Rect(login, "Pin", 130f, Body + 29f, 15f, 17f), "hud-icon-pin");
            loginMap = RoHud.Label(UiKit.Rect(login, "Map", 150f, Body + 27f, LW - 164f, 22f), "", 14, RoSoftTheme.InkSoft, TextAnchor.MiddleLeft, false);
            roLoginRewards = new RoWantedRewards(login, 130f, Body + 54f, 52f, 8f);
            RectTransform ribbon = UiKit.Rect(login, "Ribbon", (LW - 260f) * 0.5f, -4f, 260f, 260f * 64f / 274f);
            RoHud.Art(ribbon, "wanted-ribbon-navy");
            RoWantedWidgets.Centred(ribbon, "Today's Wanted", 16, Color.white, 60f, 10f, true); // clear of the two stars

            // Respawn: the banner's own round socket (its left end) holds the face.
            const float RW = 400f, RH = 62f;
            RectTransform respawn = RoBanner(safe, WantedNotice.Respawn, RW, RH);
            RoWantedWidgets.Plate(respawn, "Plate", 0f, 0f, RW, RH, "wanted-banner", 64f);
            respawnFace = RoWantedWidgets.Portrait(respawn, "Face", 1f, 1f, RH - 2f, false);
            respawnText = RoHud.Label(UiKit.Rect(respawn, "Text", RH + 12f, 0f, RW - RH - 38f, RH), "", 17, ink, TextAnchor.MiddleLeft);
            respawnText.resizeTextForBestFit = true; respawnText.resizeTextMinSize = 12; respawnText.resizeTextMaxSize = 17;

            // Area: ivory pill, shield on the left end, metres pill on the right end.
            const float AW = 440f, AH = 48f;
            RectTransform area = RoBanner(safe, WantedNotice.Area, AW, AH);
            RoWantedWidgets.Plate(area, "Plate", 0f, 0f, AW, AH, "hud-toast-line", 56f);
            RoHud.Art(UiKit.Rect(area, "Shield", 8f, -5f, 46f, 54f), "wanted-shield");
            RoHud.Label(UiKit.Rect(area, "Text", 62f, 0f, AW - 62f - 112f, AH), "A Wanted Boss lurks in this area", 15, ink, TextAnchor.MiddleLeft);
            RectTransform metres;
            areaDistance = RoHud.Pill(area, "Metres", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), -12f, 0f, 30f, "hud-lv-badge", "wanted-icon-arrow", 20f, "0 m", 15, Color.white, out metres, 84f);
            metres.GetComponent<Image>().pixelsPerUnitMultiplier = 44f / 30f;

            // Warning: hazard strip; the stripes on both ends stay clear of the text.
            const float WW = 460f, WH = 58f;
            RectTransform warning = RoBanner(safe, WantedNotice.Warning, WW, WH);
            RoWantedWidgets.Plate(warning, "Plate", 0f, 0f, WW, WH, "wanted-warning-strip", 56f);
            RoHud.Art(UiKit.Rect(warning, "Alert", 62f, 4f, 50f, 50f), "wanted-icon-alert");
            RoHud.Label(UiKit.Rect(warning, "Text", 120f, 0f, WW - 120f - 76f, WH), "WARNING  \u00B7  WANTED BOSS", 23, coral, TextAnchor.MiddleLeft, true, true, true);

            // Already: ivory pill with the info icon.
            const float AlW = 360f, AlH = 42f;
            RectTransform already = RoBanner(safe, WantedNotice.Already, AlW, AlH);
            RoWantedWidgets.Plate(already, "Plate", 0f, 0f, AlW, AlH, "hud-toast-line", 56f);
            RoHud.Art(UiKit.Rect(already, "Info", 38f, 5f, 32f, 32f), "wanted-icon-info");
            RoHud.Label(UiKit.Rect(already, "Text", 78f, 0f, AlW - 78f - 38f, AlH), "Already claimed today - no bounty.", 15, ink, TextAnchor.MiddleLeft, false);
        }

        /// <summary>Top of the RO notices: under the menu row, the RO boss bar and its cast / hint rows.</summary>
        static float RoNoticeTop { get { return Mathf.Max(RoTopMin, RoHudBossBar.Bottom + 6f); } }
    }
}
