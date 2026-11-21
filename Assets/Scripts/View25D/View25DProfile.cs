using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Look of a 2.5D map, as data (one profile per kind of map; every map listed in View25D.Maps gets <see cref="Ro3Feel"/> unless it names its own).
    /// RO3 Feel V2 follows the ChatGPT comparison of Willowbrook with Ragnarok Online 3 (21/11/2026,
    /// docs/reference/willowbrook-25d/willowbrook-25d-vs-ro3-feel-chatgpt.md sections 2-9, 13): camera pitch 43 deg from the horizontal
    /// (= 47 deg away from straight down, our tilt convention), vertical FOV 25 deg, hero on screen ~85 % of the 38 / 22 camera (ChatGPT: 96 -> 82 px at
    /// 1280x720; measured Cleric body ~78 -> ~66 px, the staff top adds ~5) by a farther camera (ViewScale 1.11: the FOV / look-at change already shrinks ~5 %),
    /// look-at on the chest; sun from the upper left at 45 deg elevation; warm sun #FFF0CD on lit things, cool shade #596F61 in shadow;
    /// fog #C6D9C0 by distance beyond the hero; dappled light, motes, a few leaves, wind sway on trees.
    /// Scene scale: 1 m = 1 tile = 16 world units (the chibi hero is ~28 units, ~1.7 m). With FOV 25 deg and the RO3 zoom the camera is ~36 m from
    /// the hero and only -9..+15 m of ground around it is on screen, so ChatGPT's camera-distance fog 18 / 50 m (which would fog the hero itself, its own
    /// warning) is applied as distance BEYOND the hero: clear around the play area, haze growing toward the far top edge.
    /// </summary>
    public sealed class View25DProfile
    {
        /// <summary>Camera tilt away from straight down, degrees (pitch from the horizontal = 90 - this).</summary>
        public float TiltDeg;
        public float FovDeg;
        /// <summary>Camera distance multiplier over the orthographic-equivalent distance: a figure at the focus shows 1 / this as tall as on the 2D camera.</summary>
        public float ViewScale = 1f;
        /// <summary>Screen centre on the hero this many units above the feet (0 = the old ground point 10 units north of the feet).</summary>
        public float LookAtHeight;
        /// <summary>RO3 lighting pass: lit sprite materials, unit shade under canopy, silhouette cast shadows, dapple, ambient FX, fog.</summary>
        public bool Ro3Look;

        // ---- sun (FakeSun reads these on an RO3 map) ----
        public float SunElevationDeg = 45f;
        /// <summary>Artistic shortening of the cast length (ChatGPT: a 4-6 m tree throws 2-4 m, so ~0.6 of the geometric length at 45 deg).</summary>
        public float CastShorten = 0.6f;
        public Color SunColor = Hex(0xFF, 0xF0, 0xCD);
        /// <summary>How much of the sun colour multiplies ground and props (1 = full #FFF0CD, which turns blues 20 % darker; 0.55 made the olive grass look khaki in game, 21/11).</summary>
        public float SunStrength = 0.35f;
        /// <summary>Same for heroes, monsters, pets: lighter, the painted characters must stay readable.</summary>
        public float UnitSunStrength = 0.25f;
        public Color ShadeColor = Hex(0x59, 0x6F, 0x61);
        /// <summary>Shade side of tree crowns and props (ChatGPT section 6: shadow side mixed toward a cool #637B63).</summary>
        public Color CrownShadeColor = Hex(0x63, 0x7B, 0x63);
        /// <summary>Darkest a unit gets standing in a full crown shadow (share of the way to the shade colour, at mask alpha >= UnitShadeFull).</summary>
        public float UnitShadeStrength = 0.30f;
        public float UnitShadeFull = 0.24f;

        // ---- fog (by camera distance beyond the hero's, world units) ----
        public Color FogColor = Hex(0xC6, 0xD9, 0xC0);
        public float FogStart = 3f * GameConfig.TileUnits, FogEnd = 16f * GameConfig.TileUnits, FogMax = 0.26f, FogDesaturate = 0.35f;
        /// <summary>Screen pass (View25DAtmosphere): haze at the top edge on top of the per-object fog, and the vignette (ChatGPT: 0.03-0.07).</summary>
        public float ScreenHaze = 0.10f, Vignette = 0.08f;

        // ---- ambient FX (visible at once on 1280x720, before the crowd LOD) ----
        public int Motes = 16, LeavesMax = 6;
        public float LeafBurstMin = 10f, LeafBurstMax = 18f;
        /// <summary>Tree sway amplitude, world units at the crown top (ChatGPT: 1-2 % of the crown height), and the dapple strength under canopies.</summary>
        public float WindSway = 1.3f, Dapple = 1f;

        /// <summary>Every listed 2.5D map: the RO3 Feel V2 preset.</summary>
        public static readonly View25DProfile Ro3Feel = new View25DProfile
        {
            TiltDeg = 47f, FovDeg = 25f, ViewScale = 1.11f, LookAtHeight = 15f, Ro3Look = true, ScreenHaze = 0.04f, Vignette = 0.04f,
        };

        /// <summary>The --view25d Hydra arena prototype keeps the first 2.5D camera (38 / 22, painted walls not converted) and none of the RO3 pass.</summary>
        public static readonly View25DProfile Prototype = new View25DProfile
        {
            TiltDeg = 38f, FovDeg = 22f, ViewScale = 1f, LookAtHeight = 0f, Ro3Look = false, FogColor = new Color(0.62f, 0.80f, 0.82f),
        };

        /// <summary>Profile of a map id (a map that wants its own look adds a case here).</summary>
        public static View25DProfile For(string mapId)
        {
            return mapId == SunkenReliquaryMap.MapId ? Prototype : Ro3Feel;
        }

        static Color Hex(int r, int g, int b) { return new Color(r / 255f, g / 255f, b / 255f, 1f); }
    }
}
