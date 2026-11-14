using System;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// Which portal, if any, a pair of feet stands on. Offline asks with the current map's Portals (gated by the save), the server with PortalsFor(that hero's flag).
    /// The feet's own tile wins; otherwise a portal tile within ReachUnits of the feet sample point counts too (user, 13/11/2026: walking into an exit often did
    /// nothing, because portal tiles sit on the map edge and the hero's box stops before its feet sample reaches them). Same rule offline and on the server.
    /// </summary>
    public static class MapPortals
    {
        /// <summary>How far outside a portal tile the feet may be and still trigger it (world units; a tile is 16).</summary>
        public const float ReachUnits = 12f;

        public static bool TryFind(MapPortal[] portals, float feetX, float feetY, out MapPortal found)
        {
            int tx, ty;
            PortalTile.OfFeet(feetX, feetY, TileMapBase.Tile, GameConfig.TilesH, out tx, out ty);
            for (int i = 0; i < portals.Length; i++)
                if (portals[i].Tx == tx && portals[i].Ty == ty) { found = portals[i]; return true; }
            float sx = feetX, sy = feetY + PortalTile.FeetSampleUp, best = float.MaxValue;
            int pick = -1;
            for (int i = 0; i < portals.Length; i++)
            {
                // portal tile as a y-up box: x [Tx, Tx+1) tiles, y from the row's bottom edge
                float x0 = portals[i].Tx * TileMapBase.Tile, y0 = (GameConfig.TilesH - 1 - portals[i].Ty) * TileMapBase.Tile;
                float dx = Math.Max(0f, Math.Max(x0 - sx, sx - (x0 + TileMapBase.Tile)));
                float dy = Math.Max(0f, Math.Max(y0 - sy, sy - (y0 + TileMapBase.Tile)));
                float d = Math.Max(dx, dy);
                if (d <= ReachUnits && d < best) { best = d; pick = i; }
            }
            found = pick >= 0 ? portals[pick] : default(MapPortal);
            return pick >= 0;
        }
    }
}
