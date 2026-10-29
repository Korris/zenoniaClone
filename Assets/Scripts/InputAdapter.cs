#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using UnityEngine;

namespace ZenoniaProto
{
    public enum GameKey { Up, Down, Left, Right, Attack, Dash, TierUp, TierDown, Enhance, Bloom, Fx }

    /// <summary>
    /// Keyboard abstraction that works whichever Active Input Handling the project ends up with
    /// (legacy Input Manager preferred; falls back to the Input System package when legacy is disabled).
    /// </summary>
    public static class InputAdapter
    {
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
            new[] { KeyCode.B },
            new[] { KeyCode.F },
        };

        public static bool Held(GameKey k)
        {
            KeyCode[] keys = Map[(int)k];
            for (int i = 0; i < keys.Length; i++) if (Input.GetKey(keys[i])) return true;
            return false;
        }

        public static bool Pressed(GameKey k)
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
            new[] { Key.B },
            new[] { Key.F },
        };

        public static bool Held(GameKey k)
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            Key[] keys = Map[(int)k];
            for (int i = 0; i < keys.Length; i++) if (kb[keys[i]].isPressed) return true;
            return false;
        }

        public static bool Pressed(GameKey k)
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            Key[] keys = Map[(int)k];
            for (int i = 0; i < keys.Length; i++) if (kb[keys[i]].wasPressedThisFrame) return true;
            return false;
        }
#else
        public static bool Held(GameKey k) { return false; }
        public static bool Pressed(GameKey k) { return false; }
#endif
    }
}
