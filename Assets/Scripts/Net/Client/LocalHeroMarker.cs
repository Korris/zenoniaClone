using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Online only: finds your own hero in a crowd when it is hidden. While another hero stands in front of yours and hides much of it, a small
    /// mint "V" above your green name, drawn over everything (a V, not a solid triangle: that reads as a quest / NPC marker); it fades in and out
    /// in about 0.15 s. The ring under the feet is PlayerIndicatorRing (its old gold ring here looked like the Cleric's gold buff ring).
    /// </summary>
    public class LocalHeroMarker : MonoBehaviour
    {
        const float ChevronY = 38f, ChevronSize = 6.5f;    // above your own name tag (+30, NameTags)
        const float CoverDx = 11f, CoverDy = 26f;          // a hero this close and lower on screen hides much of yours
        const float FadeSpeed = 7f;                        // alpha per second: about 0.15 s in or out
        static readonly Color Mint = new Color(0.45f, 1f, 0.62f, 1f); // as PlayerIndicatorRing and your green name

        SpriteRenderer chevron;
        float chevronAlpha;

        void Start()
        {
            chevron = ProceduralSprites.Child(transform, "LocalHeroChevron", ChevronSprite());
            chevron.transform.localScale = new Vector3(ChevronSize, ChevronSize, 1f);
            chevron.sortingOrder = 32767;
        }

        void LateUpdate()
        {
            HeroController hero = LocalPlayer.Hero;
            bool show = !NetMode.IsOffline && hero != null && !hero.IsDead;
            if (!show) { chevron.enabled = false; chevronAlpha = 0f; return; }

            Vector2 at = hero.Position;
            chevronAlpha = Mathf.MoveTowards(chevronAlpha, Covered(hero, at) ? 1f : 0f, FadeSpeed * Time.deltaTime);
            chevron.enabled = chevronAlpha > 0f;
            if (chevron.enabled)
            {
                float bob = Mathf.Sin(Time.time * 5f) * 1f;
                chevron.color = new Color(Mint.r, Mint.g, Mint.b, chevronAlpha);
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
