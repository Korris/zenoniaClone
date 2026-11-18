using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Which look the classic UGUI windows (shops, quest log, trade, settings, party, bag, skill tree, NPC dialogue) and the classic HUD wear:
    ///   RoSoft (default)  "RO Soft Fantasy" (docs/reference/ui-style-ro-soft-fantasy/README.md): light ivory menus, flat mint / sky buttons,
    ///                     dark translucent HUD plates. Applied by RoSoftSkin over the classic builders, so no window logic changes.
    ///   Classic           the old gold-and-navy art, untouched.
    /// Launch arg: --ui-style ro|classic. The Adventure Paper screens (--ui new / --ui-new ..., UiScreens) keep their own look either way,
    /// so all three styles can still be compared.
    /// </summary>
    public static class UiStyle
    {
        public enum Look { RoSoft, Classic }

        static bool parsed;
        static Look current = Look.RoSoft;

        public static Look Current
        {
            get
            {
                if (!parsed) Parse();
                return current;
            }
            set { parsed = true; current = value; }
        }

        /// <summary>True while the RO Soft Fantasy skin is on.</summary>
        public static bool Ro { get { return Current == Look.RoSoft; } }

        static void Parse()
        {
            parsed = true;
            try
            {
                string[] a = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(a, "--ui-style");
                if (i < 0 || i + 1 >= a.Length) return;
                string v = a[i + 1].Trim().ToLowerInvariant();
                if (v == "classic" || v == "navy" || v == "old") current = Look.Classic;
                else if (v == "ro" || v == "ro-soft" || v == "rosoft") current = Look.RoSoft;
                else Debug.LogWarning("[ZenoniaProto] --ui-style: unknown look '" + v + "' (ro, classic)");
            }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] --ui-style: " + e.Message); }
        }
    }
}
