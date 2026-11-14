using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Map exit marker, option "C" of the animated mock-ups (user, 13/11/2026; Tools/mock-portal-options-gif.py): the road fades into bright haze towards
    /// the map edge, like light at the end of the way, with soft wisps drifting and motes rising. An exit within EdgeTiles of an edge hazes towards that
    /// edge; an exit further inside (a boss-room door in a cave) has no edge to fade into and keeps the light column of MapPortalBeam.
    /// The haze is drawn for a "down" exit and turned for the other edges. Procedural, continuous motion, nothing blinks. World units: 16 per tile.
    /// </summary>
    public static class MapPortalHaze
    {
        const float EdgeTiles = 3f;                      // exit centre at most this far from an edge -> haze
        const float WidthUnits = 85f;                    // across the road (mock-up 340 px)
        const float RampUnits = 36f, InsetUnits = 40f;   // haze starts 2.5 tiles inside the exit centre (user: the effect sat off-screen at the edge) and grows over RampUnits
        const float HazeAlpha = 1f;
        const float HudExtraUnits = 32f;   // bottom exits: the lowest rows sit under the HUD, so the haze starts two tiles higher
        static readonly Color Light = new Color(1f, 1f, 0.97f);
        const int Motes = 10;
        static Sprite wispA, wispB;

        public static void Build(Transform parent, IGameMap map)
        {
            var done = new HashSet<MapPortal>();
            foreach (MapPortal p in map.Portals)
            {
                if (done.Contains(p)) continue;
                List<MapPortal> run = MapPortalBeam.Run(map, p);
                float cx = 0f, cy = 0f;
                foreach (MapPortal q in run) { done.Add(q); cx += q.Tx + 0.5f; cy += q.Ty + 0.5f; }
                cx /= run.Count; cy /= run.Count;   // tiles, y down
                // distance to each edge, in tiles: left, right, top, bottom
                float[] d = { cx, map.Width - cx, cy, map.Height - cy };
                int edge = 0;
                for (int i = 1; i < 4; i++) if (d[i] < d[edge]) edge = i;
                Vector3 centre = new Vector3(cx * GameConfig.TileUnits, GameConfig.MapH - cy * GameConfig.TileUnits, 0f);
                if (d[edge] > EdgeTiles)
                {
                    float bx = Mathf.Clamp(cx, 1f, map.Width - 1f), by = Mathf.Clamp(cy, 1f, map.Height - 1f);
                    MapPortalBeam.BuildBeam(parent, new Vector3(bx * GameConfig.TileUnits, GameConfig.MapH - by * GameConfig.TileUnits, 0f), p.TargetMapId);
                    continue;
                }
                BuildHaze(parent, centre, edge, d[edge] * GameConfig.TileUnits, p.TargetMapId);
            }
        }

        /// <summary>Haze from InsetUnits inside the exit centre out to (and a little past) the edge. edge: 0 left, 1 right, 2 top, 3 bottom.</summary>
        static void BuildHaze(Transform parent, Vector3 centre, int edge, float toEdge, string target)
        {
            float inset = InsetUnits + (edge == 3 ? HudExtraUnits : 0f);
            float length = toEdge + inset + 8f;
            // the "down" frame: +x across, -y towards the edge; rotate it to the real edge
            float angle = edge == 3 ? 0f : edge == 2 ? 180f : edge == 0 ? -90f : 90f;
            var go = new GameObject("PortalHaze_" + target);
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            int order = GameConfig.OrderBase(-1000f);   // over everything on the map: whoever walks into the exit fades into the light
            Vector3 mid = new Vector3(0f, inset - length * 0.5f, 0f);
            var body = Layer(go.transform, "Haze", Haze(length), mid, length, order, HazeAlpha);
            Layer(go.transform, "Core", Core(length), mid, length, order + 1, 1f).color = new Color(1f, 0.92f, 0.72f, 1f);   // warm bright core so it reads on pale roads too
            var anim = go.AddComponent<Motion>();
            anim.WispA = Layer(go.transform, "WispA", Wisp(ref wispA, 1), mid, length, order + 1, 0f);
            anim.WispB = Layer(go.transform, "WispB", Wisp(ref wispB, 2), mid, length, order + 1, 0f);
            anim.Mid = mid; anim.Length = length;
            anim.Motes = new SpriteRenderer[Motes]; anim.Phase = new float[Motes]; anim.OffsetX = new float[Motes];
            var rng = new System.Random(target.GetHashCode());
            for (int i = 0; i < Motes; i++)
            {
                SpriteRenderer m = ProceduralSprites.Child(go.transform, "Mote", MapPortalBeam.Mote());
                m.transform.localScale = new Vector3(3f, 3f, 1f);
                m.sortingOrder = order + 2;
                m.color = new Color(1f, 1f, 0.85f, 0f);
                anim.Motes[i] = m; anim.Phase[i] = (float)rng.NextDouble(); anim.OffsetX[i] = ((float)rng.NextDouble() - 0.5f) * WidthUnits * 0.65f;
            }
            body.color = new Color(Light.r, Light.g, Light.b, HazeAlpha);
        }

        static SpriteRenderer Layer(Transform parent, string name, Sprite s, Vector3 pos, float length, int order, float alpha)
        {
            SpriteRenderer sr = ProceduralSprites.Child(parent, name, s);
            sr.transform.localPosition = pos;
            sr.transform.localScale = new Vector3(WidthUnits, length, 1f);
            sr.sortingOrder = order;
            sr.color = new Color(Light.r, Light.g, Light.b, alpha);
            return sr;
        }

        /// <summary>Across: brightest on the road's middle (falloff^1.2). Along (v = 1 inner end, v = 0 at the edge): 0 at the inner end, full after RampUnits.</summary>
        static Sprite Haze(float length)   // one per exit: the ramp depends on the exit's length
        {
            float ramp = RampUnits / length;
            return MapPortalBeam.Make(64, 64, (u, v) => Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(u - 0.5f) * 2f), 0.8f) * Mathf.Clamp01((1f - v) / Mathf.Max(ramp, 0.05f)));
        }

        /// <summary>Narrow bright core along the road's middle, same ramp as the haze: keeps the exit readable on pale dirt and sand.</summary>
        static Sprite Core(float length)
        {
            float ramp = RampUnits / length;
            return MapPortalBeam.Make(64, 64, (u, v) => 0.55f * Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(u - 0.5f) * 3.2f), 2f) * Mathf.Clamp01((1f - v) / Mathf.Max(ramp, 0.05f)));
        }

        /// <summary>Soft blotchy wisp layer (sum of sines), masked like the haze; two seeds cross-fade so the haze seems to drift.</summary>
        static Sprite Wisp(ref Sprite cache, int seed)
        {
            if (cache != null) return cache;
            float s = seed * 1.7f;
            return cache = MapPortalBeam.Make(64, 64, (u, v) =>
            {
                float n = 0.5f + 0.25f * Mathf.Sin(u * 9f + s) * Mathf.Sin(v * 7f - s) + 0.25f * Mathf.Sin(u * 17f - s * 2f + v * 5f);
                return Mathf.Clamp01(n) * Mathf.Clamp01(1f - Mathf.Abs(u - 0.5f) * 2f) * Mathf.Clamp01((1f - v) * 2.5f) * 0.5f;
            });
        }

        /// <summary>Wisps cross-fade and slide slowly sideways; motes rise from the edge side towards the inner end and fade in and out.</summary>
        sealed class Motion : MonoBehaviour
        {
            public SpriteRenderer WispA, WispB;
            public SpriteRenderer[] Motes;
            public float[] Phase, OffsetX;
            public Vector3 Mid;
            public float Length;
            void Update()
            {
                float t = Time.time;
                float k = 0.5f + 0.5f * Mathf.Sin(t * 0.8f);
                Fade(WispA, k); Fade(WispB, 1f - k);
                WispA.transform.localPosition = Mid + new Vector3(Mathf.Sin(t * 0.5f) * 4f, 0f, 0f);
                WispB.transform.localPosition = Mid + new Vector3(Mathf.Sin(t * 0.5f + 2f) * -4f, 0f, 0f);
                for (int i = 0; i < Motes.Length; i++)
                {
                    float ph = (t / 2f + Phase[i]) % 1f;
                    float along = Mid.y - Length * 0.3f + ph * 28f;   // starts in the bright part, drifts inwards
                    Motes[i].transform.localPosition = new Vector3(OffsetX[i] + Mathf.Sin((ph + i) * 6f) * 1.5f, along, 0f);
                    Fade(Motes[i], Mathf.Sin(ph * Mathf.PI));
                }
            }
            static void Fade(SpriteRenderer r, float a) { Color c = r.color; c.a = a; r.color = c; }
        }
    }
}
