using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Thin wrappers over the shared FX helpers (UltimateFx, ShockRing, ScreenFlash, CameraFollow, GameClock) in tile / ms / 0xRRGGBB units. All no-ops without RendersFx.</summary>
    public static class SkillChoreoPrimitives
    {
        public const float Tile = GameConfig.TileUnits;
        const float FlashAlpha = 0.12f;

        public static Color Rgb(uint hex, float a = 1f) { return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f, a); }

        /// <summary>Particle burst sized from the effect radius (tiles): wider effects scatter further and throw debris harder.</summary>
        public static void Burst(ChoreoBurst b, Vector2 at, float radiusTiles, bool converge)
        {
            float r = radiusTiles * Tile;
            switch (b.Kind)
            {
                case ChoreoParticle.Embers: UltimateFx.Embers(at, b.Count, Mathf.Max(3f, r * 0.7f), 22f + r * 0.8f, Rgb(b.Color), converge ? 26f : 0f); break;
                case ChoreoParticle.Debris: UltimateFx.Debris(at, b.Count, r * 0.6f, Mathf.Min(120f, 45f + r * 1.5f)); break;
                case ChoreoParticle.Dust: UltimateFx.Dust(at, b.Count, r * 0.8f, b.Color != 0 ? Rgb(b.Color) : (Color?)null); break;
            }
        }

        public static void Ring(ChoreoRing r, Vector2 at)
        {
            int g = UltimateFx.GroundFxOrder + 6;
            ShockRing.Spawn(at, r.Radius * Tile, r.Ms / 1000f, Rgb(r.Color, 0.9f), g);
            ShockRing.Spawn(at, r.Radius * Tile * 0.62f, r.Ms / 1000f * 0.7f, Rgb(r.Color, 0.5f), g + 1, 0.72f, 0.1f, true); // inner bright echo
        }

        public static void Impact(ChoreoPhase p)
        {
            if (p.HitStopMs > 0) GameClock.HitStop(p.HitStopMs);
            if (p.Shake > 0f) CameraFollow.AddTrauma(p.Shake);
            if (p.Punch > 0f) CameraFollow.ZoomPulse(p.Punch, p.PunchMs);
            if (p.FlashMs > 0) ScreenFlash.Trigger(Rgb(p.FlashColor, FlashAlpha), p.FlashMs / 1000f);
        }

        /// <summary>Ground sprite helper: normal-blend child on the ground FX order (additive washes out to white on snow).</summary>
        public static SpriteRenderer GroundLayer(Transform parent, string name, Sprite sprite, int order)
        {
            SpriteRenderer sr = ProceduralSprites.Child(parent, name, sprite);
            sr.sortingOrder = order;
            return sr;
        }
    }
}
