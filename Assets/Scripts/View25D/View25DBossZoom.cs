using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// "Boss zoom" of Camera V3.1 (ChatGPT 24/11/2026, docs/reference/ro3-compare/ro3-camera-terrain-analysis-v2-chatgpt.txt section III:
    /// field zoom is the standard, town 5 % farther (Map3DVillage profile), boss "lùi 8-12 % khi cần đọc telegraph"): on a 3D map, while a living
    /// map boss stands within <see cref="Range"/> of the local hero, the camera eases back <see cref="Back"/> (camera distance only: pitch, FOV and
    /// the hero's pixel size at the default zoom stay the profile's). Read by View25D.ViewScale; evaluated once per frame, eased on unscaled time.
    /// </summary>
    public static class View25DBossZoom
    {
        /// <summary>Extra camera distance with a boss near (V3.1: 8-12 %).</summary>
        public const float Back = 1.10f;
        /// <summary>A boss within this many metres (tiles) of the hero counts (a big telegraph reaches ~6-8 m past the boss).</summary>
        public const float Range = 14f;
        const float EaseSeconds = 0.6f;

        static int frame = -1;
        static float factor = 1f, velocity;

        /// <summary>Camera distance multiplier now (1 = no boss near).</summary>
        public static float Factor
        {
            get
            {
                if (frame == Time.frameCount) return factor;
                frame = Time.frameCount;
                float want = Map3D.Active && BossNear() ? Back : 1f;
                factor = Mathf.SmoothDamp(factor, want, ref velocity, EaseSeconds, Mathf.Infinity, Mathf.Min(Time.unscaledDeltaTime, 0.1f));
                if (Mathf.Abs(factor - want) < 0.0005f) { factor = want; velocity = 0f; }
                return factor;
            }
        }

        static bool BossNear()
        {
            HeroController h = GameSession.Hero;
            if (h == null) return false;
            Vector2 p = h.Position;
            float r = Range * GameConfig.TileUnits;
            var all = Monster.All;
            for (int i = 0; i < all.Count; i++)
            {
                Monster m = all[i];
                if (m == null || m.IsDying || !m.IsBoss) continue;
                if ((m.Center - p).sqrMagnitude < r * r) return true;
            }
            return false;
        }
    }
}
