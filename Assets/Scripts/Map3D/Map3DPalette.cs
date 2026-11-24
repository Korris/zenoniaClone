using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Taurus colour and lighting V2.0 "ASTERIA Golden Adventure Daylight" (ChatGPT design chat, 24/11/2026,
    /// docs/reference/taurus-v4-art/palette/taurus-palette-spec-chatgpt.txt) for the 3D maps of Willowbrook Village and Clover Meadows: greener warm grass,
    /// honey dirt, cream plaza, cyan water, three-step crowns, sun #FFF1CF from the upper left, soft green shade (#52694F), almost no fog.
    /// The other 3D map (Willowbrook Meadows) keeps its own look: every value here is only read when <see cref="Taurus"/> is true.
    /// Tints multiply the shared Map3D textures (grass / dirt / paving swatches), so they were solved from rendered patches against the spec hexes
    /// (spec: "target average colour once rendered in game", not a flat fill).
    /// </summary>
    public static class Map3DPalette
    {
        /// <summary>The map on screen is one of the Taurus maps.</summary>
        public static bool Taurus
        {
            get
            {
                IGameMap m = MapRegistry.Current;
                return m != null && (m.Id == TauWillowbrookVillageMap.MapId || m.Id == TauCloverMeadowsMap.MapId);
            }
        }

        // ground tints (T-MAT01 #8FBE5D, T-MAT08 forest, T-MAT04 #C9A06A..#E0BC87, T-MAT05 #D7C5A5)
        public static readonly Color GrassTint = new Color(0.91f, 0.90f, 1.27f);
        public static readonly Color ForestTint = new Color(0.80f, 0.80f, 0.86f);
        public static readonly Color DirtTint = new Color(0.78f, 0.90f, 1.55f);
        public static readonly Color PavingTint = new Color(0.97f, 1.02f, 1.15f);
        public const float PavingContrast = 1.28f;                                         // V2.1: grout 10-15 % clearer; RO3 loop round 1: +10-15 % more

        // water (spec: #8EDAF5 bright, #4FB2D8 body, #247FA9 deep)
        public static readonly Color WaterShallow = new Color(0.45f, 0.72f, 0.82f, 0.50f);
        public static readonly Color WaterDeep = new Color(0.16f, 0.45f, 0.58f, 0.90f);
        public const float WaterCalm = 1f;                                                  // V2.1: glint area -40 %, contrast -22 %, thin low foam

        // crowns: lift the highlight cluster toward #C4D95B / #DCEB78, keep the green core (never all yellow); trunks warmer (#8B6238)
        public static readonly Color LeafT1Mul = new Color(1.06f, 1.10f, 0.97f);
        public static readonly Color LeafT2Mul = new Color(1.10f, 1.12f, 0.97f);
        public static readonly Color BarkTint = new Color(1.05f, 0.95f, 0.86f);

        // light
        public static readonly Color SunColor = new Color(1f, 0.941f, 0.816f);              // #FFF0D0 (V2.1)
        public const float SunElevation = 50f;
        public const float SunIntensity = 1.0f;                                             // V2.1 0.95-1.05
        public const float ShadowStrength = 0.40f;                                          // V2.1: shadows not darker, softer
        public static readonly Color AmbientSky = new Color(0.867f, 0.898f, 0.835f);        // #DDE5D5
        public const float AmbientMul = 0.75f;                                              // spec 0.70-0.85
        public static readonly Color SunGlobal = new Color(1.22f, 1.15f, 0.99f);
        public static readonly Color ShadeGlobal = new Color(0.62f, 0.72f, 0.64f);          // soft green shade (#52694F family), not cool grey
        public static readonly Color FogColor = new Color(0.863f, 0.902f, 0.788f);          // #DCE6C9
        public const float FogEndMetres = 420f;
        public const float GrassDensity = 0.65f;                                            // V2.1: 35 % fewer wild tufts (RO3 loop round 1: -40 % more + patches, Map3DGrass)
        public static readonly Color GrassTuftTint = new Color(0.99f, 1.02f, 0.94f);        // yellow tufts -15 % saturation
    }
}
