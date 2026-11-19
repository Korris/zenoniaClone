using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The Wanted Board in the RO Soft HUD (mockup docs/reference/ro-soft-hud/wanted-notices-mockup-chatgpt-1.png panel 10), opened instead of
    /// the full-screen paper board (WantedBoardWindow) while RoHud.Active: one ivory card in the middle of the screen over a soft dim.
    ///   header   navy "Wanted Board" ribbon, round close X
    ///   poster   the boss face in the WANTED frame with its red tag; name, "Lv. N" coral, map pin + map; the status pill: Available (mint) /
    ///            Respawns in mm:ss (coral) / Claimed today (grey); Show on Map (sky button: WantedGuide, closes the board)
    ///   Rewards  section strip + the four bounty cells
    ///   Skills   section strip + one row per boss skill: medal (alert for the enrage skill), name, the dodge hint
    ///   footer   "Next wanted in hh:mm:ss (Change at 05:00)"
    /// Same data as the paper board (WantedBossTracker), nothing sent. Esc / the X / a tap on the dim closes it.
    /// </summary>
    public sealed class RoWantedBoard : MonoBehaviour
    {
        const float W = 420f, H = 540f, Pad = 18f, SkillRowH = 46f; // H: poster + rewards + 4 skill rows + footer, nothing overlaps
        const int SkillRows = 4;
        static RoWantedBoard instance;

        Image face;
        Text nameText, levelText, mapText, statusText, footer;
        Image statusPill;
        RoWantedRewards rewards;
        readonly Text[] skillNames = new Text[SkillRows], skillHints = new Text[SkillRows];
        readonly Image[] skillMedals = new Image[SkillRows];
        readonly GameObject[] skillRows = new GameObject[SkillRows];
        int shownVersion = -1, shownSecond = -1;
        WantedPhase shownPhase;

        public static bool IsOpen { get { return instance != null && instance.gameObject.activeSelf; } }

        public static void Open()
        {
            if (!PaperWanted.Known) return;
            if (instance == null)
            {
                PaperLayout l = PaperLayout.CreateSafeArea(UiKit.Canvas(UiScreens.Root).transform, "RoWantedBoard");
                instance = l.gameObject.AddComponent<RoWantedBoard>();
                instance.Build(l);
            }
            instance.gameObject.SetActive(true);
            instance.transform.SetAsLastSibling();
            instance.shownVersion = -1;
            instance.Refresh();
        }

        public static void Close() { if (IsOpen) instance.gameObject.SetActive(false); }

        void OnDestroy() { if (instance == this) instance = null; }

        void Build(PaperLayout l)
        {
            Image dim = UiKit.Fill(l.Root, "Dim").gameObject.AddComponent<Image>();
            dim.color = new Color(0.12f, 0.16f, 0.24f, 0.35f);
            UiWinKit.MakeButton(dim, Close);
            dim.transform.SetAsFirstSibling();
            RectTransform card = UiKit.Rect(l.Safe, "Card", 0f, 0f, W, H);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = new Vector2(0f, -8f);
            RoWantedWidgets.Plate(card, "Plate", 0f, 0f, W, H, "wanted-card", 1.3f * H, true);

            RectTransform ribbon = UiKit.Rect(card, "Ribbon", (W - 240f) * 0.5f, -22f, 240f, 240f * 64f / 274f);
            RoHud.Art(ribbon, "wanted-ribbon-navy");
            RoWantedWidgets.Centred(ribbon, "Wanted Board", 16, Color.white, 44f, 10f, true);
            UiWinKit.MakeButton(RoHud.Art(UiKit.Rect(card, "Close", W - 44f, -8f, 40f, 40f), "hud-chat-close", true), Close);

            // Poster: the WANTED frame with the face, the red tag over its top edge.
            const float PosterY = 46f, PosterS = 130f;
            RectTransform frame = UiKit.Rect(card, "Poster", Pad, PosterY, PosterS, PosterS);
            RectTransform fm = UiKit.Fill(frame, "Mask", PosterS * 0.1f);
            Image mask = RoHud.Art(fm, "hud-wanted-frame-mask"); mask.preserveAspect = false; mask.color = RoSoftTheme.SkyMist;
            fm.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            face = UiKit.Fill(fm, "Face").gameObject.AddComponent<Image>(); face.preserveAspect = true; face.raycastTarget = false;
            RoHud.Art(UiKit.Fill(frame, "Frame"), "hud-wanted-frame");
            RectTransform tag;
            RoHud.Pill(frame, "Tag", new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), 0f, 0f, 22f, "hud-wanted-tag", null, 0f, "WANTED", 12, Color.white, out tag);

            float tx = Pad + PosterS + 14f, tw = W - tx - Pad;
            nameText = RoHud.Label(UiKit.Rect(card, "Name", tx, PosterY + 2f, tw, 22f), "", 19, RoHud.Ink, TextAnchor.MiddleLeft);
            nameText.resizeTextForBestFit = true; nameText.resizeTextMinSize = 13; nameText.resizeTextMaxSize = 19;
            levelText = RoHud.Label(UiKit.Rect(card, "Level", tx, PosterY + 25f, tw, 18f), "", 14, RoSoftTheme.Hex("#C8424C"), TextAnchor.MiddleLeft);
            RoHud.Art(UiKit.Rect(card, "Pin", tx, PosterY + 48f, 13f, 15f), "hud-icon-pin");
            mapText = RoHud.Label(UiKit.Rect(card, "Map", tx + 18f, PosterY + 46f, tw - 18f, 18f), "", 13, RoSoftTheme.InkSoft, TextAnchor.MiddleLeft, false);
            statusPill = RoWantedWidgets.Plate(card, "Status", tx, PosterY + 70f, 150f, 26f, "hud-channel-pill", 48f);
            statusText = RoWantedWidgets.Centred(statusPill.rectTransform, "", 13, Color.white, RoHud.PillPadX, RoHud.PillPadY, true);
            Image guide = RoWantedWidgets.Plate(card, "ShowOnMap", tx, PosterY + 100f, 150f, 30f, "btn-sky", 1.6f * 30f, true); // under the status pill: the label never wraps
            UiWinKit.MakeButton(guide, () => { WantedGuide.On = !WantedGuide.On; if (WantedGuide.On) Close(); });
            RoWantedWidgets.Centred(guide.rectTransform, "Show on Map", 12, Color.white, 8f, 4f, true);

            float y = PosterY + PosterS + 14f;
            Section(card, "Rewards", "hud-icon-gacha", y);
            rewards = new RoWantedRewards(card, Pad + 10f, y + 32f, 52f, 10f);
            y += 32f + 52f + 10f;
            Section(card, "Boss Skills", "hud-icon-skull", y);
            y += 32f;
            for (int i = 0; i < SkillRows; i++)
            {
                RectTransform row = UiKit.Rect(card, "Skill" + i, Pad + 4f, y + i * SkillRowH, W - Pad * 2f - 8f, SkillRowH - 4f);
                skillRows[i] = row.gameObject;
                skillMedals[i] = RoHud.Art(UiKit.Rect(row, "Medal", 0f, 3f, 36f, 36f), "hud-icon-skull");
                skillNames[i] = RoHud.Label(UiKit.Rect(row, "Name", 46f, 1f, row.sizeDelta.x - 50f, 18f), "", 13, RoHud.Ink, TextAnchor.MiddleLeft);
                skillHints[i] = RoHud.Label(UiKit.Rect(row, "Hint", 46f, 19f, row.sizeDelta.x - 50f, 22f), "", 11, RoSoftTheme.InkSoft, TextAnchor.UpperLeft, false);
                skillHints[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                skillHints[i].resizeTextForBestFit = true; skillHints[i].resizeTextMinSize = 9; skillHints[i].resizeTextMaxSize = 11;
            }
            footer = RoHud.Label(UiKit.Rect(card, "Footer", Pad, H - Pad - 18f, W - Pad * 2f, 18f), "", 12, RoSoftTheme.InkSoft, TextAnchor.MiddleCenter, false);
        }

        static void Section(RectTransform card, string title, string icon, float y)
        {
            Image strip = RoWantedWidgets.Plate(card, title + "Strip", Pad, y, W - Pad * 2f, 26f, "hud-chat-line", 36f);
            RoHud.Art(UiKit.Rect(strip.rectTransform, "Icon", 10f, 4f, 18f, 18f), icon);
            RoHud.Label(UiKit.Rect(strip.rectTransform, "Title", 34f, 0f, 200f, 26f), title, 14, RoHud.Ink, TextAnchor.MiddleLeft);
        }

        void Update()
        {
            if (!PaperWanted.Known || InputAdapter.Pressed(GameKey.Close)) { Close(); return; }
            Refresh();
        }

        void Refresh()
        {
            WantedBossDef today = WantedBossTracker.Today;
            if (today == null) return;
            WantedPhase phase = PaperWanted.Phase;
            if (WantedBossTracker.Status.Version != shownVersion || phase != shownPhase)
            {
                shownVersion = WantedBossTracker.Status.Version; shownPhase = phase; shownSecond = -1;
                PaperWanted.SetFace(face, today.BossType);
                nameText.text = today.BossName;
                levelText.text = "Lv. " + today.BossLevel;
                mapText.text = WantedBossTracker.MapName(today);
                rewards.Set(today);
                statusPill.sprite = RoHud.Piece(phase == WantedPhase.Alive ? "hud-channel-pill" : phase == WantedPhase.Respawning ? "hud-toast-warn" : "hud-chat-tab");
                statusText.color = phase == WantedPhase.Claimed ? RoHud.Ink : Color.white;
                WantedSkillDef[] skills = WantedSkillTable.For(today.BossType);
                for (int i = 0; i < SkillRows; i++)
                {
                    bool has = skills != null && i < skills.Length;
                    skillRows[i].SetActive(has);
                    if (!has) continue;
                    skillNames[i].text = skills[i].Name + (skills[i].Enrage ? "  <color=#C8424C>(Below " + Mathf.RoundToInt(WantedSkillClock.EnrageHpFraction * 100f) + "% HP)</color>" : "");
                    skillHints[i].text = skills[i].Hint ?? "";
                    skillMedals[i].sprite = RoHud.Piece(skills[i].Enrage ? "wanted-icon-alert" : "hud-icon-skull");
                }
            }
            int reset = PaperWanted.Seconds(WantedBossTracker.ResetLeftMs), respawn = PaperWanted.Seconds(WantedBossTracker.RespawnLeftMs);
            int second = reset * 4096 + respawn % 4096;
            if (second == shownSecond) return;
            shownSecond = second;
            statusText.text = phase == WantedPhase.Alive ? "Available" : phase == WantedPhase.Respawning ? "Respawns in " + PaperWanted.Clock(respawn, false) : "Claimed today";
            footer.text = "Next wanted in " + PaperWanted.Clock(reset, true) + "  (Change at 05:00)";
        }
    }
}
