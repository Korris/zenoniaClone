using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The WANTED card of the RO Soft HUD (02-hud.png), under the round minimap: the red "WANTED" tag pill on the ivory card, today's
    /// boss face in the gold frame with a small crown, its name, "Lv. N (Boss)" in coral, the pin + its map, and the round arrow. Data:
    /// WantedBossTracker (the server's WantedStatus online, the day's rotation offline). The coral line turns into "Respawns mm:ss" while
    /// the boss is dead and "Bounty claimed" once the hero was paid today. A tap on the card opens the Wanted board (WantedBoardWindow);
    /// the round arrow folds the card into a small tab at the screen edge (RoHudWanted.CollapseTab.cs).
    /// </summary>
    public sealed partial class RoHudWanted
    {
        const float W = RoHud.WantedW, H = RoHud.WantedH, Face = 64f, TextX = 86f;
        static readonly Color Coral = RoSoftTheme.Hex("#D0505E");

        readonly RectTransform root, body; // body = the card + respawn pill, slid off to the right when folded
        readonly Image face;
        readonly Text bossName, level, mapName, respawn;
        readonly GameObject respawnPill; // "Respawns 18:24" capsule with the clock under the card (mockup panel 9), only while the boss is down
        string shownType;
        int shownSecond = -1;
        WantedPhase shownPhase = (WantedPhase)(-1);

        public RoHudWanted(RectTransform safe)
        {
            root = RoHud.Box(safe, "RoHudWanted", RoHud.TopRight, -(RoHud.Margin + W * 0.5f), -(RoHud.WantedTop + 8f + H * 0.5f), W, H);
            body = UiKit.Fill(root, "Body");
            bodyGroup = body.gameObject.AddComponent<CanvasGroup>();
            Image card = RoHud.Art(UiKit.Fill(body, "WantedCard"), "hud-wanted-card", true);
            PaperHudPress.Tap(card.gameObject, WantedBoardWindow.Open);

            RectTransform tag;
            RoHud.Pill(body, "Tag", RoHud.TopLeft, new Vector2(0f, 0.5f), 6f, 0f, 22f, "hud-wanted-tag", null, 0f, "WANTED", 12, Color.white, out tag);
            face = FaceFrame(body, 11f, (H - Face) * 0.5f + 5f, Face);

            bossName = RoHud.Label(RoHud.TopLeftBox(body, "Name", TextX, 20f, W - TextX - 30f, 20f), "", 15, RoHud.Ink, TextAnchor.MiddleLeft);
            level = RoHud.Label(RoHud.TopLeftBox(body, "Level", TextX, 43f, W - TextX - 30f, 18f), "", 13, Coral, TextAnchor.MiddleLeft);
            RoHud.Art(RoHud.TopLeftBox(body, "Pin", TextX, 69f, 12f, 14f), "hud-icon-pin");
            mapName = RoHud.Label(RoHud.TopLeftBox(body, "Map", TextX + 15f, 67f, W - TextX - 46f, 18f), "", 12, RoHud.Ink, TextAnchor.MiddleLeft, false);
            BuildFoldArrow();
            RectTransform pill;
            respawn = RoHud.Pill(body, "Respawn", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), 0f, -6f, 32f, "hud-toast-line", "wanted-icon-clock", 20f, "Respawns 00:00", 14, RoHud.Ink, out pill, 170f);
            pill.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(26, 26, 3, 3); // clear of the stars on the pill ends
            pill.GetComponent<Image>().pixelsPerUnitMultiplier = 56f / 32f;
            respawnPill = pill.gameObject;
            respawnPill.SetActive(false);
            NameTags.Occlude(body);
            BuildTab();
            root.gameObject.SetActive(false);
        }

        /// <summary>Per frame while a hero plays; `visible` = the HUD column is shown (no window open).</summary>
        public void Tick(bool visible)
        {
            WantedBossDef def = visible ? WantedBossTracker.Today : null;
            bool on = def != null;
            if (root.gameObject.activeSelf != on) root.gameObject.SetActive(on);
            if (!on) return;
            AnimateFold();
            if (def.BossType != shownType)
            {
                shownType = def.BossType;
                face.sprite = PaperWanted.BossFace(def.BossType) ?? PaperWanted.BossCell(def.BossType);
                face.enabled = face.sprite != null;
                tabFace.sprite = face.sprite;
                tabFace.enabled = face.enabled;
                bossName.text = def.BossName;
                mapName.text = WantedBossTracker.MapName(def);
                shownPhase = (WantedPhase)(-1);
            }
            WantedPhase phase = WantedBossTracker.Status.Known ? PaperWanted.Phase : PaperWanted.Claimed ? WantedPhase.Claimed : WantedPhase.Alive;
            if (phase == WantedPhase.Respawning)
            {
                int s = PaperWanted.Seconds(WantedBossTracker.RespawnLeftMs);
                if (s == shownSecond && phase == shownPhase) return;
                shownSecond = s;
                shownPhase = phase;
                respawn.text = "Respawns <color=#C8424C>" + PaperWanted.Clock(s, s >= 3600) + "</color>";
                if (!respawnPill.activeSelf) { respawnPill.SetActive(true); level.text = "Lv. " + def.BossLevel + " (Boss)"; }
                return;
            }
            if (phase == shownPhase) return;
            shownPhase = phase;
            respawnPill.SetActive(false);
            level.text = phase == WantedPhase.Claimed ? "Lv. " + def.BossLevel + "  Bounty claimed" : "Lv. " + def.BossLevel + " (Boss)";
        }
    }
}
