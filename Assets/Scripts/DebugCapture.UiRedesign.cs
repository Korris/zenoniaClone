using System;
using System.Collections;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the Adventure Paper UI rebuild (plans/261116-1206-adventure-paper-ui-rebuild):
    ///   --ui new|old           every screen switch on / off (UiScreens; default = old until a screen is approved)
    ///   --ui-new bag,skill     only these screens rebuilt (bag, skill, pet, gacha, chat, hud, boss, wanted): A/B shots with the same other flags
    ///   --kit-sheet            the kit gallery (PaperKitSheet): every PaperKit widget on one screen; shoot it at 1920x1080, 2400x1080, 1600x1200, 3440x1440
    ///   --ui-open bag,skill    open these screens through UiScreens (what the keys and buttons call), old or rebuilt look as switched
    ///   --ui-keys "1=Inventory;2=Close;3=SkillTree"   press that GameKey at that second: the real key path (RpgSession / WindowsHost -> UiScreens);
    ///                          logs which screens are open after each press
    /// The existing flags keep working on whichever look is on: --open-inventory opens the old bag or, with --ui-new bag, the rebuilt one.
    ///
    /// Each rebuilt screen adds its own flags by implementing its hook below in its own file, DebugCapture.Ui&lt;Screen&gt;.cs
    /// (e.g. DebugCapture.UiBag.cs: `partial void DebugUiBag() { if (Flag("--bag-shelf-page")) ... }`), so no phase edits this file or
    /// DebugCapture.cs again. A hook without a body compiles to nothing. Hooks run after the hero spawned and the RPG flags were applied,
    /// right after the pet windows (ApplyRpgFlags), in the order below; use Flag / Val (DebugCapture.Pets.cs) to read the command line.
    /// </summary>
    public partial class DebugCapture
    {
        partial void DebugUiHud();    // phase 03 / 04: player status, toasts, entry buttons, touch controls, hotbar
        partial void DebugUiBoss();   // phase 03: boss bars
        partial void DebugUiBag();    // phase 05: Armory Carousel
        partial void DebugUiSkill();  // phase 06: Training Hall
        partial void DebugUiChat();   // phase 07: notice-board chat
        partial void DebugUiPet();    // phase 08: Pet Camp
        partial void DebugUiGacha();  // phase 09: Lumi's counter
        partial void DebugUiWanted(); // phase 11: Wanted board, HUD card, notifications
        partial void DebugRoHud();    // RO Soft HUD: portraits, PC clicks

        void DebugUiRedesign()
        {
            if (Flag("--kit-sheet")) PaperKitSheet.Open(UiKit.Canvas(null).transform); // the shared canvas exists once the HUD is built
            string open = Val("--ui-open");
            if (open != null)
                foreach (string name in open.Split(','))
                    UiScreens.Open((UiScreen)Enum.Parse(typeof(UiScreen), name.Trim(), true));
            string keys = Val("--ui-keys");
            if (keys != null) StartCoroutine(RunUiKeys(keys));
            DebugUiHud();
            DebugUiBoss();
            DebugUiBag();
            DebugUiSkill();
            DebugUiChat();
            DebugUiPet();
            DebugUiGacha();
            DebugUiWanted();
            DebugRoHud(); // RO Soft HUD follow-up: --look, --look-at, --ro-click, --ro-digit (DebugCapture.RoHud.cs)
        }

        /// <summary>--ui-keys: each step holds its key "pressed" for exactly one frame, like a tap on the keyboard.</summary>
        IEnumerator RunUiKeys(string script)
        {
            float start = Time.realtimeSinceStartup;
            foreach (string step in script.Split(';'))
            {
                string[] kv = step.Split('=');
                float at = float.Parse(kv[0], System.Globalization.CultureInfo.InvariantCulture);
                GameKey key = (GameKey)Enum.Parse(typeof(GameKey), kv[1].Trim(), true);
                while (Time.realtimeSinceStartup - start < at) yield return null;
                Func<GameKey, bool> before = InputAdapter.BotPressed;
                InputAdapter.BotPressed = k => k == key;
                yield return null; // the next frame's Updates see the key, then it is released
                InputAdapter.BotPressed = before;
                string openNow = "";
                foreach (UiScreen s in Enum.GetValues(typeof(UiScreen))) if (UiScreens.IsOpen(s)) openNow += s + " ";
                Debug.Log("[ZenoniaProto] ui-key " + key + " at " + at + " => open: " + (openNow == "" ? "none" : openNow) + "| modal " + UiWindows.ModalOpen);
            }
        }
    }
}
