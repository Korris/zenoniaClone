using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Willowbrook Meadows: a low-level hunting field east of Thatchwick Village. A ground-only painted backdrop (Sprites/village/tiles/willowbrook-meadows-ground.png) with the trees, rocks, shed,
    /// haystack, log, fences and palisade as separate y-sorted props (like Oakhaven), so the hero walks behind canopies. Rows only mark T border / HUD / w stream (solid)  g open ground  B bridge;
    /// props block their footprints (VillagePropDefs). A dead end: the only portal pair is the west palisade gate (rows 11-12) back to Thatchwick. MapSpawnTable keys on MapId (no boss).
    /// </summary>
    public sealed class WillowbrookMeadowsMap : TileMapBase
    {
        public const string MapId = "willowbrookMeadows";

        static readonly string[] Rows =
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TggggggggggggggggggwwwwwgggggggggggT",
            "TggggggggggggggggggTwwwwwggggggggggT",
            "TgggggggggggggggggggwwwwgggggggggggT",
            "TggggggggggggggggggwwwwggggggggggggT",
            "TggggggggggggggggggwwwwggggggggggggT",
            "TgggggggggggggggggTwwwwwgggggggggggT",
            "TgggggggggggggggggTTwwwwwggggggggggT",
            "TTgggggggggggggggggggwwwwwgggggggggT",
            "TTTgggggggggggggggggggwwwwgggggggggT",
            "TTTggggggggggggggggggggBBBBBgggggggT",
            "ggggggggggggggggggggggggwwwwgggggggT",
            "gTggggggggggggggggggggggwwwwwwgggggT",
            "TTgggggggggggggggggggggTTTwwwwwggggT",
            "TTggggggggggggggggggggggggggwwwwgggT",
            "TgggggggggggggggggggggggggggwwwwwggT",
            "TggggggggggggggggggggggggggggwwwwwwT",
            "TggggggggggggggggggggggggggggggwwwwT",
            "TgggggggggggggggggggggggggggggggwwwT",
            "TggggggggggggggggggggggggggggggggggT",
            "TggggggggggggggggggggggggggggggggggT",
            // Rows 21-23: under the HUD bar, so blocked. Extra T: palisade posts (x1-2, rows 8-14, row 11 stays open as the road), the big cliffs (the props only block their base row).
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        // [type, tx, ty]: bottom-left footprint tile (MapScenery y-sorts by it). Cut from the painting, so each sprite lands on its original pixels; trees block their trunk tile only.
        static readonly MapProp[] PropList =
        {
            new MapProp("wbOakNW", 1, 6), new MapProp("wbCliffNW", 5, 2), new MapProp("wbRockNW", 8, 2),
            new MapProp("wbRockNW2", 4, 3), new MapProp("wbOakTop2", 11, 2), new MapProp("wbOakClumpW", 13, 5),
            new MapProp("wbOakBig", 16, 6), new MapProp("wbOakTopMid", 18, 2), new MapProp("wbRockTop", 13, 5),
            new MapProp("wbCliffStreamN", 19, 3), new MapProp("wbCliffMid", 18, 8), new MapProp("wbRocksStreamE", 23, 4),
            new MapProp("wbOakTopRight", 24, 3), new MapProp("wbOakTopFar", 29, 1), new MapProp("wbRockTopEdge", 31, 1),
            new MapProp("wbRockNE", 26, 2), new MapProp("wbOakEastA", 34, 5), new MapProp("wbOakEastB", 35, 9),
            new MapProp("wbRockEast", 33, 9), new MapProp("wbOakEastMid", 35, 16), new MapProp("wbOakWest", 3, 10),
            new MapProp("wbRockWest", 10, 10), new MapProp("wbSignpost", 14, 10), new MapProp("wbOakMid", 17, 10),
            new MapProp("wbRockMid", 19, 10), new MapProp("wbRocksStreamS", 23, 14), new MapProp("wbRockStream1", 28, 12),
            new MapProp("wbRockStream2", 30, 14), new MapProp("wbRockStream3", 30, 17), new MapProp("wbPalisade", 1, 14),
            new MapProp("wbOakSWClump", 3, 19), new MapProp("wbOakSWTree", 9, 17), new MapProp("wbOakSWLow", 1, 21),
            new MapProp("wbRockSW", 8, 19), new MapProp("wbRockCliffSW", 2, 23), new MapProp("wbOakSouth", 12, 22),
            new MapProp("wbFenceW", 6, 22), new MapProp("wbFenceE", 14, 22), new MapProp("wbRockLog", 18, 22),
            new MapProp("wbRockSouth", 16, 23), new MapProp("wbLog", 19, 22), new MapProp("wbFenceHay", 20, 21),
            new MapProp("wbHaystack", 23, 21), new MapProp("wbShed", 26, 21), new MapProp("wbOakSE", 32, 23),
        };

        static readonly Vector2Int PlayerStartTile = new Vector2Int(3, 11);
        /// <summary>West gate pair back to Thatchwick's east road. Arrivals stand two tiles in from the target map's portal.</summary>
        static readonly MapPortal[] PortalList =
        {
            new MapPortal(0, 11, ThatchVillageMap.MapId, 33, 11), new MapPortal(0, 12, ThatchVillageMap.MapId, 33, 12),
        };

        public WillowbrookMeadowsMap() : base(Rows, "Tw")
        {
            foreach (MapProp p in PropList)
            {
                PropDef d;
                if (VillagePropDefs.TryGet(p.Type, out d)) BlockTiles(p.Tx, p.Ty - d.FootH + 1, d.FootW, d.FootH);
                else Debug.LogError("[ZenoniaProto] unknown village prop " + p.Type);
            }
        }

        public override string Id { get { return MapId; } }
        public override string Name { get { return "Willowbrook Meadows"; } }
        public override string GroundTexture { get { return "Sprites/village/tiles/willowbrook-meadows-ground"; } }
        public override string Music { get { return "frost-hollow"; } } // reuses Frost Hollow's track
        public override bool Hostile { get { return true; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }
        public override MapPortal[] Portals { get { return PortalList; } }
        public override MapProp[] Props { get { return PropList; } }
    }
}
