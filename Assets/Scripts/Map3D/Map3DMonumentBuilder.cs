using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The east garden monument of Willowbrook Village V2 (tauGardenMonument) as fixed 3D (user 25/11: the billboard turned with the camera and its
    /// painted flower bed swung off the ground). Hybrid like Map3DShrineGateBuilder: a round curb of dressed blocks (Map3DTownWallBuilder.Ring),
    /// a stone pillar (plinth, shaft, cap) in the town stone, the bed filled with the village flower bush sprite, and the painted details cut from the prop's own sprite
    /// (Map3DAssets.SpriteCard): the purple banner on the shaft's south / north faces, the gold fleur-de-lis finial as crossed cards.
    /// Sprite 240 x 272 px = 3.75 x 4.25 m (64 px / m). Units: metres, y up, x east, z north, origin on the ground at the bed centre.
    /// </summary>
    public static class Map3DMonumentBuilder
    {
        public struct Parts { public Mesh Stone, Cards, Flowers; }
        /// <summary>Flower clumps of the bed: the village's own flower bush sprite (clean edges; the monument sprite's beds carry painted curb stones).</summary>
        public const string FlowerSprite = "tauFlowerBushSmall";
        static Parts built;
        const int Stone = Map3DTownWallBuilder.Stone;
        const float W = 240f, H = 272f;
        public const float BedRadius = 1.6f;

        static void Box(Map3DMeshKit k, Vector3 c, Vector3 h, float ao) { Map3DGuildHallBuilder.Box(k, c, h, Stone, Map3DTownWallBuilder.Rep, ao); }

        /// <summary>Sprite sub-rect in pixels (x0, y0 from the top-left, x1, y1) as a uv rect (v up).</summary>
        static Vector4 Px(float x0, float y0, float x1, float y1) { return new Vector4(x0 / W, 1f - y1 / H, x1 / W, 1f - y0 / H); }

        /// <summary>Two crossed upright cards (x and z planes) centred at c: reads from every yaw.</summary>
        static void Crossed(Map3DMeshKit k, Vector3 c, float hw, float hh, float yaw, Vector4 uv)
        {
            for (int i = 0; i < 2; i++)
            {
                float a = (yaw + i * 90f) * Mathf.Deg2Rad;
                Vector3 right = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * hw, n = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                Map3DGuildHallBuilder.Card(k, c, right, new Vector3(0f, hh, 0f), uv, n);
            }
        }

        public static Parts Get()
        {
            if (built.Stone != null) return built;
            var s = new Map3DMeshKit();
            var c = new Map3DMeshKit();
            var fl = new Map3DMeshKit();
            // pillar: plinth, shaft (banner faces south / north), cap slab
            const float shaftHx = 0.42f, shaftHz = 0.36f, shaftTop = 2.55f;
            Box(s, new Vector3(0f, 0.18f, 0f), new Vector3(0.62f, 0.2f, 0.56f), 0.72f);
            Box(s, new Vector3(0f, 0.42f, 0f), new Vector3(0.52f, 0.06f, 0.46f), 0.85f);
            Box(s, new Vector3(0f, (0.48f + shaftTop) * 0.5f, 0f), new Vector3(shaftHx, (shaftTop - 0.48f) * 0.5f, shaftHz), 0.9f);
            Box(s, new Vector3(0f, shaftTop + 0.1f, 0f), new Vector3(0.56f, 0.1f, 0.5f), 1f);
            // painted banner (sprite x 95-145, y 72-190 = 0.78 x 1.84 m) on both faces of the shaft
            foreach (float z in new[] { -1f, 1f })
                Map3DGuildHallBuilder.Card(c, new Vector3(0f, 1.5f, z * (shaftHz + 0.02f)), new Vector3(-z * 0.39f, 0f, 0f), new Vector3(0f, 0.92f, 0f), Px(95, 72, 145, 190), new Vector3(0f, 0f, z));
            // gold fleur-de-lis finial (sprite x 100-140, y 4-50) crossed on the cap
            Crossed(c, new Vector3(0f, shaftTop + 0.2f + 0.36f, 0f), 0.31f, 0.36f, 0f, Px(100, 4, 140, 50));
            // flower clumps (user 25/11 "bông hoa xấu": upright crossed cards cut from the monument sprite showed its painted curb as pale sticks and read
            // edge-on from the 54 deg camera): the clean flower bush sprite on cards leaning INWARD 40 deg, so their face looks outward-up at the camera
            // round 2 (too small / sparse, paving showed in the bed, the oak leaf clumps read spiky): three overlapping rings of bigger cards, 60 deg, no leaves
            // round 3 (full but flat as a carpet): a 4th ring of big clumps standing steeper (35 deg) along the curb gives the bed its mound
            for (int ring = 0; ring < 4; ring++)
            {
                float tilt = (ring == 3 ? 35f : 60f) * Mathf.Deg2Rad;
                int n = ring == 0 ? 12 : ring == 1 ? 9 : ring == 2 ? 6 : 9;
                float r = ring == 0 ? 1.1f : ring == 1 ? 0.78f : ring == 2 ? 0.5f : 1.0f, hw = ring == 3 ? 0.62f : ring == 0 ? 0.62f : 0.55f;
                for (int i = 0; i < n; i++)
                {
                    float a = 2f * Mathf.PI * (i + ring * 0.37f) / n;
                    Vector3 o = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), t = new Vector3(-o.z, 0f, o.x);
                    Vector3 up = (Vector3.up * Mathf.Cos(tilt) - o * Mathf.Sin(tilt)) * hw * 0.55f, nrm = Vector3.up * Mathf.Sin(tilt) + o * Mathf.Cos(tilt);
                    Map3DGuildHallBuilder.Card(fl, o * r + Vector3.up * (ring == 3 ? 0.5f : 0.3f + ring * 0.1f), t * hw, up, new Vector4(0f, 0f, 1f, 1f), nrm);
                }
            }
            built.Stone = s.Build("Map3DMonument");
            built.Stone = Combine(built.Stone, Map3DTownWallBuilder.Ring(BedRadius, 16));
            built.Cards = c.Build("Map3DMonumentCards");
            Map3DGuildHallBuilder.Move(built.Cards);
            built.Flowers = fl.Build("Map3DMonumentFlowers");
            Map3DGuildHallBuilder.Move(built.Flowers);
            return built;
        }

        /// <summary>Pillar + curb ring in one stone mesh (both single-submesh town stone).</summary>
        static Mesh Combine(Mesh a, Mesh b)
        {
            var m = new Mesh { name = "Map3DMonumentStone" };
            m.CombineMeshes(new[] { new CombineInstance { mesh = a, transform = Matrix4x4.identity }, new CombineInstance { mesh = b, transform = Matrix4x4.identity } }, true, true);
            m.RecalculateBounds();
            return m;
        }
    }
}
