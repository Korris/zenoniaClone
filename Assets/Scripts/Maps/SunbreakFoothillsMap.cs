using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Sunbreak Foothills (World Map 2.1): the dry highland between Frost Hollow and Sunspire Oasis, 48x32 tiles, Lv16-20 plus the map boss Ridgehorn Behemoth on the raised plateau in the north-east.
    /// The ground is four ChatGPT quadrants stitched by Tools/build-sunbreak-foothills-painted.py into 2x2 chunks of 1536x1024 (Sprites/village/tiles/sunbreak-foothills-ground-r{r}c{c}.png, 64 px per tile);
    /// pines, rocks and sandstone outcrops are separate y-sorted props (SunbreakFoothillsProps, defs in SunbreakFoothillsPropDefs). Rows come from Tools/build-sunbreak-foothills-rows.py plus the hand overrides
    /// in plans/261113-1010-world-map-new-maps/layout-sunbreak/sunbreak-foothills-rows-override.txt (pasted here by Tools/write-sunbreak-foothills-map-class.py):
    /// C cliff / rock ledge (solid)  w stream (solid)  B bridge deck  s stair tread  d dirt road or sand  g grass.
    /// Three ways out, all always open: the south road (row 28, the rows below are under the HUD) to Frost Hollow, the road off the east edge (rows 15-16) to Sunspire Oasis, the ice cave at the top
    /// (row 3, above the stairs) to the Frostfang Den side dungeon. The road painted off the west edge leads nowhere: col 0 is solid and two rock piles close it. MapSpawnTable keys on MapId.
    /// </summary>
    public sealed class SunbreakFoothillsMap : TileMapBase
    {
        public const string MapId = "sunbreakFoothills";

        static readonly string[] Rows =
        {
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCgggCggCCCCCCC",
            "CCCCwwCCCCCCCCCCCCCCCCCCCCCCCCCCCgggggggggggggCC",
            "CCCCCCCCCCgggggCCCCCCCCddCCCCggCggggggggggggggCC",
            "CCCCCwCCgggggggCCCCCCCCssCCgggCgggggggggggggggCC",
            "CCCCCwwCggggggggggCCCCCssCggggCgggggggggggggggCC",
            "CCCCCCCwCCgggggggggCCCgddgCCCgCCgggggggggggggCCC",
            "CCCCCCCCwCgggggggggggggdgCggggCCCCCgggggggCCCCCC",
            "CCCCCCCwCgggggggggggggdddgggggCCCCCCssCCCCCCCCCC",
            "CCCCCCCwCCgggggggggCggddggggggggCCggssCgggCCCCCC",
            "CCCCCCgCwwCCgggCCCCgdddgCggggggggCCgddggCggCCCCC",
            "CCCCggggggwwCgCgggddddCCgggggggggggCddgggCCggCCC",
            "CCCggggdgggwwCgddddCgggggggggggggCCgddggggCggdCC",
            "CCCggggggggCwwBBggggggggggggggggCgggddggdggCdCCC",
            "CCCCggggggggCBBCgggggdggggggggggggggddCgggggggCC",
            "CCCgCCCCCCddddCwCggggggggggggggggggddddddddddddd",
            "CCggddddddddddCwCgggggggggggggdddddddCdddddddddd",
            "CgdddCCCggCgddgCwCggggggggggdddggCCCCCCgggCCCCCC",
            "CddgCCggggggggddCwwwwCgggggddgCCCgggggggCCCCCCCC",
            "CCCCgggggggggggdddddggwwwwBBdgggggdggggggCCCCCCC",
            "CCggggggggggggggCddddggggBBCCCggggggggggggggCCCC",
            "CCCggggggggggggggCCCddddddCwwwCggggggggggggggCCC",
            "CCCggggggggggggggggCdddgggggCwCggggggggggggggCCC",
            "CCgggggggggggggggggCgdgCCCCCCwCggggggggggggggdCC",
            "CCgggdgggggggggggggCddgCggggCwwwCCggggggggggddCC",
            "CCgdggggggggggggCCCgddggggggggggwwwCggggggddCCCC",
            "CCCCgggdggggggggCgggdddgggggggggggwwCggggdCCCCCC",
            "CCCCCdgggggggCCCggggdddgggggggggggCwwCCgggCCCCCC",
            "CCCCCCCCgggggdggggggdddgggggggggggggwwwCCgCCCCCC",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
        };

        // Arrival tiles are consts on purpose: Frost Hollow, the Den and Sunspire aim their portals here and this map aims back at them. Two maps reading each other's static fields
        // in their portal tables is a static-initialiser cycle, and in the player (Mono) one side then reads (0,0): the first build sent heroes from here to Frost Hollow's tile (0,0).
        /// <summary>Where the ice cave of Frost Hollow sends a hero: on the south road, two tiles above the portal row. Also the start tile of the map.</summary>
        public const int SouthArrivalTx = 21, SouthArrivalTy = 26;
        /// <summary>Where the Frostfang Den sends a hero back: the dirt at the foot of the cave stairs.</summary>
        public const int CaveArrivalTx = 23, CaveArrivalTy = 6;
        /// <summary>Where the west gate of Sunspire Oasis sends a hero: on the east road, two tiles in from the edge (row 15; the gate's second tile lands on row 16).</summary>
        public const int EastArrivalTx = 45, EastArrivalTy = 15;
        /// <summary>Where the far door of the Frostfang Den (behind the Glacier Warden) lets a hero out: the fork of the south road, 19 tiles from the ice cave and 5+ from every monster rect.</summary>
        public const int DenExitArrivalTx = 21, DenExitArrivalTy = 22;
        public static readonly Vector2Int SouthArrivalTile = new Vector2Int(SouthArrivalTx, SouthArrivalTy), CaveArrivalTile = new Vector2Int(CaveArrivalTx, CaveArrivalTy), DenExitArrivalTile = new Vector2Int(DenExitArrivalTx, DenExitArrivalTy), EastArrivalTile = new Vector2Int(EastArrivalTx, EastArrivalTy);

        /// <summary>
        /// South road (three tiles wide) to the snow in front of the ice cave of Frost Hollow; the cave mouth (two tiles) to the entrance of the Frostfang Den; the east road (two tiles) to the west gate
        /// of Sunspire, landing where the Den's old exit used to arrive.
        /// </summary>
        static readonly MapPortal[] PortalList =
        {
            new MapPortal(20, 28, FrostHollowLayout.MapId, FrostHollowLayout.DenArrivalTx, FrostHollowLayout.DenArrivalTy),
            new MapPortal(21, 28, FrostHollowLayout.MapId, FrostHollowLayout.DenArrivalTx, FrostHollowLayout.DenArrivalTy),
            new MapPortal(22, 28, FrostHollowLayout.MapId, FrostHollowLayout.DenArrivalTx, FrostHollowLayout.DenArrivalTy),
            new MapPortal(23, 3, FrostfangDenMap.MapId, 2, 10), new MapPortal(24, 3, FrostfangDenMap.MapId, 2, 10),
            new MapPortal(47, 15, SunspireOasisMap.MapId, 2, 10), new MapPortal(47, 16, SunspireOasisMap.MapId, 2, 11),
        };

        public SunbreakFoothillsMap() : base(Rows, "Cw")
        {
            foreach (MapProp p in SunbreakFoothillsProps.All)
            {
                PropDef d;
                if (VillagePropDefs.TryGet(p.Type, out d)) BlockTiles(p.Tx, p.Ty - d.FootH + 1, d.FootW, d.FootH);
                else Debug.LogError("[ZenoniaProto] unknown village prop " + p.Type);
            }
        }

        public override string Id { get { return MapId; } }
        public override string Name { get { return "Sunbreak Foothills"; } }
        public override string GroundTexture { get { return "Sprites/village/tiles/sunbreak-foothills-ground"; } }
        public override int GroundChunkCols { get { return 2; } }
        public override int GroundChunkRows { get { return 2; } }
        public override string Music { get { return "frost-hollow"; } } // no track of its own yet
        public override bool Hostile { get { return true; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(SouthArrivalTx, SouthArrivalTy, 2f); } }
        public override MapPortal[] Portals { get { return PortalList; } }
        public override MapProp[] Props { get { return SunbreakFoothillsProps.All; } }
    }
}
