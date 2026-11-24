using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Look of a 2.5D map, as data (one profile per kind of map; every map listed in View25D.Maps gets <see cref="Ro3Feel"/> unless it names its own).
    /// RO3 Feel V2 follows the ChatGPT comparison of Willowbrook with Ragnarok Online 3 (21/11/2026,
    /// docs/reference/willowbrook-25d/willowbrook-25d-vs-ro3-feel-chatgpt.md sections 2-9, 13); camera re-done 21/11 after the camera-vs-RO3 pass
    /// (see <see cref="Ro3Feel"/>: pitch 58 deg from the horizontal = 32 deg away from straight down, our tilt convention, FOV 27, hero 69 px at 720p);
    /// sun from the upper left at 45 deg elevation; warm sun #FFF0CD on lit things, cool shade #596F61 in shadow;
    /// fog #C6D9C0 by distance beyond the hero; dappled light, motes, a few leaves, wind sway on trees.
    /// Scene scale: 1 m = 1 tile = 16 world units (the chibi hero is ~28 units, ~1.7 m). With FOV 27 deg and the RO3 zoom the camera is ~32 m from
    /// the hero and only about -8..+11 m of ground around it is on screen, so ChatGPT's camera-distance fog 18 / 50 m (which would fog the hero itself, its own
    /// warning) is applied as distance BEYOND the hero: clear around the play area, haze growing toward the far top edge.
    /// </summary>
    public sealed class View25DProfile
    {
        /// <summary>Camera tilt away from straight down, degrees (pitch from the horizontal = 90 - this).</summary>
        public float TiltDeg;
        public float FovDeg;
        /// <summary>Camera distance multiplier over the orthographic-equivalent distance: a figure at the focus shows 1 / this as tall as on the 2D camera.</summary>
        public float ViewScale = 1f;
        /// <summary>When > 0: hero height in px on a 720-px screen at the default zoom; ViewScale and LookAtHeight are then derived from it and FeetY
        /// with the build's heroScale (View25DCameraOverride.ViewScaleFor / LookAtFor), so test (1.15) and release (1) builds frame the hero alike.</summary>
        public float HeroPx;
        /// <summary>Hero feet, px from the top of a 720-px screen (used with HeroPx).</summary>
        public float FeetY = 389f;
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
        /// <summary>Snow maps: snowflakes visible at once (View25DAmbientFx.TickSnow), 0 = none.</summary>
        public int Snowfall;

        /// <summary>Every listed 2.5D map: the RO3 Feel V2 preset with the RO3 camera of 21/11 (A/B shots docs/reference/willowbrook-25d/camera-ab/,
        /// ChatGPT docs/reference/willowbrook-25d/willowbrook-25d-camera-vs-ro3-chatgpt.md preset B, kept by the camera loop docs/reference/willowbrook-25d/camera-loop/):
        /// pitch 58 deg from the horizontal (tilt 32), vertical FOV 27, hero 69 px tall at 720p in every build (camera distance follows heroScale), feet 389 px
        /// from the top (54 %). ViewScale = HeroUnits x heroScale x 720 / (216 x heroPx), LookAtHeight = (feetY - 360) x 216 x ViewScale / 720 (try others with --cam25d).
        /// Was pitch 43 / FOV 25 / ViewScale 1.11 / look-at 15 (hero 70 px, feet 406): too low, the ground ran off toward a visible far edge.</summary>
        public static readonly View25DProfile Ro3Feel = new View25DProfile
        {
            TiltDeg = 32f, FovDeg = 27f, HeroPx = 69f, FeetY = 389f, Ro3Look = true, ScreenHaze = 0.04f, Vignette = 0.04f,
        };

        // ---- dark dungeon (DungeonLights: ambient + point lights instead of the sun; only when Dungeon is true) ----
        /// <summary>Indoor map: ground, props and units are lit by AmbientColor x AmbientLevel plus the map's point lights (DungeonLights) instead of the sun tint.</summary>
        public bool Dungeon;
        public Color AmbientColor = Color.white;
        /// <summary>Ambient on the ground and props (ChatGPT 0.34 on its 0-1 scale) and the floor for heroes / monsters / pets (0.48) and bosses.</summary>
        public float AmbientLevel = 1f, UnitAmbient = 1f, BossAmbient = 1f;
        /// <summary>Darkening of a screen corner (View25DAtmosphere) in the vignette colour.</summary>
        public Color VignetteColor = Color.black;
        /// <summary>Ambient motes: warm dust (outdoor) or amber spores + cyan wisps (dungeon).</summary>
        public Color MoteColorA = new Color(1f, 0.96f, 0.84f, 0.85f), MoteColorB = new Color(0.95f, 1f, 0.78f, 0.8f);
        /// <summary>Render-only border props past the map edges (View25DGroundApron): tall ones and low ones. Null = the Willowbrook forest set.</summary>
        public string[] ApronTall, ApronSmall;
        /// <summary>Dungeon laid out east (gate) to west (deepest): the ambient follows the local hero's x from GateAmbientMul at the east edge to DeepAmbientMul at the west.</summary>
        public bool DepthWestward;
        public float GateAmbientMul = 1f, DeepAmbientMul = 1f;

        /// <summary>Rootburrow, dark violet RO3 cave (21/11/2026, ChatGPT V2 "Dark Violet Cave" after the user's correction: purple like the RO3 Irmo clip at 12:25,
        /// "giống ở trong hang"; docs/reference/rootburrow-dark-25d/rootburrow-dark-dungeon-art-direction-chatgpt.md): the RO3 camera, no sun, ambient #414064 at 0.22
        /// on the world (0.45 on units, 0.48 on the boss) with cyan rune orbs / amethyst and rose crystals / glow mushrooms as point lights (DungeonLights);
        /// very short shadows from straight above (#19172F), indigo fog #292544 from 5 m beyond the hero up to 0.40, vignette #100E25 0.32, no dapple / wind /
        /// leaves, cyan and violet motes, cave drips.</summary>
        public static readonly View25DProfile DarkDungeon = new View25DProfile
        {
            TiltDeg = 32f, FovDeg = 27f, HeroPx = 69f, FeetY = 389f, Ro3Look = true,
            Dungeon = true, AmbientColor = Hex(0x41, 0x40, 0x64), AmbientLevel = 0.22f, UnitAmbient = 0.45f, BossAmbient = 0.48f,
            SunElevationDeg = 78f, CastShorten = 0.35f, SunStrength = 0f, UnitSunStrength = 0f,
            ShadeColor = Hex(0x26, 0x23, 0x45), CrownShadeColor = Hex(0x30, 0x2C, 0x4D), UnitShadeStrength = 0.15f,
            FogColor = Hex(0x29, 0x25, 0x44), FogStart = 5f * GameConfig.TileUnits, FogEnd = 14f * GameConfig.TileUnits, FogMax = 0.40f, FogDesaturate = 0.15f,
            ScreenHaze = 0.03f, Vignette = 0.32f, VignetteColor = Hex(0x10, 0x0E, 0x25),
            Motes = 14, LeavesMax = 0, WindSway = 0f, Dapple = 0f,
            MoteColorA = new Color(0.55f, 0.88f, 1f, 0.7f), MoteColorB = new Color(0.75f, 0.6f, 1f, 0.65f),
            ApronTall = new[] { "rbStonePillar", "rbCaveWall", "rbCaveWallB", "rbPurpleCrystal", "rbPinkCrystal", "rbCaveWall", "rbHangingRoots" },
            ApronSmall = new[] { "rbCaveBoulder", "rbStoneCluster", "rbStoneCluster", "rbGlowShrooms", "rbCaveBoulder", "rbPebbles" },
        };

        /// <summary>Frost Hollow 2.5D (21/11/2026, ChatGPT snow art direction docs/reference/frost-hollow-25d/snow-25d-art-direction-chatgpt.md section 7):
        /// overcast winter day on MID-VALUE snow (the ground itself is #8999AF, not white: the translucent RO-trailer skill FX must read on it), weak warm
        /// sun #FFF0D7 (~0.7 of Willowbrook's strength), snow shade #667B8E, blue-grey fog #8799AE (light, distance only), vignette 0.07, sparse slow
        /// snowfall instead of dust / leaves, pines sway a little, no canopy dapple. Border: snowy pines, dead trees, ice rocks.</summary>
        public static readonly View25DProfile SnowField = new View25DProfile
        {
            TiltDeg = 32f, FovDeg = 27f, HeroPx = 69f, FeetY = 389f, Ro3Look = true, ScreenHaze = 0.03f, Vignette = 0.07f,
            SunColor = Hex(0xFF, 0xF0, 0xD7), SunStrength = 0.25f, UnitSunStrength = 0.18f,
            ShadeColor = Hex(0x66, 0x7B, 0x8E), CrownShadeColor = Hex(0x4E, 0x65, 0x72), UnitShadeStrength = 0.25f,
            FogColor = Hex(0x87, 0x99, 0xAE), FogStart = 4f * GameConfig.TileUnits, FogEnd = 18f * GameConfig.TileUnits, FogMax = 0.16f, FogDesaturate = 0.2f,
            Motes = 0, LeavesMax = 0, WindSway = 0.7f, Dapple = 0f, Snowfall = 28,
            ApronTall = new[] { "fhPineBig", "fhPineMid", "fhPineBig", "fhPineSlim", "fhDeadTree", "fhPineMid" },
            ApronSmall = new[] { "fhRock", "fhRockSmall", "fhBushSnow", "fhRockSlab", "fhRock", "fhCrystalSmall" },
        };

        /// <summary>Frostfang Den 2.5D (21/11/2026, same art direction section 8): dim glacial cave. DungeonLights like Rootburrow but ice: ambient #344F69
        /// (ChatGPT 0.28-0.34 on its scale; our violet cave reads right at 0.22 x 1.25 on the gate floor, so 0.30), crystal light pools #79C6D9 / teal #58B9B5,
        /// fog #263E56, vignette #0E1C2C 0.16, blue motes + ice dust, drips. The boss arena stays slate grey (lights on its rim, not in its middle).</summary>
        public static readonly View25DProfile IceDungeon = new View25DProfile
        {
            TiltDeg = 32f, FovDeg = 27f, HeroPx = 69f, FeetY = 389f, Ro3Look = true,
            Dungeon = true, AmbientColor = Hex(0x34, 0x4F, 0x69), AmbientLevel = 0.34f, UnitAmbient = 0.52f, BossAmbient = 0.56f, // 0.30 -> 0.34: ChatGPT review, corridors too dark
            SunElevationDeg = 78f, CastShorten = 0.35f, SunStrength = 0f, UnitSunStrength = 0f,
            ShadeColor = Hex(0x1C, 0x2C, 0x40), CrownShadeColor = Hex(0x26, 0x38, 0x4D), UnitShadeStrength = 0.15f,
            FogColor = Hex(0x26, 0x3E, 0x56), FogStart = 5f * GameConfig.TileUnits, FogEnd = 15f * GameConfig.TileUnits, FogMax = 0.14f, FogDesaturate = 0.1f,
            ScreenHaze = 0.02f, Vignette = 0.16f, VignetteColor = Hex(0x0E, 0x1C, 0x2C),
            Motes = 14, LeavesMax = 0, WindSway = 0f, Dapple = 0f,
            MoteColorA = new Color(0.62f, 0.9f, 1f, 0.7f), MoteColorB = new Color(0.85f, 0.95f, 1f, 0.6f),
            ApronTall = new[] { "fdIceWallA", "fdIceWallB", "fdIcePillar", "fdCrystalWall", "fdIceWallA", "fdIcicleWall" },
            ApronSmall = new[] { "fhRock", "fhRockSmall", "fhRockSlab", "fhRock" },
        };

        /// <summary>Rootburrow V4 floors (21/11/2026): darker every floor down (ChatGPT layout V4: ambient 0.30 at the gate -> 0.23 on 2F -> 0.18 on 3F), the
        /// units only half as much (they must stay readable).</summary>
        public static readonly View25DProfile DarkDungeon1F = DarkDungeon.Floor(1.25f), DarkDungeon2F = DarkDungeon.Floor(1.0f), DarkDungeon3F = DarkDungeon.Floor(0.82f);

        /// <summary>Ancient Root Shrine (Taurus phase 06, ChatGPT part2 I.1: "ancient and mysterious, not a horror dungeon, not too dark"): the dungeon pass with a warm amber ambient (#8A734A) clearly brighter than Rootburrow,
        /// moss fog #485744, light pools from the Memory Amber / boss core / rune neck (DungeonLights.Extra) and the two warp crystals, amber + teal motes. GREYBOX: the border still uses Rootburrow's wall props until the shrine art lands.</summary>
        public static readonly View25DProfile RootShrine = MakeRootShrine();

        static View25DProfile MakeRootShrine()
        {
            View25DProfile p = DarkDungeon.Floor(1f);
            p.AmbientColor = Hex(0x8A, 0x73, 0x4A); p.AmbientLevel = 0.55f; p.UnitAmbient = 0.7f; p.BossAmbient = 0.7f;
            p.ShadeColor = Hex(0x3A, 0x33, 0x24); p.CrownShadeColor = Hex(0x42, 0x3A, 0x2A);
            p.FogColor = Hex(0x48, 0x57, 0x44); p.FogMax = 0.22f; p.Vignette = 0.18f; p.VignetteColor = Hex(0x1E, 0x24, 0x1A);
            p.MoteColorA = new Color(1f, 0.84f, 0.45f, 0.7f); p.MoteColorB = new Color(0.44f, 0.83f, 0.77f, 0.6f);
            return p;
        }

        View25DProfile Floor(float ambientMul)
        {
            var p = (View25DProfile)MemberwiseClone();
            p.AmbientLevel = AmbientLevel * ambientMul;
            p.UnitAmbient = UnitAmbient * Mathf.Lerp(1f, ambientMul, 0.5f);
            p.BossAmbient = BossAmbient * Mathf.Lerp(1f, ambientMul, 0.5f);
            return p;
        }

        /// <summary>The --view25d Hydra arena prototype keeps the first 2.5D camera (38 / 22, painted walls not converted) and none of the RO3 pass.</summary>
        public static readonly View25DProfile Prototype = new View25DProfile
        {
            TiltDeg = 38f, FovDeg = 22f, ViewScale = 1f, LookAtHeight = 0f, Ro3Look = false, FogColor = new Color(0.62f, 0.80f, 0.82f),
        };

        /// <summary>Profile of a map id (a map that wants its own look adds a case here).</summary>
        /// <summary>3D map prototype (Map3D, `--map3d`; ChatGPT spec section 6): pitch 52 (tilt 38) shows more trunk height and rock sides than 58,
        /// FOV 32 (spec 35; a little narrower keeps far trees from shrinking too fast with the hero still 69 px), lighting from Map3DLighting.</summary>
        public static readonly View25DProfile Map3DFeel = new View25DProfile
        {
            TiltDeg = 38f, FovDeg = 32f, HeroPx = 69f, FeetY = 389f, Ro3Look = true, ScreenHaze = 0.03f, Vignette = 0.06f, Dapple = 0f, UnitSunStrength = 0.30f, // round 2: units take a bit more scene light
        };

        /// <summary>Taurus village / meadows (phase 04, 24/11/2026): Ro3Feel with the apron forest drawn from the maps' own golden Taurus trees and bushes, not the green wf* set.</summary>
        public static readonly View25DProfile TaurusField = MakeTaurusField();

        static View25DProfile MakeTaurusField()
        {
            var p = (View25DProfile)Ro3Feel.MemberwiseClone();
            p.ApronTall = new[] { "tauMeadowOakTree", "tauMeadowOakMediumTree", "tauYoungTree", "tauMeadowOakTree", "tauElderwoodOakTree", "tauMeadowOakMediumTree" };
            p.ApronSmall = new[] { "tauLowBush", "tauWideBush", "tauRockCluster", "tauLowBush", "tauFern", "tauPebbles" };
            return p;
        }

        public static View25DProfile For(string mapId)
        {
            if (mapId == TauWillowbrookVillageMap.MapId || mapId == TauCloverMeadowsMap.MapId) return TaurusField;
            if (Map3D.IsMap3D(mapId)) return Map3DFeel;
            if (mapId == SunkenReliquaryMap.MapId) return Prototype;
            if (mapId == RootburrowMap.MapId) return DarkDungeon1F;
            if (mapId == RootburrowB2Map.MapId) return DarkDungeon2F;
            if (mapId == FrostHollowLayout.MapId) return SnowField;
            if (mapId == FrostfangDenMap.MapId) return IceDungeon;
            if (mapId == TauAncientRootShrineMap.MapId) return RootShrine;
            return mapId == RootburrowB3Map.MapId ? DarkDungeon3F : Ro3Feel;
        }

        static Color Hex(int r, int g, int b) { return new Color(r / 255f, g / 255f, b / 255f, 1f); }
    }
}
