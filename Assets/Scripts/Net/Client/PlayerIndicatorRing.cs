using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Online and offline: the ring under your OWN hero (sprite-rules "Quy tắc nhận diện nhân vật online" 2, user rule 19/11/2026), so you find
    /// yourself in a crowd. Shaped and coloured unlike every buff ring (those are gold / blue filled circles, ClericBuffFx): two thin soft mint-green
    /// ellipse lines with a faint glow between them, no fill, so the feet and the shadow stay visible. y-sorted with the shadow (heroes standing in
    /// front still cover it) and drawn just over the shadow, under the body. Never on other heroes.
    /// </summary>
    public class PlayerIndicatorRing : MonoBehaviour
    {
        /// <summary>World units wide (half as tall); the shadow is 12 x 6. NameTags keeps other names off this area.</summary>
        public const float Width = 20f;
        static readonly Color Mint = new Color(0.45f, 1f, 0.62f, 1f);

        SpriteRenderer ring;

        void Start()
        {
            ring = ProceduralSprites.Child(transform, "PlayerIndicatorRing", RingSprite());
            ring.transform.localScale = new Vector3(Width, Width, 1f);
        }

        void LateUpdate()
        {
            HeroController hero = LocalPlayer.Hero;
            bool show = hero != null && !hero.IsDead; // offline too (user 19/11/2026): the same ring whether you play alone or in a crowd
            ring.enabled = show;
            if (!show) return;
            Vector2 at = hero.Position;
            ring.transform.position = new Vector3(at.x, at.y, -0.1f); // ties with the shadow's order: the nearer z draws the ring over it
            ring.sortingOrder = GameConfig.OrderBase(at.y);
            float breathe = 0.78f + 0.22f * Mathf.Sin(Time.time * 2.6f); // slow and soft: a buff ring flashes, this one only breathes
            ring.color = new Color(Mint.r, Mint.g, Mint.b, breathe);
        }

        /// <summary>1 x 0.5 units once scaled to 1: an outer and an inner thin line (soft edges), a faint glow between them, a dark hairline outside
        /// the outer line so it reads on white snow. White, tinted by the renderer.</summary>
        static Sprite RingSprite()
        {
            const int w = 160, h = 80;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f - w * 0.5f) / (w * 0.5f), dy = (y + 0.5f - h * 0.5f) / (h * 0.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float line = Mathf.Max(Band(d, 0.9f, 0.035f), Band(d, 0.74f, 0.025f) * 0.85f);
                    float glow = d > 0.74f && d < 0.9f ? 0.22f : Band(d, 0.82f, 0.16f) * 0.18f;
                    float dark = Band(d, 0.955f, 0.02f) * 0.55f;
                    float a = Mathf.Max(line, glow);
                    if (a < 0.01f && dark < 0.01f) continue;
                    byte v = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(a / Mathf.Max(a + dark, 0.001f) + 0.0f));
                    px[y * w + x] = new Color32(v, v, v, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(a + dark)));
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Soft line profile: 1 at `at`, fading to 0 over `half` either side.</summary>
        static float Band(float d, float at, float half) { return Mathf.Clamp01(1f - Mathf.Abs(d - at) / half); }
    }
}
