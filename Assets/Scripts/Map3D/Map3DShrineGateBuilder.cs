using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Clover Meadows' two stone gates as fixed 3D (24/11, same fix as the village wall: the billboard sprites turned with the 360 deg camera):
    /// the Ancient Root Shrine entrance (tauShrineEntrance: two pillars with glowing rune panels, an elliptic voussoir arch, the tree keystone, twisting
    /// roots climbing both sides under leaf clumps) and the sealed Gemini gate (tauGeminiGateMarker: two short pillars, a stone double door with the
    /// cyan rune ring and crossed vines). Hybrid like the Guild Hall: stone blocks in the town stone (Map3DAssets.TownStone), roots in the tree bark +
    /// Taurus leaf cards, and the painted details (rune panels, keystone emblem, door face) as alpha-cut cards cut from the prop's own sprite
    /// (Map3DAssets.SpriteCard), so the look stays the ChatGPT design. Units: metres, y up, x east, z north, origin on the ground at the gate centre.
    /// </summary>
    public static class Map3DShrineGateBuilder
    {
        public struct Parts { public Mesh Stone, Roots, Cards; }
        static Parts shrine, gemini;
        const int Stone = Map3DTownWallBuilder.Stone;

        static void Box(Map3DMeshKit k, Vector3 c, Vector3 h, float ao) { Map3DGuildHallBuilder.Box(k, c, h, Stone, Map3DTownWallBuilder.Rep, ao); }
        static void Box(Map3DMeshKit k, Vector3 c, Vector3 h, Quaternion q, float ao) { Map3DGuildHallBuilder.Box(k, c, h, q, Stone, Map3DTownWallBuilder.Rep, ao); }

        /// <summary>Sprite sub-rect in pixels (x0, y0 from the top-left, x1, y1) of a w x h sprite as a uv rect (v up).</summary>
        static Vector4 Px(float x0, float y0, float x1, float y1, float w, float h) { return new Vector4(x0 / w, 1f - y1 / h, x1 / w, 1f - y0 / h); }

        /// <summary>Card on both faces (south / north) at x, centre height y, depth half hz of the block it sits on.</summary>
        static void Cards(Map3DMeshKit k, float x, float y, float hz, float hw, float hh, Vector4 uv)
        {
            foreach (float z in new[] { -1f, 1f })
                Map3DGuildHallBuilder.Card(k, new Vector3(x, y, z * (hz + 0.02f)), new Vector3(-z * hw, 0f, 0f), new Vector3(0f, hh, 0f), uv, new Vector3(0f, 0f, z));
        }

        /// <summary>Elliptic voussoir ring: centre height `spring`, inner half axes (rx, ry), ring thickness t, depth half hz, n blocks.</summary>
        static void Ring(Map3DMeshKit k, float spring, float rx, float ry, float t, float hz, int n)
        {
            for (int i = 0; i < n; i++)
            {
                float a0 = Mathf.PI * i / n, a1 = Mathf.PI * (i + 1) / n, am = (a0 + a1) * 0.5f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * (rx + t * 0.5f), spring + Mathf.Sin(a0) * (ry + t * 0.5f), 0f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * (rx + t * 0.5f), spring + Mathf.Sin(a1) * (ry + t * 0.5f), 0f);
                Vector3 along = p1 - p0;
                Box(k, (p0 + p1) * 0.5f, new Vector3(along.magnitude * 0.5f - 0.015f, t * 0.5f, hz), Quaternion.AngleAxis(Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg, Vector3.forward), 0.95f);
            }
        }

        /// <summary>Twisting root from the ground at `foot` up to `top`, bark tube with a little spiral; leaf clump cards round `top`.</summary>
        static void Root(Map3DMeshKit k, System.Random rng, Vector3 foot, Vector3 top, float r0, float twist, bool leaves, int leafN = 7, float leafSize = 0.45f)
        {
            const int N = 7;
            var path = new Vector3[N];
            var rad = new float[N];
            var col = new Color[N];
            for (int i = 0; i < N; i++)
            {
                float t = i / (N - 1f);
                Vector3 ctrl = new Vector3(foot.x, top.y * 0.8f, (foot.z + top.z) * 0.5f);   // rise beside the pillar, then bend over the arch shoulder
                Vector3 p = (1f - t) * (1f - t) * foot + 2f * (1f - t) * t * ctrl + t * t * top;
                p += new Vector3(Mathf.Sin(t * 7f + twist) * 0.12f, 0f, Mathf.Cos(t * 7f + twist) * 0.16f);
                path[i] = p;
                rad[i] = Mathf.Lerp(r0, r0 * 0.45f, Mathf.Sqrt(t));
                col[i] = new Color(Mathf.Lerp(0.8f, 1f, t), 0f, t < 0.3f ? 0.6f : 0.1f, 0f);
            }
            k.Tube(path, rad, 7, 0f, 0.25f, 2.2f, col, true);
            k.Tube(new[] { foot + new Vector3(0f, 0.15f, 0f), foot + new Vector3(Mathf.Sign(foot.x) * 0.4f, -0.05f, (float)(rng.NextDouble() - 0.5) * 0.8f) },
                new[] { r0 * 0.8f, r0 * 0.3f }, 6, 0f, 0.25f, 2.2f, new[] { col[0], col[0] }, true); // a flare of root into the ground
            if (!leaves) return;
            Leaves(k, rng, top, leafN, leafSize, 1.4f, 1.2f);
        }

        /// <summary>Leaf clump: `count` Taurus leaf cards round `at`, spread sx x sz0 m, card size `size` .. size + 0.3.</summary>
        internal static void Leaves(Map3DMeshKit k, System.Random rng, Vector3 at, int count, float size, float sx, float sz0)
        {
            Vector3 top = at;
            for (int c = 0; c < count; c++)
            {
                Vector3 o = top + new Vector3((float)(rng.NextDouble() - 0.5) * sx, (float)rng.NextDouble() * 0.6f, (float)(rng.NextDouble() - 0.5) * sz0);
                float a = (float)rng.NextDouble() * Mathf.PI, sz = size + (float)rng.NextDouble() * 0.3f;
                Vector3 right = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * sz, up = new Vector3(0f, sz * 0.8f, 0f) + new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) * sz * 0.3f;
                Vector3 n = (Vector3.up + new Vector3(o.x - top.x, 0f, o.z - top.z)).normalized;
                var nn = new[] { n, n, n, n };
                float var = (float)rng.NextDouble(), tint = (float)rng.NextDouble();
                var cc = new[] { new Color(0.8f, 0.3f, var, tint), new Color(0.8f, 0.3f, var, tint), new Color(1f, 0.6f, var, tint), new Color(1f, 0.6f, var, tint) };
                k.Card(o, right, up, Map3DMeshKit.Cell(rng.Next(16)), nn, cc);
            }
        }

        /// <summary>Three crossed cards (0 / 60 / 120 deg) of a whole sprite, w x h m, foot on the ground: organic props that have no 3D build yet
        /// (tauBrambleWallMedium) so they stop turning with the camera, like the reeds (Map3DShedBuilder.Reeds).</summary>
        public static Mesh CrossedCards(float w, float h)
        {
            var k = new Map3DMeshKit();
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI / 3f;
                Vector3 right = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * w * (i == 0 ? 0.5f : 0.36f), up = Vector3.up * h * 0.5f;
                Map3DGuildHallBuilder.Card(k, up, right, up, new Vector4(0f, 0f, 1f, 1f), new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)));
            }
            Mesh m = k.Build("Map3DCrossedCards");
            Map3DGuildHallBuilder.Move(m);
            return m;
        }

        /// <summary>Root that hugs the arch: from the capital up along the outside of the voussoir ring to near the keystone, on face z.</summary>
        static void ArchRoot(Map3DMeshKit k, System.Random rng, float side, float z, float spring)
        {
            const int N = 7;
            var path = new Vector3[N];
            var rad = new float[N];
            var col = new Color[N];
            for (int i = 0; i < N; i++)
            {
                float t = i / (N - 1f), a = Mathf.Lerp(0.05f, 1.2f, t); // angle from the side up toward the keystone
                path[i] = new Vector3(side * Mathf.Cos(a) * 3.05f, spring + Mathf.Sin(a) * 2.45f, z + Mathf.Sin(t * 9f + side) * 0.06f);
                rad[i] = Mathf.Lerp(0.2f, 0.08f, t);
                col[i] = new Color(0.9f + 0.1f * t, 0f, 0.15f, 0f);
            }
            k.Tube(path, rad, 6, 0f, 0.25f, 2.2f, col, true);
            Leaves(k, rng, path[N / 2] + Vector3.up * 0.2f, 4, 0.45f, 0.8f, 0.4f);
        }

        /// <summary>Shrine entrance over a 4 m road (pillars on the solid tiles either side), arch apex ~5.3 m like the sprite.</summary>
        public static Parts Shrine()
        {
            if (shrine.Stone != null) return shrine;
            var s = new Map3DMeshKit();
            var r = new Map3DMeshKit();
            var c = new Map3DMeshKit();
            var rng = new System.Random(11);
            const float W = 656f, H = 336f, px = 2.75f, hz = 0.6f, spring = 3.0f;
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side * px;
                Box(s, new Vector3(x, -0.2f, 0f), new Vector3(0.9f, 0.45f, hz + 0.18f), 0.72f);        // plinth
                Box(s, new Vector3(x, 1.4f, 0f), new Vector3(0.62f, 1.25f, hz), 0.85f);                 // shaft
                Box(s, new Vector3(x, 2.78f, 0f), new Vector3(0.9f, 0.16f, hz + 0.14f), 1f);            // capital slab
                Cards(c, x, 1.5f, hz, 0.36f, 0.78f, Px(168, 178, 207, 268, W, H));     // glowing rune panel
                // ChatGPT round 1 (lost the golden roots hugging the arch and the leaf crowns, thin at 90 / 270): thick roots in front of AND behind
                // each pillar, one root hugging the arch on each face, big leaf crowns deep in z
                Root(r, rng, new Vector3(x + side * 0.95f, 0f, -0.45f), new Vector3(x - side * 1.1f, 4.9f, -0.35f), 0.44f, side, true, 12, 0.6f);
                Root(r, rng, new Vector3(x + side * 1.05f, 0f, 0.55f), new Vector3(side * 0.7f, 5.2f, 0.4f), 0.38f, side * 2.3f, true, 12, 0.6f);
                Root(r, rng, new Vector3(x + side * 0.7f, 0f, -0.6f), new Vector3(x + side * 0.4f, 3.3f, -0.55f), 0.24f, side * 4f, true, 6, 0.5f);
                Root(r, rng, new Vector3(x + side * 0.6f, 0f, 0.9f), new Vector3(x + side * 0.3f, 3.0f, 0.75f), 0.22f, side * 5f, false);
                for (int zf = -1; zf <= 1; zf += 2) ArchRoot(r, rng, side, zf * (hz + 0.12f), spring);
                Leaves(r, rng, new Vector3(x + side * 0.6f, 3.0f, 0f), 10, 0.55f, 2.0f, 2.0f);
            }
            Ring(s, spring, 2.15f, 1.55f, 0.75f, hz - 0.05f, 9);
            Box(s, new Vector3(0f, spring + 1.55f + 0.4f, 0f), new Vector3(0.6f, 0.62f, hz + 0.08f), 1f); // keystone block
            Cards(c, 0f, spring + 1.55f + 0.45f, hz + 0.08f, 0.6f, 0.68f, Px(278, 8, 390, 138, W, H));     // tree emblem
            shrine.Stone = s.Build("Map3DShrineGate");
            shrine.Roots = r.Build("Map3DShrineGateRoots");
            shrine.Cards = c.Build("Map3DShrineGateCards");
            Map3DGuildHallBuilder.Move(shrine.Cards);
            return shrine;
        }

        /// <summary>Sealed Gemini gate on its 4 x 2 solid block: pillars, stone double door with the painted rune ring / vines, leaf tufts.</summary>
        public static Parts Gemini()
        {
            if (gemini.Stone != null) return gemini;
            var s = new Map3DMeshKit();
            var r = new Map3DMeshKit();
            var c = new Map3DMeshKit();
            var rng = new System.Random(23);
            const float W = 304f, H = 176f;
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side * 1.6f;
                Box(s, new Vector3(x, -0.15f, 0f), new Vector3(0.5f, 0.4f, 0.5f), 0.72f);
                Box(s, new Vector3(x, 1.0f, 0f), new Vector3(0.32f, 0.85f, 0.32f), 0.85f);
                Box(s, new Vector3(x, 1.95f, 0f), new Vector3(0.42f, 0.13f, 0.42f), 1f);
                Cards(c, x, 1.15f, 0.32f, 0.16f, 0.27f, Px(44, 80, 72, 120, W, H));
                Root(r, rng, new Vector3(x + side * 0.3f, 0f, -0.3f), new Vector3(x - side * 0.1f, 2.2f, -0.2f), 0.09f, side, true);
            }
            Box(s, new Vector3(0f, 1.15f, 0f), new Vector3(1.15f, 1.15f, 0.2f), 0.85f);                   // door slab
            Cards(c, 0f, 1.17f, 0.2f, 1.15f, 1.13f, Px(84, 24, 226, 166, W, H));                           // painted door face
            gemini.Stone = s.Build("Map3DGeminiGate");
            gemini.Roots = r.Build("Map3DGeminiGateRoots");
            gemini.Cards = c.Build("Map3DGeminiGateCards");
            Map3DGuildHallBuilder.Move(gemini.Cards);
            return gemini;
        }
    }
}
