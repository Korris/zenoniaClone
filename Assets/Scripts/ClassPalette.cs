using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// One colour identity per class for every basic-attack effect (slash sprites, sword trail ribbon, impact sparks / flash / ring,
    /// projectiles, bursts, additive glow). All values live here. Tint recolours the painted FX art (luminance kept, hottest pixels
    /// stay white, see SpriteTint) so every effect keeps a white-hot core.
    ///   Warrior RED   Wizard VIOLET-BLUE (Arcane)   Mechanic YELLOW (or BLUE with C)   Assassin PURPLE (violet / near-black wisps, own dagger FX shapes)   Cleric GREEN   Fighter ORANGE (shares the mechanic's palette)
    /// </summary>
    public sealed class ClassPalette
    {
        public Color Tint;              // main hue the art is recoloured to
        public Color Core, Mid, Edge;   // trail ribbon / ring colours: white-hot core -> mid -> saturated edge
        public Color Glow;              // additive bloom copy colour
        public Color Wisp;              // dark smoke / shadow colour (assassin)
        public Gradient Sparks;         // impact spark colour over life

        public static readonly ClassPalette Neutral = Make(new Color(1f, 0.62f, 0.2f), Color.white, new Color(1f, 0.55f, 0.15f), new Color(0.9f, 0.12f, 0.1f), new Color(1f, 0.85f, 0.7f));
        public static readonly ClassPalette Red = Make(new Color(1f, 0.1f, 0.06f), Color.white, new Color(1f, 0.3f, 0.08f), new Color(0.7f, 0f, 0.07f), new Color(1f, 0.16f, 0.1f)); // blood red: crimson edge, red-orange band, thin white core
        public static readonly ClassPalette Orange = Make(new Color(1f, 0.58f, 0.1f), Color.white, new Color(1f, 0.7f, 0.2f), new Color(0.95f, 0.38f, 0.04f), new Color(1f, 0.75f, 0.35f));
        public static readonly ClassPalette Yellow = Make(new Color(1f, 0.82f, 0.12f), Color.white, new Color(1f, 0.9f, 0.3f), new Color(0.9f, 0.6f, 0.05f), new Color(1f, 0.88f, 0.4f)); // mechanic: electric yellow, amber-brass edge (class colour 2026-11-08)
        public static readonly ClassPalette Blue = Make(new Color(0.15f, 0.6f, 1f), Color.white, new Color(0.35f, 0.82f, 1f), new Color(0.08f, 0.38f, 1f), new Color(0.5f, 0.85f, 1f));
        public static readonly ClassPalette Arcane = Make(new Color(0.55f, 0.35f, 1f), new Color(0.95f, 0.92f, 1f), new Color(0.62f, 0.45f, 1f), new Color(0.32f, 0.2f, 0.95f), new Color(0.6f, 0.45f, 1f)); // wizard: violet-blue like the reference
        public static readonly ClassPalette Violet = Make(new Color(0.6f, 0.3f, 1f), new Color(0.95f, 0.9f, 1f), new Color(0.72f, 0.5f, 1f), new Color(0.38f, 0.12f, 0.8f), new Color(0.75f, 0.5f, 1f), new Color(0.2f, 0.08f, 0.35f));
        public static readonly ClassPalette Green = Make(new Color(0.22f, 0.92f, 0.45f), Color.white, new Color(0.55f, 1f, 0.5f), new Color(0.08f, 0.68f, 0.34f), new Color(0.6f, 1f, 0.55f));

        /// <summary>Palette of the hero being played (impact sparks read it); skills switch to Neutral while they deal damage.</summary>
        public static ClassPalette Current = Neutral;

        /// <summary>Mechanic FX colour (hotkey C): false = yellow (default, class colour), true = blue.</summary>
        public static bool MechanicBlue;

        /// <summary>Palette of a worn weapon's glow colour: light core of the same hue (no white), the hue itself as band, a darker edge. Cached per hex.</summary>
        public static ClassPalette ForWeapon(string glowHex, ClassPalette fallback)
        {
            Color c;
            if (string.IsNullOrEmpty(glowHex) || !ColorUtility.TryParseHtmlString(glowHex, out c)) return fallback;
            ClassPalette p;
            if (weaponPalettes.TryGetValue(glowHex, out p)) return p;
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            Color mid = Color.HSVToRGB(h, Mathf.Max(s, 0.9f), 1f);
            Color core = Color.HSVToRGB(h, 0.45f, 1f);
            Color edge = Color.HSVToRGB(h, 1f, 0.65f);
            p = Make(mid, core, mid, edge, mid);
            weaponPalettes[glowHex] = p;
            return p;
        }
        static readonly System.Collections.Generic.Dictionary<string, ClassPalette> weaponPalettes = new System.Collections.Generic.Dictionary<string, ClassPalette>();

        static ClassPalette Make(Color tint, Color core, Color mid, Color edge, Color glow, Color? wisp = null)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(core, 0f), new GradientColorKey(mid, 0.22f), new GradientColorKey(edge, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return new ClassPalette { Tint = tint, Core = core, Mid = mid, Edge = edge, Glow = glow, Wisp = wisp ?? edge * 0.5f, Sparks = g };
        }
    }
}
