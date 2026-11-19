using System;
using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>One exit as the light FX sees it: touching portal tiles with the same target merged, and the spot the light stands on.</summary>
    public struct PortalExit
    {
        public string Target;
        public int Tiles;                // portal tiles in the run
        public int Edge;                 // nearest map edge within EdgeTiles: 0 left, 1 right, 2 top, 3 bottom; -1 = inner exit (boss door)
        public float AnchorTx, AnchorTy; // where the light stands, in tiles (y down, fractional: tile centre = n + 0.5)
    }

    /// <summary>
    /// Engine-free placement of the portal light (MapPortalLightFx): the portal tiles and their triggers are never moved, only the picture is placed.
    /// An exit on a map edge gets its light InsetTiles inside the map (bottom exits clear the rows under the HUD), stepping back towards the portal while the
    /// spot is a solid tile, so it stands on the road leading out. An inner exit keeps its own centre. Breath is the slow, continuous brightness swing.
    /// </summary>
    public static class MapPortalExits
    {
        public const float EdgeTiles = 3f;       // exit centre at most this far from an edge counts as an edge exit (same rule as MapPortalHaze)
        public const float InsetTiles = 1.6f;    // light centre this far inside the map from an edge exit
        public const float HudInsetTiles = 3.6f; // bottom exits: the lowest 3 rows sit under the HUD (docs/map-design-rules.md rule 8)
        public const float BreathSeconds = 3.6f, BreathLow = 0.8f;

        public static List<PortalExit> Of(IGameMap map)
        {
            var list = new List<PortalExit>();
            var done = new HashSet<int>();
            MapPortal[] ps = map.Portals;
            for (int i = 0; i < ps.Length; i++)
            {
                if (done.Contains(i)) continue;
                List<int> run = Run(ps, i);
                float cx = 0f, cy = 0f;
                foreach (int k in run) { done.Add(k); cx += ps[k].Tx + 0.5f; cy += ps[k].Ty + 0.5f; }
                cx /= run.Count; cy /= run.Count;
                list.Add(Place(map, ps[i].TargetMapId, run.Count, cx, cy));
            }
            return list;
        }

        static List<int> Run(MapPortal[] ps, int first)
        {
            var run = new List<int> { first };
            for (int i = 0; i < run.Count; i++)
                for (int k = 0; k < ps.Length; k++)
                    if (ps[k].TargetMapId == ps[first].TargetMapId && !run.Contains(k) && Math.Abs(ps[k].Tx - ps[run[i]].Tx) + Math.Abs(ps[k].Ty - ps[run[i]].Ty) == 1) run.Add(k);
            return run;
        }

        static PortalExit Place(IGameMap map, string target, int tiles, float cx, float cy)
        {
            float[] d = { cx, map.Width - cx, cy, map.Height - cy };
            int edge = 0;
            for (int i = 1; i < 4; i++) if (d[i] < d[edge]) edge = i;
            var e = new PortalExit { Target = target, Tiles = tiles, Edge = d[edge] <= EdgeTiles ? edge : -1, AnchorTx = cx, AnchorTy = cy };
            if (e.Edge < 0) return e;
            float want = edge == 3 ? HudInsetTiles : InsetTiles;
            // the light's distance from the edge, walked back towards the portal (0.5 tile steps) while it lands on a solid tile
            for (float at = Math.Max(want, d[edge]); at >= d[edge]; at -= 0.5f)
            {
                float ax = edge <= 1 ? (edge == 0 ? at : map.Width - at) : cx;
                float ay = edge >= 2 ? (edge == 2 ? at : map.Height - at) : cy;
                if (!map.IsSolidTile((int)Math.Floor(ax), (int)Math.Floor(ay)) || at - 0.5f < d[edge])
                { e.AnchorTx = ax; e.AnchorTy = ay; break; }
            }
            return e;
        }

        /// <summary>Brightness multiplier at time t (s): a cosine between BreathLow and 1 over BreathSeconds. Continuous, no steps, no blink.</summary>
        public static float Breath(float t, float phase)
        {
            double k = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * (t / BreathSeconds + phase));
            return (float)(BreathLow + (1.0 - BreathLow) * k);
        }
    }
}
