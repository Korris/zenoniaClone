using UnityEngine;

/// <summary>
/// A curved ribbon mesh for skill effects (Power Slash crescent, Whirlwind wind bands): an arc of `ArcDeg` degrees, outer radius 1 (scaled by
/// the transform), `Thick` wide in the middle and tapered to nothing at both tips, drawn twice: an alpha-blended coloured body and an additive
/// thin core. Paint(head, tail, alpha) reveals the part between tail and head (0..1 along the arc), so the arc can sweep in and be eaten
/// from the back. Vertex colours are rewritten in place (one array per ribbon): no allocation per frame.
/// </summary>
public class ArcRibbon3D
{
    const int Seg = 24;
    public readonly GameObject Go;
    readonly Mesh mesh;
    readonly Color[] colors;
    readonly MeshRenderer body, core;
    readonly MaterialPropertyBlock mpb = new MaterialPropertyBlock();
    static readonly int TintId = Shader.PropertyToID("_TintColor");

    /// <summary>Local frame: the arc lies in the XY plane around the origin, +X = its middle, the sweep starts at +Y.</summary>
    public ArcRibbon3D(string name, float arcDeg, float thick, Material bodyMat, Material coreMat, Color bodyTint, Color coreTint)
    {
        mesh = new Mesh { name = name };
        var v = new Vector3[(Seg + 1) * 2]; var uv = new Vector2[v.Length]; var t = new int[Seg * 6];
        for (int i = 0; i <= Seg; i++)
        {
            float u = (float)i / Seg, a = Mathf.Lerp(arcDeg / 2f, -arcDeg / 2f, u) * Mathf.Deg2Rad;
            float w = thick * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * u)), 0.7f);   // Max(0): Sin(PI) is slightly negative and Pow would give NaN
            Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            v[i * 2] = d * (1f - w); v[i * 2 + 1] = d;
            uv[i * 2] = new Vector2(u, 0f); uv[i * 2 + 1] = new Vector2(u, 1f);
        }
        for (int i = 0; i < Seg; i++) { int q = i * 2, k = i * 6; t[k] = q; t[k + 1] = q + 1; t[k + 2] = q + 2; t[k + 3] = q + 1; t[k + 4] = q + 3; t[k + 5] = q + 2; }
        colors = new Color[v.Length];
        mesh.vertices = v; mesh.uv = uv; mesh.triangles = t; mesh.colors = colors; mesh.RecalculateBounds();
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2.2f);

        Go = new GameObject(name);
        body = Part(Go.transform, name + "Body", bodyMat, bodyTint);
        core = Part(Go.transform, name + "Core", coreMat, coreTint);
        Go.SetActive(false);
    }

    MeshRenderer Part(Transform parent, string name, Material m, Color tint)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        mpb.SetColor(TintId, tint); r.SetPropertyBlock(mpb);
        return r;
    }

    public void Show(bool on) { if (Go.activeSelf != on) Go.SetActive(on); }

    /// <summary>Places the arc: `center`, its middle pointing along `mid`, its plane containing `side` (the direction the sweep starts from).</summary>
    public void Place(Vector3 center, Vector3 mid, Vector3 side, float radius)
    {
        Go.transform.position = center;
        Go.transform.rotation = Quaternion.LookRotation(Vector3.Cross(mid, side), side);
        Go.transform.localScale = Vector3.one * radius;
    }

    /// <summary>Shows the arc between `tail` and `head` (0..1 along it) with soft 0.15 edges, brighter in the middle, times `alpha`.</summary>
    public void Paint(float head, float tail, float alpha)
    {
        for (int i = 0; i <= Seg; i++)
        {
            float u = (float)i / Seg;
            float a = alpha * Mathf.Clamp01((head - u) / 0.15f) * Mathf.Clamp01((u - tail) / 0.15f) * (0.5f + 0.5f * Mathf.Sin(Mathf.PI * u));
            colors[i * 2] = colors[i * 2 + 1] = new Color(1f, 1f, 1f, a);
        }
        mesh.colors = colors;
    }
}
