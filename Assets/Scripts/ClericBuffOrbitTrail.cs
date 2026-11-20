using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The gold swirl of Spirit Blessing (concept tay-buff-concept-1 / 4): a small light head circling a hero on a flat ellipse, leaving a mesh
    /// trail (TrailRenderer + LightTrail), so the arc bends round the body in space. On the far half of the ellipse it sorts just behind the hero,
    /// on the near half just over the hero's own layers (never in the crowd-wide FX band). Two modes: `lifeMs` &gt; 0 = one rising spiral from
    /// y0 to y1 then it stops emitting and dies (receive burst); `lifeMs` &lt;= 0 = loops forever between y0 and y1, its owner sets Alpha and
    /// destroys it (maintained status).
    /// </summary>
    public sealed class ClericBuffOrbitTrail : MonoBehaviour
    {
        HeroController hero;
        TrailRenderer trail;
        MeshRenderer head;
        Color headColor, tailColor;
        float age, rx, ry, y0, y1, periodMs, lifeMs, phase, trailMs, dim, shownAlpha = -1f;

        /// <summary>0..1, multiplied with the spawn dim (owner fades a looping swirl in / out).</summary>
        public float Alpha = 1f;

        public static ClericBuffOrbitTrail Spawn(Transform parent, HeroController hero, ClericBuffStatusLook.Set look, float rx, float ry, float y0, float y1,
            float periodMs, float lifeMs, float phase, float width, float trailSec, float dim)
        {
            if (hero == null) return null;
            var go = new GameObject("ClericBuffSwirl");
            go.transform.SetParent(parent, false);
            var o = go.AddComponent<ClericBuffOrbitTrail>();
            o.hero = hero; o.rx = rx; o.ry = ry; o.y0 = y0; o.y1 = y1; o.periodMs = Mathf.Max(100f, periodMs); o.lifeMs = lifeMs; o.phase = phase;
            o.trailMs = trailSec * 1000f; o.dim = dim; o.headColor = look.Light; o.tailColor = look.Main;
            go.transform.position = o.Point(0f);
            o.trail = ClericAttackLightFx.MakeTrail(go.transform, width, trailSec, dim);
            o.trail.sharedMaterial = look.Trail;
            o.head = LightSkillAssets.Quad(go.transform, "Head", look.Head, false, 0);
            o.head.transform.localScale = new Vector3(width * 1.6f, width * 1.6f, 1f);
            o.Lay();
            return o;
        }

        Vector3 Point(float ms)
        {
            float a = phase + ms / periodMs * Mathf.PI * 2f;
            float u = lifeMs > 0f ? Mathf.Clamp01(ms / lifeMs) : 0.5f + 0.5f * Mathf.Sin(ms * 0.0011f);
            Vector2 p = hero.Position + new Vector2(Mathf.Cos(a) * rx, Mathf.Lerp(y0, y1, u) + Mathf.Sin(a) * ry);
            return new Vector3(p.x, p.y, 0f);
        }

        void LateUpdate()
        {
            if (hero == null || hero.IsDead) { Destroy(gameObject); return; }
            age += GameClock.Dt * 1000f;
            if (lifeMs > 0f && age > lifeMs + trailMs + 50f) { Destroy(gameObject); return; }
            Lay();
        }

        void Lay()
        {
            transform.position = Point(age);
            float a = phase + age / periodMs * Mathf.PI * 2f;
            bool near = Mathf.Sin(a) < 0f;                                                   // lower half of the ellipse = nearer the camera
            int baseOrder = GameConfig.OrderBase(hero.Position.y);
            int order = near ? baseOrder + 8 : baseOrder - 3;
            float k = Mathf.Clamp01(Alpha) * dim;
            if (lifeMs > 0f) k *= Mathf.Clamp01((lifeMs - age) / 160f) * Mathf.Clamp01(age / 60f); // eases in, fades over the spiral's last 160 ms
            bool on = FeelConfig.FxOn && k > 0.02f && (lifeMs <= 0f || age < lifeMs);
            trail.emitting = on;
            trail.sortingOrder = order;
            head.enabled = on;
            head.sortingOrder = order + 1;
            if (on) LightSkillAssets.Set(head, k);
            if (Mathf.Abs(k - shownAlpha) > 0.04f) // a gradient allocates: only when the alpha really moved
            {
                shownAlpha = k;
                var g = new Gradient();
                g.SetKeys(
                    new[] { new GradientColorKey(headColor, 0f), new GradientColorKey(tailColor, 0.4f), new GradientColorKey(tailColor, 1f) },
                    new[] { new GradientAlphaKey(k, 0f), new GradientAlphaKey(0.65f * k, 0.4f), new GradientAlphaKey(0f, 1f) });
                trail.colorGradient = g;
            }
        }
    }
}
