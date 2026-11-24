using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Willowbrook Village's south gate as real 3D (was 3 billboard sprites: tauGateTowerL / tauGateTowerR / tauGateArchBeam, which turned with the
    /// camera). Same design as the painted gate: per side a square cream-stone tower block with merlons, a round turret with two windows under a
    /// terracotta cone roof and a gold finial, a blue lily banner on the block (front and back), golden timber jamb posts on the road side; over the
    /// road a double timber arch with spokes and the crest at the apex. Stone / timber: Map3DAssets.TownStone (Guild atlas A), roof: TownRoof
    /// (wood atlas shingles, terracotta), banner / crest / windows: Guild atlas B cards (Map3DAssets.GuildB).
    /// Units: metres, y up, x east, z north; tower origin = centre of its 4 x 2.6 m block on the ground, arch origin = centre of the road gap.
    /// </summary>
    public static class Map3DGateBuilder
    {
        const int Stone = Map3DTownWallBuilder.Stone, Timber = Map3DTownWallBuilder.Timber, Shingles = Map3DWoodBuilder.Shingles;
        const float Rep = Map3DTownWallBuilder.Rep, HalfW = 2f, HalfD = 1.3f, BlockH = 3.6f, TurretR = 0.9f, TurretTop = 5.5f;
        public const float BlockWidth = HalfW * 2f;

        public struct Parts { public Mesh Stone, Roof, Detail; }
        static Parts west, east;
        static Mesh arch, archDetail;
        static float archSpan;

        static void Box(Map3DMeshKit k, Vector3 c, Vector3 h, float ao, int cell = Stone) { Map3DGuildHallBuilder.Box(k, c, h, cell, cell == Stone ? Rep : 1.4f, ao); }

        /// <summary>Tower meshes; roadEast = the gate road is east of this tower (the west tower).</summary>
        public static Parts Tower(bool roadEast)
        {
            Parts p = roadEast ? west : east;
            if (p.Stone != null) return p;
            float rs = roadEast ? 1f : -1f;
            var k = new Map3DMeshKit();
            var roof = new Map3DMeshKit();
            var d = new Map3DMeshKit();
            Box(k, new Vector3(0f, -0.35f, 0f), new Vector3(HalfW + 0.06f, 0.55f, HalfD + 0.06f), 0.7f);           // footing
            Box(k, new Vector3(0f, 0.2f + (BlockH - 0.2f) * 0.5f, 0f), new Vector3(HalfW, (BlockH - 0.2f) * 0.5f, HalfD), 0.82f); // block
            Box(k, new Vector3(0f, 0.38f, 0f), new Vector3(HalfW + 0.05f, 0.18f, HalfD + 0.05f), 0.8f);             // plinth course
            Box(k, new Vector3(0f, BlockH, 0f), new Vector3(HalfW + 0.09f, 0.16f, HalfD + 0.09f), 0.97f);           // top course
            foreach (float z in new[] { -1f, 1f })                                                                  // merlons: outer edge (3) + road corners
            {
                Box(k, new Vector3(-rs * (HalfW - 0.3f), BlockH + 0.44f, z * (HalfD - 0.28f)), new Vector3(0.3f, 0.3f, 0.28f), 1f);
                Box(k, new Vector3(rs * (HalfW - 0.22f), BlockH + 0.4f, z * (HalfD - 0.22f)), new Vector3(0.22f, 0.26f, 0.22f), 1f);
            }
            Box(k, new Vector3(-rs * (HalfW - 0.3f), BlockH + 0.44f, 0f), new Vector3(0.3f, 0.3f, 0.26f), 1f);
            Vector3 tc = new Vector3(rs * 0.45f, 0f, 0f);                                                           // turret toward the road, as painted
            Map3DTownWallBuilder.Cylinder(k, tc, BlockH, TurretTop, TurretR, 16, Stone, Rep, 0.85f);
            Map3DTownWallBuilder.Cylinder(k, tc, TurretTop - 0.22f, TurretTop, TurretR + 0.07f, 16, Stone, Rep, 1f); // band under the roof
            Map3DTownWallBuilder.Cone(roof, tc, TurretTop + 0.02f, TurretTop + 2.0f, TurretR + 0.22f, 16, Shingles, 0.8f); // steep: from above the cone must not read as a dome
            Box(k, tc + Vector3.up * (TurretTop + 2.07f), new Vector3(0.07f, 0.1f, 0.07f), 1f, Timber);             // gold finial
            Box(k, tc + Vector3.up * (TurretTop + 2.25f), new Vector3(0.11f, 0.1f, 0.11f), 1f, Timber);
            foreach (float z in new[] { -1f, 1f })                                                                  // golden jamb posts on the road face
                Box(k, new Vector3(rs * (HalfW + 0.08f), BlockH * 0.5f, z * (HalfD - 0.1f)), new Vector3(0.12f, BlockH * 0.5f, 0.15f), 0.85f, Timber);
            Box(k, new Vector3(rs * (HalfW + 0.06f), BlockH - 0.25f, 0f), new Vector3(0.1f, 0.12f, HalfD - 0.1f), 0.9f, Timber);
            foreach (float z in new[] { -1f, 1f })
            {
                Vector3 n = new Vector3(0f, 0f, z);
                Map3DGuildHallBuilder.Card(d, new Vector3(-rs * 0.35f, 2.15f, z * (HalfD + 0.03f)), new Vector3(-z * 0.58f, 0f, 0f), new Vector3(0f, 0.92f, 0f), Map3DGuildHallBuilder.BBanner, n);
                foreach (float wx in new[] { -0.32f, 0.32f })                                                       // turret windows
                {
                    Vector3 dir = new Vector3(Mathf.Sin(wx), 0f, z * Mathf.Cos(wx));                                // on the turret's front / back, 0.32 rad apart
                    Vector3 right = Vector3.Cross(Vector3.up, dir).normalized * 0.17f;
                    Map3DGuildHallBuilder.Card(d, tc + dir * (TurretR + 0.02f) + Vector3.up * 4.75f, right, new Vector3(0f, 0.27f, 0f), Map3DGuildHallBuilder.BWindow, dir);
                }
            }
            p.Stone = k.Build("Map3DGateTower");
            p.Roof = roof.Build("Map3DGateRoof");
            p.Detail = d.Build("Map3DGateTowerDetail");
            Map3DGuildHallBuilder.Move(p.Detail);
            if (roadEast) west = p; else east = p;
            return p;
        }

        /// <summary>Arch over a road gap `span` m wide: an outer timber beam (elliptic, springing at 3.2 m inside the tower faces, apex ~5.5 m),
        /// a thinner inner rim, 5 spokes between them and the crest card at the apex (front and back).</summary>
        public static Mesh Arch(float span, out Mesh detail)
        {
            if (arch != null && Mathf.Approximately(span, archSpan)) { detail = archDetail; return arch; }
            var k = new Map3DMeshKit();
            var d = new Map3DMeshKit();
            float rx = span * 0.5f + 0.15f, ry = 2.25f, spring = 3.2f;
            const int N = 12;
            for (int ring = 0; ring < 2; ring++)
            {
                float ex = ring == 0 ? rx : rx - 0.42f, ey = ring == 0 ? ry : ry - 0.42f, th = ring == 0 ? 0.13f : 0.07f, hz = ring == 0 ? 0.24f : 0.16f;
                for (int i = 0; i < N; i++)
                {
                    float t0 = Mathf.PI * i / N, t1 = Mathf.PI * (i + 1) / N;
                    Vector3 p0 = new Vector3(Mathf.Cos(t0) * ex, spring + Mathf.Sin(t0) * ey, 0f), p1 = new Vector3(Mathf.Cos(t1) * ex, spring + Mathf.Sin(t1) * ey, 0f);
                    Vector3 mid = (p0 + p1) * 0.5f, along = p1 - p0;
                    Quaternion q = Quaternion.AngleAxis(Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg, Vector3.forward);
                    Map3DGuildHallBuilder.Box(k, mid, new Vector3(along.magnitude * 0.5f + 0.03f, th, hz), q, Timber, 1.4f, 0.95f);
                    if (ring == 0 && i % 2 == 1) // gold plates at every other joint (a little wider, lighter)
                        Map3DGuildHallBuilder.Box(k, p1, new Vector3(0.1f, th + 0.04f, hz + 0.03f), q, Timber, 0.6f, 1f);
                }
            }
            for (int s = 1; s <= 5; s++) // spokes
            {
                float t = Mathf.PI * s / 6f;
                Vector3 o = new Vector3(Mathf.Cos(t) * rx, spring + Mathf.Sin(t) * ry, 0f), i = new Vector3(Mathf.Cos(t) * (rx - 0.42f), spring + Mathf.Sin(t) * (ry - 0.42f), 0f);
                Vector3 along = o - i;
                Map3DGuildHallBuilder.Box(k, (o + i) * 0.5f, new Vector3(along.magnitude * 0.5f, 0.06f, 0.14f), Quaternion.AngleAxis(Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg, Vector3.forward), Timber, 1.4f, 0.9f);
            }
            foreach (float z in new[] { -1f, 1f })
                Map3DGuildHallBuilder.Card(d, new Vector3(0f, spring + ry - 0.05f, z * 0.34f), new Vector3(-z * 0.5f, 0f, 0f), new Vector3(0f, 0.66f, 0f), Map3DGuildHallBuilder.BCrest, new Vector3(0f, 0f, z));
            arch = k.Build("Map3DGateArch");
            archDetail = d.Build("Map3DGateArchDetail");
            Map3DGuildHallBuilder.Move(archDetail);
            archSpan = span;
            detail = archDetail;
            return arch;
        }
    }
}
