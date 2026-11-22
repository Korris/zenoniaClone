using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZenoniaProto
{
    /// <summary>
    /// Merges the generated props of a 3D map (Map3DProps, Map3DBorderForest) into one mesh per material per 16 x 16 m cell, built once after the map:
    /// the first build drew ~1000 separate two-material renderers (x2 for the shadow pass) and ran at 28 fps where the 2.5D map runs at 90.
    /// Wind, lighting and the hero dissolve work in world space, so nothing changes on screen; the camera still culls by cell.
    /// </summary>
    public static class Map3DBatcher
    {
        const float Cell = 16f * Map3D.M;
        struct Item { public Mesh Mesh; public int Sub; public Matrix4x4 M; }
        static readonly Dictionary<long, List<Item>> Groups = new Dictionary<long, List<Item>>();
        static readonly List<Material> MatIndex = new List<Material>();

        public static void Clear() { Groups.Clear(); MatIndex.Clear(); }

        /// <summary>Queue a generated mesh (submesh 0 with m0, submesh 1 with m1 when it has triangles) at this transform.</summary>
        public static void Add(Mesh mesh, Material m0, Material m1, Vector3 pos, Quaternion rot, float scale, bool castShadow = true)
        {
            Matrix4x4 m = Matrix4x4.TRS(pos, rot, Vector3.one * scale);
            long cx = Mathf.FloorToInt(pos.x / Cell) + 1000, cy = Mathf.FloorToInt(pos.y / Cell) + 1000;
            for (int s = 0; s < 2 && s < mesh.subMeshCount; s++)
            {
                if (mesh.GetIndexCount(s) == 0) continue;
                Material mat = s == 0 ? m0 : m1;
                int mi = MatIndex.IndexOf(mat);
                if (mi < 0) { MatIndex.Add(mat); mi = MatIndex.Count - 1; }
                long key = ((long)mi << 41) | ((castShadow ? 1L : 0L) << 40) | (cx << 20) | cy;
                List<Item> l;
                if (!Groups.TryGetValue(key, out l)) Groups[key] = l = new List<Item>();
                l.Add(new Item { Mesh = mesh, Sub = s, M = m });
            }
        }

        /// <summary>Builds the merged renderers under `parent` and forgets the queue.</summary>
        public static void Flush(Transform parent)
        {
            var root = new GameObject("Map3DProps").transform;
            root.SetParent(parent, false);
            int n = 0;
            foreach (KeyValuePair<long, List<Item>> kv in Groups)
            {
                Material mat = MatIndex[(int)(kv.Key >> 41)];
                bool cast = ((kv.Key >> 40) & 1) != 0;
                if (Map3D.Test("noleaves") && mat.shader.name.Contains("Foliage")) continue;
                List<Item> items = kv.Value;
                var ci = new CombineInstance[items.Count];
                for (int i = 0; i < items.Count; i++) ci[i] = new CombineInstance { mesh = items[i].Mesh, subMeshIndex = items[i].Sub, transform = items[i].M };
                var mesh = new Mesh { name = "Map3DBatch", hideFlags = HideFlags.DontSave, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(ci, true, true);
                var go = new GameObject("Batch_" + mat.name);
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = cast ? ShadowCastingMode.On : ShadowCastingMode.Off; // the border forest casts none: shadows were 60 % of the frame
                mr.receiveShadows = true;
                n++;
            }
            Debug.Log("[ZenoniaProto] Map3D props merged into " + n + " renderers");
            Clear();
        }
    }
}
