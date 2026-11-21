using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// One falling head of an element light FX (RO3 Soft Fantasy V1, 2026-11-21: Astral Cataclysm stars, Ice Storm shards): a glyph quad (optionally
    /// turned along its fall), a small flash core and a short soft LightTrail (rule 9: 0.1-0.2 H wide, ~0.2 s, head brightest). Driven by fall
    /// progress so its owner can land it exactly on a damage step; speeds up as it falls. Replaces LightMeteor's blue HDR head (Sổ lỗi 2026-11-19).
    /// </summary>
    public sealed class ElementMeteor
    {
        readonly GameObject go;
        readonly MeshRenderer head, core;
        readonly TrailRenderer trail;
        readonly Vector2 from, to;
        readonly bool turn;
        readonly float trailSeconds;
        float landedAt = -1f;

        /// <summary>`turn`: the glyph's top points along the fall (shards); else it keeps its angle. `trailWidth` 0 = no trail (crowd LOD).</summary>
        public ElementMeteor(Transform parent, Vector2 from, Vector2 to, Material headMat, float headW, float headH, Material coreMat, float coreSize,
            ClericLightLook.Pal trailPal, float trailWidth, float trailSeconds, float alpha, bool turn)
        {
            this.from = from; this.to = to; this.turn = turn; this.trailSeconds = trailSeconds;
            go = new GameObject("ElementMeteor");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(from.x, from.y, 0f); // before the trail exists: no streak in from the origin
            head = LightSkillAssets.Quad(go.transform, "Head", headMat, false, FeelConfig.FxOrder - 12);
            head.transform.localScale = new Vector3(headW, headH, 1f);
            LightSkillAssets.Set(head, alpha);
            core = LightSkillAssets.Quad(go.transform, "Core", coreMat, false, FeelConfig.FxOrder - 11);
            core.transform.localScale = new Vector3(coreSize, coreSize, 1f);
            LightSkillAssets.Set(core, alpha * 0.8f);
            if (trailWidth > 0f)
            {
                trail = ClericAttackLightFx.MakeTrail(go.transform, trailWidth, trailSeconds, alpha, trailPal);
                trail.Clear();
            }
            if (turn)
            {
                Vector2 d = to - from;
                head.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f); // mask points up: tip leads the fall
            }
        }

        /// <summary>Position at fall progress p (0 sky .. 1 ground): accelerating.</summary>
        Vector2 At(float p) { return Vector2.Lerp(from, to, p * p * (0.6f + 0.4f * p)); }

        /// <summary>Moves to fall progress p; `sec` = owner age (times the trail fade after landing). Spins the glyph by `spinDeg` when not turned.</summary>
        public void Tick(float p, float sec, float spinDeg = 0f)
        {
            if (landedAt >= 0f || go == null) return;
            Vector2 pos = At(Mathf.Clamp01(p));
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            if (!turn && spinDeg != 0f) head.transform.localRotation = Quaternion.Euler(0f, 0f, spinDeg);
            head.enabled = core.enabled = FeelConfig.FxOn;
            if (p < 1f) return;
            landedAt = sec;
            if (trail != null) trail.emitting = false;
            head.enabled = core.enabled = false;
        }

        public bool Done(float sec) { return landedAt >= 0f && sec - landedAt > trailSeconds + 0.05f; }

        public void Destroy() { if (go != null) Object.Destroy(go); }
    }
}
