using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZenoniaProto
{
    /// <summary>
    /// Merges the generated props of a 3D map (Map3DProps, Map3DBorderForest, Map3DGrass) into one mesh per material per 16 x 16 m cell, built once
    /// after the map: the first build drew ~1000 separate two-material renderers (x2 for the shadow pass) and ran at 28 fps where the 2.5D map runs at 90.
    /// Wind, lighting and the hero dissolve work in world space, so nothing changes on screen; the camera still culls by cell.
    /// Each queued piece says whether it casts and its grass density tier, so the graphics quality (GraphicsQuality, 22/11 perf pass) can switch
    /// shadow casting and grass density per renderer without a rebuild: casters cast in realtime at High only (Medium bakes them once:
    /// Map3DBakedShadow, BakeCasters), tiers 0-2 show at High, 0-1 at Medium, 0 at Low.
    /// Leaf cards come in two submeshes (Map3DMeshKit): 1 = cards that never cast, 2 = the ~3/8 that do (dappled canopy shadows), so the shadow
    /// pass only rasterises the casting cards instead of clipping 2/3 of every crown per pixel.
    /// Tree impostors (22/11 perf pass 2): a generated tree / bush queued with its impostor key gets its root position written into every merged
    /// vertex through the combine's lightmap offsets (scale 0, offset = data: uv2 = root x, y; uv3 = root z, tree index + 1), which the Map3D shaders read for
    /// the distance cross-fade (Map3DCommon M3DImpFade), and is queued for its billboard (Map3DImpostors).
    /// </summary>
    public static class Map3DBatcher
    {
        const float Cell = 16f * Map3D.M;
        struct Item { public Mesh Mesh; public int Sub; public Matrix4x4 M; public Vector4 Root, RootZ; }
        struct Tagged { public Renderer R; public bool Cast; public int Tier; public Material Mat; }
        static readonly Dictionary<long, List<Item>> Groups = new Dictionary<long, List<Item>>();
        static readonly List<Material> MatIndex = new List<Material>();
        static readonly List<Tagged> Live = new List<Tagged>();
        static bool hooked;

        public static void Clear() { Groups.Clear(); MatIndex.Clear(); Live.RemoveAll(t => t.R == null); } // + renderers of maps already left

        /// <summary>Queue a generated mesh (submesh 0 with m0, leaf submeshes 1 / 2 with m1 when they have triangles) at this transform.
        /// tier: grass density tier (0 always shown, 1 hidden at Low, 2 shown at High only); impostor: Map3DTreeBuilder key of a tree / bush that
        /// swaps to an impostor billboard in the distance (-1 = never).</summary>
        public static void Add(Mesh mesh, Material m0, Material m1, Vector3 pos, Quaternion rot, float scale, bool castShadow = true, int tier = 0, int impostor = -1)
        {
            Matrix4x4 m = Matrix4x4.TRS(pos, rot, Vector3.one * scale);
            Vector4 root = Vector4.zero, rootZ = Vector4.zero;
            int tree = impostor >= 0 ? Map3DImpostors.Queue(impostor, mesh, m0, m1, pos, rot, scale) : -1;
            if (tree >= 0)
            {
                root = new Vector4(0f, 0f, pos.x, pos.y);  // uv * 0 + offset: the same value on every vertex of this tree
                rootZ = new Vector4(0f, 0f, pos.z, tree + 1f);
            }
            long cx = Mathf.FloorToInt(pos.x / Cell) + 1000, cy = Mathf.FloorToInt(pos.y / Cell) + 1000;
            for (int s = 0; s < 3 && s < mesh.subMeshCount; s++)
            {
                if (mesh.GetIndexCount(s) == 0) continue;
                Material mat = s == 0 ? m0 : m1;
                bool c = s != 1 && castShadow; // submesh 1 = leaf cards that never cast (the shadow pass clipped them anyway)
                if (!c) mat = Map3DAssets.NoShadowPass(mat); // foliage: out of the shadow maps and the depth pre-pass too
                int mi = MatIndex.IndexOf(mat);
                if (mi < 0) { MatIndex.Add(mat); mi = MatIndex.Count - 1; }
                long key = ((long)mi << 44) | ((long)tier << 42) | ((c ? 1L : 0L) << 40) | (cx << 20) | cy;
                List<Item> l;
                if (!Groups.TryGetValue(key, out l)) Groups[key] = l = new List<Item>();
                l.Add(new Item { Mesh = mesh, Sub = s, M = m, Root = root, RootZ = rootZ });
            }
        }

        /// <summary>Builds the merged renderers under `parent` and forgets the queue.</summary>
        public static void Flush(Transform parent)
        {
            var root = new GameObject("Map3DProps").transform;
            root.SetParent(parent, false);
            int n = 0;
            var stats = new Dictionary<string, Vector3Int>(); // per shader: renderers, vertices, triangles (perf log)
            foreach (KeyValuePair<long, List<Item>> kv in Groups)
            {
                Material mat = MatIndex[(int)(kv.Key >> 44)];
                int tier = (int)((kv.Key >> 42) & 3);
                bool cast = ((kv.Key >> 40) & 1) != 0;
                if (Map3D.Test("noleaves") && mat.shader.name.Contains("Foliage")) continue;
                List<Item> items = kv.Value;
                var ci = new CombineInstance[items.Count];
                for (int i = 0; i < items.Count; i++)
                    ci[i] = new CombineInstance { mesh = items[i].Mesh, subMeshIndex = items[i].Sub, transform = items[i].M, lightmapScaleOffset = items[i].Root, realtimeLightmapScaleOffset = items[i].RootZ };
                var mesh = new Mesh { name = "Map3DBatch", hideFlags = HideFlags.DontSave, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(ci, true, true, true); // hasLightmapData: uv2 / uv3 = the tree-root offsets above
                if (!rootChecked && items[0].Root.z != 0f) CheckRoots(mesh, items[0]);
                var go = new GameObject("Batch_" + mat.name);
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.receiveShadows = true;
                Register(mr, cast, tier);
                n++;
                string sk = mat.shader.name.Replace("ZenoniaProto/Map3D", "") + (cast ? "-cast" : "-nocast");
                Vector3Int st; stats.TryGetValue(sk, out st);
                stats[sk] = st + new Vector3Int(1, mesh.vertexCount, (int)(mesh.GetIndexCount(0) / 3));
            }
            foreach (KeyValuePair<string, Vector3Int> kv in stats)
                Debug.Log("[ZenoniaProto] Map3D batch " + kv.Key + ": " + kv.Value.x + " renderers, " + kv.Value.y + " verts, " + kv.Value.z + " tris");
            Debug.Log("[ZenoniaProto] Map3D props merged into " + n + " renderers (quality " + GraphicsQuality.Name(GraphicsQuality.Current) + ")");
            Clear();
        }

        static bool rootChecked;

        /// <summary>Logs once whether the combine wrote the tree root into uv2 / uv3 as expected (the impostor cross-fade depends on it).</summary>
        static void CheckRoots(Mesh merged, Item first)
        {
            rootChecked = true;
            var a = new List<Vector2>(); var b = new List<Vector2>();
            merged.GetUVs(1, a); merged.GetUVs(2, b);
            bool ok = a.Count > 0 && b.Count > 0 && Mathf.Abs(a[0].x - first.Root.z) < 0.01f && Mathf.Abs(a[0].y - first.Root.w) < 0.01f && Mathf.Abs(b[0].y - first.RootZ.w) < 0.01f;
            Debug.Log("[ZenoniaProto] Map3D tree-root uv " + (ok ? "OK" : "WRONG (impostor fade off for this map)") + ": uv2 " + (a.Count > 0 ? a[0].ToString() : "none")
                + " uv3 " + (b.Count > 0 ? b[0].ToString() : "none") + " expected (" + first.Root.z + ", " + first.Root.w + ") (" + first.RootZ.z + ", " + first.RootZ.w + ")");
            if (!ok) Map3DImpostors.Disable();
        }

        /// <summary>Puts a renderer of the 3D map under the quality switch (merged batches, terrain chunks, cutout sprites) and applies it now.</summary>
        public static void Register(Renderer r, bool cast, int tier = 0)
        {
            if (!hooked) { hooked = true; GraphicsQuality.Changed += ApplyAll; }
            var t = new Tagged { R = r, Cast = cast, Tier = tier, Mat = r is MeshRenderer ? r.sharedMaterial : null };
            Live.Add(t);
            Apply(t, GraphicsQuality.Current);
        }

        static void Apply(Tagged t, GraphicsQuality.Level q)
        {
            bool casts = t.Cast && q == GraphicsQuality.Level.High;
            t.R.shadowCastingMode = casts ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (t.Mat != null && t.Cast) t.R.sharedMaterial = casts ? t.Mat : Map3DAssets.NoShadowPass(t.Mat); // no realtime shadows: out of the depth pre-pass too
            t.R.enabled = t.Tier <= (q == GraphicsQuality.Level.High ? 2 : q == GraphicsQuality.Level.Medium ? 1 : 0);
        }

        static readonly List<int> bakeLayers = new List<int>();

        /// <summary>Map3DBakedShadow: on = every renderer that casts at High goes on `layer` and shows (the bake camera sees only that layer);
        /// off = back to its own layer and the current quality. Returns how many renderers took part.</summary>
        public static int BakeCasters(int layer, bool on)
        {
            int n = 0;
            if (on) bakeLayers.Clear();
            for (int i = 0; i < Live.Count; i++)
            {
                Tagged t = Live[i];
                if (t.R == null || !t.Cast) { if (on) bakeLayers.Add(-1); continue; }
                if (on) { bakeLayers.Add(t.R.gameObject.layer); t.R.gameObject.layer = layer; t.R.enabled = true; }
                else if (i < bakeLayers.Count && bakeLayers[i] >= 0) t.R.gameObject.layer = bakeLayers[i];
                n++;
            }
            if (!on) ApplyAll();
            return n;
        }

        static void ApplyAll()
        {
            GraphicsQuality.Level q = GraphicsQuality.Current;
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                if (Live[i].R == null) { Live.RemoveAt(i); continue; } // the map was left
                Apply(Live[i], q);
            }
        }
    }
}
