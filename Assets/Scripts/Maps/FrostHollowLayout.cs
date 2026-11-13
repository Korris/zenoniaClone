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
        /// The south-west corner goes back to the village gate; World Map 2.0 step 1 (13/11/2026) re-aimed it at Thatchwick's east road (it used to go to Oakhaven, before Thatchwick sat between
        /// Oakhaven and Frost Hollow on the route). The mouth of the ice cave on the east cliff (three tiles, row 20) is the way on: World Map 2.1 (13/11/2026) re-aimed it from the Frostfang Den
        /// to the south road of Sunbreak Foothills (the Den is now a side dungeon behind Sunbreak's own ice cave). Sunbreak's way back arrives at (68,22), two tiles in front of the cave.
        /// The names still say "Den": the portal and the arrival tile did not move.
        /// </summary>
        const int DenPortalTx = 67, DenPortalTy = 20;
        /// <summary>Where the way back from Sunbreak Foothills lands (the Frostfang Den's until World Map 2.1): the snow in front of the ice cave, two tiles south of its portal row.</summary>
        public const int DenArrivalTx = 68, DenArrivalTy = 22; // consts: Sunbreak's portal table reads them while this class may still be initialising (see SunbreakFoothillsMap)
        public static readonly Vector2Int DenArrivalTile = new Vector2Int(DenArrivalTx, DenArrivalTy);

        static readonly MapPortal[] PortalList =
        {
            new MapPortal(2, 21, ThatchVillageMap.MapId, 33, 11),
            new MapPortal(DenPortalTx, DenPortalTy, SunbreakFoothillsMap.MapId, SunbreakFoothillsMap.SouthArrivalTx, SunbreakFoothillsMap.SouthArrivalTy),
            new MapPortal(DenPortalTx + 1, DenPortalTy, SunbreakFoothillsMap.MapId, SunbreakFoothillsMap.SouthArrivalTx, SunbreakFoothillsMap.SouthArrivalTy),
            new MapPortal(DenPortalTx + 2, DenPortalTy, SunbreakFoothillsMap.MapId, SunbreakFoothillsMap.SouthArrivalTx, SunbreakFoothillsMap.SouthArrivalTy),
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
