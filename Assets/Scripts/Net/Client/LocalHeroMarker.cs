using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Online only: finds your own hero in a crowd. A gold ring under the feet (y-sorted like the shadow, so heroes standing in front
    /// still cover it) and, while another hero stands in front of yours and hides much of it, a small gold "V" just above the head drawn over
    /// everything (a V, not a solid triangle: that reads as a quest / NPC marker). It fades in and out in about 0.15 s.
    /// </summary>
    public class LocalHeroMarker : MonoBehaviour
    {
        const float RingW = 22f;                          // world units wide (11 tall); the shadow is 12 x 6
        const float ChevronY = 30f, ChevronSize = 6.5f;    // just above the hair (name tags of others sit at +30)
        const float CoverDx = 11f, CoverDy = 26f;          // a hero this close and lower on screen hides much of yours
        const float FadeSpeed = 7f;                        // alpha per second: about 0.15 s in or out
        static readonly Color Gold = new Color(1f, 0.72f, 0.05f, 1f);

        SpriteRenderer ring, chevron;
        float chevronAlpha;

        void Start()
        {
            ring = ProceduralSprites.Child(transform, "LocalHeroRing", RingSprite());
            ring.transform.localScale = new Vector3(RingW, RingW, 1f);
            chevron = ProceduralSprites.Child(transform, "LocalHeroChevron", ChevronSprite());
            chevron.transform.localScale = new Vector3(ChevronSize, ChevronSize, 1f);
            chevron.sortingOrder = 32767;
        }

        void LateUpdate()
        {
            HeroController hero = LocalPlayer.Hero;
            bool show = !NetMode.IsOffline && hero != null && !hero.IsDead;
            ring.enabled = show;
            if (!show) { chevron.enabled = false; chevronAlpha = 0f; return; }

            Vector2 at = hero.Position;
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 4f);
            ring.transform.position = new Vector3(at.x, at.y, -0.1f); // ties with the shadow's order: the nearer z draws the ring over it
            ring.sortingOrder = GameConfig.OrderBase(at.y);
            ring.color = new Color(Gold.r, Gold.g, Gold.b, pulse);

            chevronAlpha = Mathf.MoveTowards(chevronAlpha, Covered(hero, at) ? 1f : 0f, FadeSpeed * Time.deltaTime);
            chevron.enabled = chevronAlpha > 0f;
            if (chevron.enabled)
            {
                float bob = Mathf.Sin(Time.time * 5f) * 1f;
                chevron.color = new Color(Gold.r, Gold.g, Gold.b, chevronAlpha);
                chevron.transform.position = new Vector3(at.x, at.y + ChevronY + bob, -0.1f);
            }
        }

        /// <summary>True when another living hero stands just in front of yours (lower on screen) and close enough to hide the body.</summary>
        static bool Covered(HeroController hero, Vector2 at)
        {
            Zone zone = Zone.Main;
            if (zone == null) return false;
            var all = zone.Heroes.All;
            for (int i = 0; i < all.Count; i++)
            {
                HeroController h = all[i];
                if (h == null || h == hero || h.IsDead) continue;
                Vector2 p = h.Position;
                float dy = at.y - p.y;
                if (dy > 0f && dy < CoverDy && Mathf.Abs(p.x - at.x) < CoverDx) return true;
            }
            return false;
        }

        /// <summary>Ellipse ring (1 x 0.5 units once scaled to 1): a thick white band (tinted by the renderer) inside a dark rim, so it reads
        /// on white snow as well as on dark ground, and a faint fill.</summary>
        static Sprite RingSprite()
        {
            const int w = 128, h = 64;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f - w * 0.5f) / (w * 0.5f), dy = (y + 0.5f - h * 0.5f) / (h * 0.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color32 c;
                    if (d > 0.98f) continue;
                    if (d > 0.9f || (d > 0.68f && d < 0.73f)) c = new Color32(70, 40, 0, 230);      // dark rim outside and inside the band
                    else if (d >= 0.73f) c = new Color32(255, 255, 255, 255);                         // the band
                    else c = new Color32(255, 255, 255, 45);                                          // faint fill
                    px[y * w + x] = c;
                }
            return MakeSprite(px, w, h);
        }

        /// <summary>Downward "V": two thick strokes meeting at the bottom, white (tinted by the renderer) inside a dark outline, 1 x 1 units.</summary>
        static Sprite ChevronSprite()
        {
            const int s = 40;
            var px = new Color32[s * s];
            Vector2 apex = new Vector2(0.5f, 0.12f), left = new Vector2(0.08f, 0.9f), right = new Vector2(0.92f, 0.9f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    var p = new Vector2((x + 0.5f) / s, (y + 0.5f) / s);
                    float d = Mathf.Min(SegmentDistance(p, apex, left), SegmentDistance(p, apex, right));
                    if (d < 0.09f) px[y * s + x] = new Color32(255, 255, 255, 255);   // the stroke
                    else if (d < 0.14f) px[y * s + x] = new Color32(50, 28, 0, 255);  // dark outline
                }
            return MakeSprite(px, s, s);
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        static Sprite MakeSprite(Color32[] px, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w, 0, SpriteMeshType.FullRect);
        }
    }
}
