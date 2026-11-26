using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// One arrow coming down in Arrow Rain / Starfall Volley (ArcherRainLightFx, 2026-11-26). The arrow is the painted arrow sprite (ArrowBoltRig,
    /// a solid object) pointing along its fall; around it light only: a soft mesh trail (rule 9) and, for Starfall's star arrows, a gold star glow
    /// on the head. Driven by fall progress so its owner lands it exactly on the drop's damage step; speeds up as it falls.
    /// </summary>
    public sealed class ArcherFallingArrow
    {
        readonly GameObject go;
        readonly ArrowBoltRig rig;
        readonly Transform trailRoot;
        readonly MeshRenderer star, core;
        readonly Vector2 from, to;
        float spin;
        bool done;

        /// <summary>`trailWidth` 0 = no trail (crowd LOD); `starPal` non-null = star arrow (gold glow on the head).</summary>
        public ArcherFallingArrow(Transform parent, Vector2 from, Vector2 to, float scale, ClericLightLook.Pal trailPal, float trailWidth, ClericLightLook.Pal starPal, float alpha)
        {
            this.from = from; this.to = to;
            go = new GameObject("ArcherFallingArrow");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(from.x, from.y, 0f);
            Vector2 d = to - from;
            rig = new ArrowBoltRig(go.transform);
            rig.Begin(from, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, scale, ClassPalette.Leaf);
            if (trailWidth > 0f)
            {
                trailRoot = new GameObject("Trail").transform;
                trailRoot.SetParent(go.transform, false);
                TrailRenderer t = ClericAttackLightFx.MakeTrail(trailRoot, trailWidth, 0.18f, alpha, trailPal);
                t.Clear();
            }
            if (starPal == null) return;
            star = LightSkillAssets.Quad(go.transform, "Star", ArcherLightLook.Glyph(ArcherLightLook.Star, starPal), false, FeelConfig.FxOrder - 10);
            star.transform.localScale = new Vector3(13f, 13f, 1f);
            LightSkillAssets.Set(star, alpha * 0.95f);
            core = LightSkillAssets.Quad(go.transform, "Core", ArcherLightLook.Flash(starPal), false, FeelConfig.FxOrder - 9);
            core.transform.localScale = new Vector3(10f, 10f, 1f);
            LightSkillAssets.Set(core, alpha * 0.75f);
        }

        /// <summary>Position at fall progress p (0 sky .. 1 ground): accelerating.</summary>
        Vector2 At(float p) { return Vector2.Lerp(from, to, p * p * (0.55f + 0.45f * p)); }

        /// <summary>Moves to fall progress p; at 1 the arrow is gone (the landing burst takes over) and the trail fades on its own.</summary>
        public void Tick(float p, float dt)
        {
            if (done || go == null) return;
            Vector2 pos = At(Mathf.Clamp01(p));
            rig.Tick(pos, dt);
            if (trailRoot != null) trailRoot.position = new Vector3(pos.x, pos.y, 0f);
            if (star != null)
            {
                spin += dt * 420f;
                star.transform.position = core.transform.position = new Vector3(pos.x, pos.y, 0f);
                star.transform.localRotation = Quaternion.Euler(0f, 0f, spin);
                star.enabled = core.enabled = FeelConfig.FxOn;
            }
            if (p < 1f) return;
            done = true;
            rig.End();
            if (star != null) star.enabled = core.enabled = false;
            Object.Destroy(go, 0.3f);
        }

        public bool Done { get { return done; } }

        public void Destroy() { if (go != null) Object.Destroy(go); }
    }
}
