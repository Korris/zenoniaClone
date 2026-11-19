using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// One meteor explosion of the light Ice Storm, all quads driven by the FX age (ms since this drop landed):
    ///  - ground light: soft round LightBlob on the ground band (below every y-sorted sprite), the URP Light2D stand-in. Intensity follows
    ///    LightSkillTiming.GroundLight: full in 0.15 s, back to 0 when the explosion ends (0.56 s). Tint pass shows on snow, HDR pass on dark ground.
    ///  - impact ring (LightBlob + burst-ring mask) growing out, radial ray burst (LightBeamScroll polar, dissolving), short light shaft.
    /// Size comes from the skill's ImpactRadius (docs/sprite-rules.md VFX rule 2).
    /// </summary>
    public sealed class LightImpact
    {
        const float RingSquash = 0.8f;         // ground ellipse: the map is seen almost from above (round warning circles)
        readonly GameObject go;
        readonly MeshRenderer ground, ring, rays, shaft;
        readonly float radius;
        readonly bool full;

        /// <summary>full = false: only the ground light and the ring (the wave pulse over the whole storm circle), no ray burst or shaft.</summary>
        public LightImpact(Transform parent, Vector2 at, float impactRadius, bool full = true)
        {
            radius = impactRadius;
            this.full = full;
            go = new GameObject("LightImpact");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            ground = LightSkillAssets.Quad(go.transform, "GroundLight", LightSkillAssets.Glow, false, UltimateFx.GroundFxOrder + 2);
            ring = LightSkillAssets.Quad(go.transform, "Ring", LightSkillAssets.Ring, false, FeelConfig.FxOrder - 18);
            if (full)
            {
                rays = LightSkillAssets.Quad(go.transform, "Rays", LightSkillAssets.Burst, false, FeelConfig.FxOrder - 17);
                shaft = LightSkillAssets.Quad(go.transform, "Shaft", LightSkillAssets.Beam, true, FeelConfig.FxOrder - 16);
            }
            else ring.sortingOrder = UltimateFx.GroundFxOrder + 3; // the storm-wide ring lies on the ground, under the heroes and monsters
            Tick(0f);
        }

        /// <summary>Lays out every layer for `ms` after landing. Returns false once the explosion is over (then call Destroy).</summary>
        public bool Tick(float ms)
        {
            float t = ms / LightSkillTiming.ExplosionMs;
            if (t >= 1f) return false;

            float light = LightSkillTiming.GroundLight(Mathf.Max(ms, 1f));
            float g = radius * ((full ? 3.0f : 2.3f) + 0.5f * t);                                  // light pool a bit wider than the blast, swelling slowly
            ground.transform.localScale = new Vector3(g, g * RingSquash, 1f);
            LightSkillAssets.Set(ground, light);

            float grow = 1f - (1f - Mathf.Min(1f, ms / 320f)) * (1f - Mathf.Min(1f, ms / 320f)); // ease-out 0..1 over 0.32 s
            float rs = radius * 2f * (0.35f + 0.75f * grow);
            ring.transform.localScale = new Vector3(rs, rs * RingSquash, 1f);
            LightSkillAssets.Set(ring, 1f - Smooth(0.25f, 0.85f, t));

            if (!full) return true;
            float bs = radius * 2.6f;
            rays.transform.localScale = new Vector3(bs, bs * RingSquash, 1f);
            LightSkillAssets.Set(rays, 1f - Smooth(0.6f, 1f, t), Smooth(0.2f, 0.9f, t), 0.15f + 0.7f * grow);

            float sh = Mathf.Min(1f, ms / 60f) * (1f - Smooth(0.15f, 0.5f, t));      // a bright shaft for ~0.25 s where the meteor struck
            shaft.transform.localScale = new Vector3(radius * 0.9f * (1f - 0.5f * t), radius * 4.2f, 1f);
            shaft.enabled = sh > 0.01f;
            LightSkillAssets.Set(shaft, sh, Smooth(0.1f, 0.5f, t));
            return true;
        }

        public void Destroy() { if (go != null) Object.Destroy(go); }

        static float Smooth(float a, float b, float x) { float k = Mathf.Clamp01((x - a) / (b - a)); return k * k * (3f - 2f * k); }
    }
}
