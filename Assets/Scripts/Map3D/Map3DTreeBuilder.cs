using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Generated trees and bushes of the 3D map (ChatGPT spec section 2: "trunk mesh + hand-painted leaf cards + spherical normals + soft diffuse",
    /// never textureless blobs). Broadleaf (oak, young oak, ancient oak, birch, willow): bent tapered trunk with a root flare, 4-6 branches toward
    /// the clusters, 7-11 leaf clusters on the crown ellipsoid, 9-12 cards each (40 % along the cluster surface, 40 % crossed, 20 % facing the default
    /// view), normals blended 0.85 toward the cluster / crown ellipsoid; willow adds hanging strands. Pine: straight trunk, 9 tiers of needle sprays
    /// radiating and drooping (two crossed cards per spray), cone normals 0.65. Bush: 3-5 low clusters. Cards darken toward the crown interior and
    /// underside (vertex r), sway by vertex g, vary by cluster (b), and some take the second tint (a). Meshes are cached: 4 variants per kind.
    /// Units: metres, y up (Map3DMeshKit converts).
    /// </summary>
    public static class Map3DTreeBuilder
    {
        public enum Kind { Oak, OakYoung, OakAncient, Birch, Willow, Pine, Bush, BushLilac, BushWhite }
        public const int Variants = 4;
        static readonly Dictionary<int, Mesh> Cache = new Dictionary<int, Mesh>();
        static readonly Vector3 ViewDir = new Vector3(0f, 0.85f, -0.53f); // toward the camera at yaw 0 (pitch 58), y-up frame (z north)

        /// <summary>Bark strip of the atlas: 0 oak, 1 pine, 2 birch, 3 willow.</summary>
        static float BarkStrip(Kind k) { return k == Kind.Pine ? 1 : k == Kind.Birch ? 2 : k == Kind.Willow ? 3 : 0; }

        public static string Look(Kind k)
        {
            switch (k)
            {
                case Kind.OakYoung: return "oakYoung";
                case Kind.Birch: return "birch";
                case Kind.Willow: return "willow";
                case Kind.Pine: return "pine";
                case Kind.Bush: return "bush";
                case Kind.BushLilac: return "bushLilac";
                case Kind.BushWhite: return "bushWhite";
                default: return "oak";
            }
        }

        public static Mesh Get(Kind k, int variant)
        {
            int key = (int)k * 16 + variant;
            Mesh m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            var rng = new System.Random(9173 + key * 31);
            var kit = new Map3DMeshKit();
            switch (k)
            {
                case Kind.Pine: Pine(kit, rng); break;
                case Kind.Bush: case Kind.BushLilac: case Kind.BushWhite: Bush(kit, rng, k); break;
                default: Broadleaf(kit, rng, k); break;
            }
            m = kit.Build("Map3DTree_" + k + "_" + variant);
            Cache[key] = m;
            return m;
        }

        static float R(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }
        static Vector3 InSphere(System.Random r) { Vector3 v; do { v = new Vector3(R(r, -1, 1), R(r, -1, 1), R(r, -1, 1)); } while (v.sqrMagnitude > 1f); return v; }

        // ---------------- broadleaf ----------------
        static void Broadleaf(Map3DMeshKit kit, System.Random rng, Kind k)
        {
            float H, crownR, trunkR; int clusters;
            switch (k)
            {
                case Kind.OakYoung: H = R(rng, 4.4f, 5.0f); crownR = R(rng, 1.7f, 2.0f); trunkR = 0.17f; clusters = 7; break;
                case Kind.OakAncient: H = R(rng, 7.0f, 7.6f); crownR = R(rng, 3.2f, 3.6f); trunkR = 0.42f; clusters = 11; break;
                case Kind.Birch: H = R(rng, 5.8f, 6.6f); crownR = R(rng, 1.6f, 1.9f); trunkR = 0.15f; clusters = 8; break;
                case Kind.Willow: H = R(rng, 5.2f, 5.8f); crownR = R(rng, 2.8f, 3.2f); trunkR = 0.30f; clusters = 9; break;
                default: H = R(rng, 5.9f, 6.8f); crownR = R(rng, 2.4f, 2.8f); trunkR = 0.30f; clusters = 9; break;
            }
            float crownRy = k == Kind.Birch ? H * 0.30f : k == Kind.Willow ? H * 0.26f : H * 0.30f;
            Vector3 C = new Vector3(R(rng, -0.2f, 0.2f), H - crownRy * 1.05f, R(rng, -0.2f, 0.2f));
            float strip = BarkStrip(k) * 0.25f;

            // trunk: sinks 0.2 m, flares at the root, bends a little toward the crown centre
            var path = new List<Vector3>(); var rad = new List<float>(); var cols = new List<Color>();
            float topH = C.y + crownRy * 0.1f;
            Vector3 lean = new Vector3(C.x, 0f, C.z);
            float[] t = { -0.04f, 0f, 0.06f, 0.25f, 0.5f, 0.75f, 1f };
            foreach (float s in t)
            {
                float hh = Mathf.Max(-0.2f, s * topH);
                Vector3 bend = lean * (s * s) + new Vector3(Mathf.Sin(s * 3.1f + rng.Next(9)) * 0.08f, 0f, Mathf.Cos(s * 2.3f) * 0.06f) * s;
                path.Add(new Vector3(bend.x, hh, bend.z));
                float flare = s < 0.07f ? Mathf.Lerp(1.75f, 1.25f, s / 0.07f) : 1f;
                rad.Add(trunkR * Mathf.Lerp(1f, 0.45f, s) * flare);
                cols.Add(new Color(Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(s * 3f)), s * 0.25f, s < 0.1f ? 0.7f : 0.15f, 0f));
            }
            kit.Tube(path, rad, 8, strip, 0.25f, 2.2f, cols, true);

            // clusters on the crown ellipsoid: one on top, a ring round the middle, a few lower
            var centres = new List<Vector3>(); var radii = new List<float>();
            centres.Add(C + new Vector3(R(rng, -0.3f, 0.3f), crownRy * 0.55f, R(rng, -0.3f, 0.3f))); radii.Add(crownR * 0.55f);
            int ring = clusters - 1 - clusters / 4;
            float a0 = R(rng, 0f, 6.28f);
            for (int i = 0; i < ring; i++)
            {
                float a = a0 + i * 6.283f / ring + R(rng, -0.25f, 0.25f);
                centres.Add(C + new Vector3(Mathf.Cos(a) * crownR * 0.62f, R(rng, -0.05f, 0.25f) * crownRy, Mathf.Sin(a) * crownR * 0.62f));
                radii.Add(crownR * R(rng, 0.42f, 0.52f));
            }
            for (int i = 0; i < clusters / 4; i++)
            {
                float a = a0 + 0.5f + i * 6.283f / Mathf.Max(1, clusters / 4);
                centres.Add(C + new Vector3(Mathf.Cos(a) * crownR * 0.45f, -crownRy * 0.45f, Mathf.Sin(a) * crownR * 0.45f));
                radii.Add(crownR * 0.42f);
            }
            // branches from the upper trunk to the outer clusters
            for (int i = 1; i < centres.Count; i += (k == Kind.OakYoung ? 2 : 1))
            {
                Vector3 from = path[4] + (path[5] - path[4]) * R(rng, 0f, 1f);
                Vector3 to = Vector3.Lerp(from, centres[i], 0.75f);
                Vector3 mid = Vector3.Lerp(from, to, 0.5f) + Vector3.up * 0.25f;
                kit.Tube(new[] { from, mid, to }, new[] { trunkR * 0.38f, trunkR * 0.25f, trunkR * 0.12f }, 5, strip, 0.25f, 1.6f,
                    new[] { new Color(0.85f, 0.25f, 0.1f, 0f), new Color(0.9f, 0.4f, 0.1f, 0f), new Color(0.9f, 0.55f, 0f, 0f) }, false);
            }
            float cardSize = k == Kind.Birch ? 0.50f : k == Kind.OakAncient ? 0.75f : 0.63f; // half-extent, metres (round 1: -12 %, denser core)
            float tint2 = k == Kind.Birch ? 0.35f : 0.22f;
            for (int c = 0; c < centres.Count; c++)
                Cluster(kit, rng, centres[c], radii[c], C, new Vector3(crownR, crownRy, crownR), k == Kind.Birch ? 10 : 13, cardSize, k == Kind.Birch ? 0.78f : 0.73f, tint2);
            if (k == Kind.Willow) WillowStrands(kit, rng, centres, radii);
        }

        static void Cluster(Map3DMeshKit kit, System.Random rng, Vector3 cc, float cr, Vector3 crown, Vector3 crownR, int cards, float size, float sph, float tint2)
        {
            float clusterVar = R(rng, 0f, 1f);
            float alt = rng.NextDouble() < tint2 ? 1f : 0f;
            var nrm = new Vector3[4]; var col = new Color[4];
            for (int i = 0; i < cards; i++)
            {
                Vector3 c = cc + InSphere(rng) * cr * 0.62f;
                Vector3 outward = (c - cc).sqrMagnitude > 1e-4f ? (c - cc).normalized : Vector3.up;
                Vector3 n;
                float pick = (float)rng.NextDouble();
                if (pick < 0.4f) n = (outward + InSphere(rng) * 0.35f).normalized;
                else if (pick < 0.8f) n = InSphere(rng).normalized;
                else n = (ViewDir + InSphere(rng) * 0.3f).normalized;
                if (n.sqrMagnitude < 0.01f) n = Vector3.up;
                Vector3 right = Vector3.Cross(Mathf.Abs(n.y) > 0.9f ? Vector3.forward : Vector3.up, n).normalized;
                Vector3 up = Vector3.Cross(n, right);
                float roll = R(rng, -0.7f, 0.7f);
                Vector3 r2 = right * Mathf.Cos(roll) + up * Mathf.Sin(roll), u2 = up * Mathf.Cos(roll) - right * Mathf.Sin(roll);
                float s = size * R(rng, 0.8f, 1.2f);
                Vector3[] corners = { c - r2 * s - u2 * s, c + r2 * s - u2 * s, c + r2 * s + u2 * s, c - r2 * s + u2 * s };
                for (int q = 0; q < 4; q++)
                {
                    Vector3 p = corners[q];
                    Vector3 sc = (p - cc) / cr, sw = new Vector3((p.x - crown.x) / crownR.x, (p.y - crown.y) / crownR.y, (p.z - crown.z) / crownR.z);
                    Vector3 sn = (sc.normalized * 0.55f + sw.normalized * 0.45f).normalized;
                    nrm[q] = Vector3.Lerp(n, sn, sph).normalized;
                    float inside = Mathf.Clamp01(sw.magnitude);
                    float ao = Mathf.Clamp01(0.22f + 0.58f * inside + 0.30f * sw.y) * (sw.y < -0.2f ? 0.77f : 1f); // underside x0.77, darker core (rounds 1-2)
                    col[q] = new Color(ao, Mathf.Lerp(0.55f, 1f, inside), clusterVar, alt);
                }
                kit.Card(c, r2 * s, u2 * s, Map3DMeshKit.Cell(rng.Next(16)), nrm, col);
            }
            // round 2: two dark interior filler cards per cluster (solid crown core, less "leaf-card bouquet")
            for (int i = 0; i < 2; i++)
            {
                Vector3 c = cc + InSphere(rng) * cr * 0.25f - (cc - crown).normalized * cr * 0.2f;
                Vector3 n = InSphere(rng).normalized; if (n.sqrMagnitude < 0.01f) n = Vector3.up;
                Vector3 right = Vector3.Cross(Mathf.Abs(n.y) > 0.9f ? Vector3.forward : Vector3.up, n).normalized * size * 1.1f, up = Vector3.Cross(n, right.normalized) * size * 1.1f;
                for (int q = 0; q < 4; q++) { nrm[q] = (c - crown).normalized; col[q] = new Color(0.18f, 0.5f, clusterVar, alt); }
                kit.Card(c, right, up, Map3DMeshKit.Cell(rng.Next(16)), nrm, col);
            }
        }

        static void WillowStrands(Map3DMeshKit kit, System.Random rng, List<Vector3> centres, List<float> radii)
        {
            var nrm = new Vector3[4]; var col = new Color[4];
            for (int c = 1; c < centres.Count; c++)
                for (int i = 0; i < 4; i++)
                {
                    float a = R(rng, 0f, 6.28f);
                    Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    Vector3 top = centres[c] + dir * radii[c] * R(rng, 0.5f, 0.9f) - Vector3.up * radii[c] * 0.2f;
                    float len = R(rng, 0.9f, 1.6f);
                    Vector3 right = Vector3.Cross(Vector3.up, dir).normalized * 0.32f;
                    Vector3 half = Vector3.down * len * 0.5f;
                    Vector3 mid = top + half;
                    Vector3 outN = (dir + Vector3.up * 0.2f).normalized;
                    for (int q = 0; q < 4; q++) { nrm[q] = outN; col[q] = new Color(q < 2 ? 0.55f : 0.85f, q < 2 ? 1f : 0.6f, R(rng, 0f, 1f), 1f); }
                    kit.Card(mid, right, -half, Map3DMeshKit.Cell(rng.Next(16)), nrm, col);
                }
        }

        // ---------------- pine ----------------
        static void Pine(Map3DMeshKit kit, System.Random rng)
        {
            float H = R(rng, 5.8f, 6.8f), baseR = R(rng, 1.45f, 1.7f);
            var path = new List<Vector3> { new Vector3(0, -0.2f, 0), new Vector3(0, 0.05f, 0), new Vector3(0.03f, H * 0.4f, 0.02f), new Vector3(0.02f, H * 0.98f, 0f) };
            var rad = new List<float> { 0.30f, 0.24f, 0.15f, 0.04f };
            var cols = new List<Color> { new Color(0.55f, 0, 0.6f, 0), new Color(0.7f, 0, 0.3f, 0), new Color(0.9f, 0.1f, 0.1f, 0), new Color(1f, 0.3f, 0f, 0) };
            kit.Tube(path, rad, 7, 0.25f, 0.25f, 2.2f, cols, true);
            const int Tiers = 9;
            var nrm = new Vector3[4]; var col = new Color[4];
            float golden = 2.39996f, ang = R(rng, 0f, 6.28f);
            for (int ti = 0; ti < Tiers; ti++)
            {
                float t = ti / (float)(Tiers - 1);
                float h = Mathf.Lerp(H * 0.20f, H * 0.93f, t) + R(rng, -0.08f, 0.08f);
                float reach = baseR * Mathf.Pow(1f - t, 0.85f) + 0.35f;
                int sprays = ti == Tiers - 1 ? 4 : 7;
                for (int s = 0; s < sprays; s++)
                {
                    ang += golden;
                    Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    float droop = R(rng, 0.25f, 0.5f);
                    Vector3 outD = (dir - Vector3.up * droop).normalized;
                    float len = reach * R(rng, 0.9f, 1.15f);
                    Vector3 basePt = new Vector3(0f, h, 0f) + dir * 0.06f;
                    Vector3 c = basePt + outD * len * 0.5f;
                    Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
                    for (int cross = 0; cross < 2; cross++)
                    {
                        // the spray plane turned round its own axis: ~30 deg from flat, then the crossed copy 90 deg further
                        float roll = (cross == 0 ? 0.5f : 2.07f) + R(rng, -0.2f, 0.2f);
                        Vector3 w = (side * Mathf.Cos(roll) + Vector3.Cross(outD, side) * Mathf.Sin(roll)).normalized * len * 0.26f;
                        Vector3 r = outD * len * 0.5f;
                        for (int q = 0; q < 4; q++)
                        {
                            Vector3 p = c + (q == 0 || q == 3 ? -r : r) + (q < 2 ? -w : w);
                            Vector3 cone = (new Vector3(p.x, 0f, p.z).normalized * 0.8f + Vector3.up * 0.6f).normalized;
                            nrm[q] = Vector3.Lerp(Vector3.Cross(outD, w).normalized, cone, 0.68f).normalized;
                            bool tip = q == 1 || q == 2;
                            float ao = Mathf.Clamp01(0.35f + 0.45f * t + (tip ? 0.25f : 0f));
                            col[q] = new Color(ao, tip ? 0.9f : 0.3f, (float)rng.NextDouble(), t > 0.6f ? 1f : 0f);
                        }
                        kit.Card(c, r, w, Map3DMeshKit.Cell(rng.Next(16), 0.25f, 0.75f), nrm, col);
                    }
                }
            }
        }

        // ---------------- bush ----------------
        static void Bush(Map3DMeshKit kit, System.Random rng, Kind k)
        {
            float rad = R(rng, 0.75f, 0.95f);
            Vector3 centre = new Vector3(0f, rad * 0.55f, 0f);
            int n = rng.Next(3, 6);
            float tint2 = k == Kind.Bush ? 0.2f : 0.45f;
            for (int i = 0; i < n; i++)
            {
                float a = i * 6.283f / n + R(rng, -0.4f, 0.4f);
                Vector3 c = centre + new Vector3(Mathf.Cos(a) * rad * 0.45f, R(rng, -0.1f, 0.25f), Mathf.Sin(a) * rad * 0.45f);
                Cluster(kit, rng, c, rad * 0.65f, centre, new Vector3(rad, rad * 0.7f, rad), 7, 0.42f, 0.9f, tint2);
            }
        }
    }
}
