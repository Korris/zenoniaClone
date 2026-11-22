using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZenoniaProto
{
    /// <summary>
    /// Tree impostors of a 3D map (22/11 perf pass 2; leaves are the heaviest part of the fill-bound 3D map). Every generated tree / bush placed
    /// through Map3DProps gets, besides its merged mesh, one camera-facing billboard in a merged impostor mesh (Shaders/Map3DImpostor). Past
    /// <see cref="StartAhead"/> metres beyond the hero (distance from the camera to the tree root; High 6, Medium 3, Low 0 m; 0.5 m hysteresis) a
    /// tree swaps: its fade (one texel per tree in _M3DImpFade) runs 0 -> 1 in <see cref="SwapSeconds"/>, mesh and billboard drawing with
    /// complementary dither meanwhile; at 1 the mesh tree collapses in its vertex shader and only the billboard draws (Map3DCommon M3DImpFade).
    /// A distance band instead of the timed swap left trees half-dithered while the camera rested (seen on the first try). The billboard samples the two nearest of 8 baked views (Map3DImpostorBaker) for the tree's own turn and lights
    /// them like the mesh: scene sun, soft ramp, the baked prop shadow at the real surface position (Medium and High; Map3DBakedShadow), fog.
    /// Views are baked at the elevation the impostor zone is seen from (camera pitch - FOV / 4: it lies between the hero and the top of the screen)
    /// and re-baked when that moves more than 4 deg; nearer than the hero the perspective sees trees from too many angles for one bake, so the start
    /// stays at or past the hero. Leaf shadows are unchanged: the sun's shadow map
    /// and the shadow bake still draw every mesh tree; the impostors never cast.
    /// Test flags: `--map3d-test noimpostor` (old behaviour), `--impostor-dist M` (start, metres past the hero), `--impostor-dump` (atlas PNG).
    /// </summary>
    public sealed class Map3DImpostors : MonoBehaviour
    {
        public const float SwapSeconds = 0.35f, Hysteresis = 0.5f, RebakeDeg = 4f, TexW = 256f;

        /// <summary>Metres past the hero where the cross-fade starts, per graphics level.</summary>
        public static float StartAhead(GraphicsQuality.Level q) { return q == GraphicsQuality.Level.High ? 6f : q == GraphicsQuality.Level.Medium ? 3f : 0f; }
        const float Cell = 32f * Map3D.M;
        struct Inst { public int Key, Index; public Vector3 Pos; public float Yaw, Scale; }
        static readonly List<Inst> Insts = new List<Inst>();
        static readonly Dictionary<int, Map3DImpostorKey> Keys = new Dictionary<int, Map3DImpostorKey>();
        static readonly int ImpId = Shader.PropertyToID("_M3DImp"), FadeTexId = Shader.PropertyToID("_M3DImpFade");
        static bool disabled;
        static Map3DImpostors current;

        RenderTexture albedo, normal;
        Material mat;
        List<Map3DImpostorKey> keyList;
        Vector3[] roots;
        float[] fade;
        bool[] far;
        byte[] texels;
        Texture2D fadeTex;
        bool snap = true;
        float bakedPitch, lastBake, aheadOverride = float.NaN;

        /// <summary>Impostors are on for the 3D map on screen (Map3DBakedShadow bakes the prop shadow for them at High too).</summary>
        public static bool Active { get { return current != null && current.enabled; } }

        static bool Enabled { get { return !disabled && !Map3D.Test("noimpostor"); } }

        /// <summary>New map (Map3D.Build): forget the last map's queue.</summary>
        public static void Clear() { Insts.Clear(); Keys.Clear(); disabled = false; }

        /// <summary>The tree-root check of the merge failed (Map3DBatcher): no impostors for this map, meshes draw at every distance.</summary>
        public static void Disable() { disabled = true; Shader.SetGlobalVector(ImpId, Vector4.zero); }

        /// <summary>Map3DBatcher.Add: a tree / bush of this key at pos / rot / scale; returns its tree index, -1 = impostors off (the mesh then carries no tree root).</summary>
        public static int Queue(int key, Mesh mesh, Material bark, Material leaf, Vector3 pos, Quaternion rot, float scale)
        {
            if (!Enabled || mesh == null || leaf == null) return -1;
            if (!Keys.ContainsKey(key))
                Keys[key] = new Map3DImpostorKey { Key = key, Mesh = mesh, Bark = bark, Leaf = leaf, Px = Map3DTreeBuilder.IsBush(key) ? Map3DImpostorBaker.BushPx : Map3DImpostorBaker.TreePx };
            Insts.Add(new Inst { Key = key, Index = Insts.Count, Pos = pos, Yaw = rot.eulerAngles.z * Mathf.Deg2Rad, Scale = scale });
            return Insts.Count - 1;
        }

        /// <summary>After the props are merged (Map3D.Finish): bakes the views and builds the billboard meshes.</summary>
        public static void Build(Transform mapRoot)
        {
            Shader.SetGlobalVector(ImpId, Vector4.zero);
            if (!Enabled || Insts.Count == 0) return;
            Shader s = Resources.Load<Shader>("Shaders/Map3DImpostor");
            if (s == null || !s.isSupported || !Map3DImpostorBaker.Ready()) { Debug.LogWarning("[ZenoniaProto] Map3D impostor shaders missing: impostors off"); disabled = true; return; }
            var go = new GameObject("Map3DImpostors");
            go.transform.SetParent(mapRoot, false);
            go.AddComponent<Map3DImpostors>().Init(s);
        }

        void Init(Shader s)
        {
            float t0 = Time.realtimeSinceStartup;
            current = this;
            keyList = new List<Map3DImpostorKey>(Keys.Values);
            foreach (Map3DImpostorKey k in keyList) Map3DImpostorBaker.Measure(k);
            int h = Map3DImpostorBaker.Layout(keyList);
            albedo = Atlas(Map3DImpostorBaker.AtlasW, h, "Map3DImpostorAlbedo");
            normal = Atlas(Map3DImpostorBaker.AtlasW / 2, h / 2, "Map3DImpostorNormal");
            mat = new Material(s) { name = "Map3DImpostor", hideFlags = HideFlags.DontSave };
            mat.SetTexture("_Albedo", albedo);
            mat.SetTexture("_Normal", normal);
            bakedPitch = CameraPitch();
            Map3DImpostorBaker.Bake(keyList, albedo, normal, bakedPitch);
            lastBake = Time.unscaledTime;
            BuildMeshes();
            int n = Insts.Count, rows = Mathf.CeilToInt(n / TexW);
            roots = new Vector3[n]; fade = new float[n]; far = new bool[n];
            for (int t = 0; t < n; t++) roots[t] = Insts[t].Pos;
            texels = new byte[(int)TexW * rows];
            fadeTex = new Texture2D((int)TexW, rows, TextureFormat.R8, false, true) { name = "Map3DImpostorFade", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            Shader.SetGlobalTexture(FadeTexId, fadeTex);
            string[] a = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(a, "--impostor-dist");
            float d;
            if (i >= 0 && i + 1 < a.Length && float.TryParse(a[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d)) aheadOverride = d;
            if (System.Array.IndexOf(a, "--impostor-dump") >= 0)
                Map3DImpostorBaker.Dump(albedo, System.IO.Path.Combine(Application.persistentDataPath, "impostor-atlas.png"));
            Debug.Log("[ZenoniaProto] Map3D impostors: " + Insts.Count + " trees, " + keyList.Count + " kinds, atlas " + albedo.width + "x" + albedo.height + " baked at pitch "
                + bakedPitch.ToString("F0") + " in " + Mathf.RoundToInt((Time.realtimeSinceStartup - t0) * 1000f) + " ms");
            Insts.Clear();
        }

        static RenderTexture Atlas(int w, int h, string name)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
            { name = name, useMipMap = true, autoGenerateMips = false, filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp, anisoLevel = 1, hideFlags = HideFlags.DontSave };
            rt.Create();
            return rt;
        }

        /// <summary>Elevation the impostors are seen from: camera pitch below the horizon minus a quarter of the vertical FOV (the impostor zone lies
        /// between the hero, seen at the pitch, and the top of the screen); 44 (3D map default 52 - 32 / 4) before the camera is placed.</summary>
        static float CameraPitch()
        {
            Camera cam = Camera.main;
            if (cam == null || cam.orthographic) return 44f;
            float p = Mathf.Asin(Mathf.Clamp(cam.transform.forward.z, -1f, 1f)) * Mathf.Rad2Deg; // down = +z
            return p < 20f || p > 85f ? 44f : p - cam.fieldOfView * 0.25f;
        }

        /// <summary>One quad per tree (4 vertices at its crown centre; the shader spreads them toward the camera's right / up), merged per 32 m cell.</summary>
        void BuildMeshes()
        {
            var groups = new Dictionary<long, List<Inst>>();
            foreach (Inst t in Insts)
            {
                long key = ((long)(Mathf.FloorToInt(t.Pos.x / Cell) + 1000) << 20) | (long)(Mathf.FloorToInt(t.Pos.y / Cell) + 1000);
                List<Inst> l;
                if (!groups.TryGetValue(key, out l)) groups[key] = l = new List<Inst>();
                l.Add(t);
            }
            Vector2[] corners = { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(1f, 1f), new Vector2(-1f, 1f) };
            foreach (List<Inst> l in groups.Values)
            {
                int n = l.Count * 4;
                var pos = new Vector3[n]; var c0 = new List<Vector4>(n); var root = new List<Vector2>(n); var rootZ = new List<Vector2>(n); var rect = new List<Vector4>(n);
                var idx = new int[l.Count * 6];
                Bounds b = new Bounds();
                float maxR = 0f;
                for (int i = 0; i < l.Count; i++)
                {
                    Inst t = l[i];
                    Map3DImpostorKey k = Keys[t.Key];
                    Vector3 pivot = t.Pos + Quaternion.AngleAxis(t.Yaw * Mathf.Rad2Deg, Vector3.forward) * (k.Center * t.Scale);
                    float r = k.Radius * t.Scale;
                    maxR = Mathf.Max(maxR, r);
                    if (i == 0) b = new Bounds(pivot, Vector3.zero); else b.Encapsulate(pivot);
                    for (int q = 0; q < 4; q++)
                    {
                        pos[i * 4 + q] = pivot;
                        c0.Add(new Vector4(corners[q].x, corners[q].y, r, t.Yaw));
                        root.Add(new Vector2(t.Pos.x, t.Pos.y));
                        rootZ.Add(new Vector2(t.Pos.z, t.Index + 1f));
                        rect.Add(k.Rect);
                    }
                    int v = i * 4;
                    idx[i * 6] = v; idx[i * 6 + 1] = v + 2; idx[i * 6 + 2] = v + 1; idx[i * 6 + 3] = v; idx[i * 6 + 4] = v + 3; idx[i * 6 + 5] = v + 2;
                }
                var mesh = new Mesh { name = "Map3DImpostorBatch", hideFlags = HideFlags.DontSave, indexFormat = n > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.vertices = pos;
                mesh.SetUVs(0, c0); mesh.SetUVs(1, root); mesh.SetUVs(2, rootZ); mesh.SetUVs(3, rect);
                mesh.SetTriangles(idx, 0);
                b.Expand(maxR * 2f);
                mesh.bounds = b;
                var go = new GameObject("ImpostorBatch");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }

        void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null || cam.orthographic) { Shader.SetGlobalVector(ImpId, Vector4.zero); return; }
            float pitch = CameraPitch();
            if (Mathf.Abs(pitch - bakedPitch) > RebakeDeg && Time.unscaledTime - lastBake > 0.2f)
            {
                bakedPitch = pitch;
                lastBake = Time.unscaledTime;
                Map3DImpostorBaker.Bake(keyList, albedo, normal, bakedPitch);
            }
            HeroController h = GameSession.Hero;
            float d = h != null ? Vector3.Distance(cam.transform.position, h.transform.position) : View25DCamera.Distance(cam);
            float ahead = float.IsNaN(aheadOverride) ? StartAhead(GraphicsQuality.Current) : aheadOverride;
            UpdateFades(cam.transform.position, d + ahead * Map3D.M);
            Shader.SetGlobalVector(ImpId, new Vector4(TexW, 1f / TexW, 1f, 1f / fadeTex.height));
        }

        /// <summary>Per tree: far / near with hysteresis, fade toward it over SwapSeconds (snapped on the first frame), texels uploaded when any moved.</summary>
        void UpdateFades(Vector3 camPos, float start)
        {
            float step = snap ? 1f : Time.unscaledDeltaTime / SwapSeconds, hi = start + Hysteresis * Map3D.M, lo = start - Hysteresis * Map3D.M;
            float hi2 = hi * hi, lo2 = lo * lo;
            bool dirty = snap;
            for (int i = 0; i < roots.Length; i++)
            {
                float d2 = (roots[i] - camPos).sqrMagnitude;
                if (d2 > hi2) far[i] = true; else if (d2 < lo2) far[i] = false;
                float target = far[i] ? 1f : 0f;
                if (fade[i] == target) continue;
                fade[i] = Mathf.MoveTowards(fade[i], target, step);
                texels[i] = (byte)Mathf.RoundToInt(fade[i] * 255f);
                dirty = true;
            }
            snap = false;
            if (!dirty) return;
            fadeTex.SetPixelData(texels, 0);
            fadeTex.Apply(false);
        }

        void OnDestroy()
        {
            if (current == this) { current = null; Shader.SetGlobalVector(ImpId, Vector4.zero); }
            if (albedo != null) { albedo.Release(); Destroy(albedo); }
            if (normal != null) { normal.Release(); Destroy(normal); }
            if (mat != null) Destroy(mat);
            if (fadeTex != null) Destroy(fadeTex);
            foreach (MeshFilter f in GetComponentsInChildren<MeshFilter>()) if (f.sharedMesh != null) Destroy(f.sharedMesh);
        }
    }
}
