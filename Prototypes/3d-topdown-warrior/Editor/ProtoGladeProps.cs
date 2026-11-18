using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Glade dressing and light (ChatGPT forest spec, forest-3d-spec-chatgpt.txt): the six Tripo models placed after the concept (5 big root
/// trees on the left / top / foreground rim, 5 pines on the right and back, 2 stumps above the clearing, 7 rock clusters by the path, trees
/// and stream, 1 bridge over the stream, 18 bushes round the clearing), random scale and yaw, plus code grass / flower tufts from the
/// ChatGPT leaf atlas. Models live in Assets/Glade/&lt;name&gt;/&lt;name&gt;.fbx with &lt;name&gt;_basecolor / _normal maps; a missing model is skipped.
/// </summary>
public static partial class ProtoBuilder
{
    // v11 (ChatGPT layout + 15): 7 big trees, 7 pines on the north / east rim, 2 stumps, 10 rocks (6 rim, 3 path, 1 stream)
    static readonly Vector2[] GladeBigTrees = { new Vector2(3f, 6f), new Vector2(8f, 19.5f), new Vector2(6.5f, 4f), new Vector2(12f, 26.3f), new Vector2(23.8f, 27.3f), new Vector2(24.2f, 11f), new Vector2(28f, 15.5f) };   // v12; v16: 2 trees moved out of the foreground zone (ChatGPT: x 9-21, z 7-15 in front of the spawn)
    static readonly Vector2[] GladePines = { new Vector2(8f, 29.5f), new Vector2(15.5f, 29.8f), new Vector2(17.5f, 27f), new Vector2(29.5f, 11.5f), new Vector2(29.2f, 8f), new Vector2(29f, 1.5f), new Vector2(23f, 1f) };
    static readonly Vector2[] GladeStumps = { new Vector2(7f, 20f), new Vector2(20f, 25f) };
    static readonly Vector2[] GladeRocks = { new Vector2(1.5f, 11f), new Vector2(8.5f, 26.5f), new Vector2(27.5f, 29f), new Vector2(26f, 9f), new Vector2(20f, 4f), new Vector2(9f, 3f), new Vector2(7f, 12.5f), new Vector2(18.5f, 13f), new Vector2(19.5f, 19.8f), new Vector2(24.6f, 19.6f), new Vector2(21.4f, 23.6f) };   // v12: + bank rocks at both bridge ends

    /// <summary>A Tripo model as a prop: Standard material from its maps (+ fade twin for camera occlusion), scaled to `height` metres, yaw, set on the terrain.</summary>
    static GameObject GladeModel(Transform parent, string name, Vector2 at, float height, float yaw, bool fade = true)
    {
        string fbx = "Assets/Glade/" + name + "/" + name + ".fbx";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        if (model == null) return null;
        string dir = "Assets/Glade/" + name + "/" + name;
        // one material per model kind per build (v15 fix: the old "load the .mat if it exists" kept the first build's material forever, so the
        // v13-v15 colour tweaks never reached the reused C:/temp project)
        bool canopy = name == "bigtree" || name == "pine" || name == "bush" || name == "rocks";   // v17: rocks share the shader for moss / sun-facing tint
        if (!gladeMat.TryGetValue(name, out var mat))
        {
            string normal = File.Exists(dir + "_normal.jpg") ? dir + "_normal.jpg" : null;
            if (canopy)
            {
                // Proto/Canopy (v15, ChatGPT item 1): leaf volume shading; crown height range from the model's mesh bounds (object space)
                mat = new Material(Shader.Find("Proto/Canopy")) { mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "_basecolor.jpg") };
                if (normal != null) mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
                // world-up expressed in the mesh's object space (Tripo FBX children may be axis-converted), and the vertex range along it
                var mf = model.GetComponentInChildren<MeshFilter>();
                Vector3 up = mf != null ? mf.transform.InverseTransformDirection(Vector3.up).normalized : Vector3.up;
                float lo = float.MaxValue, hi = float.MinValue;
                if (mf != null) foreach (var vtx in mf.sharedMesh.vertices) { float d = Vector3.Dot(vtx, up); lo = Mathf.Min(lo, d); hi = Mathf.Max(hi, d); }
                if (lo > hi) { lo = 0f; hi = 1f; }
                mat.SetVector("_UpObj", up); mat.SetFloat("_MinY", lo); mat.SetFloat("_MaxY", hi);
                AssetDatabase.DeleteAsset(dir + ".mat"); AssetDatabase.CreateAsset(mat, dir + ".mat");
            }
            else mat = Mat(dir + ".mat", dir + "_basecolor.jpg", normal, Color.white);
            mat.SetFloat("_Metallic", 0f); mat.SetFloat("_Glossiness", 0.03f);   // v13: smoothness 0-0.05 (painterly, no plastic sheen)
            if (name == "bush") mat.color = new Color(1.21f, 1.24f, 1.14f);   // v15: +10 % (dark bush blocks)
            if (name == "bigtree" || name == "pine") mat.color = new Color(1.02f, 1.0f, 0.93f);   // v13: warmer leaves
            if (name == "rocks") { mat.color = new Color(1.22f, 1.2f, 1.14f); mat.SetFloat("_BumpScale", 0.8f); mat.SetFloat("_Moss", 0.18f); }   // v15: lighter stone, normal -20 %
            gladeMat[name] = mat;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
        go.transform.SetParent(parent, true);
        foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
        // keep the FBX root's own import rotation (Tripo files are Z-up: the root carries -90° X). Overwriting it with a pure yaw laid every tree
        // on its side - the "cây bị đổ" the user kept seeing since v12 (bounds were 6-12 m wide but only 4-6 m tall)
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * model.transform.rotation;
        FitHeight(go, new Vector3(at.x, GladeHeight(at) - 0.05f, at.y), height);
        if (fade)
        {
            // one fade twin per model kind: FadeTwin deletes and recreates its asset, so calling it per instance left the earlier trees
            // pointing at a deleted material (magenta when OcclusionFade swapped it in, v14 test)
            if (!gladeFade.TryGetValue(name, out var fadeMat))
            {
                if (canopy) { fadeMat = new Material(mat) { shader = Shader.Find("Proto/CanopyFade") }; AssetDatabase.DeleteAsset(dir + "-fade.mat"); AssetDatabase.CreateAsset(fadeMat, dir + "-fade.mat"); }
                else fadeMat = FadeTwin(mat, dir + "-fade.mat");
                gladeFade[name] = fadeMat;
            }
            var fm = go.AddComponent<FadeMaterials>(); fm.Opaque = mat; fm.Fade = fadeMat;
        }
        return go;
    }

    static readonly System.Collections.Generic.Dictionary<string, Material> gladeFade = new System.Collections.Generic.Dictionary<string, Material>(), gladeMat = new System.Collections.Generic.Dictionary<string, Material>();

    /// <summary>Distance (m) from p to the nearest walkable ground (GladeFlat > 0.55, as in GladeRows), sampled on a 0.4 m grid out to 9 m.</summary>
    static float WalkDist(Vector2 p)
    {
        float best = 9f;
        for (float dx = -9f; dx <= 9f; dx += 0.4f) for (float dz = -9f; dz <= 9f; dz += 0.4f)
        {
            float d = Mathf.Sqrt(dx * dx + dz * dz); if (d >= best) continue;
            var q = new Vector2(p.x + dx, p.y + dz);
            if (q.x >= 0f && q.y >= 0f && q.x <= GladeSize && q.y <= GladeSize && GladeFlat(q) > 0.55f) best = d;
        }
        return best;
    }

    static void GladeProps(System.Random rnd, Vector3 heroStart)
    {
        gladeFade.Clear(); gladeMat.Clear();
        var parent = new GameObject("Props").transform;
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        int placed = 0;
        var footMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Env/BlobShadow.mat");
        void PutTree(string name, Vector2 p, float h)
        {
            float d = WalkDist(p);
            if (d < 3f) { Put("bush", p, 1.6f, 0.1f); return; }
            Put(name, p, Mathf.Min(h, d < 5f ? 4.5f : d < 8f ? 5.5f : h), 0.08f);
        }
        void Put(string name, Vector2 p, float h, float scaleJitter, float yaw = -1f)
        {
            if (GladeModel(parent, name, p, h * R(1f - scaleJitter, 1f + scaleJitter), yaw < 0f ? R(0f, 360f) : yaw) != null)
            {
                placed++;
                if (name != "bridge" && footMat != null) Decal(parent, new Vector3(p.x, GladeHeight(p) + 0.01f, p.y), name == "bigtree" ? 3.2f : name == "pine" ? 2.4f : name == "rocks" ? 2.6f : 1.8f, footMat);   // v13: feet 8-12 % darker
            }
        }
        // v14: foreground (south, low z) trees 12 % smaller so they frame instead of walling the camera
        // tree-lean fix (ChatGPT 18/11): cap tree height by the distance from its foot to the nearest walkable ground -
        // < 3 m: no tall tree (a 1.6 m bush instead), 3-5 m: <= 4.5 m, 5-8 m: <= 5.5 m, farther: the full 6-7 m
        foreach (var p in GladeBigTrees) PutTree("bigtree", p, p.y < 10f ? 6.2f : 7f);
        foreach (var p in GladePines) PutTree("pine", p, p.y < 10f ? 5.3f : 6f);
        // backdrop forest outside the play area (no gameplay, just fills the view at the map edge)
        var brnd = new System.Random(11);
        for (int i = 0; i < 46; i++)
        {
            float bx = -8.5f + (float)brnd.NextDouble() * 47f, bz = -8.5f + (float)brnd.NextDouble() * 47f;
            if (bx > -1.5f && bx < 31.5f && bz > -1.5f && bz < 31.5f) { i--; continue; }   // only the ring outside the 30 x 30 m map
            Put(brnd.NextDouble() < 0.5 ? "bigtree" : "pine", new Vector2(bx, bz), 6f, 0.15f);
        }
        foreach (var p in GladeStumps) Put("stump", p, 1.3f, 0.1f);
        foreach (var p in GladeRocks) Put("rocks", p, 1.4f, 0.2f);
        foreach (var p in new[] { new Vector2(22f, 24.6f), new Vector2(25.4f, 20.4f), new Vector2(20.6f, 21.6f), new Vector2(7.6f, 23.8f), new Vector2(8.8f, 24.6f), new Vector2(25.6f, 13.6f), new Vector2(26.2f, 12.4f), new Vector2(11.2f, 19.8f), new Vector2(19.6f, 11.6f), new Vector2(9.6f, 15.8f), new Vector2(11.5f, 8.2f), new Vector2(19f, 8.6f) }) Put("bush", p, 1.3f, 0.1f);   // + v14 three bushes pulled in toward the clearing   // v12 bridge + v13 top-left / right clusters   // v12: bushes round the bridge
        Vector2 dir = GladePath[7] - GladePath[5];
        Put("bridge", GladeBridge, 1.2f, 0f, Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg);
        // v11: 28 bushes in clusters - 20 at tree feet / rim (2-3 per cluster), 8 beside the path - never on the path, in the stream or the 6.5 m clearing
        bool BushOk(Vector2 p) => SegDist(p, GladePath) > 1.8f && GladeStreamDepth(p) < 0.1f && Vector2.Distance(p, GladeCentre) > 5.9f && p.x > 0.5f && p.y > 0.5f && p.x < 29.5f && p.y < 29.5f;
        var anchors = GladeBigTrees.Concat(GladePines).ToArray();
        for (int i = 0, tries = 0; i < 20 && tries < 600; tries++)
        {
            var c = anchors[rnd.Next(anchors.Length)]; float a = R(0f, Mathf.PI * 2f);
            var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * R(1.6f, 3.2f);
            if (!BushOk(p)) continue; Put("bush", p, 1.3f, 0.08f); i++;   // v12: x1.2-1.4
        }
        for (int i = 0, tries = 0; i < 8 && tries < 400; tries++)
        {
            int s = rnd.Next(GladePath.Length - 1); var a = GladePath[s]; var b = GladePath[s + 1];
            var q = Vector2.Lerp(a, b, R(0f, 1f)); var side = new Vector2(-(b - a).y, (b - a).x).normalized * (rnd.NextDouble() < 0.5 ? -1f : 1f);
            var p = q + side * R(2f, 2.8f);
            if (!BushOk(p)) continue; Put("bush", p, 1.3f, 0.08f); i++;   // v12: x1.2-1.4
        }
        Debug.Log("[proto] glade models placed " + placed);

        // grass / flower tufts (code, leaf atlas): thick on the rim and along the path edge, sparse in the clearing, none on the path core
        var tuftMat = LeafMat("LeafTuft", 0.025f, out _);
        var tall = new Mesh[4]; var shortG = new Mesh[2]; var flowers = new Mesh[2];
        for (int i = 0; i < 4; i++) tall[i] = TuftMesh(i, 0);
        for (int i = 0; i < 2; i++) { shortG[i] = TuftMesh(i, 1); flowers[i] = TuftMesh(i, 2); }
        int tufts = 0;
        // v11: grass in clusters of 5-12 where a noise mask is high (30-40 % of the ground left bare), 3 sizes ~0.12 / 0.25 / 0.4 m;
        // 40 flower clusters and 75 pebble clusters, 70 % on the rim / path edge
        bool GrassOk(Vector2 p) => GladePathGrass(p) > 0.7f && GladeStreamDepth(p) < 0.3f && Vector2.Distance(p, new Vector2(heroStart.x, heroStart.z)) > 1.2f && p.x > 0f && p.y > 0f && p.x < GladeSize && p.y < GladeSize;
        void Tuft(Mesh m, Vector2 p, float s) { MeshProp(parent, "Tuft", m, tuftMat, null, new Vector3(p.x, GladeHeight(p) - 0.02f, p.y), s, R(0f, 360f), false); tufts++; }
        // v12 (ChatGPT): 100 tall-grass + 50 low-leaf + 80 flower clusters; 70 % on the clearing rim / forest edge, 20 % along the path, 10 % mid-clearing
        Vector2 Spot()
        {
            for (int t = 0; t < 200; t++)
            {
                double z = rnd.NextDouble(); Vector2 p;
                if (z < 0.7) { float a2 = R(0f, 6.283f); p = GladeCentre + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * R(4.6f, 10f); }   // v14: rim ring pulled in ~1 m
                else if (z < 0.9) { int sg = rnd.Next(GladePath.Length - 1); var q = Vector2.Lerp(GladePath[sg], GladePath[sg + 1], R(0f, 1f)); var d2 = (GladePath[sg + 1] - GladePath[sg]).normalized; p = q + new Vector2(-d2.y, d2.x) * R(1.4f, 2.4f) * (rnd.NextDouble() < 0.5 ? -1f : 1f); }
                else p = GladeCentre + new Vector2(R(-4.5f, 4.5f), R(-4.5f, 4.5f));
                if (GrassOk(p)) return p;
            }
            return new Vector2(-100f, -100f);
        }
        void Cluster(int count, System.Action<Vector2> put)
        {
            for (int c = 0; c < count; c++) { var centre = Spot(); if (centre.x < -50f) continue; int n = 4 + rnd.Next(6); for (int k = 0; k < n; k++) { var p = centre + new Vector2(R(-0.6f, 0.6f), R(-0.6f, 0.6f)); if (GrassOk(p)) put(p); } }
        }
        Cluster(100, p => Tuft(tall[rnd.Next(4)], p, rnd.NextDouble() < 0.4 ? R(1.6f, 1.9f) : R(1.0f, 1.25f)));
        Cluster(50, p => Tuft(shortG[rnd.Next(2)], p, R(1.1f, 1.5f)));
        Cluster(80, p => { if (rnd.NextDouble() < 0.6) Tuft(flowers[rnd.Next(2)], p, R(1.2f, 1.6f)); });
        var purpleMat = LeafMat("LeafTuftPurple", 0.025f, out _); purpleMat.SetColor("_Color", new Color(0.78f, 0.58f, 1f));   // v14: concept's purple flowers (tinted white flower card)
        var groupCentres = new System.Collections.Generic.List<Vector2>();   // v16: 11 flower groups, each 3-5 clusters
        for (int g = 0, tries = 0; g < 11 && tries < 400; tries++)
        {
            float ag = R(0f, 6.283f); var gc = GladeCentre + new Vector2(Mathf.Cos(ag), Mathf.Sin(ag)) * R(5.2f, 9f);
            if (!GrassOk(gc) || groupCentres.Exists(o => Vector2.Distance(o, gc) < 2.2f)) continue; groupCentres.Add(gc); g++;
        }
        int perGroup = 0, groupIx = 0;
        for (int c = 0; c < groupCentres.Count * 5; c++)
        {
            Vector2 centre;
            if (perGroup-- <= 0) { perGroup = 2 + rnd.Next(3); groupIx++; }
            if (groupIx > groupCentres.Count) break;
            centre = groupCentres[groupIx - 1] + new Vector2(R(-1.1f, 1.1f), R(-1.1f, 1.1f));
            if (!GrassOk(centre)) continue;
            bool purple = rnd.NextDouble() < 0.6;
            for (int k = 0; k < 3; k++) { var p = centre + new Vector2(R(-0.3f, 0.3f), R(-0.3f, 0.3f)); MeshProp(parent, "Flower", flowers[rnd.Next(2)], purple ? purpleMat : tuftMat, null, new Vector3(p.x, GladeHeight(p) - 0.02f, p.y), R(1.2f, 1.55f), R(0f, 360f), false); tufts++; }   // v15: flowers +18 %
        }
        var pebbles = new Mesh[4]; for (int i = 0; i < 4; i++) pebbles[i] = RockMesh(i);
        var bankMat = FoliageMat("GladeBankStone", false, 0f, out _); bankMat.SetColor("_Color", new Color(0.9f, 0.88f, 0.82f));
        for (int i = 0; i < GladeStream.Length - 1; i++) for (int k = 0; k < 4; k++)   // v14: ~16   // v13: bank stones both sides of each stream segment
        {
            var a2 = GladeStream[i]; var b2 = GladeStream[i + 1]; var q = Vector2.Lerp(a2, b2, R(0.15f, 0.85f)); var sd = new Vector2(-(b2 - a2).y, (b2 - a2).x).normalized * (k % 2 == 0 ? 1f : -1f);
            var p = q + sd * R(0.85f, 1.15f); if (Vector2.Distance(p, GladeBridge) < 1.8f) continue;
            MeshProp(parent, "BankStone", pebbles[rnd.Next(4)], bankMat, null, new Vector3(p.x, GladeHeight(p) - 0.05f, p.y), k < 2 ? R(0.45f, 0.8f) : R(0.15f, 0.3f), R(0f, 360f), true);
        }
        var pebbleMat = FoliageMat("GladePebble", false, 0f, out _); pebbleMat.SetColor("_Color", new Color(0.72f, 0.68f, 0.6f));   // darker, warmer than the bare code-rock colours
        for (int c = 0, tries = 0; c < 30 && tries < 4000; tries++)   // v12: -50 %
        {
            var p = new Vector2(R(0f, GladeSize), R(0f, GladeSize));
            bool edge = GladePathGrass(p) > 0.25f && GladePathGrass(p) < 0.9f;            // path rim
            if (GladeStreamDepth(p) > 0.5f || !edge) continue;   // only on the path rim (v11: scattered pebbles read as white dots)
            c++; for (int k = 0; k < 3; k++) { var q = p + new Vector2(R(-0.3f, 0.3f), R(-0.3f, 0.3f)); MeshProp(parent, "Pebble", pebbles[rnd.Next(4)], pebbleMat, null, new Vector3(q.x, GladeHeight(q) - 0.03f, q.y), R(0.12f, 0.35f), R(0f, 360f), false); }
        }
        Debug.Log("[proto] glade tufts " + tufts);
    }

    /// <summary>ChatGPT forest light: sun #FFF0C9 1.1 at X48 Y-35, gradient ambient, soft 2048 shadows to 35 m, exponential fog #CAD5B4 0.008.</summary>
    static void LightingGlade()
    {
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.1f; sun.color = Hex("FFF1CC");   // v13: warmer
        sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.67f;   // v14: softer sun.shadowBias = 0.05f; sun.shadowNormalBias = 0.4f;
        sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
        sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight; RenderSettings.ambientIntensity = 0.55f;   // v11: 0.8 washed the scene out
        RenderSettings.ambientSkyColor = Hex("C9D8D0"); RenderSettings.ambientEquatorColor = Hex("A8B694"); RenderSettings.ambientGroundColor = Hex("6C7654");
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogColor = Hex("CAD5B4"); RenderSettings.fogDensity = 0.002f;   // v11: 0.008 laid a white haze over the 18 m view
        QualitySettings.shadowDistance = 35f; QualitySettings.shadowCascades = 2;
    }
}
