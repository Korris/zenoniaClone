using UnityEngine;
using UnityEngine.Rendering;

namespace ZenoniaProto
{
    /// <summary>
    /// Terrain mesh of a 3D map (Map3D): the height field (Map3DHeightField, 0.5 m vertices, map + 30 m apron) cut into chunks of 32 x 32 m so the
    /// camera culls what is behind it, normals from the height differences, one shared material (Map3DTerrain shader: 4 painted layers + slope rock).
    /// The surface sits <see cref="Sink"/> units below its height so the flat decor, telegraphs, rings and ground FX still drawn at z = 0 on the walkable
    /// tiles never z-fight with it (feet float ~1.5 cm, invisible). Receives real shadows; casts them at High (hills shade the valley edges at low sun).
    /// </summary>
    public static class Map3DTerrain
    {
        public const float Sink = 0.25f;  // world units (1.5 cm)
        const int ChunkCells = 64;        // vertices per chunk side - 1 (32 m)

        public static void Build(Transform parent, IGameMap map, Map3DHeightField f)
        {
            var root = new GameObject("Map3DTerrain").transform;
            root.SetParent(parent, false);
            Material mat = Map3DAssets.Terrain(f);
            for (int cy = 0; cy < f.Ny - 1; cy += ChunkCells)
                for (int cx = 0; cx < f.Nx - 1; cx += ChunkCells)
                {
                    int w = Mathf.Min(ChunkCells, f.Nx - 1 - cx), h = Mathf.Min(ChunkCells, f.Ny - 1 - cy);
                    var go = new GameObject("Chunk_" + cx + "_" + cy);
                    go.transform.SetParent(root, false);
                    go.AddComponent<MeshFilter>().sharedMesh = Chunk(f, cx, cy, w, h);
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = mat;
                    mr.receiveShadows = true;
                    Map3DBatcher.Register(mr, true); // hills shading the valley: realtime at High, baked at Medium (GraphicsQuality)
                }
        }

        static Mesh Chunk(Map3DHeightField f, int cx, int cy, int w, int h)
        {
            int vw = w + 1, vh = h + 1;
            var v = new Vector3[vw * vh];
            var n = new Vector3[vw * vh];
            for (int j = 0; j < vh; j++)
                for (int i = 0; i < vw; i++)
                {
                    int gi = cx + i, gj = cy + j;
                    float tx = f.MinTx + gi * Map3DHeightField.Step, ty = f.MinTy + gj * Map3DHeightField.Step;
                    Vector3 p = Map3D.World(tx, ty, f.H[gj * f.Nx + gi]);
                    p.z += Sink;
                    v[j * vw + i] = p;
                    // normal from central differences (world: x east, y north, -z up)
                    float hl = f.H[gj * f.Nx + Mathf.Max(0, gi - 1)], hr = f.H[gj * f.Nx + Mathf.Min(f.Nx - 1, gi + 1)];
                    float hn = f.H[Mathf.Max(0, gj - 1) * f.Nx + gi], hs = f.H[Mathf.Min(f.Ny - 1, gj + 1) * f.Nx + gi];
                    float dhdx = (hr - hl) / (2f * Map3DHeightField.Step), dhdy = (hn - hs) / (2f * Map3DHeightField.Step); // per metre, y north
                    n[j * vw + i] = new Vector3(-dhdx, -dhdy, -1f).normalized;
                }
            var tri = new int[w * h * 6];
            int t = 0;
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    int a = j * vw + i, b = a + 1, c = a + vw, d = c + 1;
                    // row j is north of row j + 1 (ty grows south = world y down); wind so the face points up (-z) toward the camera
                    tri[t++] = a; tri[t++] = b; tri[t++] = c; // clockwise seen from above (Unity front face)
                    tri[t++] = b; tri[t++] = d; tri[t++] = c;
                }
            var m = new Mesh { name = "Map3DTerrainChunk", hideFlags = HideFlags.DontSave };
            m.indexFormat = vw * vh > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            m.vertices = v;
            m.normals = n;
            m.triangles = tri;
            m.RecalculateBounds();
            return m;
        }
    }
}
