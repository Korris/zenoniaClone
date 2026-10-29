using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Tiny generated sprites: 1x1-unit white quad (bars) and a soft ellipse (shadows).</summary>
    public static class ProceduralSprites
    {
        static Sprite whiteCenter, whiteLeft, shadow;

        /// <summary>1x1 world-unit white square. pivotLeft => pivot at the left edge (for scalable bars).</summary>
        public static Sprite White(bool pivotLeft)
        {
            if (pivotLeft && whiteLeft != null) return whiteLeft;
            if (!pivotLeft && whiteCenter != null) return whiteCenter;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var white = new Color32(255, 255, 255, 255);
            tex.SetPixels32(new[] { white, white, white, white });
            tex.filterMode = FilterMode.Point;
            tex.Apply(false, true);
            Sprite s = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(pivotLeft ? 0f : 0.5f, 0.5f), 2f, 0, SpriteMeshType.FullRect);
            if (pivotLeft) whiteLeft = s; else whiteCenter = s;
            return s;
        }

        /// <summary>Soft black ellipse, 1 unit wide x 0.5 tall (scale it to the wanted size).</summary>
        public static Sprite Shadow()
        {
            if (shadow != null) return shadow;
            const int w = 64, h = 32;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f - w * 0.5f) / (w * 0.5f);
                    float dy = (y + 0.5f - h * 0.5f) / (h * 0.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((1f - d) / 0.25f); // soft rim
                    px[y * w + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            shadow = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w, 0, SpriteMeshType.FullRect);
            return shadow;
        }

        /// <summary>Creates a child SpriteRenderer with the given sprite.</summary>
        public static SpriteRenderer Child(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            return sr;
        }
    }
}
