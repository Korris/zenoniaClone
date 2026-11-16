namespace ZenoniaProto
{
    /// <summary>
    /// InputAdapter: the name of the physical key bound to a GameKey, for the key caps of the HUD ("B", "1", "Space", "Tab", "Esc").
    /// Read from the live binding table (after UseWebBindings on the web HUD), so a cap always shows the key that really works.
    /// Builds a string: call it when the HUD is built, not every frame.
    /// </summary>
    public static partial class InputAdapter
    {
        /// <summary>Label of binding number `index` of `k` (0 = the first key of its row); "" when there is none.</summary>
        public static string KeyLabel(GameKey k, int index = 0)
        {
#if ENABLE_LEGACY_INPUT_MANAGER || ENABLE_INPUT_SYSTEM
            var keys = Map[(int)k];
            return index >= 0 && index < keys.Length ? Pretty(keys[index].ToString()) : "";
#else
            return "";
#endif
        }

        /// <summary>Attack on the hotbar: the mouse button (the web HUD attacks with it) and the Space-bar style key of the row ("LMB / Space").</summary>
        public static string AttackLabel()
        {
            string key = KeyLabel(GameKey.Attack, 1);
            if (key == "") key = KeyLabel(GameKey.Attack);
            return key == "" ? "LMB" : "LMB / " + key;
        }

        /// <summary>KeyCode / Key enum names to what is printed on a keyboard.</summary>
        static string Pretty(string name)
        {
            foreach (string prefix in DigitPrefixes)
                if (name.Length == prefix.Length + 1 && name.StartsWith(prefix, System.StringComparison.Ordinal)) return name.Substring(prefix.Length);
            switch (name)
            {
                case "Return": case "KeypadEnter": case "NumpadEnter": return "Enter";
                case "Escape": return "Esc";
                case "LeftShift": case "RightShift": return "Shift";
                case "UpArrow": return "Up";
                case "DownArrow": return "Down";
                case "LeftArrow": return "Left";
                case "RightArrow": return "Right";
                default: return name;
            }
        }

        static readonly string[] DigitPrefixes = { "Alpha", "Digit", "Keypad", "Numpad" };
    }
}
