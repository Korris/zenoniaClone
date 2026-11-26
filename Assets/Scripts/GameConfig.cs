using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Shared constants. 1 world unit == 1 world pixel of the web game (hero frame = 24 units, view = 240x160 units).</summary>
    public static class GameConfig
    {
        public const int FramePx = 192;          // hero / walk-monster frame size in source pixels
        public const float FrameUnits = 24f;     // world units a hero frame covers
        public const float FramePpu = FramePx / FrameUnits; // 8 source px per world unit
        public const float BaseViewHeightUnits = 160f; // web: 960x640 canvas at 4x => 160 world px tall
        /// <summary>Gameplay camera zoom-out over the web view, everywhere (user 2026-11-19: "zoom xa luôn kể cả khi ko đánh boss"; ChatGPT camera
        /// comparison: 1.35x for a 20-40 player Wanted fight, 1.15x party, 1.5x 50+). One knob: CameraFollow fits it to small maps (no void outside).</summary>
        public const float WorldZoomOut = 1.35f;
        public const float ViewHeightUnits = BaseViewHeightUnits * WorldZoomOut; // 216
        public const int TileUnits = 16;
        /// <summary>Largest map in tiles; sizes the interest grid of every zone (a smaller map just leaves cells unused).</summary>
        public const int MaxTilesW = 100, MaxTilesH = 100; // Willowbrook Village V2 (Taurus social hub, 25/11/2026) is 100x100
        /// <summary>Size of MapRegistry.Current, the map the running code belongs to (a zone server switches it in Zone.Enter).</summary>
        public static int TilesW { get { return MapRegistry.Current.Width; } }
        public static int TilesH { get { return MapRegistry.Current.Height; } }
        public static float MapW { get { return TilesW * TileUnits; } }
        public static float MapH { get { return TilesH * TileUnits; } }
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
