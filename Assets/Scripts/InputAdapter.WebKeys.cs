#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZenoniaProto
{
    /// <summary>
    /// Web HUD keys (HudLayout.IsWeb): 1 / 2 / 3 press the three hotbar skill slots (as U / I / O do), Q / E drink the HP / MP potion,
    /// and the left mouse button attacks when it went down on the world (not on a UI panel). The debug tier keys (E / Q) are dropped.
    /// E near an NPC still talks (the MP potion waits until no NPC is in range).
    /// </summary>
    public static partial class InputAdapter
    {
        static bool webKeys;
        static int mouseFrame = -1;
        static bool mouseOnWorld, mouseHeld;

        /// <summary>Called once by HudLayout when the web HUD is chosen.</summary>
        public static void UseWebBindings()
        {
            if (webKeys) return;
            webKeys = true;
#if ENABLE_LEGACY_INPUT_MANAGER
            Map[(int)GameKey.HpPotion] = new[] { KeyCode.Q };
            Map[(int)GameKey.MpPotion] = new[] { KeyCode.E };
            Map[(int)GameKey.TierUp] = new KeyCode[0];
            Map[(int)GameKey.TierDown] = new KeyCode[0];
            Map[(int)GameKey.SlotKey1] = new[] { KeyCode.Alpha1, KeyCode.Keypad1, KeyCode.U };
            Map[(int)GameKey.SlotKey2] = new[] { KeyCode.Alpha2, KeyCode.Keypad2, KeyCode.I, KeyCode.L };
            Map[(int)GameKey.SlotKey3] = new[] { KeyCode.Alpha3, KeyCode.Keypad3, KeyCode.O };
#elif ENABLE_INPUT_SYSTEM
            Map[(int)GameKey.HpPotion] = new[] { Key.Q };
            Map[(int)GameKey.MpPotion] = new[] { Key.E };
            Map[(int)GameKey.TierUp] = new Key[0];
            Map[(int)GameKey.TierDown] = new Key[0];
            Map[(int)GameKey.SlotKey1] = new[] { Key.Digit1, Key.Numpad1, Key.U };
            Map[(int)GameKey.SlotKey2] = new[] { Key.Digit2, Key.Numpad2, Key.I, Key.L };
            Map[(int)GameKey.SlotKey3] = new[] { Key.Digit3, Key.Numpad3, Key.O };
#endif
        }

        /// <summary>The physical key went down this frame, even while the chat owns the keyboard (Tab switches the chat channel then).</summary>
        public static bool RawPressed(GameKey k) { return KeyPressed(k); }

        /// <summary>Web: E talks to the NPC in range instead of drinking.</summary>
        static bool WebKeyBlocked(GameKey k) { return webKeys && k == GameKey.MpPotion && UiWindows.InteractPrompt; }

        /// <summary>Web: the left button is held after it went down on the world (a click on the chat / hotbar / minimap never attacks).</summary>
        static bool MouseAttack()
        {
            if (Application.isMobilePlatform || !Application.isFocused) return false;
            int frame = Time.frameCount;
            if (frame != mouseFrame)
            {
                mouseFrame = frame;
                bool down, held;
#if ENABLE_LEGACY_INPUT_MANAGER
                down = Input.GetMouseButtonDown(0); held = Input.GetMouseButton(0);
#elif ENABLE_INPUT_SYSTEM
                Mouse m = Mouse.current;
                down = m != null && m.leftButton.wasPressedThisFrame; held = m != null && m.leftButton.isPressed;
#else
                down = held = false;
#endif
                if (down) mouseOnWorld = EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
                mouseHeld = held && mouseOnWorld;
            }
            return mouseHeld;
        }
    }
}
