using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Shared constants. 1 world unit == 1 world pixel of the web game (hero frame = 24 units, view = 240x160 units).</summary>
    public static class GameConfig
    {
        public const int FramePx = 192;          // hero / walk-monster frame size in source pixels
        public const float FrameUnits = 24f;     // world units a hero frame covers
        public const float FramePpu = FramePx / FrameUnits; // 8 source px per world unit
        public const float ViewHeightUnits = 160f; // web: 960x640 canvas at 4x => 160 world px tall
        public const int TileUnits = 16;
        public const int TilesW = FrostHollowMap.TilesW;
        public const int TilesH = FrostHollowMap.TilesH;
        public const float MapW = TilesW * TileUnits;
        public const float MapH = TilesH * TileUnits;
        public static Vector2 HeroStart { get { return MapRegistry.Current.PlayerStart; } } // start tile of the map the sim runs

        public const int OrderStride = 8;        // sub-orders per entity (shadow, gear behind, body, gear front, bar, fx)
        public const int GroundOrder = -32000;

        /// <summary>Y-sort base order: lower on screen (smaller y) draws in front.</summary>
        public static int OrderBase(float y)
        {
            return -Mathf.RoundToInt(y * 4f) * OrderStride;
        }
    }

    public static class CombatMath
    {
        /// <summary>Web damage formula: max(1, round(atk * mult * variance(0.9..1.1) - def * 0.5)).</summary>
        public static int Damage(float atk, float mult, float def)
        {
            float v = Random.Range(0.9f, 1.1f);
            return Mathf.Max(1, Mathf.RoundToInt(atk * mult * v - def * 0.5f));
        }
    }
}
