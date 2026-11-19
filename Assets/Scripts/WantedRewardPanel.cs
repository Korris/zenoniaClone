using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// "BOUNTY CLAIMED!": the panel shown when the zone paid this character the daily bounty (WantedBossTracker.BountyPaid), until OK
    /// (wanted-notifications.png section 6; wanted-board-poster-states-*.png panel 4). It shows the boss and the fixed bounty only: gold,
    /// upgrade stones, HP and MP potions. The mockup's "Your Loot" column, contribution bar and Collect All wait for the loot rules
    /// (phase 12) and are not built. Not a modal: the fight around the hero goes on, so the game is not paused and the keys keep working;
    /// the panel is brought to the front of the canvas so its buttons are never under a control layer.
    /// </summary>
    public sealed partial class WantedRewardPanel
    {
        const float W = 300f, H = 264f;

        readonly PaperLayout layout;
        Image face;
        Text nameText;
        WantedRewardRow rewards;
        RoWantedRewards roRewards; // RO Soft look (WantedRewardPanel.RoSoft.cs)

        public bool IsOpen { get { return layout.Root.gameObject.activeSelf; } }

        /// <summary>A bounty panel is up (NameTags: IMGUI tags would draw over it).</summary>
        public static bool AnyOpen { get; private set; }

        public WantedRewardPanel(RectTransform safe)
        {
            // Its own safe-area root on the canvas: it can come to the front without lifting the card and the banners over the windows.
            layout = PaperLayout.CreateSafeArea(safe.GetComponentInParent<Canvas>().rootCanvas.transform, "PaperWantedReward");
            if (RoHud.Active) { BuildRo(layout.Safe); layout.Root.gameObject.SetActive(false); return; }
            RectTransform panel = PaperKit.Frame(layout.Safe, "Panel", 0f, 0f, W, H, PaperFrame.Paper);
            Vector2 mid = new Vector2(0.5f, 0.5f);
            panel.anchorMin = panel.anchorMax = panel.pivot = mid;
            // Ribbon top 158 units below the screen top, panel bottom 172 above the screen bottom: clear of the PC toast pill under the boss
            // bar and of the mobile one above the chat lines ("The X has fallen!" arrives just before the bounty); a WARNING ribbon stays readable above it.
            panel.anchoredPosition = new Vector2(0f, -16f);

            // Boss medallion (the boss bar's ring, phase 03: its window is x 49..208, y 46..196 of 256 x 242).
            const float MedW = 86f, MedH = MedW * 242f / 256f;
            RectTransform med = UiKit.Rect(panel, "Medallion", (W - MedW) * 0.5f, 48f, MedW, MedH);
            face = PaperWanted.RingPortrait(med, MedW, MedH, PaperPiece.BossPlate, MedW * 0.185f, MedH * 0.17f, MedW * 0.63f);
            PaperArt.Draw(UiKit.Fill(med, "Ring"), PaperPiece.BossMedallion, Color.clear, PaperTheme.Gold, MedW * 0.5f);
            nameText = PaperTheme.Title(panel, "Name", 20f, 128f, W - 40f, 22f, "", PaperTheme.SectionSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter);
            PaperWanted.Label(panel, "Daily", (W - 96f) * 0.5f, 150f, 96f, 18f, "Daily Bounty");
            rewards = new WantedRewardRow(panel, (W - (4f * 44f + 3f * 10f)) * 0.5f, 172f, 44f, 10f);
            new PaperButton(panel, "Ok", 32f, 222f, 96f, 32f, "OK", PaperTone.Gold, Close);
            new PaperButton(panel, "Board", 138f, 222f, 130f, 32f, "Wanted Board", PaperTone.Blue, () => { Close(); UiScreens.Open(UiScreen.Wanted); });

            // wanted/ribbon-bounty-laurel 329 x 125 over the top rim; its band is x 60..270, y 20..76.
            const float RibW = 290f, RibH = RibW * 125f / 329f, RibS = RibW / 329f;
            RectTransform ribbon = UiKit.Rect(panel, "Ribbon", (W - RibW) * 0.5f, -46f, RibW, RibH);
            PaperArt.Draw(ribbon, PaperPiece.WantedRibbonBounty, PaperTheme.Red, PaperTheme.BtnRedEdge, 14f);
            PaperTheme.Title(ribbon, "Text", 0f, 20f * RibS, RibW, 56f * RibS, "BOUNTY CLAIMED!", 20, PaperTheme.TitleGold, PaperInk.Outlined, TextAnchor.MiddleCenter);
            layout.Root.gameObject.SetActive(false);
        }

        public void Open(WantedBossDef def)
        {
            PaperWanted.SetFace(face, def.BossType);
            nameText.text = def.BossName + " defeated";
            if (rewards != null) rewards.Set(def);
            if (roRewards != null) roRewards.Set(def);
            layout.Root.gameObject.SetActive(true);
            layout.Root.SetAsLastSibling();
            AnyOpen = true;
        }

        public void Close() { AnyOpen = false; if (IsOpen) layout.Root.gameObject.SetActive(false); }
    }
}
