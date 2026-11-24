using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Willowbrook Village's Adventure Guild Hall as a hybrid 3D building (ChatGPT "Guild Hall Hybrid 3D Production Spec", 24/11/2026,
    /// docs/reference/ro3-compare/rounds/r2/guild-hall-hybrid-3d-spec-chatgpt.txt; RO3 look loop round 2: "Guild Hall hybrid 3D" is the village's next step).
    /// Replaces the painted tauGuildHallHouse sprite on the 3D map only (the 2.5D map, the rows' 10 x 5 m solid block and the server are unchanged).
    /// Built from boxes / slabs / triangles in metres (y up, x east, z north, origin = centre of the 10 x 5 m block on the ground, door to the south):
    /// stone plinth 9.6 x 4.6 x 0.55, hall body 8.8 x 3.55 x 3.15, front piers 0.45 m proud of the door wall (the recessed entrance), main roof with its
    /// ridge along x and 0.7 m eaves, a steep front cross gable over the door (the crest sits on its triangle), side gable ends, timber corner posts /
    /// belts / plates, two steps and a red carpet that run 0.35 m past the block, banners, a notice board, windows (atlas B has no lantern: none).
    /// Texture: two ChatGPT atlases (Resources/Map3D/guild-atlas-a.png = 2x2 seamless plaster / stone / timber / slate roof, drawn by Map3DPainted
    /// _Mode 2; guild-atlas-b.png = door, windows, crest, banners, steps, carpet, decals, uv-mapped and alpha-cut by Map3DSpriteCutout).
    /// </summary>
    public static class Map3DGuildHallBuilder
    {
        public const int Plaster = 0, Stone = 1, Timber = 2, Roof = 3; // atlas A cells (top-left, top-right, bottom-left, bottom-right)

        /// <summary>Sub-rects of atlas B (u0, v0, u1, v1; v up), measured on the drawn atlas (docs/reference/ro3-compare/guild-hall-3d/).</summary>
        /// guild-atlas-b-chatgpt-1: arched door in its frame, small arched window, notice board, shield-and-swords crest, lily banner, blue-gold trim, red carpet.
        public static readonly Vector4 BDoor = new Vector4(0.035f, 0.665f, 0.317f, 0.971f), BWindow = new Vector4(0.341f, 0.817f, 0.460f, 0.946f),
            BBoard = new Vector4(0.135f, 0.513f, 0.350f, 0.654f), BCrest = new Vector4(0.531f, 0.733f, 0.703f, 0.962f), BBanner = new Vector4(0.720f, 0.742f, 0.849f, 0.947f),
            BTrim = new Vector4(0.533f, 0.684f, 0.962f, 0.721f), BCarpet = new Vector4(0.313f, 0.110f, 0.457f, 0.458f);

        static Mesh structure, detail;

        public static bool HasArt { get { return Resources.Load<Texture2D>("Map3D/guild-atlas-a") != null && Resources.Load<Texture2D>("Map3D/guild-atlas-b") != null; } }

        /// <summary>The structure mesh (atlas A, submesh 0).</summary>
        public static Mesh Structure { get { if (structure == null) Build(); return structure; } }
        /// <summary>The detail cards (atlas B, submesh 0, alpha-cut).</summary>
        public static Mesh Detail { get { if (detail == null) Build(); return detail; } }

        const float PlinthH = 0.55f, WallH = 3.5f, Eave = PlinthH + WallH, Ridge = Eave + 2.1f; // round 3: wall +0.35 m, roof rise -14 %, same total height
        const float BodyW = 8.8f, BodyD = 3.55f, BodyZ = 0.25f;      // body centre a little north: a 0.7 m stone terrace before the door
        const float PierD = 0.45f, DoorHalf = 1.65f;

        static void Build()
        {
            var a = new Map3DMeshKit();
            var b = new Map3DMeshKit();
            var rng = new System.Random(7);
            float bf = BodyZ - BodyD * 0.5f;                            // body front face z
            float bk = BodyZ + BodyD * 0.5f;
            // P01 plinth, P02 body
            Box(a, new Vector3(0f, PlinthH * 0.5f, 0f), new Vector3(4.8f, PlinthH * 0.5f, 2.3f), Stone, 2.2f, 0.78f);
            Box(a, new Vector3(0f, PlinthH + WallH * 0.5f, BodyZ), new Vector3(BodyW * 0.5f, WallH * 0.5f, BodyD * 0.5f), Plaster, 3.2f, 0.82f);
            // P03 front piers + lintel (the door sits on the body wall 0.45 m behind their faces)
            float pz = bf - PierD * 0.5f;
            foreach (float s in new[] { -1f, 1f })
            {
                float cx = s * (DoorHalf + (BodyW * 0.5f - DoorHalf) * 0.5f);
                Box(a, new Vector3(cx, PlinthH + WallH * 0.5f, pz), new Vector3((BodyW * 0.5f - DoorHalf) * 0.5f, WallH * 0.5f, PierD * 0.5f), Plaster, 3.2f, 0.82f);
                Box(a, new Vector3(s * DoorHalf, PlinthH + WallH * 0.5f, pz - 0.02f), new Vector3(0.1f, WallH * 0.5f + 0.02f, PierD * 0.5f + 0.04f), Timber, 1.6f, 0.85f); // jamb posts
                Box(a, new Vector3(s * BodyW * 0.5f, PlinthH + WallH * 0.5f, pz - 0.02f), new Vector3(0.11f, WallH * 0.5f + 0.02f, PierD * 0.5f + 0.04f), Timber, 1.6f, 0.85f); // front corners
                Box(a, new Vector3(s * BodyW * 0.5f, PlinthH + WallH * 0.5f, bk), new Vector3(0.11f, WallH * 0.5f + 0.02f, 0.11f), Timber, 1.6f, 0.85f);       // back corners
            }
            Box(a, new Vector3(0f, Eave - 0.3f, pz), new Vector3(DoorHalf, 0.3f, PierD * 0.5f), Plaster, 3.2f, 0.9f);                  // lintel band
            // round 3 "hốc cửa chưa đủ sâu": dark inner cheeks of the recess and a shadowed soffit under the lintel
            foreach (float s in new[] { -1f, 1f })
                Box(a, new Vector3(s * (DoorHalf - 0.06f), PlinthH + (WallH - 0.6f) * 0.5f, pz), new Vector3(0.06f, (WallH - 0.6f) * 0.5f, PierD * 0.5f - 0.01f), Timber, 1.6f, 0.45f);
            Box(a, new Vector3(0f, Eave - 0.62f, pz + 0.02f), new Vector3(DoorHalf - 0.1f, 0.02f, PierD * 0.5f - 0.02f), Timber, 1.6f, 0.4f);
            Box(a, new Vector3(0f, PlinthH + 1.75f, pz - PierD * 0.5f - 0.03f), new Vector3(BodyW * 0.5f + 0.05f, 0.08f, 0.05f), Timber, 1.6f, 0.9f); // belt
            Box(a, new Vector3(0f, Eave - 0.04f, pz - PierD * 0.5f - 0.03f), new Vector3(BodyW * 0.5f + 0.08f, 0.1f, 0.06f), Timber, 1.6f, 0.9f);   // wall plate
            Box(a, new Vector3(0f, Eave - 0.04f, bk + 0.03f), new Vector3(BodyW * 0.5f + 0.08f, 0.1f, 0.06f), Timber, 1.6f, 0.9f);
            // main roof: ridge along x over the body, 0.7 m eaves front / back, 0.75 m at the gable ends
            float halfD = BodyD * 0.5f + PierD * 0.5f, slope = Mathf.Atan2(Ridge - Eave, halfD);
            float rz = BodyZ - PierD * 0.25f;
            foreach (float s in new[] { -1f, 1f })
            {
                // round 3 "mái đang lấn át mặt tiền": the front (camera-side) eave only 0.35 m past the facade so the wall, door and banners show; back keeps 0.7 m
                float run = halfD + (s < 0f ? 0.35f : 0.7f), rise = (Ridge - Eave) * run / halfD, len = Mathf.Sqrt(run * run + rise * rise);
                Quaternion q = Quaternion.AngleAxis(s * slope * Mathf.Rad2Deg, Vector3.right);
                Vector3 c = new Vector3(0f, Ridge - rise * 0.5f + 0.06f, rz + s * run * 0.5f);
                Slab(a, c, new Vector3(BodyW * 0.5f + 0.75f, 0.07f, len * 0.5f), q, Roof, 1.6f, s);
            }
            Box(a, new Vector3(0f, Ridge + 0.1f, rz), new Vector3(BodyW * 0.5f + 0.8f, 0.11f, 0.13f), Timber, 1.6f, 0.95f);            // ridge cap
            foreach (float s in new[] { -1f, 1f })                                                                                       // side gable ends
                Tri(a, new Vector3(s * BodyW * 0.5f, Eave, rz - halfD), new Vector3(s * BodyW * 0.5f, Eave, rz + halfD), new Vector3(s * BodyW * 0.5f, Ridge, rz), new Vector3(s, 0f, 0f), Plaster, 3.2f);
            // front cross gable over the entrance (steeper, the crest on its face)
            float gw = 2.3f, gz0 = pz - PierD * 0.5f - 0.05f, gz1 = rz; // round 3: wider front gable so the crest is not under the slopes
            float grun = gw + 0.3f, grise = (Ridge - Eave) * grun / gw, gslope = Mathf.Atan2(grise, grun), glen = Mathf.Sqrt(grun * grun + grise * grise);
            foreach (float s in new[] { -1f, 1f })
            {
                Quaternion q = Quaternion.AngleAxis(-s * gslope * Mathf.Rad2Deg, Vector3.forward);
                Vector3 c = new Vector3(s * grun * 0.5f, Ridge - grise * 0.5f + 0.08f, (gz0 - 0.15f + gz1) * 0.5f);
                SlabZ(a, c, new Vector3(glen * 0.5f, 0.07f, (gz1 - gz0 + 0.15f) * 0.5f), q, Roof, 1.6f);
            }
            Box(a, new Vector3(0f, Ridge + 0.12f, (gz0 - 0.15f + gz1) * 0.5f), new Vector3(0.13f, 0.12f, (gz1 - gz0 + 0.15f) * 0.5f + 0.05f), Timber, 1.6f, 0.95f);
            Tri(a, new Vector3(gw, Eave, gz0), new Vector3(-gw, Eave, gz0), new Vector3(0f, Ridge, gz0), new Vector3(0f, 0f, -1f), Plaster, 3.2f);
            foreach (float s in new[] { -1f, 1f })                                                                                       // barge boards
            {
                Vector3 e = new Vector3(s * (gw + 0.2f), Eave - 0.15f, gz0 - 0.08f), top = new Vector3(0f, Ridge + 0.05f, gz0 - 0.08f);
                Vector3 mid = (e + top) * 0.5f; float l = (top - e).magnitude * 0.5f;
                Box(a, mid, new Vector3(l, 0.09f, 0.07f), Quaternion.AngleAxis(Mathf.Atan2(top.y - e.y, top.x - e.x) * Mathf.Rad2Deg, Vector3.forward), Timber, 1.6f, 0.95f);
            }
            // steps (2 risers up to the plinth) in front of the block
            Box(a, new Vector3(0f, 0.09f, -2.62f), new Vector3(1.55f, 0.09f, 0.28f), Stone, 1.4f, 0.8f);
            Box(a, new Vector3(0f, 0.27f, -2.45f + 0.03f), new Vector3(1.4f, 0.09f, 0.18f), Stone, 1.4f, 0.85f);
            Box(a, new Vector3(0f, 0.2f, -2.42f), new Vector3(1.55f, 0.2f, 0.12f), Stone, 1.4f, 0.8f);

            // ---- atlas B details (cards just proud of the surfaces) ----
            Card(b, new Vector3(0f, PlinthH + 1.42f, bf - 0.03f), new Vector3(1.32f, 0f, 0f), new Vector3(0f, 1.43f, 0f), BDoor, Vector3.back);     // P04 + P05 arched door in its frame (2.64 x 2.86, +10 %, under the lintel)
            Card(b, new Vector3(0f, Eave - 0.3f, pz - PierD * 0.5f - 0.04f), new Vector3(1.6f, 0f, 0f), new Vector3(0f, 0.14f, 0f), BTrim, Vector3.back); // blue-gold band over the entrance
            Card(b, new Vector3(0f, Eave + 0.72f, gz0 - 0.04f), new Vector3(0.7f, 0f, 0f), new Vector3(0f, 0.93f, 0f), BCrest, Vector3.back);         // P20 crest on the gable (+17 %)
            foreach (float s in new[] { -1f, 1f })
            {
                float px = s * 2.65f, fz = pz - PierD * 0.5f - 0.05f;
                Card(b, new Vector3(px + s * 0.75f, PlinthH + 2.05f, fz - 0.08f), new Vector3(0.46f, 0f, 0f), new Vector3(0f, 0.73f, 0f), BBanner, Vector3.back); // P16 / P17 banners, +14 %, in front of the piers
                if (s > 0f) Card(b, new Vector3(px - 0.45f, PlinthH + 1.75f, fz), new Vector3(0.42f, 0f, 0f), new Vector3(0f, 0.46f, 0f), BWindow, Vector3.back); // front window (east pier)
                else Card(b, new Vector3(px + 0.4f, PlinthH + 1.05f, fz), new Vector3(0.7f, 0f, 0f), new Vector3(0f, 0.46f, 0f), BBoard, Vector3.back);   // quest notice board (west pier)
                Card(b, new Vector3(s * (BodyW * 0.5f + 0.03f), PlinthH + 1.6f, BodyZ), new Vector3(0f, 0f, -s * 0.42f), new Vector3(0f, 0.46f, 0f), BWindow, new Vector3(s, 0f, 0f)); // P21
            }
            Card(b, new Vector3(0f, PlinthH + 1.6f, bk + 0.03f), new Vector3(-0.42f, 0f, 0f), new Vector3(0f, 0.46f, 0f), BWindow, Vector3.forward);   // P22 rear
            // P13 red carpet from the door down the steps (a sloped card)
            Vector3 top0 = new Vector3(0f, PlinthH + 0.01f, bf - 0.1f), bot0 = new Vector3(0f, 0.02f, -2.95f);
            Vector3 mid0 = (top0 + bot0) * 0.5f, upv = (top0 - bot0) * 0.5f;
            Card(b, mid0, new Vector3(0.62f, 0f, 0f), upv, BCarpet, Vector3.Cross(new Vector3(1f, 0f, 0f), upv).normalized);
            structure = a.Build("Map3DGuildHall");
            detail = b.Build("Map3DGuildHallDetail");
            Move(detail);
        }

        /// <summary>The kit writes cards to the leaf submesh (1): copy them into submesh 0 so Map3DBatcher draws them with the cutout material.</summary>
        internal static void Move(Mesh m)
        {
            var t = new List<int>(m.GetTriangles(1));
            t.AddRange(m.GetTriangles(2));
            m.subMeshCount = 1;
            m.SetTriangles(t, 0);
        }

        internal static Color Col(float ao, int cell) { return new Color(ao, 0f, 0f, cell / 3f); }

        /// <summary>Axis box (half sizes h), every face tiled at `rep` metres per texture in its atlas-A cell; lower half darkened toward `ao`.</summary>
        internal static void Box(Map3DMeshKit k, Vector3 c, Vector3 h, int cell, float rep, float ao) { Box(k, c, h, Quaternion.identity, cell, rep, ao); }

        internal static void Box(Map3DMeshKit k, Vector3 c, Vector3 h, Quaternion q, int cell, float rep, float ao)
        {
            Vector3[] n = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (Vector3 f in n)
            {
                Vector3 u = Mathf.Abs(f.y) > 0.5f ? Vector3.forward : Vector3.up, r = Vector3.Cross(u, f);
                Vector3 fc = Vector3.Scale(f, h), ru = Vector3.Scale(r, h), uu = Vector3.Scale(u, h);
                Vector3[] p = { fc - ru - uu, fc + ru - uu, fc + ru + uu, fc - ru + uu };
                int b = k.Count;
                for (int i = 0; i < 4; i++)
                {
                    Vector3 w = q * p[i];
                    float s1 = Vector3.Dot(w + c, q * r), s2 = Vector3.Dot(w + c, q * u); // world-anchored uv: neighbouring boxes line up
                    k.Vert(c + w, q * f, new Vector2(s1 / rep, s2 / rep), Col(p[i].y > 0f || Mathf.Abs(f.y) > 0.5f ? 1f : ao, cell));
                }
                k.Tri(false, b, b + 2, b + 1); k.Tri(false, b, b + 3, b + 2);
            }
        }

        /// <summary>Roof slab along x turned about x: top face uv u = x (tile rows run along the eave), v = down the slope.</summary>
        static void Slab(Map3DMeshKit k, Vector3 c, Vector3 h, Quaternion q, int cell, float rep, float side)
        {
            Box(k, c, h, q, cell, rep, 0.95f);
        }

        /// <summary>Cross-gable slab (ridge along z) turned about z.</summary>
        static void SlabZ(Map3DMeshKit k, Vector3 c, Vector3 h, Quaternion q, int cell, float rep)
        {
            Box(k, c, h, q, cell, rep, 0.95f);
        }

        /// <summary>Flat triangle (a, b, top) facing `n`, atlas-A cell tiled in world metres.</summary>
        static void Tri(Map3DMeshKit k, Vector3 a, Vector3 b, Vector3 top, Vector3 n, int cell, float rep)
        {
            Vector3 r = Vector3.Cross(Vector3.up, n);
            int i0 = k.Count;
            foreach (Vector3 p in new[] { a, b, top }) k.Vert(p, n, new Vector2(Vector3.Dot(p, r) / rep, p.y / rep), Col(1f, cell));
            k.Tri(false, i0, i0 + 1, i0 + 2);
            k.Tri(false, i0, i0 + 2, i0 + 1); // both windings: the gable reads from inside the roof too when the camera dips
        }

        /// <summary>Atlas-B card centred at c (half extents right / up) facing `n`.</summary>
        internal static void Card(Map3DMeshKit k, Vector3 c, Vector3 right, Vector3 up, Vector4 uv, Vector3 n)
        {
            var nn = new[] { n, n, n, n };
            var cc = new[] { Color.white, Color.white, Color.white, Color.white };
            k.Card(c, right, up, uv, nn, cc);
        }
    }
}
