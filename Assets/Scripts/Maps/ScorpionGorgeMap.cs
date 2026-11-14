using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Scorpion Gorge (World Map 2.1, region 3): the red sandstone canyon east of Amber Dunes, 48x32 tiles, Lv26-30 plus the map boss Dune Devourer in the
    /// sand-swirl pit in the south-east. The ground is four ChatGPT quadrants stitched by Tools/build-scorpion-gorge-painted.py into 2x2 chunks of 1536x1024
    /// (Sprites/village/tiles/scorpion-gorge-ground-r{r}c{c}.png, 64 px per tile); cacti, rock piles and the sun stele are separate y-sorted props
    /// (ScorpionGorgeProps, defs in ScorpionGorgePropDefs). Rows come from Tools/build-scorpion-gorge-rows.py plus the hand overrides in
    /// plans/261113-1923-scorpion-gorge/layout-scorpion-gorge/scorpion-gorge-rows-override.txt (pasted here by Tools/write-scorpion-gorge-map-class.py):
    /// C canyon wall (solid)  w dry riverbed / pool (solid)  B bridge deck  g cracked earth, road, dune sand or the boss arena's sand swirl (walkable).
    /// One way out is open today: the west gate (col 0, rows 14-15) to Amber Dunes. The sandstone temple at the top (Sunspire Vault) and the gap in the
    /// east wall towards the wetland (Mirewatch) are painted in but have no destination map yet, so both are left solid / unconnected (see the install report).
    /// MapSpawnTable keys on MapId.
    /// </summary>
    public sealed class ScorpionGorgeMap : TileMapBase
    {
        public const string MapId = "scorpionGorge";

        static readonly string[] Rows =
        {
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
            "CgggggggwwwggggggggggCCCCCCCgggggCgggggggggggggC",
            "CgggggggwwwwggCggggggCCCCCCCgggggggggggggggggggC",
            "CggggCgggwwwwgggggggggCCCCCCgggggggggggggggggggC",
            "CgggggggggwwwwgggCgggggggggggggggggggggggggggggC",
            "CggggggggCgwwwwCgggggggggggggggggggggggggggggggC",
            "CggggggggggwwwwwggggCggggggggggCggCggggggggggggC",
            "CgggggggggggwwwwwCgggggggggCgggggggggggggggggggC",
            "CggggCgggggCCwwwwgggggggggggCggggggggggCgggggggC",
            "CCgggggCgCCgwwwwgggggggggggggggggggggggCgggggggC",
            "CgggggggggggwwwwgggggggCggggggggggggggggCggggggg",
            "CgggggggggCwwwwwgggggggggggCggggggggggggggCggwwg",
            "CgggCggggggwwwwwgggggggggggggggggggggggggggggggg",
            "ggggggggggggCBBgggggggCgggCCggggggggggggggggggwg",
            "ggggCggggggggBBgggggggggggggCggggggggggggggggggg",
            "CgggCggggggggwwwwgggggggggggggCggggggggggggggggg",
            "CCgggggggggCgwwwwgggggggggggggCgggggggggCggggggg",
            "CggggggggggggwwwwgggggggggggCCggCCCCgggggggggggC",
            "CggggggggggggwwwwwggggggggCggggggCCggggggggggggC",
            "CgggCCggggggggwwwwwgggCCCgggggggggCCgggggggggggC",
            "CggggggggggggggggCBBgggggggggggggggggggggggggggC",
            "CgggggggggggggggggBBggCggggggggggggggggggggggggC",
            "CggCgggggggggggwwwwgCggggggggggggggggggggggggggC",
            "CggggggggggggCgwwwwCgggggggggggggggggggggggggggC",
            "CgggggggggggggggwwwwgggggggggggggggggggggggggggC",
            "CggggCCggggggggwwwwwggggggggggggggggggggggggCgCC",
            "CgggggggggggCggCwwwwCggggggggggggggggggggggggggC",
            "CgggggggggggggggwwwgggCggggggggggggggggggggggggC",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
        };

        // Arrival tile is a const on purpose (see docs/map-design-rules.md): Amber Dunes aims its portal here and this map aims back at Amber Dunes. Two maps
        // reading each other's static fields in their portal tables is a static-initialiser cycle that reads (0,0) on one side in the Unity player.
        /// <summary>Where the Amber Dunes gate sends a hero: on the main road, three tiles in from the west edge. Also the start tile of the map.</summary>
        public const int WestArrivalTx = 3, WestArrivalTy = 14;
        public static readonly Vector2Int WestArrivalTile = new Vector2Int(WestArrivalTx, WestArrivalTy);
        /// <summary>Where the Emberfen Marsh gate sends a hero: on the east road end, three tiles in from the east edge.</summary>
        public const int EastArrivalTx = 44, EastArrivalTy = 15;

        /// <summary>West gate (two tiles wide) to Amber Dunes. East gap (two tiles wide): TEMPORARY gate to Emberfen Marsh until Mirewatch exists (13/11/2026,
        /// ChatGPT design call: Amber Dunes -> Scorpion Gorge -> Emberfen; later Scorpion Gorge -> Mirewatch -> Emberfen). The temple to Sunspire Vault has no map yet.</summary>
        static readonly MapPortal[] PortalList =
        {
            new MapPortal(0, 14, AmberDunesMap.MapId, AmberDunesMap.ScorpionGorgeArrivalTx, AmberDunesMap.ScorpionGorgeArrivalTy),
            new MapPortal(0, 15, AmberDunesMap.MapId, AmberDunesMap.ScorpionGorgeArrivalTx, AmberDunesMap.ScorpionGorgeArrivalTy),
            new MapPortal(47, 14, EmberfenMarshMap.MapId, EmberfenMarshMap.WestArrivalTx, EmberfenMarshMap.WestArrivalTy),
            new MapPortal(47, 15, EmberfenMarshMap.MapId, EmberfenMarshMap.WestArrivalTx, EmberfenMarshMap.WestArrivalTy),
        };

        public ScorpionGorgeMap() : base(Rows, "Cw")
        {
            foreach (MapProp p in ScorpionGorgeProps.All)
            {
                PropDef d;
                if (VillagePropDefs.TryGet(p.Type, out d)) BlockTiles(p.Tx, p.Ty - d.FootH + 1, d.FootW, d.FootH);
                else Debug.LogError("[ZenoniaProto] unknown village prop " + p.Type);
            }
        }

        public override string Id { get { return MapId; } }
        public override string Name { get { return "Scorpion Gorge"; } }
        public override string GroundTexture { get { return "Sprites/village/tiles/scorpion-gorge-ground"; } }
        public override int GroundChunkCols { get { return 2; } }
        public override int GroundChunkRows { get { return 2; } }
        public override string Music { get { return "frost-hollow"; } } // no track of its own yet
        public override bool Hostile { get { return true; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(WestArrivalTx, WestArrivalTy, 2f); } }
        public override MapPortal[] Portals { get { return PortalList; } }
        public override MapProp[] Props { get { return ScorpionGorgeProps.All; } }
    }
}
