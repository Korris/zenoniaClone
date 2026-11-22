using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZenoniaProto
{
    /// <summary>
    /// Small mesh writer for the generated 3D props (Map3DTreeBuilder, Map3DRockBuilder). Shapes are written in METRES in a y-up frame
    /// (x east, y up, z north) and converted on output to the game's object space (x, z north -> world y, up -> -z, x16 units).
    /// Two submeshes: 0 = solid (bark / stone / wood, Map3DPainted), 1 = leaf cards (Map3DFoliage). Vertex colour channels follow those shaders.
    /// </summary>
    public sealed class Map3DMeshKit
    {
        readonly List<Vector3> pos = new List<Vector3>(1024);
        readonly List<Vector3> nrm = new List<Vector3>(1024);
        readonly List<Vector2> uv = new List<Vector2>(1024);
        readonly List<Color> col = new List<Color>(1024);
        readonly List<int> solid = new List<int>(2048), leaves = new List<int>(2048);

        public int Count { get { return pos.Count; } }

        public int Vert(Vector3 p, Vector3 n, Vector2 t, Color c)
        {
            pos.Add(p); nrm.Add(n); uv.Add(t); col.Add(c);
            return pos.Count - 1;
        }

        public void Tri(bool leaf, int a, int b, int c) { List<int> l = leaf ? leaves : solid; l.Add(a); l.Add(b); l.Add(c); }

        public void SetColor(int i, Color c) { col[i] = c; }
        public Vector3 Pos(int i) { return pos[i]; }

        /// <summary>Tapered tube along `path` (y-up metres), `sides` round, radius per point, bark strip uv (u inside [u0, u0 + du], v = length / vScale).
        /// Colour per point: r AO, g wind weight; b = moss amount.</summary>
        public void Tube(IList<Vector3> path, IList<float> radius, int sides, float u0, float du, float vScale, IList<Color> colors, bool capEnd)
        {
            int n = path.Count;
            int start = pos.Count;
            float len = 0f;
            Vector3 prevSide = Vector3.right;
            for (int k = 0; k < n; k++)
            {
                Vector3 dir = k < n - 1 ? (path[k + 1] - path[k]).normalized : (path[k] - path[k - 1]).normalized;
                if (k > 0) len += Vector3.Distance(path[k], path[k - 1]);
                Vector3 side = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.95f ? Vector3.forward : Vector3.up).normalized;
                if (Vector3.Dot(side, prevSide) < 0f) side = -side;
                prevSide = side;
                Vector3 other = Vector3.Cross(dir, side);
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    Vector3 off = side * Mathf.Cos(a) + other * Mathf.Sin(a);
                    Vert(path[k] + off * radius[k], off, new Vector2(u0 + du * s / sides, len / vScale), colors[k]);
                }
            }
            for (int k = 0; k < n - 1; k++)
                for (int s = 0; s < sides; s++)
                {
                    int a = start + k * (sides + 1) + s, b = a + 1, c = a + sides + 1, d = c + 1;
                    Tri(false, a, c, b); Tri(false, b, c, d);
                }
            if (!capEnd) return;
            Vector3 tip = path[n - 1], tdir = (path[n - 1] - path[n - 2]).normalized;
            int centre = Vert(tip + tdir * radius[n - 1] * 0.3f, tdir, new Vector2(u0 + du * 0.5f, len / vScale), colors[n - 1]);
            int ring = start + (n - 1) * (sides + 1);
            for (int s = 0; s < sides; s++) Tri(false, ring + s, centre, ring + s + 1);
        }

        /// <summary>Box centred at c (y-up metres) with half sizes h, turned by yaw (radians) round y; flat normals; colour per vertex (r AO by height).</summary>
        public void Box(Vector3 c, Vector3 h, float yaw, Color top, Color bottom)
        {
            Quaternion q = Quaternion.AngleAxis(yaw * Mathf.Rad2Deg, Vector3.up);
            Vector3[] n = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (Vector3 f in n)
            {
                Vector3 u = Mathf.Abs(f.y) > 0.5f ? Vector3.forward : Vector3.up, r = Vector3.Cross(u, f);
                Vector3 fc = Vector3.Scale(f, h), ru = Vector3.Scale(r, h), uu = Vector3.Scale(u, h);
                Vector3[] p = { fc - ru - uu, fc + ru - uu, fc + ru + uu, fc - ru + uu };
                int b = Count;
                for (int k = 0; k < 4; k++) Vert(c + q * p[k], q * f, Vector2.zero, p[k].y > 0f ? top : bottom);
                Tri(false, b, b + 2, b + 1); Tri(false, b, b + 3, b + 2);
            }
        }

        /// <summary>Leaf card: quad centred at c (y-up metres) spanned by right / up half-extents; uv rect (u0, v0, u1, v1); normals per corner given.</summary>
        public void Card(Vector3 c, Vector3 right, Vector3 up, Vector4 uvRect, Vector3[] normals, Color[] colors)
        {
            int a = Vert(c - right - up, normals[0], new Vector2(uvRect.x, uvRect.y), colors[0]);
            int b = Vert(c + right - up, normals[1], new Vector2(uvRect.z, uvRect.y), colors[1]);
            int d = Vert(c + right + up, normals[2], new Vector2(uvRect.z, uvRect.w), colors[2]);
            int e = Vert(c - right + up, normals[3], new Vector2(uvRect.x, uvRect.w), colors[3]);
            Tri(true, a, d, b); Tri(true, a, e, d); // double-sided material (Cull Off): one winding is enough
        }

        /// <summary>Mesh in game object space: (x, y, z) metres y-up -> (x, z, -y) x 16 units.</summary>
        public Mesh Build(string name)
        {
            const float M = Map3D.M;
            var p = new Vector3[pos.Count];
            var n = new Vector3[pos.Count];
            for (int i = 0; i < p.Length; i++)
            {
                Vector3 q = pos[i], m = nrm[i];
                p[i] = new Vector3(q.x * M, q.z * M, -q.y * M);
                n[i] = new Vector3(m.x, m.z, -m.y).normalized;
            }
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.indexFormat = p.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = p;
            mesh.normals = n;
            mesh.SetUVs(0, uv);
            mesh.SetColors(col);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(FlipWinding(solid), 0); // shapes are written counter-clockwise from outside (easier to reason about); Unity's front face is clockwise
            mesh.SetTriangles(leaves, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        static List<int> FlipWinding(List<int> t)
        {
            var o = new List<int>(t.Count);
            for (int i = 0; i < t.Count; i += 3) { o.Add(t[i]); o.Add(t[i + 2]); o.Add(t[i + 1]); }
            return o;
        }

        /// <summary>Atlas cell k (0..15) of a 4x4 atlas, image row 0 on top, as a uv rect (u0, v0, u1, v1); vFrom..vTo picks a band inside the cell.</summary>
        public static Vector4 Cell(int k, float vFrom = 0f, float vTo = 1f)
        {
            float u0 = (k % 4) / 4f, row = k / 4;
            float v0 = 1f - (row + 1f) / 4f;
            return new Vector4(u0, v0 + vFrom / 4f, u0 + 0.25f, v0 + vTo / 4f);
        }
    }
}
