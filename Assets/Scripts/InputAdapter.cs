#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using System;
using UnityEngine;

namespace ZenoniaProto
{
    public enum GameKey
    {
        Up, Down, Left, Right, Attack, Dash, TierUp, TierDown, Enhance, Bloom, Fx,
        Class1, Class2, Class3,   // class select screen (1 / 2 / 3)
        Switch1, Switch2, Switch3, // in-game class switch (F1 / F2 / F3)
        SolidDebug,                // F9: tint the solid tiles
        Class4, Class5,            // class select screen (4 / 5)
        Switch4, Switch5,          // in-game class switch (F4 / F5)
        Skill,                     // L: class signature skill / ultimate
        FxColor,                   // C: mechanic FX colour (orange / blue)
        Inventory,                 // B: inventory / equipment panel
        HpPotion, MpPotion,        // 1 / 2 in game (the same keys pick a class on the select screen)
        Close,                     // Esc: close a panel
        TouchToggle,               // F7: show / hide the on-screen D-pad + buttons
        DebugHudToggle,            // F8: show / hide the debug HUD (skill line, FPS, key help)
        Interact,                  // E: talk to a nearby NPC / advance a dialogue (UiWindows)
        Chat                       // Enter: focus the chat input / confirm a dialogue step (UiWindows)
    }

    /// <summary>
    /// Keyboard abstraction that works whichever Active Input Handling the project ends up with
    /// (legacy Input Manager preferred; falls back to the Input System package when legacy is disabled).
    /// Bot* hooks let the screenshot demo (DebugCapture) drive the hero without a keyboard; the on-screen TouchControls (D-pad + buttons)
    /// feed the same keys.
    /// </summary>
    public static class InputAdapter
    {
        public static Func<GameKey, bool> BotHeld, BotPressed;
#if ENABLE_LEGACY_INPUT_MANAGER
        static readonly KeyCode[][] Map =
        {
            new[] { KeyCode.W, KeyCode.UpArrow },
            new[] { KeyCode.S, KeyCode.DownArrow },
            new[] { KeyCode.A, KeyCode.LeftArrow },
            new[] { KeyCode.D, KeyCode.RightArrow },
            new[] { KeyCode.J, KeyCode.Space },
            new[] { KeyCode.K, KeyCode.LeftShift, KeyCode.RightShift },
            new[] { KeyCode.E },
            new[] { KeyCode.Q },
            new[] { KeyCode.R },
            new[] { KeyCode.F6 },
            new[] { KeyCode.F },
            new[] { KeyCode.Alpha1, KeyCode.Keypad1 },
            new[] { KeyCode.Alpha2, KeyCode.Keypad2 },
            new[] { KeyCode.Alpha3, KeyCode.Keypad3 },
            new[] { KeyCode.F1 },
            new[] { KeyCode.F2 },
            new[] { KeyCode.F3 },
            new[] { KeyCode.F9 },
            new[] { KeyCode.Alpha4, KeyCode.Keypad4 },
            new[] { KeyCode.Alpha5, KeyCode.Keypad5 },
            new[] { KeyCode.F4 },
            new[] { KeyCode.F5 },
            new[] { KeyCode.L },
            new[] { KeyCode.C },
            new[] { KeyCode.B, KeyCode.I },
            new[] { KeyCode.Alpha1, KeyCode.Keypad1 },
            new[] { KeyCode.Alpha2, KeyCode.Keypad2 },
            new[] { KeyCode.Escape },
            new[] { KeyCode.F7 },
            new[] { KeyCode.F8 },
            new[] { KeyCode.E },
            new[] { KeyCode.Return, KeyCode.KeypadEnter },
        };

        static bool KeyHeld(GameKey k)
        {
            KeyCode[] keys = Map[(int)k];
            for (int i = 0; i < keys.Length; i++) if (Input.GetKey(keys[i])) return true;
            return false;
        }

        static bool KeyPressed(GameKey k)
        {
            KeyCode[] keys = Map[(int)k];
            for (int i = 0; i < keys.Length; i++) if (Input.GetKeyDown(keys[i])) return true;
            return false;
        }
#elif ENABLE_INPUT_SYSTEM
        static readonly Key[][] Map =
        {
            new[] { Key.W, Key.UpArrow },
            new[] { Key.S, Key.DownArrow },
            new[] { Key.A, Key.LeftArrow },
            new[] { Key.D, Key.RightArrow },
            new[] { Key.J, Key.Space },
            new[] { Key.K, Key.LeftShift, Key.RightShift },
            new[] { Key.E },
            new[] { Key.Q },
            new[] { Key.R },
            new[] { Key.F6 },
            new[] { Key.F },
            new[] { Key.Digit1, Key.Numpad1 },
            new[] { Key.Digit2, Key.Numpad2 },
            new[] { Key.Digit3, Key.Numpad3 },
            new[] { Key.F1 },
            new[] { Key.F2 },
            new[] { Key.F3 },
            new[] { Key.F9 },
            new[] { Key.Digit4, Key.Numpad4 },
            new[] { Key.Digit5, Key.Numpad5 },
            new[] { Key.F4 },
            new[] { Key.F5 },
            new[] { Key.L },
            new[] { Key.C },
            new[] { Key.B, Key.I },
            new[] { Key.Digit1, Key.Numpad1 },
            new[] { Key.Digit2, Key.Numpad2 },
            new[] { Key.Escape },
            new[] { Key.F7 },
            new[] { Key.F8 },
            new[] { Key.E },
            new[] { Key.Enter, Key.NumpadEnter },
        };

        static bool KeyHeld(GameKey k)
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            Key[] keys = Map[(int)k];
            for (int i = 0; i < keys.Length; i++) if (kb[keys[i]].isPressed) return true;
            return false;
        }

        static bool KeyPressed(GameKey k)
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            Key[] keys = Map[(int)k];
            for (int i = 0; i < keys.Length; i++) if (kb[keys[i]].wasPressedThisFrame) return true;
            return false;
        }
#else
        static bool KeyHeld(GameKey k) { return false; }
        static bool KeyPressed(GameKey k) { return false; }
#endif

        // Double-tap a direction = dash. Polled once per frame (first reader wins), from touch + keyboard only (bots / AI drive their own dash).
        static readonly DoubleTapDetector tapDetector = new DoubleTapDetector();
        static int tapFrame = -1;
        static bool tapDash;

        static bool DoubleTapDash()
        {
            int frame = Time.frameCount;
            if (frame == tapFrame) return tapDash;
            tapFrame = frame;
            tapDash = tapDetector.Update(PhysicalHeld(GameKey.Up), PhysicalHeld(GameKey.Down), PhysicalHeld(GameKey.Left), PhysicalHeld(GameKey.Right), Time.unscaledTime);
            return tapDash;
        }

        static bool PhysicalHeld(GameKey k) { return TouchControls.Held(k) || KeyHeld(k); }

        /// <summary>Keys that still work while a window owns the keyboard (dialogue, quest offer, chat typing): the window keys themselves.</summary>
        static bool Gated(GameKey k) { return UiWindows.BlocksGameplay && k != GameKey.Close && k != GameKey.Interact && k != GameKey.Chat; }

        public static bool Held(GameKey k)
        {
            if (Gated(k)) return false;
            return (BotHeld != null && BotHeld(k)) || TouchControls.Held(k) || KeyHeld(k);
        }

        public static bool Pressed(GameKey k)
        {
            if (Gated(k) || (k == GameKey.TierUp && UiWindows.InteractPrompt)) return false; // E talks to the NPC instead of changing the weapon tier
            return (BotPressed != null && BotPressed(k)) || TouchControls.Pressed(k) || KeyPressed(k) || (k == GameKey.Dash && DoubleTapDash());
        }
    }
}
