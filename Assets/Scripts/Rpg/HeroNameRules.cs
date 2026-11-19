using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Hero name rules shared by the online create step (same as server-api schemas.js: trimmed, 3 to 16 of A-Z a-z 0-9 _) and the offline one,
    /// where a name must also be free among the heroes saved on this device (compared without case, like the server's nameLower index).
    /// </summary>
    public static class HeroNameRules
    {
        public const int MinLength = 3, MaxLength = 16;
        public const string RuleText = "Name: 3 to 16 letters, digits or _";
        public const string TakenText = "A hero on this device already has that name";

        public static bool IsNameChar(char c) { return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_'; }

        /// <summary>Null when the name is allowed (after trimming), else the rule it breaks.</summary>
        public static string Problem(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length < MinLength || name.Length > MaxLength) return RuleText;
            foreach (char c in name) if (!IsNameChar(c)) return RuleText;
            return null;
        }

        /// <summary>Problem, then: another hero in `taken` already has this name (case-insensitive).</summary>
        public static string Problem(string name, IEnumerable<string> taken)
        {
            string rule = Problem(name);
            if (rule != null) return rule;
            string want = name.Trim();
            foreach (string t in taken) if (string.Equals((t ?? "").Trim(), want, StringComparison.OrdinalIgnoreCase)) return TakenText;
            return null;
        }
    }
}
