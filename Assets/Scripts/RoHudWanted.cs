using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The WANTED card of the RO Soft HUD (02-hud.png), under the round minimap: the red "WANTED" tag pill on the ivory card, today's
    /// boss face in the gold frame with a small crown, its name, "Lv. N (Boss)" in coral, the pin + its map, and the round arrow. Data:
    /// WantedBossTracker (the server's WantedStatus online, the day's rotation offline). The coral line turns into "Respawns mm:ss" while
    /// the boss is dead and "Bounty claimed" once the hero was paid today. A tap opens the Wanted board (WantedBoardWindow).
    /// </summary>
    public sealed class RoHudWanted
    {
        const float W = RoHud.WantedW, H = RoHud.WantedH, Face = 64f, TextX = 86f;
        static readonly Color Coral = RoSoftTheme.Hex("#D0505E");

        readonly RectTransform root;
        readonly Image face;
        readonly Text bossName, level, mapName;
        string shownType;
        int shownSecond = -1;
        WantedPhase shownPhase = (WantedPhase)(-1);

        public RoHudWanted(RectTransform safe)
        {
            root = RoHud.Box(safe, "RoHudWanted", RoHud.TopRight, -(RoHud.Margin + W * 0.5f), -(RoHud.WantedTop + 8f + H * 0.5f), W, H);
            Image card = RoHud.Art(UiKit.Fill(root, "Card"), "hud-wanted-card", true);
            PaperHudPress.Tap(card.gameObject, WantedBoardWindow.Open);

            RectTransform tag;
            RoHud.Pill(root, "Tag", RoHud.TopLeft, new Vector2(0f, 0.5f), 6f, 0f, 22f, "hud-wanted-tag", null, 0f, "WANTED", 12, Color.white, out tag);

            RectTransform frame = RoHud.TopLeftBox(root, "Face", 11f, (H - Face) * 0.5f + 5f, Face, Face);
            RectTransform m = UiKit.Fill(frame, "Mask", 4f);
            Image mask = RoHud.Art(m, "hud-wanted-frame-mask");
            mask.preserveAspect = false;
            mask.color = PaperWanted.PortraitBack;
            m.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            face = UiKit.Fill(m, "Boss").gameObject.AddComponent<Image>();
            face.preserveAspect = true;
            face.raycastTarget = false;
            RoHud.Art(UiKit.Fill(frame, "Frame"), "hud-wanted-frame");
            RoHud.Art(RoHud.TopLeftBox(frame, "Crown", -5f, -7f, 18f, 18f), "hud-icon-crown-small");

            bossName = RoHud.Label(RoHud.TopLeftBox(root, "Name", TextX, 20f, W - TextX - 30f, 20f), "", 15, RoHud.Ink, TextAnchor.MiddleLeft);
            level = RoHud.Label(RoHud.TopLeftBox(root, "Level", TextX, 43f, W - TextX - 30f, 18f), "", 13, Coral, TextAnchor.MiddleLeft);
            RoHud.Art(RoHud.TopLeftBox(root, "Pin", TextX, 69f, 12f, 14f), "hud-icon-pin");
            mapName = RoHud.Label(RoHud.TopLeftBox(root, "Map", TextX + 15f, 67f, W - TextX - 46f, 18f), "", 12, RoHud.Ink, TextAnchor.MiddleLeft, false);
            RectTransform arrow = RoHud.Box(root, "Arrow", new Vector2(1f, 0.5f), -20f, -16f, 26f, 26f);
            RoHud.Art(arrow, "hud-btn-arrow-big");
            root.gameObject.SetActive(false);
        }

        /// <summary>Per frame while a hero plays; `visible` = the HUD column is shown (no window open).</summary>
        public void Tick(bool visible)
        {
            WantedBossDef def = visible ? WantedBossTracker.Today : null;
            bool on = def != null;
            if (root.gameObject.activeSelf != on) root.gameObject.SetActive(on);
            if (!on) return;
            if (def.BossType != shownType)
            {
                shownType = def.BossType;
                face.sprite = PaperWanted.BossFace(def.BossType) ?? PaperWanted.BossCell(def.BossType);
                face.enabled = face.sprite != null;
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
                level.text = "Respawns " + PaperWanted.Clock(s, s >= 3600);
                return;
            }
            if (phase == shownPhase) return;
            shownPhase = phase;
            level.text = phase == WantedPhase.Claimed ? "Lv. " + def.BossLevel + "  Bounty claimed" : "Lv. " + def.BossLevel + " (Boss)";
        }
    }
}
