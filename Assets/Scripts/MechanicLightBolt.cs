using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// A flying Mechanic shot drawn with shaders (RO3 rule 9): a glowing head turned along its flight + a short soft mesh trail (head brightest),
    /// optional smoke puffs left behind (rockets) and a growth out of the muzzle so a big head never covers the shooter. Follows a path
    /// (u 0..1 -> world point) over `ms`; with `hitRadius` > 0 it checks the monsters alive when it was fired (snapshot: one killed by this very
    /// shot still pops) and calls `onHit` once per monster it reaches (`pierce` false: stops at the first). `onEnd` gets the last point and
    /// whether anything was hit. Lives on a ClericLightFx host (ends with it, cleared with it on a map change). Picture only: damage is the sim's.
    /// </summary>
    public sealed class MechanicLightBolt : MonoBehaviour
    {
        const float SubStep = 4f;
        readonly List<Monster> candidates = new List<Monster>(16);
        Func<float, Vector2> path;
        Action<Vector2, Monster> onHit;
        Action<Vector2, bool> onEnd;
        ClericLightFx host;
        Transform rig;
        MeshRenderer head, halo;
        float haloD;
        TrailRenderer trail;
        float puffSize = 3.2f;
        float ms, age, hitR, growUnits, startSize, puffEvery, puffLeft, travelled, behindUnits, headAlpha;
        Vector2 headSize, last;
        bool pierce, any, done;
        int behindOrder;

        public static MechanicLightBolt Fire(ClericLightFx host, Func<float, Vector2> path, float ms, Material headMat, Vector2 headSize,
            ClericLightLook.Pal trailPal, float trailWidth, float trailSeconds)
        {
            var go = new GameObject("MechBolt");
            go.transform.SetParent(host.transform, false);
            MechanicLightBolt b = go.AddComponent<MechanicLightBolt>();
            b.host = host; b.path = path; b.ms = Mathf.Max(1f, ms); b.headSize = headSize; b.startSize = 1f;
            b.rig = new GameObject("Rig").transform;
            b.rig.SetParent(go.transform, false);
            b.last = path(0f);
            b.rig.position = new Vector3(b.last.x, b.last.y, 0f);
            b.headAlpha = host.Lod == FxTier.Own ? 1f : host.Lod == FxTier.Near ? 0.8f : 0.55f;
            b.head = LightSkillAssets.Quad(b.rig, "Head", headMat, false, FeelConfig.FxOrder - 12);
            float k = host.Lod == FxTier.Own ? 1f : host.Lod == FxTier.Near ? 0.6f : 0f; // raid rule 12: strangers' shots are their head only
            if (trailPal != null && k > 0f)
            {
                b.trail = ClericAttackLightFx.MakeTrail(b.rig, trailWidth * (k < 1f ? 0.8f : 1f), trailSeconds, k, trailPal);
                b.trail.Clear();
            }
            b.Place(0f);
            return b;
        }

        /// <summary>Pops on every monster within `radius` of the head (alive now), once each; `pierce` false = the shot ends at the first.</summary>
        public MechanicLightBolt Hits(float radius, bool pierceAll, Action<Vector2, Monster> hit)
        {
            hitR = radius; pierce = pierceAll; onHit = hit;
            candidates.Clear();
            foreach (Monster m in Monster.All) if (!m.IsDying) candidates.Add(m);
            return this;
        }

        public MechanicLightBolt OnEnd(Action<Vector2, bool> end) { onEnd = end; return this; }

        /// <summary>Head starts at `fromSize` of its size and is full `units` out of the muzzle (big shells).</summary>
        public MechanicLightBolt Grow(float fromSize, float units) { startSize = fromSize; growUnits = Mathf.Max(1f, units); Place(0f); return this; }

        /// <summary>Drawn just behind the shooter (sorting `order`) for its first `units` (shots fired up, away from the camera).</summary>
        public MechanicLightBolt BehindFor(int order, float units) { behindOrder = order; behindUnits = units; Place(0f); return this; }

        /// <summary>A round glow `d` units across around the head (hot core of rockets / big shots), drawn under it.</summary>
        public MechanicLightBolt Halo(Material m, float d)
        {
            halo = LightSkillAssets.Quad(rig, "Halo", m, false, FeelConfig.FxOrder - 13);
            haloD = d;
            Place(age);
            return this;
        }

        /// <summary>A smoke puff left behind every `units` travelled (rocket exhaust).</summary>
        public MechanicLightBolt Exhaust(float units, float size = 3.2f) { puffEvery = Mathf.Max(2f, units); puffLeft = 0f; puffSize = size; return this; }

        void Update()
        {
            if (done) return;
            float dt = GameClock.Dt * 1000f;
            if (dt <= 0f) return;
            float a0 = age;
            age = Mathf.Min(ms, age + dt);
            Vector2 from = path(a0 / ms), to = path(age / ms);
            float dist = Vector2.Distance(from, to);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist / SubStep));
            for (int i = 1; i <= steps && !done; i++)
            {
                Vector2 p = path(Mathf.Lerp(a0, age, i / (float)steps) / ms);
                travelled += Vector2.Distance(last, p);
                Turn(p);
                last = p;
                if (hitR > 0f) Check(p);
                Exhale(p);
            }
            Place(age);
            if (!done && age >= ms) Finish(last);
        }

        void Turn(Vector2 p)
        {
            Vector2 d = p - last;
            if (d.sqrMagnitude > 0.0004f) head.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        void Check(Vector2 p)
        {
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                Monster m = candidates[i];
                if (m == null) { candidates.RemoveAt(i); continue; }
                Rect b = m.Box;
                if (!Rpg.SkillGeometry.InCircle(p.x, p.y, hitR, b.xMin, b.yMin, b.xMax, b.yMax)) continue;
                candidates.RemoveAt(i);
                any = true;
                if (onHit != null) onHit(new Vector2(m.Center.x, Mathf.Min(p.y, m.Center.y + 4f)), m);
                if (!pierce) { Finish(p); return; }
            }
        }

        void Exhale(Vector2 p)
        {
            if (puffEvery <= 0f || host == null) return;
            puffLeft -= SubStep;
            if (puffLeft > 0f) return;
            puffLeft = puffEvery;
            if (host.Lod == FxTier.Far) return;
            float s = puffSize * UnityEngine.Random.Range(0.8f, 1.2f);
            host.Upright(MechanicLightLook.SmokeMat, ClericLightFx.Band.Over, false, host.AgeMs, host.AgeMs + UnityEngine.Random.Range(380f, 560f), s, s * 2f, s, s * 2f, 0.7f, p - host.Anchor + UnityEngine.Random.insideUnitCircle, -3)
                .Floor().Rise(5f).Dissolve();
        }

        void Place(float t)
        {
            rig.position = new Vector3(last.x, last.y, 0f);
            float g = growUnits > 0f ? Mathf.Lerp(startSize, 1f, Mathf.Clamp01(travelled / growUnits)) : 1f;
            head.transform.localScale = new Vector3(headSize.x * g, headSize.y * g, 1f);
            head.enabled = FeelConfig.FxOn;
            bool behind = behindOrder != 0 && travelled < behindUnits;
            head.sortingOrder = behind ? behindOrder : FeelConfig.FxOrder - 12;
            if (trail != null) trail.sortingOrder = behind ? behindOrder - 1 : FeelConfig.FxOrder - 14;
            float fadeIn = Mathf.Clamp01(t / 40f + 0.35f);
            LightSkillAssets.Set(head, headAlpha * fadeIn);
            if (halo != null)
            {
                halo.transform.localScale = new Vector3(haloD * g, haloD * g, 1f);
                halo.enabled = FeelConfig.FxOn;
                halo.sortingOrder = head.sortingOrder - 1;
                LightSkillAssets.Set(halo, 0.8f * headAlpha * fadeIn);
            }
        }

        void Finish(Vector2 at)
        {
            done = true;
            head.enabled = false;
            if (halo != null) halo.enabled = false;
            if (trail != null) trail.emitting = false;
            if (onEnd != null) onEnd(at, any);
            Destroy(gameObject, trail != null ? trail.time + 0.05f : 0f);
        }
    }
}
