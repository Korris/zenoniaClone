using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>Which portal, if any, a pair of feet stands on. Offline asks with the current map's Portals (gated by the save), the server with PortalsFor(that hero's flag).</summary>
    public static class MapPortals
    {
        public static bool TryFind(MapPortal[] portals, float feetX, float feetY, out MapPortal found)
        {
            int tx, ty;
            PortalTile.OfFeet(feetX, feetY, TileMapBase.Tile, TileMapBase.H, out tx, out ty);
            for (int i = 0; i < portals.Length; i++)
                if (portals[i].Tx == tx && portals[i].Ty == ty) { found = portals[i]; return true; }
            found = default(MapPortal);
            return false;
        }
    }
}
