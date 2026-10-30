using System.Text;

namespace ZenoniaProto
{
    /// <summary>
    /// Phase 1 login: name + class + a shared dev key, no accounts, no persistence (real accounts / tickets arrive in phase 3). Everything the client
    /// sends is treated as hostile: names are reduced to short printable text, the key is compared in constant time, the class byte is range checked.
    /// </summary>
    public static class DevLoginStub
    {
        public const int MaxNameLength = 16;

        /// <summary>Letters, digits, space, '-', '_' and '.' only, trimmed, at most 16 characters; empty becomes "Hero".</summary>
        public static string SanitizeName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Hero";
            var sb = new StringBuilder(MaxNameLength);
            foreach (char c in raw)
            {
                if (sb.Length >= MaxNameLength) break;
                if (c > 126 || !(char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_' || c == '.')) continue;
                sb.Append(c);
            }
            string s = sb.ToString().Trim();
            return s.Length == 0 ? "Hero" : s;
        }

        /// <summary>Constant-time compare so the dev key cannot be guessed by timing; an empty expected key means open server.</summary>
        public static bool KeyOk(string expected, string given)
        {
            if (string.IsNullOrEmpty(expected)) return true;
            given = given ?? "";
            int diff = expected.Length ^ given.Length;
            for (int i = 0; i < expected.Length; i++) diff |= expected[i] ^ (i < given.Length ? given[i] : '\0');
            return diff == 0;
        }

        public static bool TryClass(byte index, out HeroClassDef def)
        {
            def = index < HeroClassDef.All.Length ? HeroClassDef.All[index] : null;
            return def != null;
        }
    }
}
