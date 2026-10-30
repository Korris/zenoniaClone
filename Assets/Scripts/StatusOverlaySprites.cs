using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Tiny generated status icons drawn over monsters: a red mark reticle (ring + 4 ticks) and a yellow star (stun). Pivot centre, sizes in world units.</summary>
    public static class StatusOverlaySprites
    {
        const int Px = 32;
        static Sprite reticle, star;

        /// <summary>14 x 14 unit red reticle.</summary>
        public static Sprite Reticle()
        {
            if (reticle != null) return reticle;
            var px = new Color32[Px * Px];
            for (int y = 0; y < Px; y++)
                for (int x = 0; x < Px; x++)
                {
                    float dx = x + 0.5f - Px * 0.5f, dy = y + 0.5f - Px * 0.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    bool ring = d > 9.5f && d < 12f;
                    bool tick = d > 6f && d <= 15.5f && (Mathf.Abs(dx) < 1.2f || Mathf.Abs(dy) < 1.2f);
                    if (ring || tick) px[y * Px + x] = new Color32(255, 70, 60, 255);
                }
            reticle = Make(px, 14f);
            return reticle;
        }

        /// <summary>5 x 5 unit yellow four-point star.</summary>
        public static Sprite Star()
        {
            if (star != null) return star;
            var px = new Color32[Px * Px];
            for (int y = 0; y < Px; y++)
                for (int x = 0; x < Px; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - Px * 0.5f) / (Px * 0.5f), dy = Mathf.Abs(y + 0.5f - Px * 0.5f) / (Px * 0.5f);
                    if (Mathf.Sqrt(dx) + Mathf.Sqrt(dy) < 1f) px[y * Px + x] = new Color32(255, 230, 80, 255);
                }
            star = Make(px, 5f);
            return star;
        }

        static Sprite Make(Color32[] px, float units)
        {
            var tex = new Texture2D(Px, Px, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, Px, Px), new Vector2(0.5f, 0.5f), Px / units, 0, SpriteMeshType.FullRect);
        }
    }
}
