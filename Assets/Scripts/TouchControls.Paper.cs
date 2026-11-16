using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// TouchControls: the compact HUD look (UiScreens.UseNew(UiScreen.Hud)). The switch is read every frame (capture flags and a later
    /// settings toggle flip it after the HUD was built): the first time it is on, PaperHudControls is built next to the old layer, and
    /// from then on whichever look is switched on is the one shown. Input rules are untouched: the new controls feed the same GameKeys.
    /// Two keys exist only with this HUD, as on hud-compact-pc-v1.png (the labels under the menu icons):
    ///   N    the quest list, and N again closes it
    ///   Esc  Settings, but only when nothing else is open: Esc keeps closing windows, the bag, the "..." strip first
    /// </summary>
    public partial class TouchControls
    {
        PaperHudControls paper;
        bool settingsOnEsc;

        /// <summary>The paper controls, once built (capture scripts); null before.</summary>
        internal static PaperHudControls Paper { get { return instance != null ? instance.paper : null; } }

        /// <summary>True when the paper look is switched on; builds it on first use.</summary>
        bool UsePaper()
        {
            if (!UiScreens.UseNew(UiScreen.Hud)) return false;
            if (paper == null) paper = new PaperHudControls(layer);
            return true;
        }

        /// <summary>Shows / hides and ticks the paper controls. `visible` = this frame's `show` of Update and the paper look is on.</summary>
        void UpdatePaper(HeroController hero, bool visible, bool paperOn)
        {
            if (paper == null) return;
            if (paper.Visible != visible)
            {
                paper.SetVisible(visible);
                if (!visible) ReleaseHeld(); // never leave a direction stuck
            }
            if (hero != null && paperOn) paper.Tick(hero, Time.unscaledDeltaTime);
            if (paperOn) PaperKeys(hero);
        }

        static void ReleaseHeld() { for (int i = 0; i < KeyCount; i++) held[i] = false; }

        void PaperKeys(HeroController hero)
        {
            if (hero == null) return;
            // Nothing is open and nobody is typing: only then may Esc / N open something.
            bool free = !UiWindows.BlocksGameplay && !UiScreens.IsOpen(UiScreen.Bag) && !HeroChangeScreen.IsOpen && !AccountScreenGui.MenuOpen && !ChatWindow.RecentlyTyping;
            if (InputAdapter.Pressed(GameKey.Close))
            {
                if (paper.Entry.MoreOpen) paper.Entry.CloseMore();
                else if (free) settingsOnEsc = true; // opened in LateUpdate: WindowsHost closes every modal on this same key press
            }
            if (free && InputAdapter.Pressed(GameKey.Quests)) QuestLogWindow.Open();
            else if (QuestLogWindow.IsOpen && InputAdapter.WindowPressed(GameKey.Quests)) QuestLogWindow.Close();
        }

        void LateUpdate()
        {
            if (!settingsOnEsc) return;
            settingsOnEsc = false;
            SettingsWindow.Open();
        }
    }
}
