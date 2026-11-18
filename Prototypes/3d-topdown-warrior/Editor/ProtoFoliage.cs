using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Mesh-building helpers for the forest foliage: icosphere lumps with light-top / dark-bottom vertex colours (Proto/Foliage shader), tapered
/// trunks, mesh assets saved under Assets/Env/Meshes, the foliage materials and the shared-mesh prop placer. The leaf-card trees, bushes and
/// grass tufts built from these live in ProtoFoliageLeaves.cs.
/// </summary>
public static partial class ProtoBuilder
{
    /// <summary>Growing vertex / colour / index lists that end up as one mesh asset.</summary>
    class MeshBuf
    {
        public readonly List<Vector3> V = new List<Vector3>(), N = new List<Vector3>();
        public readonly List<Color> C = new List<Color>();
        public readonly List<Vector2> UV = new List<Vector2>();
        public readonly List<int> T = new List<int>();

        public Mesh Save(string path)
        {
            var m = new Mesh { name = Path.GetFileNameWithoutExtension(path) };
            if (V.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(V); m.SetNormals(N); m.SetColors(C); m.SetUVs(0, UV); m.SetTriangles(T, 0); m.RecalculateBounds();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }

    static readonly Dictionary<int, (Vector3[] v, int[] t)> icoCache = new Dictionary<int, (Vector3[], int[])>();

    /// <summary>Unit icosphere, subdivided `level` times (0 = 12 verts, 1 = 42, 2 = 162).</summary>
    static (Vector3[] v, int[] t) Ico(int level)
    {
        if (icoCache.TryGetValue(level, out var hit)) return hit;
        float p = (1f + Mathf.Sqrt(5f)) / 2f;
        var v = new List<Vector3> { new Vector3(-1, p, 0), new Vector3(1, p, 0), new Vector3(-1, -p, 0), new Vector3(1, -p, 0), new Vector3(0, -1, p), new Vector3(0, 1, p),
            new Vector3(0, -1, -p), new Vector3(0, 1, -p), new Vector3(p, 0, -1), new Vector3(p, 0, 1), new Vector3(-p, 0, -1), new Vector3(-p, 0, 1) };
        for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized;
        var t = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8, 3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
        for (int l = 0; l < level; l++)
        {
            var mid = new Dictionary<long, int>(); var nt = new List<int>();
            int Mid(int a, int b)
            {
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (mid.TryGetValue(key, out int i)) return i;
                v.Add(((v[a] + v[b]) * 0.5f).normalized); mid[key] = v.Count - 1; return v.Count - 1;
            }
            for (int i = 0; i < t.Count; i += 3)
            {
                int a = t[i], b = t[i + 1], c = t[i + 2], ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                nt.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            t = nt;
        }
        return icoCache[level] = (v.ToArray(), t.ToArray());
    }

    /// <summary>Linear-space colour on the dark -> mid -> light ramp; k 0..1.</summary>
    static Color Ramp(Color dark, Color mid, Color light, float k)
    {
        k = Mathf.Clamp01(k);
        return k < 0.5f ? Color.Lerp(dark, mid, k * 2f) : Color.Lerp(mid, light, (k - 0.5f) * 2f);
    }

    /// <summary>
    /// A lumpy ellipsoid added to `b`. Shading: 55 % from how much the surface faces up, 45 % from its height inside the crown (yMin..yMax),
    /// faces pointing down get another 20 % darker. `jitter` varies each lump's tint a little so the crown does not read as one flat colour.
    /// </summary>
    static void Lump(MeshBuf b, Vector3 c, Vector3 radii, int level, System.Random rnd, float lumpy, Color dark, Color mid, Color light, float yMin, float yMax, float jitter = 0.04f)
    {
        var (iv, it) = Ico(level);
        Vector3 k1 = Random3(rnd) * 3f, k2 = Random3(rnd) * 5f; float p1 = (float)rnd.NextDouble() * 6.3f, p2 = (float)rnd.NextDouble() * 6.3f;
        float tint = 1f + ((float)rnd.NextDouble() - 0.5f) * 2f * jitter;
        int baseIndex = b.V.Count;
        foreach (var d in iv)
        {
            float bump = 1f + lumpy * (Mathf.Sin(Vector3.Dot(d, k1) + p1) * 0.6f + Mathf.Sin(Vector3.Dot(d, k2) + p2) * 0.4f);
            Vector3 pos = c + Vector3.Scale(d, radii) * bump;
            Vector3 n = new Vector3(d.x / radii.x, d.y / radii.y, d.z / radii.z).normalized;
            float k = 0.55f * (n.y * 0.5f + 0.5f) + 0.45f * Mathf.InverseLerp(yMin, yMax, pos.y);
            Color col = Ramp(dark, mid, light, k) * tint; if (n.y < -0.3f) col *= 0.8f;
            col.a = 1f;
            b.V.Add(pos); b.N.Add(n); b.C.Add(col); b.UV.Add(Vector2.zero);
        }
        foreach (int i in it) b.T.Add(baseIndex + i);
    }

    static Vector3 Random3(System.Random rnd) => new Vector3((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f).normalized;

    /// <summary>Tapered 8-sided trunk from y0 to y1, darker at the root.</summary>
    static void Trunk(MeshBuf b, float y0, float y1, float r0, float r1, Color bark)
    {
        int baseIndex = b.V.Count, sides = 8;
        for (int i = 0; i <= sides; i++)
        {
            float a = i * Mathf.PI * 2f / sides; Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            b.V.Add(d * r0 + Vector3.up * y0); b.N.Add(d); b.C.Add(bark * 0.7f); b.UV.Add(Vector2.zero);
            b.V.Add(d * r1 + Vector3.up * y1); b.N.Add(d); b.C.Add(bark); b.UV.Add(Vector2.zero);
        }
        for (int i = 0; i < sides; i++) { int a = baseIndex + i * 2; b.T.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 }); }
    }




    /// <summary>Proto/Foliage material (+ its Proto/FoliageFade twin when `fadePath` is set) saved under Assets/Env.</summary>
    static Material FoliageMat(string name, bool doubleSided, float sway, out Material fade, string fadePath = null)
    {
        var m = new Material(Shader.Find("Proto/Foliage")) { enableInstancing = true };
        m.SetFloat("_Cull", doubleSided ? 0f : 2f); m.SetFloat("_Sway", sway);
        AssetDatabase.DeleteAsset("Assets/Env/" + name + ".mat"); AssetDatabase.CreateAsset(m, "Assets/Env/" + name + ".mat");
        fade = null;
        if (fadePath != null)
        {
            fade = new Material(Shader.Find("Proto/FoliageFade")); fade.SetFloat("_Cull", doubleSided ? 0f : 2f);
            AssetDatabase.DeleteAsset(fadePath); AssetDatabase.CreateAsset(fade, fadePath);
        }
        return m;
    }

    /// <summary>One shared-mesh prop: scaled, turned, optionally registered for occlusion fading. Grass skips shadows (too small to matter).</summary>
    static GameObject MeshProp(Transform parent, string name, Mesh mesh, Material mat, Material fade, Vector3 at, float scale, float yaw, bool castShadow = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false); go.transform.position = at; go.transform.localScale = Vector3.one * scale; go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
        r.shadowCastingMode = castShadow ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        if (fade != null) { var fm = go.AddComponent<FadeMaterials>(); fm.Opaque = mat; fm.Fade = fade; }
        return go;
    }
}
