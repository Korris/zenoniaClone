using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Willowbrook Meadows: the big (72x48) low-level hunting field east of Thatchwick Village. The ground is a ChatGPT painting (outpainted from the first 36x24 field, which now fills cols 0-35, rows 12-35)
    /// cut into 2x2 chunks (Tools/build-willowbrook-large-painted.py); trees, rocks, ruins, reeds and the barricade are separate y-sorted props (WillowbrookMeadowsProps, WillowbrookMeadowsFrameProps,
    /// WillowbrookMeadowsScatterProps) so the hero walks behind canopies. Rows (Tools/build-willowbrook-large-rows.py): T border / HUD rows / old cliffs  w stream and pond  B bridge  g open ground;
    /// props block their own footprints. A dead end: the only portal pair is the west gate (rows 23-24), which World Map 2.0 step 1 (13/11/2026) re-aimed at Oakhaven's new south gate at
    /// (13,21)/(14,21) (agent C); arrivals land on (13,20)/(14,20), the walkable tile just north of that gate. The main road ends at the barricade on the east edge. MapSpawnTable keys on MapId (no boss).
    /// </summary>
    public sealed class WillowbrookMeadowsMap : TileMapBase
    {
        public const string MapId = "willowbrookMeadows";

        // Row 0-1 and 45-47 (under the HUD bar) are the border; cols 70-71 the east border; the old section keeps its hand-classified rows (+12).
        static readonly string[] Rows =
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TgggggggggggggggggggggggwwggggggggggggggggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggggggwwgggggggggggggggggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggggwwgggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggwwggggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggwwwgggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggwwggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggwgggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggwwggggggggggggggggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggggggwwgggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggwwggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggggwwgggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggggwwgggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggTgwwwggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggggwwgggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggwwggggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggwwggggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggTgwwwgggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggTTTwwwggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TTggggggggggggggggggggwwwgggggggggggggggggggggggggggggggggggggggggggggTT",
            "TTTggggggggggggggggggggwwwggggggggggggggggggggggggggggggggggggggggggggTT",
            "TTTggggggggggggggggggggBBBBBggggggggggggggggggggggggggggggggggggggggggTT",
            "gggggggggggggggggggggggggwwgggggggggggggggggggggggggggggggggggggggggggTT",
            "gTggggggggggggggggggggggggwwwgggggggggggggggggggggggggggggggggggggggggTT",
            "TTgggggggggggggggggggggTTTgwwwwgggggggggggggggggggggggggggggggggggggggTT",
            "TTgggggggggggggggggggggggggggwwgggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggwwwgggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggwwwwggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggwwwwggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggggwwwwggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggggggwwwwggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggggggggwwwgggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggggggggggwwwgggggggggggggggggggggggggggTT",
            "TggggggggggggggggggggggggggggggggggggggggwwwggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggggggggggggwwggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggggggggggggwwggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggggggggggwwwwggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggggggwwwwwwwwwgggggggggggggggggggggggggTT",
            "TggggggggggggggggggggggggggggggggwwwwwwwwwwwwwwgggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggwwwwwwwwwwwwwwwgggggggggggggggggggggggTT",
            "TggggggggggggggggggggggggggggggwwwwwwwwwwwwwwwwgggggggggggggggggggggggTT",
            "TggggggggggggggggggggggggggggggwwwwwwwwwwwwwwwwgggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggwwwwwwwwwwwwwgggggggggggggggggggggggggTT",
            "TggggggggggggggggggggggggggggggggwwwwwwwwwwgggggggggggggggggggggggggggTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        static readonly Vector2Int PlayerStartTile = new Vector2Int(3, 23);
        /// <summary>
        /// West gate pair, now to Oakhaven (World Map 2.0 step 1, 13/11/2026: Willowbrook tay &lt;-&gt; Oakhaven nam, replacing the old link to Thatchwick). New Oakhaven's south gate is
        /// (13,21)/(14,21) and sends players back to this map's gate tile (0,23)/(0,24); these arrive at (13,20)/(14,20), the open tile just north of that gate.
        /// </summary>
        static readonly MapPortal[] PortalList =
        {
            new MapPortal(0, 23, OakhavenVillageMap.MapId, 13, 20), new MapPortal(0, 24, OakhavenVillageMap.MapId, 14, 20),
            // South gate (World Map 2.1, 13/11/2026): where the trail east of the pond runs off the bottom edge, one row above the HUD rows 45-47. To Bramblewood Forest's top road;
            // Bramblewood sends heroes back to (47,42), two tiles up the same trail.
            new MapPortal(46, 44, BramblewoodForestMap.MapId, 17, 2), new MapPortal(47, 44, BramblewoodForestMap.MapId, 18, 2),
        };

        public WillowbrookMeadowsMap() : base(Rows, "Tw")
        {
            foreach (MapProp p in WillowbrookMeadowsProps.All)
            {
                PropDef d;
                if (VillagePropDefs.TryGet(p.Type, out d)) BlockTiles(p.Tx, p.Ty - d.FootH + 1, d.FootW, d.FootH);
                else Debug.LogError("[ZenoniaProto] unknown village prop " + p.Type);
            }
        }

        public override string Id { get { return MapId; } }
        public override string Name { get { return "Willowbrook Meadows"; } }
        public override string GroundTexture { get { return "Sprites/village/tiles/willowbrook-large-ground"; } }
        public override int GroundChunkCols { get { return 2; } }
        public override int GroundChunkRows { get { return 2; } }
        public override string Music { get { return "frost-hollow"; } } // reuses Frost Hollow's track
        public override bool Hostile { get { return true; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }
        public override MapPortal[] Portals { get { return PortalList; } }
        public override MapProp[] Props { get { return WillowbrookMeadowsProps.All; } }
    }
}
