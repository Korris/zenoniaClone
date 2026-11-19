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

            // Login: card 360 x 112 under a navy ribbon that overhangs its top by 18.
            const float LW = 360f, LH = 130f, CardTop = 18f;
            RectTransform login = RoBanner(safe, WantedNotice.Login, LW, LH);
            RoWantedWidgets.Plate(login, "Card", 0f, CardTop, LW, LH - CardTop, "wanted-card", 1.5f * (LH - CardTop)); // corners ~19 units
            loginFace = RoWantedWidgets.Portrait(login, "Face", 14f, CardTop + 14f, 84f, true);
            loginName = RoHud.Label(UiKit.Rect(login, "Name", 112f, CardTop + 14f, LW - 126f, 22f), "", 16, ink, TextAnchor.MiddleLeft);
            loginName.supportRichText = true;
            RoHud.Art(UiKit.Rect(login, "Pin", 112f, CardTop + 40f, 14f, 16f), "hud-icon-pin");
            loginMap = RoHud.Label(UiKit.Rect(login, "Map", 130f, CardTop + 38f, LW - 144f, 20f), "", 13, RoSoftTheme.InkSoft, TextAnchor.MiddleLeft, false);
            roLoginRewards = new RoWantedRewards(login, 112f, CardTop + 60f, 36f, 6f);
            RectTransform ribbon = UiKit.Rect(login, "Ribbon", (LW - 250f) * 0.5f, -10f, 250f, 250f * 64f / 274f);
            RoHud.Art(ribbon, "wanted-ribbon-navy");
            RoWantedWidgets.Centred(ribbon, "Today's Wanted", 15, Color.white, 58f, 10f, true); // clear of the two stars

            // Respawn: the banner's own round socket (its left end) holds the face.
            const float RW = 360f, RH = 54f;
            RectTransform respawn = RoBanner(safe, WantedNotice.Respawn, RW, RH);
            RoWantedWidgets.Plate(respawn, "Plate", 0f, 0f, RW, RH, "wanted-banner", 64f);
            respawnFace = RoWantedWidgets.Portrait(respawn, "Face", 1f, 1f, RH - 2f, false);
            respawnText = RoHud.Label(UiKit.Rect(respawn, "Text", RH + 10f, 0f, RW - RH - 34f, RH), "", 15, ink, TextAnchor.MiddleLeft);
            respawnText.resizeTextForBestFit = true; respawnText.resizeTextMinSize = 11; respawnText.resizeTextMaxSize = 15;

            // Area: ivory pill, shield on the left end, metres pill on the right end.
            const float AW = 392f, AH = 42f;
            RectTransform area = RoBanner(safe, WantedNotice.Area, AW, AH);
            RoWantedWidgets.Plate(area, "Plate", 0f, 0f, AW, AH, "hud-toast-line", 56f);
            RoHud.Art(UiKit.Rect(area, "Shield", 8f, -4f, 40f, 46f), "wanted-shield");
            RoHud.Label(UiKit.Rect(area, "Text", 56f, 0f, AW - 56f - 104f, AH), "A Wanted Boss lurks in this area", 13, ink, TextAnchor.MiddleLeft);
            RectTransform metres;
            areaDistance = RoHud.Pill(area, "Metres", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), -10f, 0f, 26f, "hud-lv-badge", "wanted-icon-arrow", 14f, "0 m", 12, Color.white, out metres, 64f);

            // Warning: hazard strip; the stripes on both ends stay clear of the text.
            const float WW = 400f, WH = 46f;
            RectTransform warning = RoBanner(safe, WantedNotice.Warning, WW, WH);
            RoWantedWidgets.Plate(warning, "Plate", 0f, 0f, WW, WH, "wanted-warning-strip", 56f);
            RoHud.Art(UiKit.Rect(warning, "Alert", 52f, 4f, 38f, 38f), "wanted-icon-alert");
            RoHud.Label(UiKit.Rect(warning, "Text", 96f, 0f, WW - 96f - 66f, WH), "WARNING  ·  WANTED BOSS", 17, coral, TextAnchor.MiddleLeft, true, true, true);

            // Already: ivory pill with the info icon.
            const float AlW = 330f, AlH = 36f;
            RectTransform already = RoBanner(safe, WantedNotice.Already, AlW, AlH);
            RoWantedWidgets.Plate(already, "Plate", 0f, 0f, AlW, AlH, "hud-toast-line", 56f);
            RoHud.Art(UiKit.Rect(already, "Info", 36f, 5f, 26f, 26f), "wanted-icon-info");
            RoHud.Label(UiKit.Rect(already, "Text", 68f, 0f, AlW - 68f - 36f, AlH), "Already claimed today - no bounty.", 13, ink, TextAnchor.MiddleLeft, false);
        }

        /// <summary>Top of the RO notices: under the menu row, the RO boss bar and its cast / hint rows.</summary>
        static float RoNoticeTop { get { return Mathf.Max(RoTopMin, RoHudBossBar.Bottom + 6f); } }
    }
}
