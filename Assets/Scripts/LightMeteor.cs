using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// One falling meteor of the light Ice Storm: a glowing head (LightBlob quad) and a TrailRenderer tail whose gradient runs white (core) -> ice blue -> transparent.
    /// The project builds its objects in code (no prefab assets), so the "prefab" is a template GameObject made once (inactive, hidden) and Instantiate'd per meteor.
    /// Smooth at 60 FPS: the meteor moves on a straight line every frame and the trail adds a point each 0.5 units, so the tail never kinks; it keeps fading after landing.
    /// </summary>
    public sealed class LightMeteor
    {
        const float HeadSize = 12f, TrailWidth = 7f, TrailSeconds = 0.22f;
        static GameObject template;

        readonly GameObject go;
        readonly MeshRenderer head;
        readonly TrailRenderer trail;
        readonly Vector2 from, to;
        float landedAt = -1f;

        /// <summary>True once the tail has faded after landing (the owner then calls Destroy).</summary>
        public bool Done(float sec) { return landedAt >= 0f && sec - landedAt > TrailSeconds + 0.05f; }

        /// <summary>p0 = fall progress on the first frame (a drop due right at the cast starts part-way down; p0 1 = already landed, nothing to draw).</summary>
        public LightMeteor(Transform parent, Vector2 from, Vector2 to, float p0)
        {
            this.from = from; this.to = to;
            go = Object.Instantiate(Template(), parent, false);
            go.name = "LightMeteor";
            head = go.transform.Find("Head").GetComponent<MeshRenderer>();
            trail = go.GetComponentInChildren<TrailRenderer>(true);
            Vector2 start = At(p0);
            go.transform.position = new Vector3(start.x, start.y, 0f); // before activating, so the tail does not streak in from the template's spot
            head.enabled = p0 < 1f;
            go.SetActive(true);
            trail.Clear();
            trail.emitting = p0 < 1f;
        }

        Vector2 At(float p) { return Vector2.Lerp(from, to, p * p * (0.6f + 0.4f * p)); } // speeds up as it falls

        /// <summary>Moves the meteor to fall progress p (0 sky .. 1 ground); sec = FX age, used to time the tail fade after landing.</summary>
        public void Tick(float p, float sec)
        {
            if (landedAt >= 0f) return;
            Vector2 pos = At(p);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            if (p < 1f) return;
            landedAt = sec;
            trail.emitting = false;
            head.enabled = false;
        }

        public void Destroy() { if (go != null) Object.Destroy(go); }

        /// <summary>The code-built "prefab": Head quad + Trail child, all sorted in the FX band above the y-sorted world (like the combo projectiles).</summary>
        static GameObject Template()
        {
            if (template != null) return template;
            template = new GameObject("LightMeteorTemplate"); // inactive scene object: a scene reload destroys it and the next meteor rebuilds it
            template.SetActive(false);
            MeshRenderer h = LightSkillAssets.Quad(template.transform, "Head", LightSkillAssets.Core, false, FeelConfig.FxOrder - 12);
            h.transform.localScale = new Vector3(HeadSize, HeadSize, 1f);

            var t = new GameObject("Trail");
            t.transform.SetParent(template.transform, false);
            TrailRenderer tr = t.AddComponent<TrailRenderer>();
            tr.sharedMaterial = LightSkillAssets.Trail;
            tr.time = TrailSeconds;
            tr.minVertexDistance = 0.5f;
            tr.numCapVertices = 4;
            tr.alignment = LineAlignment.View;
            tr.textureMode = LineTextureMode.Stretch;
            tr.sortingOrder = FeelConfig.FxOrder - 14;
            tr.widthMultiplier = TrailWidth;
            tr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.3f, 0.6f), new Keyframe(1f, 0f));
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.55f, 0.88f, 1f), 0.25f), new GradientColorKey(new Color(0.2f, 0.6f, 1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.75f, 0.35f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = g;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.emitting = false;
            return template;
        }
    }
}
