using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Willowbrook Meadows: the big (72x48) low-level hunting field east of Thatchwick Village. The ground is a ChatGPT painting (outpainted from the first 36x24 field, which now fills cols 0-35, rows 12-35)
    /// cut into 2x2 chunks (Tools/build-willowbrook-large-painted.py); trees, rocks, ruins, reeds and the barricade are separate y-sorted props (WillowbrookMeadowsProps, WillowbrookMeadowsFrameProps,
    /// WillowbrookMeadowsScatterProps) so the hero walks behind canopies. Rows (Tools/build-willowbrook-large-rows.py): T border / HUD rows / old cliffs  w stream and pond  B bridge  g open ground;
    /// props block their own footprints. A dead end: the only portal pair is the west gate (rows 23-24) back to Thatchwick; the main road ends at the barricade on the east edge. MapSpawnTable keys on MapId (no boss).
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
            "TgggggggggggggggggggwwggggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggwwggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggwgggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggwwggggggggggggggggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggggggwwgggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggwwggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggggwggggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggwwwwwggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggTwwwwwgggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggwwwwggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggwwwwgggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggwwwwgggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggTwwwwwggggggggggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggTTwwwwwgggggggggggggggggggggggggggggggggggggggggggggTT",
            "TTgggggggggggggggggggwwwwwggggggggggggggggggggggggggggggggggggggggggggTT",
            "TTTgggggggggggggggggggwwwwggggggggggggggggggggggggggggggggggggggggggggTT",
            "TTTggggggggggggggggggggBBBBBggggggggggggggggggggggggggggggggggggggggggTT",
            "ggggggggggggggggggggggggwwwwggggggggggggggggggggggggggggggggggggggggggTT",
            "gTggggggggggggggggggggggwwwwwwggggggggggggggggggggggggggggggggggggggggTT",
            "TTgggggggggggggggggggggTTTwwwwwgggggggggggggggggggggggggggggggggggggggTT",
            "TTggggggggggggggggggggggggggwwwwggggggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggwwwwwgggggggggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggggggggggggwwwwwwgggggggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggggggggggggggwwwwwggggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggwwwwwgggggggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggggggwwwgggggggggggggggggggggggggggggggTT",
            "TggggggggggggggggggggggggggggggggggggggwwgggggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggggggggggwwwgggggggggggggggggggggggggggTT",
            "TggggggggggggggggggggggggggggggggggggggggwwgggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggggggggggggwwggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggggggggggggwwggggggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggggggggggwwwgggggggggggggggggggggggggggTT",
            "TggggggggggggggggggggggggggggggggggggwwwwwwwwgggggggggggggggggggggggggTT",
            "TggggggggggggggggggggggggggggggggggwwwwwwwwwwwwgggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggggwwwwwwwwwwwwwgggggggggggggggggggggggTT",
            "TggggggggggggggggggggggggggggggggwwwwwwwwwwwwwwgggggggggggggggggggggggTT",
            "TggggggggggggggggggggggggggggggggwwwwwwwwwwwwwwgggggggggggggggggggggggTT",
            "TgggggggggggggggggggggggggggggggggwwwwwwwwwwggggggggggggggggggggggggggTT",
            "TggggggggggggggggggggggggggggggggggwwwwwwwggggggggggggggggggggggggggggTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        static readonly Vector2Int PlayerStartTile = new Vector2Int(3, 23);
        /// <summary>West gate pair back to Thatchwick's east road. Arrivals stand two tiles in from the target map's portal.</summary>
        static readonly MapPortal[] PortalList =
        {
            new MapPortal(0, 23, ThatchVillageMap.MapId, 33, 11), new MapPortal(0, 24, ThatchVillageMap.MapId, 33, 12),
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
