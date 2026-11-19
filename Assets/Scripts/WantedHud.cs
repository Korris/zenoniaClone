using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The Wanted Boss pieces of the Adventure Paper HUD, in one safe-area root of their own: the hanging card (WantedHudCard), the notices
    /// in the toast area (WantedNotices), the bounty panel (WantedRewardPanel) and the guide arrow (WantedGuide). Created with the HUD
    /// (RpgHud.Create) and ticked from here. Everything shows only with the HUD switch on (UiScreens.UseNew(UiScreen.Hud)) and while the
    /// server's WantedStatus is known (PaperWanted.Known): with the switch off, offline, or before the status arrived, nothing is built and
    /// the old chat lines / toast of WantedBossTracker are all the player gets.
    /// </summary>
    public sealed class WantedHud : MonoBehaviour
    {
        static WantedHud instance;

        PaperLayout layout;
        WantedHudCard card;
        WantedNotices notices;
        WantedGuide guide;

        public static void Create(Transform canvas)
        {
            PaperLayout l = PaperLayout.CreateSafeArea(canvas, "PaperWantedHud");
            instance = l.gameObject.AddComponent<WantedHud>();
            instance.layout = l;
        }

        void OnEnable() { WantedBossTracker.BountyPaid += OnBountyPaid; }
        void OnDisable() { WantedBossTracker.BountyPaid -= OnBountyPaid; }

        void OnDestroy()
        {
            if (instance != this) return;
            instance = null;
            NotificationToast.Suspended = false;
        }

        /// <summary>The card's tap surface, for capture scripts that tap it through the EventSystem; null while it is not built.</summary>
        public static RectTransform CardRoot { get { return instance != null && instance.card != null ? instance.card.Root : null; } }

        /// <summary>Queues a notice by hand (capture flags; the game raises them from WantedNotices.Tick).</summary>
        public static void DebugNotice(WantedNotice kind)
        {
            if (instance != null && instance.Ready()) instance.notices.Enqueue(kind, WantedBossTracker.Today);
        }

        void OnBountyPaid(WantedBossDef def)
        {
            if (!(UiScreens.UseNew(UiScreen.Hud) || RoHud.Active) || !Ready()) return;
            WantedGuide.On = false; // the hunt is over for today
            notices.OnBountyPaid(def);
        }

        /// <summary>Builds the pieces the first time they are needed.</summary>
        bool Ready()
        {
            if (!PaperWanted.Known) return false;
            if (card != null) return true;
            guide = new WantedGuide(layout.Safe);   // lowest: the arrow never covers the card or a notice
            card = new WantedHudCard(layout.Safe);
            notices = new WantedNotices(layout.Safe);
            return true;
        }

        void Update()
        {
            bool ro = RoHud.Active; // RO Soft HUD: its own card (RoHudWanted); the notices, the bounty panel and the guide run here in the RO look
            bool on = (UiScreens.UseNew(UiScreen.Hud) || ro) && !HeroChangeScreen.IsOpen && Ready();
            if (card == null) return;
            if (!on)
            {
                card.SetVisible(false);
                guide.SetVisible(false);
                notices.Reset();
                return;
            }
            HeroController hero = GameSession.Hero;
            bool hudShown = !UiWindows.ModalOpen && !Hud.CaptureClean; // like the minimap: a window hides the column
            // Mobile: the More fan opens over the card's place (HudAnchors); this root lies above the controls, so the card steps aside while the fan is up.
            PaperHudControls controls = TouchControls.Paper;
            bool fanOver = !HudLayout.IsWeb && controls != null && controls.Entry.MoreOpen;
            float dt = Time.unscaledDeltaTime;
            card.Tick(hudShown && !fanOver && !ro);
            guide.Tick(hero, hudShown);
            notices.Tick(hero, dt);
        }
    }
}
