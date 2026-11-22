using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Procedural 3D greatsword of the Warrior's EARTH BREAK (22/11/2026, user: "làm model kiếm 3D"; replaces the flat painted sprite that bent when
    /// the 2.5D / 3D camera turned). Built once in code, in the pixel frame of the ChatGPT-painted front view
    /// (Resources/Sprites/classes/warrior/fx/earth-break/greatsword.png, 320 x 768, steel-ivory blade + gold, RO trailer Warrior palette), so that
    /// art is the hand-painted texture: every vertex takes the planar front-view uv of its (x, y), the back mirrors it, side walls take their rim
    /// colour. Model units: tip at the origin, up = +y, 1 = the whole sword (768 px); the painted front faces -z (toward a camera looking along +z).
    /// Parts, drawn in this order (the shader writes no depth, so later parts cover earlier ones where they overlap):
    ///  - grip + pommel: one lathe (16 sides) on the painted radius profile (leather grip, ball pommel);
    ///  - blade: 8-station cross-section on the painted half-width profile, flat-shaded facets: sharp edge -> edge bevel -> flat -> fuller wall ->
    ///    recessed gold fuller (the rune channel), thickness 17 % of the half width, thinning to the point;
    ///  - crossguard: slab between the traced outline of the painted guard (73 points) at both depths, thicker at the centre than at the horn
    ///    tips, front / back as a grid inside the outline (the shader clips the painted alpha, so the curls and gaps are exact);
    ///  - gem: a raised octahedron on the guard's painted diamond (vertex alpha 1 = gem glow in the shader).
    /// </summary>
    public static class EarthBreakGreatswordMesh
    {
        const float W = 320f, Hpx = 768f, Cx = 160f;

        /// <summary>Painted blade half-width (px) by row: rows inside the guard first, then down to the point.</summary>
        static readonly float[] BladeRows = { 244f, 280f, 292f, 304f, 316f, 328f, 340f, 352f, 376f, 400f, 424f, 448f, 472f, 496f, 508f, 520f, 532f, 544f, 556f, 568f, 580f, 592f, 604f, 616f, 628f, 640f, 652f, 664f, 676f, 688f, 700f, 712f, 724f, 736f, 748f, 756f, 762f, 768f };
        static readonly float[] BladeHalf = { 92f, 92f, 88f, 85f, 83f, 82f, 81f, 80.5f, 79f, 78f, 77.5f, 76.5f, 76.5f, 77f, 78.5f, 80.5f, 83f, 87f, 91.5f, 88.5f, 84.5f, 80.5f, 76.5f, 72f, 67.5f, 62.5f, 57.5f, 52.5f, 47f, 41.5f, 35.5f, 29.5f, 24f, 18f, 11.5f, 7.5f, 4.5f, 0.6f };

        /// <summary>Cross-section stations across the blade (s = x / half width) and their depth (share of the blade's half thickness).</summary>
        static readonly float[] Station = { -1f, -0.8f, -0.36f, -0.31f, 0.31f, 0.36f, 0.8f, 1f };
        static readonly float[] Depth = { 0f, 0.55f, 1f, 0.72f, 0.72f, 1f, 0.55f, 0f };

        /// <summary>Grip + pommel lathe: row (px) and radius (px), top to bottom (into the guard).</summary>
        static readonly float[] LatheRows = { 0f, 3f, 6f, 9f, 13f, 17f, 21f, 25f, 29f, 33f, 37f, 41f, 45f, 49f, 53f, 58f, 64f, 76f, 100f, 112f, 117f, 124f };
        static readonly float[] LatheR = { 0f, 8f, 17f, 23f, 27f, 33f, 36f, 38.5f, 39.5f, 39.5f, 37.5f, 35f, 32f, 27.5f, 27f, 24f, 22.5f, 21.5f, 21.5f, 22f, 24.5f, 28f };

        /// <summary>Outline of the painted crossguard (px, traced from the art's alpha between rows 121 and 291).</summary>
        static readonly float[] Guard = { 4,171, 6,197, 18,212, 23,227, 39,241, 48,241, 49,227, 54,218, 63,210, 77,207, 88,215, 90,222, 86,231, 80,236, 63,243, 49,254, 49,257, 64,273, 72,291, 247,291, 254,274, 270,257, 270,254, 259,245, 238,236, 229,225, 231,214, 241,207, 254,209, 264,217, 270,229, 271,241, 276,242, 284,238, 296,226, 300,218, 300,212, 312,198, 315,186, 314,171, 308,157, 303,157, 300,165, 292,149, 276,132, 267,126, 252,121, 243,123, 246,134, 244,147, 240,151, 231,154, 221,152, 220,147, 213,142, 192,133, 187,121, 131,121, 127,133, 105,142, 98,148, 97,152, 87,154, 79,152, 72,143, 75,122, 61,122, 42,132, 26,149, 18,165, 16,158, 12,156, 8,160 };

        /// <summary>Painted gem diamond on the guard: centre, half width / height, how far it stands out (px).</summary>
        const float GemX = 160f, GemY = 188f, GemHalfW = 29f, GemHalfH = 44f, GemDepth = 24f;

        static Mesh mesh;
        static readonly List<Vector3> V = new List<Vector3>(4096);
        static readonly List<Vector2> U = new List<Vector2>(4096);
        static readonly List<Color32> C = new List<Color32>(4096);
        static readonly List<int> T = new List<int>(12000);

        /// <summary>The shared mesh (built on first use).</summary>
        public static Mesh Get()
        {
            if (mesh != null) return mesh;
            V.Clear(); U.Clear(); C.Clear(); T.Clear();
            Lathe();
            Blade();
            GuardSlab();
            Gem();
            mesh = new Mesh { name = "EarthBreakGreatsword" };
            mesh.SetVertices(V); mesh.SetUVs(0, U); mesh.SetColors(C); mesh.SetTriangles(T, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ---- helpers: px frame -> model, triangles wound so their normal points `outward` (Unity front face = clockwise from outside) ----

        static int Vert(float px, float py, float pz, float gem = 0f)
        {
            V.Add(new Vector3((px - Cx) / Hpx, (Hpx - py) / Hpx, -pz / Hpx));
            U.Add(new Vector2(px / W, 1f - py / Hpx));
            C.Add(new Color32(255, 255, 255, (byte)Mathf.RoundToInt(gem * 255f)));
            return V.Count - 1;
        }

        static void Tri(int a, int b, int c, Vector3 outward)
        {
            Vector3 n = Vector3.Cross(V[b] - V[a], V[c] - V[a]);
            if (Vector3.Dot(n, outward) < 0f) { int s = b; b = c; c = s; }
            T.Add(a); T.Add(b); T.Add(c);
        }

        static void Quad(int a, int b, int c, int d, Vector3 outward) { Tri(a, b, c, outward); Tri(b, d, c, outward); } // a b / c d

        // ---- parts ----

        static void Lathe()
        {
            const int Sides = 16;
            int rows = LatheRows.Length;
            var ring = new int[rows, Sides + 1];
            for (int k = 0; k < rows; k++)
                for (int s = 0; s <= Sides; s++)
                {
                    float a = s * Mathf.PI * 2f / Sides, r = LatheR[k];
                    ring[k, s] = Vert(Cx + Mathf.Cos(a) * r, LatheRows[k], -Mathf.Sin(a) * r);
                }
            for (int k = 0; k < rows - 1; k++)
                for (int s = 0; s < Sides; s++)
                {
                    float a = (s + 0.5f) * Mathf.PI * 2f / Sides;
                    float dr = (LatheR[k + 1] - LatheR[k]) / Mathf.Max(1f, LatheRows[k + 1] - LatheRows[k]);
                    Vector3 o = new Vector3(Mathf.Cos(a), dr, Mathf.Sin(a)); // radial, tipped up where the profile widens downward
                    Quad(ring[k, s], ring[k, s + 1], ring[k + 1, s], ring[k + 1, s + 1], o);
                }
        }

        static void Blade()
        {
            // the fuller first, edges last: with no depth writes the outer facets must cover the inner ones
            int[] order = { 3, 2, 4, 1, 5, 0, 6 };
            for (int side = -1; side <= 1; side += 2) // -1 = painted front (toward -z), +1 = back
                foreach (int i in order)
                {
                    int prevA = -1, prevB = -1;
                    for (int r = 0; r < BladeRows.Length; r++)
                    {
                        float py = BladeRows[r], hw = BladeHalf[r], t = 0.17f * hw;
                        int a = Vert(Cx + Station[i] * hw, py, -side * Depth[i] * t), b = Vert(Cx + Station[i + 1] * hw, py, -side * Depth[i + 1] * t);
                        if (prevA >= 0) Quad(prevA, prevB, a, b, new Vector3(0f, 0f, side));
                        prevA = a; prevB = b;
                    }
                }
        }

        static float GuardHalf(float px) { return 14f - 6f * Mathf.Abs(px - Cx) / 156f; }

        static void GuardSlab()
        {
            int n = Guard.Length / 2;
            float area = 0f; // shoelace in the px frame (y down): sign tells which side of an edge is outside
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += Guard[2 * i] * Guard[2 * j + 1] - Guard[2 * j] * Guard[2 * i + 1];
            }
            float sgn = area > 0f ? 1f : -1f;
            for (int i = 0; i < n; i++) // walls
            {
                int j = (i + 1) % n;
                float x0 = Guard[2 * i], y0 = Guard[2 * i + 1], x1 = Guard[2 * j], y1 = Guard[2 * j + 1];
                float ex = x1 - x0, ey = y1 - y0;
                Vector3 outward = new Vector3(ey * sgn, ex * sgn, 0f); // edge normal in the px frame, y flipped into model space
                int a = Vert(x0, y0, GuardHalf(x0)), b = Vert(x1, y1, GuardHalf(x1)), c = Vert(x0, y0, -GuardHalf(x0)), d = Vert(x1, y1, -GuardHalf(x1));
                Quad(a, b, c, d, outward);
            }
            const float Step = 6f;
            float minX = 4f, maxX = 315f, minY = 121f, maxY = 291f;
            int cols = Mathf.CeilToInt((maxX - minX) / Step), rows = Mathf.CeilToInt((maxY - minY) / Step);
            for (int side = -1; side <= 1; side += 2)
                for (int gy = 0; gy < rows; gy++)
                    for (int gx = 0; gx < cols; gx++)
                    {
                        float x0 = minX + gx * Step, y0 = minY + gy * Step, x1 = Mathf.Min(maxX, x0 + Step), y1 = Mathf.Min(maxY, y0 + Step);
                        if (!NearGuard(x0, y0, x1, y1)) continue;
                        int a = Vert(x0, y0, -side * GuardHalf(x0)), b = Vert(x1, y0, -side * GuardHalf(x1)), c = Vert(x0, y1, -side * GuardHalf(x0)), d = Vert(x1, y1, -side * GuardHalf(x1));
                        Quad(a, b, c, d, new Vector3(0f, 0f, side));
                    }
        }

        /// <summary>A grid cell touches the guard outline (any corner or the centre inside): the shader clips the rest by the painted alpha.</summary>
        static bool NearGuard(float x0, float y0, float x1, float y1)
        {
            return Inside(x0, y0) || Inside(x1, y0) || Inside(x0, y1) || Inside(x1, y1) || Inside((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
        }

        static bool Inside(float x, float y)
        {
            int n = Guard.Length / 2;
            bool c = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = Guard[2 * i], yi = Guard[2 * i + 1], xj = Guard[2 * j], yj = Guard[2 * j + 1];
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) c = !c;
            }
            return c;
        }

        static void Gem()
        {
            float half = GuardHalf(GemX);
            for (int side = -1; side <= 1; side += 2)
            {
                float z = -side * (half + GemDepth); // apex
                int apex = Vert(GemX, GemY, z, 1f);
                int top = Vert(GemX, GemY - GemHalfH, -side * half, 1f), right = Vert(GemX + GemHalfW, GemY, -side * half, 1f);
                int bottom = Vert(GemX, GemY + GemHalfH, -side * half, 1f), left = Vert(GemX - GemHalfW, GemY, -side * half, 1f);
                Vector3 c = V[apex];
                int[] rim = { top, right, bottom, left };
                for (int k = 0; k < 4; k++)
                {
                    int a = rim[k], b = rim[(k + 1) % 4];
                    Vector3 mid = (V[a] + V[b] + c) / 3f, centre = new Vector3(0f, (Hpx - GemY) / Hpx, 0f);
                    Tri(apex, a, b, mid - centre);
                }
            }
        }
    }
}
