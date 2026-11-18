using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// "Willowbrook Glade": the real-3D forest (direction A, ChatGPT concept + spec in docs/reference/3d-warrior-prototype/forest-3d-chatgpt/).
/// A 30 x 30 m code-built terrain mesh (121 x 121 vertices, 0.25 m step) replaces the flat 2D painting: a flat grass clearing in the middle,
/// a dirt path curving from the west edge through the clearing to a wooden bridge in the north-east, a stream with a real channel under the
/// bridge, low hills (0.5-1.4 m) all round the rim where the trees stand. The walkable ground (clearing + path + bridge) is kept at y ~0 so
/// characters need no height following; TileWalk gets a generated tile grid of the same layout. Ground layers are painted into vertex
/// colours (R main grass, G light grass, B path dirt, A dark soil) and blended by the Proto/TerrainSplat shader.
/// </summary>
public static partial class ProtoBuilder
{
    const float GladeSize = 30f;
    static readonly Vector2 GladeCentre = new Vector2(15f, 15f);   // v11: ChatGPT layout centre (0,0) + 15
    // v11 path (-15,-8) -> (0,-1) -> (15,9) through the bridge, 2-3 bends
    static readonly Vector2[] GladePath = { new Vector2(-1f, 6.5f), new Vector2(5f, 9.5f), new Vector2(10f, 13.2f), new Vector2(15f, 14f), new Vector2(19.5f, 16.5f), new Vector2(21.8f, 19.8f), new Vector2(23.5f, 22.3f), new Vector2(26f, 25f), new Vector2(31f, 27f) };   // v12: through the moved bridge
    static readonly Vector2[] GladeStream = { new Vector2(19.7f, 31f), new Vector2(21.7f, 25.1f), new Vector2(23.7f, 22.1f), new Vector2(26.2f, 18.6f), new Vector2(29.2f, 16.6f), new Vector2(31f, 15.6f) };   // v12: 2-3 m closer   // v11: (7,15) -> (14,5) under the bridge
    static readonly Vector2 GladeBridge = new Vector2(23.5f, 22.3f);   // v11: (10,9), 9-11 m from the centre

    static float SegDist(Vector2 p, Vector2[] line)
    {
        float best = float.MaxValue;
        for (int i = 0; i < line.Length - 1; i++)
        {
            Vector2 a = line[i], ab = line[i + 1] - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
        }
        return best;
    }

    static float Noise(Vector2 p, float scale) => Mathf.PerlinNoise(p.x / scale + 31.7f, p.y / scale + 11.3f);

    /// <summary>1 inside the flat playable ground (clearing Ø 13 m, path 2.8 m wide, bridge), fading to 0 over ~1.5 m.</summary>
    static float GladeFlat(Vector2 p)
    {
        float clearing = 1f - Mathf.SmoothStep(0f, 1f, (Vector2.Distance(p, GladeCentre) - 5.4f + (Noise(p, 3f) - 0.5f) * 1.2f) / 1.8f);
        float path = 1f - Mathf.SmoothStep(0f, 1f, (SegDist(p, GladePath) - 1.3f) / 1.2f);
        float bridge = 1f - Mathf.SmoothStep(0f, 1f, (Vector2.Distance(p, GladeBridge) - 1.6f) / 0.8f);
        return Mathf.Clamp01(Mathf.Max(clearing, Mathf.Max(path, bridge)));
    }

    /// <summary>0 on the path core, 1 on grass; 0.6-0.9 m noisy transition (ChatGPT: smoothstep + noise so the rim is not straight).</summary>
    static float GladePathGrass(Vector2 p)
    {
        float g = Mathf.SmoothStep(0f, 1f, (SegDist(p, GladePath) - 1.05f + (Noise(p, 0.9f) - 0.5f) * 0.35f) / 0.4f);   // v14: core 2.1 m, blend 0.4 m (v13 looked smeared)
        foreach (var patch in GladePatches)   // v12: 20 loose patches 0.3-0.8 m - dirt on grass beside the path, grass islands on the path
        {
            float k = 1f - Mathf.SmoothStep(0f, 1f, (Vector2.Distance(p, new Vector2(patch.x, patch.y)) - patch.z + (Noise(p, 0.5f) - 0.5f) * 0.25f) / 0.35f);
            if (k > 0f) g = patch.w > 0f ? Mathf.Lerp(g, 1f, k) : Mathf.Min(g, 1f - k);
        }
        return g;
    }

    static Vector4[] patches;
    static Vector4[] GladePatches
    {
        get
        {
            if (patches != null) return patches;
            var rnd = new System.Random(23); var list = new System.Collections.Generic.List<Vector4>();
            while (list.Count < 38)   // v14: 29 loose + 9 worn (v13's 64 smeared the path)
            {
                int s = rnd.Next(GladePath.Length - 1); var a = GladePath[s]; var b = GladePath[s + 1];
                var q = Vector2.Lerp(a, b, (float)rnd.NextDouble()); var side = new Vector2(-(b - a).y, (b - a).x).normalized;
                bool grassIsland = list.Count % 3 == 0 && list.Count >= 9;   // first 9 = worn dirt tongues at the path rim
                var c = q + side * ((float)rnd.NextDouble() - 0.5f) * (grassIsland ? 1.2f : 3.6f);
                if (!grassIsland && SegDistRaw(c) < 1.4f) continue;
                list.Add(new Vector4(c.x, c.y, 0.3f + (float)rnd.NextDouble() * 0.5f, grassIsland ? 1f : 0f));   // v14: no patch over 0.8 m
            }
            return patches = list.ToArray();
        }
    }

    static float SegDistRaw(Vector2 p) => SegDist(p, GladePath);   // v11: 2.4 m wide, rim noise ±0.3 m

    static float GladeStreamDepth(Vector2 p) => 1f - Mathf.SmoothStep(0f, 1f, (SegDist(p, GladeStream) - 0.6f) / 0.7f);   // v13: ~1.5 m wide   // 1 in the channel, 2 m wide with sloped banks

    static float GladeHeight(Vector2 p)
    {
        float flat = GladeFlat(p), rim = 1f - flat;
        float hills = (1.0f + 1.0f * Noise(p, 6f)) * rim * rim + (Noise(p, 1.3f) - 0.5f) * 0.25f * rim;   // v11: rim hills +1.0..+2.0 m + bumps
        float h = hills + (Noise(p, 2.2f) - 0.5f) * 0.04f * flat;                                           // walkable ground stays at ~0
        return h - 0.65f * GladeStreamDepth(p);   // stream bed -0.65 m (the bridge spans it)
    }

    /// <summary>Writes the TileWalk grid (0.8 m tiles, row 0 = north): g = walkable, B = bridge, w = stream, T = rim / forest.</summary>
    static string GladeRows(string path)
    {
        int n = Mathf.CeilToInt(GladeSize / Tile);
        var sb = new StringBuilder();
        for (int ty = 0; ty < n; ty++)
        {
            for (int tx = 0; tx < n; tx++)
            {
                var p = new Vector2((tx + 0.5f) * Tile, (n - 1 - ty + 0.5f) * Tile);
                bool onBridge = Vector2.Distance(p, GladeBridge) < 1.7f && SegDist(p, GladePath) < 1.6f;
                sb.Append(onBridge ? 'B' : GladeStreamDepth(p) > 0.4f ? 'w' : GladeFlat(p) > 0.55f ? 'g' : 'T');
            }
            sb.Append('\n');
        }
        File.WriteAllText(path, sb.ToString()); AssetDatabase.ImportAsset(path);
        return path;
    }

    /// <summary>Terrain mesh + splat colours + the stream water sheet.</summary>
    static void GladeTerrain()
    {
        // tree-lean fix (ChatGPT 18/11): 50 x 50 m backdrop round the 30 x 30 m play area so the camera never sees past the map edge
        const int N = 201; const float step = 0.25f, origin = -10f;
        var v = new Vector3[N * N]; var c = new Color[N * N]; var uv = new Vector2[N * N];
        for (int z = 0; z < N; z++) for (int x = 0; x < N; x++)
        {
            var p = new Vector2(origin + x * step, origin + z * step); int i = z * N + x;
            v[i] = new Vector3(p.x, GladeHeight(p), p.y); uv[i] = p;
            float dirt = 1f - GladePathGrass(p), flat = GladeFlat(p), stream = GladeStreamDepth(p);   // (patches come through GladePathGrass)
            float soil = Mathf.Clamp01((1f - flat) * 0.55f + stream * 0.9f + (Noise(p, 2f) - 0.6f) * 0.8f * (1f - flat));
            float light = Mathf.Clamp01((Noise(p, 4f) - 0.45f) * 2.5f) * flat;
            float grass = Mathf.Max(0f, 1f - dirt - soil - light);
            float sum = grass + light + dirt + soil;
            c[i] = new Color(grass / sum, light / sum, dirt / sum, soil / sum);
        }
        var t = new int[(N - 1) * (N - 1) * 6]; int k = 0;
        for (int z = 0; z < N - 1; z++) for (int x = 0; x < N - 1; x++)
        {
            int a = z * N + x, b = a + 1, d = a + N, e = d + 1;
            t[k++] = a; t[k++] = d; t[k++] = b; t[k++] = b; t[k++] = d; t[k++] = e;
        }
        var mesh = new Mesh { name = "glade-terrain", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = v; mesh.colors = c; mesh.uv = uv; mesh.triangles = t; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        Directory.CreateDirectory("Assets/Env/Meshes");
        AssetDatabase.DeleteAsset("Assets/Env/Meshes/glade-terrain.asset"); AssetDatabase.CreateAsset(mesh, "Assets/Env/Meshes/glade-terrain.asset");
        var go = new GameObject("Terrain");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = GladeTerrainMaterial();

        // stream water: a strip following the channel at -0.40 m, #7EAFAC alpha 0.72, glossy
        var water = new Material(Shader.Find("Proto/Water"));   // v16: #7FAEAA + two ripple layers + thin bank foam (ProtoWater.shader)
        AssetDatabase.DeleteAsset("Assets/Env/GladeWater.mat"); AssetDatabase.CreateAsset(water, "Assets/Env/GladeWater.mat");
        var wb = new MeshBuf();
        for (int i = 0; i < GladeStream.Length - 1; i++)
        {
            Vector2 a = GladeStream[i], b = GladeStream[i + 1], side = new Vector2(-(b - a).y, (b - a).x).normalized * 1.05f;
            int s = wb.V.Count;
            var corners = new[] { a - side, a + side, b + side, b - side }; var across = new[] { 0f, 1f, 1f, 0f };   // uv.x = across the strip (foam at 0 and 1)
            for (int ci = 0; ci < 4; ci++) { var q = corners[ci]; wb.V.Add(new Vector3(q.x, -0.4f, q.y)); wb.N.Add(Vector3.up); wb.C.Add(Color.white); wb.UV.Add(new Vector2(across[ci], 0f)); }
            wb.T.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
        }
        var wgo = new GameObject("Stream");
        wgo.AddComponent<MeshFilter>().sharedMesh = wb.Save("Assets/Env/Meshes/glade-stream.asset");
        var wr = wgo.AddComponent<MeshRenderer>(); wr.sharedMaterial = water; wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// <summary>Proto/TerrainSplat with the four ground textures (ChatGPT tileable swatches when present, else plain tinted noise).</summary>
    static Material GladeTerrainMaterial()
    {
        var m = new Material(Shader.Find("Proto/TerrainSplat"));
        (string name, string hex, float tile)[] layers = { ("grass", "789647", 2f), ("grass-light", "A1B76B", 3f), ("dirt", "CBA16B", 2.5f), ("soil", "806B49", 2f) };
        for (int i = 0; i < 4; i++)
        {
            string png = "Assets/Env/glade-" + layers[i].name + ".png";
            var tex = File.Exists(png) ? AssetDatabase.LoadAssetAtPath<Texture2D>(png) : NoiseSwatch(png, Hex(layers[i].hex), i);
            m.SetTexture("_Tex" + i, tex); m.SetFloat("_Tile" + i, 1f / layers[i].tile);
        }
        AssetDatabase.DeleteAsset("Assets/Env/GladeTerrain.mat"); AssetDatabase.CreateAsset(m, "Assets/Env/GladeTerrain.mat");
        return m;
    }

    /// <summary>Fallback ground swatch: the ChatGPT hex colour with soft two-scale value noise (seamless), until a painted swatch exists.</summary>
    static Texture2D NoiseSwatch(string png, Color baseColor, int seed)
    {
        const int S = 256;
        // seamless noise = sum of 24 plane waves with integer frequencies (v9 test: torus-sampled Perlin showed a diamond pattern)
        var rnd = new System.Random(17 + seed); var waves = new Vector4[24];
        for (int i = 0; i < waves.Length; i++) waves[i] = new Vector4(rnd.Next(-9, 10), rnd.Next(-9, 10), (float)rnd.NextDouble() * 6.28f, 0.5f + (float)rnd.NextDouble());
        return CodeTexture(png, S, S, (u, v) =>
        {
            float n = 0f;
            foreach (var w in waves) n += Mathf.Sin(2f * Mathf.PI * (w.x * u + w.y * v) + w.z) * w.w;
            Color c = baseColor * (1f + n * 0.025f); c.a = 1f; return c;
        }, TextureWrapMode.Repeat);
    }
}
