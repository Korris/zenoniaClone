#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Web HUD keys (HudLayout.IsWeb): 1 / 2 / 3 press the three hotbar skill slots (as U / I / O do), Q / E drink the HP / MP potion.
    /// The left mouse button is click-to-move / click-to-attack on every desktop build (ClickMove, 22/11/2026; it used to hold the attack). The debug tier keys (E / Q) are dropped.
    /// E near an NPC still talks (the MP potion waits until no NPC is in range).
    /// </summary>
    public static partial class InputAdapter
    {
        static bool webKeys;

        /// <summary>Called once by HudLayout when the web HUD is chosen.</summary>
        public static void UseWebBindings()
        {
            if (webKeys) return;
            webKeys = true;
#if ENABLE_LEGACY_INPUT_MANAGER
            Map[(int)GameKey.HpPotion] = new[] { KeyCode.Q };
            Map[(int)GameKey.MpPotion] = new[] { KeyCode.E };
            // Q / E drink here: the debug gear look (weapon + armour outfit of one art tier) moves to X (up) / Z (down) in test builds, none in a release
            Map[(int)GameKey.TierUp] = ReleaseBuild.IsRelease ? new KeyCode[0] : new[] { KeyCode.X };
            Map[(int)GameKey.TierDown] = ReleaseBuild.IsRelease ? new KeyCode[0] : new[] { KeyCode.Z };
            Map[(int)GameKey.SlotKey1] = new[] { KeyCode.Alpha1, KeyCode.Keypad1, KeyCode.U };
            Map[(int)GameKey.SlotKey2] = new[] { KeyCode.Alpha2, KeyCode.Keypad2, KeyCode.I, KeyCode.L };
            Map[(int)GameKey.SlotKey3] = new[] { KeyCode.Alpha3, KeyCode.Keypad3, KeyCode.O };
#elif ENABLE_INPUT_SYSTEM
            Map[(int)GameKey.HpPotion] = new[] { Key.Q };
            Map[(int)GameKey.MpPotion] = new[] { Key.E };
            Map[(int)GameKey.TierUp] = ReleaseBuild.IsRelease ? new Key[0] : new[] { Key.X };
            Map[(int)GameKey.TierDown] = ReleaseBuild.IsRelease ? new Key[0] : new[] { Key.Z };
            Map[(int)GameKey.SlotKey1] = new[] { Key.Digit1, Key.Numpad1, Key.U };
            Map[(int)GameKey.SlotKey2] = new[] { Key.Digit2, Key.Numpad2, Key.I, Key.L };
            Map[(int)GameKey.SlotKey3] = new[] { Key.Digit3, Key.Numpad3, Key.O };
#endif
        }

        /// <summary>The physical key went down this frame, even while the chat owns the keyboard (Tab switches the chat channel then).</summary>
        public static bool RawPressed(GameKey k) { return KeyPressed(k); }

        /// <summary>Web: E talks to the NPC in range instead of drinking.</summary>
        static bool WebKeyBlocked(GameKey k) { return webKeys && k == GameKey.MpPotion && UiWindows.InteractPrompt; }
    }
}
