using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZenoniaProto
{
    /// <summary>
    /// Water surface of a 3D map (Map3D): one flat transparent sheet <see cref="Level"/> m below the walkable ground over every 0.5 m cell near the
    /// water channel of the splat (stream, pond). The terrain bank crosses the level by itself, so the shoreline follows the painted water; vertex
    /// alpha = depth under the surface (0..1 over 0.45 m) for the shallow -> deep colour and the light bank rim (Map3DWater shader).
    /// </summary>
    public static class Map3DWater
    {
        public const float Level = -0.12f; // metres (default; a map with a sunken creek sets its own: Map3DHeightField.WaterLevel)
        static readonly Vector2 Flow = new Vector2(0.35f, -0.75f); // the stream runs south-east to the pond

        public static void Build(Transform parent, IGameMap map, Map3DHeightField f)
        {
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var tris = new List<int>();
            var index = new Dictionary<int, int>();
            float s = Map3DHeightField.Step;
            for (int j = 0; j < f.Ny - 1; j++)
                for (int i = 0; i < f.Nx - 1; i++)
                {
                    float tx = f.MinTx + (i + 0.5f) * s, ty = f.MinTy + (j + 0.5f) * s;
                    if (tx < -1f || ty < -1f || tx > f.MapW + 1f || ty > f.MapH + 1f) continue;
                    if (Wet(f, tx, ty) < 0.02f) continue;
                    int a = V(f, i, j, verts, cols, index), b = V(f, i + 1, j, verts, cols, index), c = V(f, i, j + 1, verts, cols, index), d = V(f, i + 1, j + 1, verts, cols, index);
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            if (tris.Count == 0) return;
            var m = new Mesh { name = "Map3DWater", hideFlags = HideFlags.DontSave, indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            m.SetVertices(verts);
            m.SetColors(cols);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            var go = new GameObject("Map3DWater");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            Material wm = Map3DAssets.Water;
            wm.SetTexture("_Height", f.HeightTex);
            wm.SetVector("_HRect", f.HeightRect);
            wm.SetVector("_HSize", f.HeightSize);
            wm.SetFloat("_Level", f.WaterLevel);
            wm.SetFloat("_DepthRange", f.DeepLake ? 1.8f : 0.45f); // shared material: set again for every map
            wm.SetFloat("_Waves", f.DeepLake ? 1f : 0f);
            mr.sharedMaterial = wm;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = GameConfig.GroundOrder + 2; // transparent: under ground FX and units (both drawn later in the same queue band)
        }

        /// <summary>Water nearby (max of the splat water channel 1 m round the point): cells just outside the channel keep the sheet over the bank.</summary>
        static float Wet(Map3DHeightField f, float tx, float ty)
        {
            float w = 0f;
            for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++) w = Mathf.Max(w, f.Splat(tx + dx * 0.75f, ty + dy * 0.75f, 3));
            return w;
        }

        static int V(Map3DHeightField f, int i, int j, List<Vector3> verts, List<Color> cols, Dictionary<int, int> index)
        {
            int key = j * f.Nx + i, k;
            if (index.TryGetValue(key, out k)) return k;
            float tx = f.MinTx + i * Map3DHeightField.Step, ty = f.MinTy + j * Map3DHeightField.Step;
            verts.Add(Map3D.World(tx, ty, f.WaterLevel));
            float depth = Mathf.Clamp01((f.WaterLevel - f.H[key]) / 0.45f);
            cols.Add(new Color(0.5f + Flow.x * 0.5f, 0.5f + Flow.y * 0.5f, 0f, depth));
            k = verts.Count - 1;
            index[key] = k;
            return k;
        }
    }
}
