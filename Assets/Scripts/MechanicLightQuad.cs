using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// One shader quad laid along a direction (muzzle flare, recoil streak, beam from the sky) for the Mechanic light FX: ClericLightFx layers do not
    /// turn, these do. Lives under a ClericLightFx host (ends with it, cleared with it on a map change). Size eases s0 -> s1 (length x width),
    /// alpha: quick fade in, held, smooth fade out over the second half (or `inMs` / `outMs`). Picture only.
    /// </summary>
    public sealed class MechanicLightQuad : MonoBehaviour
    {
        MeshRenderer r;
        float t0, t1, age, alpha, inMs = -1f, outMs = -1f;
        Vector2 s0, s1;
        bool standing;

        /// <summary>
        /// A quad on `host` at world `at`, turned `angleDeg` (0 = +x), from `t0` to `t1` ms (host time), size (length, width) `from` -> `to` units.
        /// standing = bottom pivot (beams standing on the ground; angle 0 then means upright), else centred; `pivotBack` = the quad starts at `at`
        /// and runs forward along the angle (flares out of a muzzle). order = sorting order.
        /// </summary>
        public static MechanicLightQuad Add(ClericLightFx host, Material m, Vector2 at, float angleDeg, float t0, float t1, Vector2 from, Vector2 to,
            float alpha, int order, bool standing = false, bool pivotBack = false)
        {
            var go = new GameObject("MechQuad");
            go.transform.SetParent(host.transform, false);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            MechanicLightQuad q = go.AddComponent<MechanicLightQuad>();
            q.r = LightSkillAssets.Quad(go.transform, m.name, m, standing, order);
            if (pivotBack) q.r.transform.localPosition = new Vector3(0.5f, 0f, 0f); // child offset in quad units, scaled with the parent
            q.r.enabled = false;
            q.t0 = t0; q.t1 = t1; q.s0 = from; q.s1 = to; q.alpha = alpha * KindDim(host); q.standing = standing;
            q.age = host.AgeMs;
            q.Apply();
            return q;
        }

        static float KindDim(ClericLightFx host) { FxTier t = host.Lod; return t == FxTier.Own ? 1f : t == FxTier.Near ? 0.85f : 0.5f; }

        /// <summary>Explicit envelope: fade in over `inMs`, fade out over the last `outMs`.</summary>
        public MechanicLightQuad Env(float fadeIn, float fadeOut) { inMs = fadeIn; outMs = fadeOut; return this; }

        public int Order { set { if (r != null) r.sortingOrder = value; } }

        void Update() { age += GameClock.Dt * 1000f; Apply(); }

        void Apply()
        {
            float span = Mathf.Max(1f, t1 - t0), local = age - t0, u = local / span;
            bool on = u >= 0f && u < 1f && FeelConfig.FxOn;
            r.enabled = on;
            if (!on) { if (u >= 1f) Destroy(gameObject); return; }
            float grow = 1f - (1f - u) * (1f - u) * (1f - u);
            Vector2 s = Vector2.LerpUnclamped(s0, s1, grow);
            transform.localScale = standing ? new Vector3(s.y, s.x, 1f) : new Vector3(s.x, s.y, 1f);
            float fi, fo;
            if (inMs >= 0f) { fi = inMs > 0f ? Mathf.Clamp01(local / inMs) : 1f; fo = outMs > 0f ? Mathf.Clamp01((span - local) / outMs) : 1f; }
            else { fi = Mathf.Clamp01(local / Mathf.Min(50f, span * 0.25f)); float k = Mathf.Clamp01((u - 0.45f) / 0.55f); fo = 1f - k * k * (3f - 2f * k); }
            LightSkillAssets.Set(r, alpha * fi * fo);
        }
    }
}
