using System;
using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// The way to another map through the portal graph (MapRegistry.All + PortalsFor), for the Wanted Boss guide arrow: which portal of the map the hero stands on
    /// leads, by the fewest map changes, towards the boss's map. Pure data, no state; on the boss's own map there is no portal to take (the caller points at the boss tile).
    /// A boss gate is per map and per hero, so the caller says which maps' gates are open for this hero (`gateOpen`, null = every gate shut).
    /// </summary>
    public static class WantedRoute
    {
        /// <summary>The next map on the shortest way from `fromMapId` to `toMapId`; null on the same map, for an unknown map, or when no portal chain leads there.</summary>
        public static string NextMap(string fromMapId, string toMapId, Func<string, bool> gateOpen = null)
        {
            if (fromMapId == toMapId || MapRegistry.Get(fromMapId) == null || MapRegistry.Get(toMapId) == null) return null;
            // Breadth first from the start; firstStep[map] = the neighbour of the start map the search left through to get there.
            var firstStep = new Dictionary<string, string> { { fromMapId, null } };
            var open = new Queue<string>();
            open.Enqueue(fromMapId);
            while (open.Count > 0)
            {
                string at = open.Dequeue();
                IGameMap map = MapRegistry.Get(at);
                if (map == null) continue; // a portal to an unregistered map: a dead end (ZoneHost and MapGraphTests report it)
                foreach (MapPortal p in map.PortalsFor(gateOpen != null && gateOpen(at)))
                {
                    if (firstStep.ContainsKey(p.TargetMapId)) continue;
                    string step = firstStep[at] ?? p.TargetMapId;
                    if (p.TargetMapId == toMapId) return step;
                    firstStep[p.TargetMapId] = step;
                    open.Enqueue(p.TargetMapId);
                }
            }
            return null;
        }

        /// <summary>
        /// The portal of `fromMapId` to walk into: one that leads to NextMap, and of those the tile closest to (nearTx, nearTy) (the hero's tile; a map can have
        /// several doors to the same neighbour, like the Den's two exits). Without a near tile: the first one in the map's list. Null when NextMap is null.
        /// </summary>
        public static MapPortal? NextPortal(string fromMapId, string toMapId, Func<string, bool> gateOpen = null, int nearTx = -1, int nearTy = -1)
        {
            string next = NextMap(fromMapId, toMapId, gateOpen);
            if (next == null) return null;
            MapPortal? best = null;
            int bestDist = int.MaxValue;
            foreach (MapPortal p in MapRegistry.Get(fromMapId).PortalsFor(gateOpen != null && gateOpen(fromMapId)))
            {
                if (p.TargetMapId != next) continue;
                if (nearTx < 0 || nearTy < 0) return p;
                int dx = p.Tx - nearTx, dy = p.Ty - nearTy, dist = dx * dx + dy * dy;
                if (dist < bestDist) { bestDist = dist; best = p; }
            }
            return best;
        }

        /// <summary>The phase file's simple form: one answer for every gate (today no map has a gate, MapGraphTests.MapsAnswerTheSameEitherWayNowThatNoMapHasAGate).</summary>
        public static MapPortal? NextPortal(string fromMapId, string toMapId, bool gateOpen) { return NextPortal(fromMapId, toMapId, id => gateOpen); }
    }
}
