using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The Wanted pill of the compact HUD (hud-compact-parts-board-states-v2.png, section 6; about 5.5 % of the screen height; place:
    /// HudAnchors.WantedCardTopRight, under the row of menu icons): a dark see-through capsule with the boss's face in a ring on its left end
    /// and one of three states
    ///   ALIVE + the countdown to the next boss  |  Respawns + the countdown to its return  |  CLAIMED
    /// A tap opens the Wanted board (UiScreens). Always shown while the status is known; it never shows how many players are fighting.
    /// The text is rebuilt only when the state or the second changed.
    /// </summary>
    public sealed class WantedHudCard
    {
        const float W = 156f, H = 35f, IconSize = 15f, TextX = H + 4f + IconSize + 3f, RingHole = 0.085f;
        const string ClockColor = "<color=#F9E384>";

        public readonly RectTransform Root;
        readonly Image face, stateIcon;
        readonly GameObject tick; // flat stand-in for the CLAIMED icon
        readonly Text text;
        int shownVersion = -1, shownSecond = -1;
        WantedPhase shownPhase;
        Vector2 shownAt = new Vector2(-1f, -1f);

        public WantedHudCard(RectTransform safe)
        {
            Root = UiWinKit.Anchored(safe, "WantedCard", Vector2.one, Vector2.one, 0f, 0f, W, H);
            Place();
            Image pill = HudCompact.Capsule(Root, PaperPiece.HcWantedPill, HudCompact.Dark, HudCompact.Rim, H * 0.5f, true);
            PaperHudPress.Tap(pill.gameObject, () => UiScreens.Open(UiScreen.Wanted));
            Image ring;
            face = HudCompact.Portrait(Root, "Portrait", 0f, 0f, H, PaperPiece.HcWantedRing, null, RingHole, out ring);
            stateIcon = UiKit.Icon(Root, "Icon", H + 4f, (H - IconSize) * 0.5f, IconSize);
            tick = PaperKit.Mark(Root, PaperMark.Check, H + 4f + IconSize * 0.5f, H * 0.5f, IconSize);
            text = PaperTheme.Title(Root, "Text", TextX, 0f, W - TextX - 8f, H, "", PaperTheme.BodySize, PaperTheme.TextLight, PaperInk.Outlined);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            Root.gameObject.SetActive(false);
        }

        /// <summary>The anchor is read every frame: it differs per layout and the menu row above it may move.</summary>
        void Place()
        {
            Vector2 at = HudAnchors.WantedCardTopRight;
            if (at == shownAt) return;
            shownAt = at;
            Root.anchoredPosition = new Vector2(-at.x, -at.y);
        }

        public void SetVisible(bool on) { if (Root.gameObject.activeSelf != on) Root.gameObject.SetActive(on); }

        /// <summary>Per frame while the status is known. `visible` = the HUD column is shown (no window open).</summary>
        public void Tick(bool visible)
        {
            SetVisible(visible);
            if (!visible) return;
            Place();
            WantedPhase phase = PaperWanted.Phase;
            int version = WantedBossTracker.Status.Version;
            if (version != shownVersion || phase != shownPhase) ShowState(phase, version);
            if (phase == WantedPhase.Claimed) return;
            bool alive = phase == WantedPhase.Alive;
            int second = PaperWanted.Seconds(alive ? WantedBossTracker.ResetLeftMs : WantedBossTracker.RespawnLeftMs);
            if (second == shownSecond) return;
            shownSecond = second;
            text.text = (alive ? "ALIVE " : "Respawns ") + ClockColor + PaperWanted.Clock(second, second >= 3600) + "</color>";
        }

        void ShowState(WantedPhase phase, int version)
        {
            shownVersion = version; shownPhase = phase; shownSecond = -1;
            PaperWanted.SetFace(face, WantedBossTracker.Today.BossType);
            bool claimed = phase == WantedPhase.Claimed, alive = phase == WantedPhase.Alive;
            Sprite icon = PaperArt.Get(claimed ? PaperPiece.HcWantedClaimed : alive ? PaperPiece.HcWantedAlive : PaperPiece.HcWantedRespawn);
            if (icon == null && !claimed) icon = PaperArt.Get(alive ? PaperPiece.WantedIconAlive : PaperPiece.WantedIconRespawn); // the board's icons until the pill's own arrive
            stateIcon.sprite = icon;
            stateIcon.enabled = icon != null;
            tick.SetActive(claimed && icon == null);
            text.color = claimed ? PaperWanted.AliveGreen : PaperTheme.TextLight;
            if (claimed) text.text = "CLAIMED";
        }
    }
}
