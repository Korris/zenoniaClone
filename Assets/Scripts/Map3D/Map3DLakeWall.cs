using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Stone embankment round Willowbrook Village's lake and pond (user 25/11: "thành hồ chưa làm kiểu 3D sâu nên nước không tạo cảm giác sâu";
    /// reference: Ragnarok's canal walls dropping straight into deep water). The heightfield (Map3DHeightField.DeepLake) keeps the plaza flat right up
    /// to the painted water edge (splat water 0.5), drops the bed to -1.3 m behind it and lowers the water to <see cref="Map3DHeightField.LakeLevel"/>;
    /// this builds, along that edge (marching squares on the splat, 0.5 m cells): a coping slab at +0.07 m (0.1 m over the plaza, covering the
    /// terrain's cell-wide step) and a vertical dressed-stone face 0.32 m out into the water, from the slab down to -1.6 m, in the town stone.
    /// Render only: the lake tiles are solid, the rows do not change. Units: kit metres (x east, y up, z north = -ty), origin at tile (0, 0).
    /// </summary>
    public static class Map3DLakeWall
    {
        const float Step = 0.5f, Off = 0.32f, CapIn = 0.24f, CapTop = 0.07f, Bottom = -1.9f;

        public static void Build(IGameMap map, Map3DHeightField f)
        {
            if (!f.DeepLake) return;
            var k = new Map3DMeshKit();
            int nx = Mathf.CeilToInt(f.MapW / Step), ny = Mathf.CeilToInt(f.MapH / Step), segs = 0;
            var v = new float[(nx + 1) * (ny + 1)];
            for (int j = 0; j <= ny; j++)
                for (int i = 0; i <= nx; i++) v[j * (nx + 1) + i] = f.Splat(i * Step, j * Step, 3) - 0.5f;
            var pts = new Vector2[4];
            for (int j = 0; j < ny; j++)
                for (int i = 0; i < nx; i++)
                {
                    float a = v[j * (nx + 1) + i], b = v[j * (nx + 1) + i + 1], c = v[(j + 1) * (nx + 1) + i + 1], d = v[(j + 1) * (nx + 1) + i];
                    int n = 0;
                    Vector2 p0 = new Vector2(i, j) * Step, p1 = new Vector2(i + 1, j) * Step, p2 = new Vector2(i + 1, j + 1) * Step, p3 = new Vector2(i, j + 1) * Step;
                    if ((a < 0f) != (b < 0f)) pts[n++] = Vector2.Lerp(p0, p1, a / (a - b));
                    if ((b < 0f) != (c < 0f)) pts[n++] = Vector2.Lerp(p1, p2, b / (b - c));
                    if ((c < 0f) != (d < 0f)) pts[n++] = Vector2.Lerp(p2, p3, c / (c - d));
                    if ((d < 0f) != (a < 0f)) pts[n++] = Vector2.Lerp(p3, p0, d / (d - a));
                    for (int s = 0; s + 1 < n; s += 2) { Segment(k, f, pts[s], pts[s + 1]); segs++; }
                }
            if (segs == 0) return;
            Vector3 pos = Map3D.World(0f, 0f, 0f);
            pos.z += Map3DTerrain.Sink;
            Map3DBatcher.Add(k.Build("Map3DLakeWall"), Map3DAssets.TownStone, Map3DAssets.TownStone, pos, Quaternion.identity, 1f);
            Debug.Log("[ZenoniaProto] Map3D lake wall: " + segs + " segments");
        }

        /// <summary>One edge piece between tile points a and b (y down): slab + lip + face, facing the water (the side where the splat rises).</summary>
        static void Segment(Map3DMeshKit k, Map3DHeightField f, Vector2 a, Vector2 b)
        {
            Vector2 mid = (a + b) * 0.5f;
            const float e = 0.25f;
            Vector2 g = new Vector2(f.Splat(mid.x + e, mid.y, 3) - f.Splat(mid.x - e, mid.y, 3), f.Splat(mid.x, mid.y + e, 3) - f.Splat(mid.x, mid.y - e, 3));
            if (g.sqrMagnitude < 1e-8f) return;
            g.Normalize();
            Vector2 tan = (b - a).normalized;
            a -= tan * 0.03f; b += tan * 0.03f;                                  // overlap the neighbours: no hairline gaps
            Vector3 A = new Vector3(a.x, 0f, -a.y), B = new Vector3(b.x, 0f, -b.y);
            Vector3 w = new Vector3(g.x, 0f, -g.y), t = new Vector3(tan.x, 0f, -tan.y), up = Vector3.up;
            const int Cell = Map3DTownWallBuilder.Stone;
            const float Rep = Map3DTownWallBuilder.Rep;
            float ua = Vector3.Dot(A, t) / Rep, ub = Vector3.Dot(B, t) / Rep;
            Color top = Map3DGuildHallBuilder.Col(1f, Cell), lipC = Map3DGuildHallBuilder.Col(0.9f, Cell), hi = Map3DGuildHallBuilder.Col(0.8f, Cell), lo = Map3DGuildHallBuilder.Col(0.45f, Cell);
            // coping slab (top), from 0.1 m over the plaza to just past the face
            Vector3 i0 = A - w * CapIn + up * CapTop, i1 = B - w * CapIn + up * CapTop, o0 = A + w * (Off + 0.06f) + up * CapTop, o1 = B + w * (Off + 0.06f) + up * CapTop;
            Map3DTownWallBuilder.Quad(k, i0, i1, o1, o0, up, Uv(i0), Uv(i1), Uv(o1), Uv(o0), top);
            // slab lip (front edge, 0.12 m)
            Vector3 l0 = o0 - up * 0.12f, l1 = o1 - up * 0.12f;
            Map3DTownWallBuilder.Quad(k, l0, l1, o1, o0, w, new Vector2(ua, 0f), new Vector2(ub, 0f), new Vector2(ub, 0.07f), new Vector2(ua, 0.07f), lipC);
            // the wall face into the water
            Vector3 f0 = A + w * Off + up * (CapTop - 0.12f), f1 = B + w * Off + up * (CapTop - 0.12f), b0 = A + w * Off + up * Bottom, b1 = B + w * Off + up * Bottom;
            Face(k, b0, b1, f1, f0, w, new Vector2(ua, Bottom / Rep), new Vector2(ub, Bottom / Rep), new Vector2(ub, 0f), new Vector2(ua, 0f), lo, hi);
        }

        /// <summary>Quad b0 b1 t1 t0 (bottom pair first) facing n, darker at the bottom (AO into the deep water).</summary>
        static void Face(Map3DMeshKit k, Vector3 b0, Vector3 b1, Vector3 t1, Vector3 t0, Vector3 n, Vector2 u0, Vector2 u1, Vector2 u2, Vector2 u3, Color lo, Color hi)
        {
            if (Vector3.Dot(Vector3.Cross(b1 - b0, t1 - b0), n) < 0f) { Vector3 q = b0; b0 = b1; b1 = q; q = t0; t0 = t1; t1 = q; Vector2 u = u0; u0 = u1; u1 = u; u = u3; u3 = u2; u2 = u; }
            int s = k.Count;
            k.Vert(b0, n, u0, lo); k.Vert(b1, n, u1, lo); k.Vert(t1, n, u2, hi); k.Vert(t0, n, u3, hi);
            k.Tri(false, s, s + 2, s + 1); k.Tri(false, s, s + 3, s + 2);
        }

        static Vector2 Uv(Vector3 p) { return new Vector2(p.x / Map3DTownWallBuilder.Rep, p.z / Map3DTownWallBuilder.Rep); }
    }
}
