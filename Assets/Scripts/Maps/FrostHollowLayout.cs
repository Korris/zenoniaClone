using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Frost Hollow, 72x48 tiles of 16 units: the old 36x24 valley is the north-west quarter (same tile coordinates, so its zones, the King at (7,5), the start and the village gate did not move),
    /// the rest is a hunting field painted in the same style. The backdrop is four 2304x1536 chunks (Sprites/tiles/frost-hollow-large-ground-r{r}c{c}.png) from Tools/build-frost-hollow-large-painted.py,
    /// the rows come from Tools/build-frost-hollow-large-rows.py (plan 261105-1240-frost-hollow-large-map, layout/frost-hollow-72x48-rows-painted.txt).
    /// C cliff / rock / cave (solid)  T pines (solid, unused by the painting)  w river (solid; the frozen lake is walkable ice)  B bridge deck  s snow / stone ground.
    /// Rows are listed top-down like the web (row 0 = top of the map); world y points up, so the row of a point is TilesH-1-floor(y/16).
    /// The static FrostHollowMap keeps the Frost Hollow spawn data and forwards collision to whichever map is current.
    /// </summary>
    public sealed class FrostHollowLayout : TileMapBase
    {
        static readonly string[] Rows =
        {
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
            "CCCCCCCCCCCCsssCwwwCCCCCsCCwCCCCCsCCCCCssssCCCsssssssCCCCCCCCCCCCCCCCCCC",
            "CCCCCCCssCCsssCsswwwwCCssssssssCsssCsCCCCCsssssssssssssCCCCCCCCCCCCCCCCC",
            "CCCCCCssssssssssswwwwCsssssssCCCsssssssCCCCsssssssssssssCCCCCCCCCCCCCCCC",
            "CssssssssssssssssswwwCsssssssCsCssCCCsssCCCCCsssssssssCCCsssssCCCCCCCCCC",
            "CssssssssssssssssCCwwwCsssssssssssCCCCCCCCCCCCCCCBBCCCCsssssssssssssCCCC",
            "CssssssssssssssssCCwwwCCsssssssssssssssssssssssCCBBCCssssssssssssssssCCC",
            "CssssssssssssssCCCCwwCssssssssssssssssssssssssssCssCsssssssssssssssssCCC",
            "CssssssssssssCCCCCCwwwCsssssssssssssssssssssssssssssssssssssssssssssCCCC",
            "CsCCCssssCCCCCCCCCCwwwssssssssssssssssssssssssssssssssssssssssssssssCCCC",
            "CCCCCssCCCCssssCCCCwwCCCsssssssssssssssssssssssssssssssssssssssssssssCCC",
            "CCssssssCCsssssssCCwwCCsssCssssssssssssssssssssssssssssssssssssssssssCCC",
            "CsCssssssssssssssBBBBBssssCCCssCCsssssssssssssssssssssssssssssssssssssCC",
            "CssssssssssssCCssCCCCssssssCCssCCCCsssssssssssssssssssssssssssssssssCCCC",
            "CsssssssssssCCCCCCwwCCssssssssssCCCCssssssssssssssssssssssssssCssssCCCCC",
            "CsssssssssssCCCCCwwwwCssssssssssssCCCCCssssssssssssssssssssCCCCssssCCCCC",
            "CssssssssssssCCCCwwwwsssssssssssssssCCCCCCsssssssssssssCsCCCssssssCCCCCC",
            "CsssssssssssssCsCwwwCCssssssssssssssssCCCCCCCBBCssssCCCCCCCsssssssCCCCCC",
            "CssssssssssCCsssCwwwwCCCssssssssCCssssssssCCBBBCCCCCCCCssssCCsssCCCCsCCC",
            "CssssssssssssssCwwwwwCCsssssssCCCsssCssssssCBBCsCCCCssssssCCssssCCsssCCC",
            "CsssssssssssCCCCCwwwwCCCCCCCCCCCCCCCCCsssssssssssssssssssCCsssssCCssssCC",
            "CCssssssssCCCCssCwwwwwCCCCsCCCCsCCCssssssssssssssssssCCCCCCCsssssCssssCC",
            "CCssssssssCCCsssCCCwwwCCssssssssssssssssssssCBCsssssCCCsCssCCsssssssssCC",
            "CCCCCssCCCCsssCCCCCwwwwsssssssssCCCCsssCCCCCCBCCCCCCCCssssCCCCCCssssssCC",
            "CCCCCssCCCsssCsCsCCwwwwsssCssssCCCCssCCCsCCsssssssssssssssssssssssssCCCC",
            "CCsssssssssssssssCwwwwwCsCCCCssssCCsCCsssssssssssssssssssssssssssssssCCC",
            "CsssssssssssssssCCwwwwwCCCsssCCCssCsCsssssssssssssssssssssssssssssssssCC",
            "CsssssssssssssssCwwwwwwCCssssssCsssCCssssssssssssssssssssssssssssssCCCCC",
            "CsssssssssssssssBBBBBBBBssssssssssssCssssssssssssssssssssssssssssssssCCC",
            "CsssssssssssssssBBBBBBBBsssssssCssCCssssssssssssssssssssssssssssssssssCC",
            "CssssssssssssssssCwwwwwCssssssssCssssssssssssssssssssssssssssssssssssCCC",
            "CsssssssssssssssCCwwwwwCCssssssCCCsssssssssssssssssssssssssssssssssssCCC",
            "CsssssssssssssssssCwwwwwCCsssCCCCssCsssssssssssssssssssssssssssssssssCCC",
            "CsssssssssssssssCCCwwwwwwCCCCCCssCsssCsssssssssssssssssssssssssssssssCCC",
            "CCCCssssssssssCCCCCwwwwwwssssssssCCCCssssssssssssssssssssssssssssssssCCC",
            "CCssCCssssssCCsssCwwwwwwCCsssCCCCCsCCCsssssssssssssssssssssssssssssCCCCC",
            "CssssCCsssssssssCCwwwwwssCssCCCsssssssssssssssssssssssssCCsssssssssssCCC",
            "CCssssssssssssssCwwwwCssssssssssssssssssssssssssssssCCCCssCCCssssssssCCC",
            "CssssssssssssssswwwwCCsssssssssssssssssssssssssssBCCCsssssssCCssssssssCC",
            "CssssssssssssssssswwwwCsssssssssssssssssssCCssCCCBssssssssssssCsssssssCC",
            "CssssssssssssssssCwwwwwCssssssssssssssssCCssCCssssssssssssssCCssssssssCC",
            "CsssssssssssssssssswwwwwCsssssssssssssCCCsssssssssssssssssCCssssssssssCC",
            "CsssssssssssssssssCCwwwwCssssssssssssssssssssssssssssssCCsssssssssssssCC",
            "CsssssssssssssssCCCCwwwssssssssssssssssssssssssssssssssssCCCssssssssssCC",
            "CsssssssssssssCCwwwwwsssssssssssssCCssssssssssssssssssssssssssssssssssCC",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
        };

        /// <summary>
        /// The south-west corner goes back to the village gate (arrival north of the village's south portal); the mouth of the ice cave on the east cliff (three tiles, row 20) goes on to the
        /// Frostfang Den boss map, the way to Sunspire Oasis. The Den's way back arrives at (68,22), two tiles in front of the cave.
        /// </summary>
        const int DenPortalTx = 67, DenPortalTy = 20;
        /// <summary>Where the Frostfang Den's way back lands: the snow in front of the ice cave, two tiles south of its portal row.</summary>
        public static readonly Vector2Int DenArrivalTile = new Vector2Int(68, 22);

        static readonly MapPortal[] PortalList =
        {
            new MapPortal(2, 21, OakhavenVillageMap.MapId, 16, 19),
            new MapPortal(DenPortalTx, DenPortalTy, FrostfangDenMap.MapId, 2, 10),
            new MapPortal(DenPortalTx + 1, DenPortalTy, FrostfangDenMap.MapId, 2, 10),
            new MapPortal(DenPortalTx + 2, DenPortalTy, FrostfangDenMap.MapId, 2, 10),
        };

        public const string MapId = "frostHollow"; // the web / save id; the launch arg spells it frost-hollow
        public static readonly Vector2Int PlayerStartTile = new Vector2Int(4, 21);

        /// <summary>Rows of the map; SaveSerializer.Parse derives the Frost Hollow save migration from it (24 rows before the map grew; SaveVersion 3 and 48 rows ship together, FrostHollowLargeMapTests guards it).</summary>
        public static int RowCount { get { return Rows.Length; } }

        public FrostHollowLayout() : base(Rows, "TCw")
        {
            foreach (MapProp p in FrostHollowProps.All)
            {
                PropDef d;
                if (VillagePropDefs.TryGet(p.Type, out d)) BlockTiles(p.Tx, p.Ty - d.FootH + 1, d.FootW, d.FootH);
                else Debug.LogError("[ZenoniaProto] unknown village prop " + p.Type);
            }
        }

        public override string Id { get { return MapId; } }
        public override string Name { get { return "Frost Hollow"; } }
        public override string GroundTexture { get { return "Sprites/tiles/frost-hollow-large-ground"; } }
        public override int GroundChunkCols { get { return 2; } }
        public override int GroundChunkRows { get { return 2; } }
        public override string Music { get { return "frost-hollow"; } }
        public override bool Hostile { get { return true; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }
        public override MapPortal[] Portals { get { return PortalList; } }
        public override MapProp[] Props { get { return FrostHollowProps.All; } }
    }
}
