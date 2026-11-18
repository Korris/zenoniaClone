using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Sees through props between the camera and the hero, Ragnarok style. Two rules (ChatGPT tree-lean fix, 18/11, after "sao cái cây vẫn bị đổ"):
///  1. Anything on the camera -> hero segment is faded (dithered trees keep 30 % of their pixels, other props swap to their Fade material).
///  2. Tall trees (> 3.5 m) in the hero's foreground zone - 0-10 m from the hero toward the camera on the ground, ±7 m to the side, measured
///     along the current camera direction - are dithered when their crown covers > 15 % of the screen (keep 15 %) or > 30 % (keep 10 %).
///     At most 2 such trees at once, the biggest first; trees on rule 1 always fade. Never the v15 "fade every foreground tree" rule.
/// Fade in 0.15 s, back to opaque in 0.25 s. No colliders needed: a prop is a group of renderers under one FadeMaterials component.
/// </summary>
public class OcclusionFade : MonoBehaviour
{
    public Transform Hero;
    public float FadedAlpha = 0.22f;

    class Group { public Renderer[] R; public Material Opaque, Fade; public bool Dither; public float Alpha = 1f, Goal = 1f, Cover; public Bounds B; }
    readonly List<Group> groups = new List<Group>();
    MaterialPropertyBlock block;
    Camera cam;
    static readonly Vector3[] corner = new Vector3[8];

    void Start()
    {
        block = new MaterialPropertyBlock();
        cam = GetComponent<Camera>();
        foreach (var fm in FindObjectsByType<FadeMaterials>(FindObjectsSortMode.None))
        {
            var rs = fm.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            groups.Add(new Group { R = rs, Opaque = fm.Opaque, Fade = fm.Fade, Dither = fm.Opaque != null && fm.Opaque.HasProperty("_Dither") });
        }
    }

    void LateUpdate()
    {
        if (Hero == null || cam == null) return;
        Vector3 a = transform.position, b = Hero.position + Vector3.up * 0.5f;
        Vector3 ab = b - a; float len = ab.magnitude; Vector3 dir = ab / len;
        Vector3 toCam = a - Hero.position; toCam.y = 0f; toCam = toCam.sqrMagnitude > 0.001f ? toCam.normalized : Vector3.back;
        Vector3 side = new Vector3(toCam.z, 0f, -toCam.x);

        var candidates = new List<Group>();
        foreach (var g in groups)
        {
            Bounds bb = g.R[0].bounds; for (int i = 1; i < g.R.Length; i++) bb.Encapsulate(g.R[i].bounds);
            g.B = bb; g.Goal = 1f; g.Cover = 0f;
            // rule 1: on the camera -> hero segment (ignore things behind the hero)
            Vector3 c = bb.center;
            float t = Mathf.Clamp(Vector3.Dot(c - a, dir), 0f, len - 0.3f);
            bool blocks = t < len - 0.3f && Vector3.Distance(a + dir * t, c) < bb.extents.magnitude * 0.55f + 0.25f;
            if (blocks) g.Goal = g.Dither ? 0.3f : FadedAlpha;
            // rule 2: tall tree in the hero's foreground zone, judged by how much of the screen it covers
            if (!g.Dither || bb.size.y < 3.5f) continue;
            Vector3 rel = bb.center - Hero.position; rel.y = 0f;
            float along = Vector3.Dot(rel, toCam), lateral = Mathf.Abs(Vector3.Dot(rel, side));
            if (along < 0f || along > 10f || lateral > 7f) continue;
            g.Cover = ScreenCover(bb);
            if (g.Cover > 0.15f) candidates.Add(g);
        }
        foreach (var g in candidates.OrderByDescending(x => x.Cover).Take(2))
            g.Goal = Mathf.Min(g.Goal, g.Cover > 0.30f ? 0.10f : 0.15f);

        foreach (var g in groups) Apply(g);
    }

    /// <summary>Screen fraction of the bounds' projected rectangle (clipped to the viewport), x0.7 because a crown is round, not a box.</summary>
    float ScreenCover(Bounds bb)
    {
        Vector3 mn = bb.min, mx = bb.max;
        for (int i = 0; i < 8; i++) corner[i] = new Vector3((i & 1) == 0 ? mn.x : mx.x, (i & 2) == 0 ? mn.y : mx.y, (i & 4) == 0 ? mn.z : mx.z);
        float x0 = 1f, y0 = 1f, x1 = 0f, y1 = 0f;
        foreach (var p in corner)
        {
            Vector3 v = cam.WorldToViewportPoint(p);
            if (v.z < 0.05f) return 1f;   // part of it is behind / at the camera: it fills the view
            x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y);
        }
        float w = Mathf.Clamp01(x1) - Mathf.Clamp01(x0), h = Mathf.Clamp01(y1) - Mathf.Clamp01(y0);
        return Mathf.Max(0f, w) * Mathf.Max(0f, h) * 0.7f;
    }

    void Apply(Group g)
    {
        if (Mathf.Approximately(g.Alpha, g.Goal)) return;
        g.Alpha = Mathf.MoveTowards(g.Alpha, g.Goal, (g.Goal < g.Alpha ? 0.9f / 0.15f : 0.9f / 0.25f) * Time.unscaledDeltaTime);   // in 0.15 s, back 0.25 s
        foreach (var r in g.R)
        {
            if (g.Dither) { r.GetPropertyBlock(block); block.SetFloat("_Dither", g.Alpha); r.SetPropertyBlock(block); continue; }
            bool faded = g.Alpha < 0.999f;
            if (faded && r.sharedMaterial != g.Fade) r.sharedMaterial = g.Fade;
            if (!faded && r.sharedMaterial != g.Opaque) { r.sharedMaterial = g.Opaque; r.SetPropertyBlock(null); continue; }
            r.GetPropertyBlock(block);
            block.SetColor("_Color", new Color(1f, 1f, 1f, g.Alpha));
            r.SetPropertyBlock(block);
        }
    }
}
