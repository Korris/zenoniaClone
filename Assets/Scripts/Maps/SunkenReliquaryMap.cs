using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Sunken Reliquary: the flooded ruins past Emberfen Marsh, 58x24, the painted backdrop in 2 chunks (Sprites/village/tiles/sunken-reliquary-ground-r0c{0,1}.png, 29 tiles each)
    /// with the HD props y-sorted on top. The rows only drive collision: T ruin wall and flood (solid)  d stone causeway  p temple flagstones. The causeway enters at the west edge near
    /// the top and links two large rooms, a side room and a long hall; a 2-tile causeway (rows 15-16) runs east from the hall to the Mire Hydra arena, a rounded 19x15-tile court
    /// (about 280 floor tiles, 4x the old 70-tile ring) built for 30-40 player Wanted fights by Tools/build-sunken-reliquary-wide-arena.py (also prints these rows).
    /// The map grew east only (22 columns): the row count and so every world coordinate and save stayed the same. A second causeway from the hall ends at (0,15-16) with nothing
    /// beyond it yet. There is no exit yet (Cinderwatch Hold is not built); when it is, World Map 2.0's "no boss gates" rule (13/11/2026) means that exit opens with no kill
    /// required, like every other portal. MapSpawnTable keys on MapId.
    /// </summary>
    public sealed class SunkenReliquaryMap : TileMapBase
    {
        public const string MapId = "sunkenReliquary";

        static readonly string[] Rows =
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTppppppppppTTTTpppppppppTTTTppppTTTTTTTTTTTTTTTTTTTTTTTT",
            "dddppppppppppTTTTpppppppppTTTTppppTTTTTTTTTTTTTTTTTTTTTTTT",
            "dddppppppppppddddpppppppppddddppppTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTppppppppppTTTTpppppppppTTTTppppTTTTpppppppppppppTTTTTTT",
            "TTTppppppppppTTTTpppppppppTTTTTTTTTTTpppppppppppppppTTTTTT",
            "TTTppppppppppTTTTpppppppppTTTTTTTTTTpppppppppppppppppTTTTT",
            "TTTppppppppppTTTTTTddTTTTTTTTTTTTTTpppppppppppppppppppTTTT",
            "TTTTTTTTTTTTTTTTTTTddTTTTTTTTTTTTTTpppppppppppppppppppTTTT",
            "TTTTTTTTTTTTTTTTTTTddTTTTTTTTTTTTTTpppppppppppppppppppTTTT",
            "TTTTTTTTTTTTTTTTTTTddTTTTTTTTTTTTTTpppppppppppppppppppTTTT",
            "TTTTTTTTTTTTTTTTTTTddTTTTTTTTTTTTTTpppppppppppppppppppTTTT",
            "TTTpppppppppppppppppppTTTTTTTTTTTTTpppppppppppppppppppTTTT",
            "TTTpppppppppppppppppppTTTTTTTTTTTTTpppppppppppppppppppTTTT",
            "dddpppppppppppppppppppdddddddddddddpppppppppppppppppppTTTT",
            "dddpppppppppppppppppppdddddddddddddpppppppppppppppppppTTTT",
            "TTTpppppppppppppppppppTTTTTTTTTTTTTTpppppppppppppppppTTTTT",
            "TTTpppppppppppppppppppTTTTTTTTTTTTTTTpppppppppppppppTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTpppppppppppppTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            // Rows 21-23: under the HUD bar, so they are blocked.
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        // [type, tx, ty]: tx / ty = bottom-left footprint tile. Everything stays off the causeways; the arena's middle is left clear for the fight.
        static readonly MapProp[] PropList =
        {
            // Pillars in the corners of the rooms and the hall
            new MapProp("mossyPillar", 4, 3), new MapProp("mossyPillar", 11, 3), new MapProp("mossyPillar", 4, 7), new MapProp("mossyPillar", 11, 7),
            new MapProp("mossyPillar", 18, 3), new MapProp("mossyPillar", 24, 3),
            new MapProp("mossyPillar", 5, 14), new MapProp("mossyPillar", 12, 14), new MapProp("mossyPillar", 5, 17), new MapProp("mossyPillar", 12, 17),
            new MapProp("swampLantern", 18, 7), new MapProp("swampLantern", 24, 7), new MapProp("brokenArch", 15, 18), new MapProp("sunkenShrine", 31, 5),
            // The arena: the hydra's nest at the east wall behind the boss, pillars in the four corners; the middle and the causeway mouth stay clear for the crowd
            new MapProp("hydraNest", 51, 12), new MapProp("mossyPillar", 38, 6), new MapProp("mossyPillar", 50, 6), new MapProp("mossyPillar", 38, 18), new MapProp("mossyPillar", 50, 18),
        };

        static readonly Vector2Int PlayerStartTile = new Vector2Int(3, 3);
        /// <summary>West edge near the top: back to Emberfen Marsh's path (two tiles wide, so a pair of portals).</summary>
        static readonly MapPortal[] PortalList = { new MapPortal(0, 3, EmberfenMarshMap.MapId, 33, 2), new MapPortal(0, 4, EmberfenMarshMap.MapId, 33, 2) };

        public SunkenReliquaryMap() : base(Rows, "Tw")
        {
            foreach (MapProp p in PropList)
            {
                PropDef d;
                if (VillagePropDefs.TryGet(p.Type, out d)) BlockTiles(p.Tx, p.Ty - d.FootH + 1, d.FootW, d.FootH);
                else Debug.LogError("[ZenoniaProto] unknown village prop " + p.Type);
            }
        }

        public override string Id { get { return MapId; } }
        public override string Name { get { return "Sunken Reliquary"; } }
        public override string GroundTexture { get { return "Sprites/village/tiles/sunken-reliquary-ground"; } }
        public override int GroundChunkCols { get { return 2; } }
        public override int GroundChunkRows { get { return 1; } }
        public override string Music { get { return "frost-hollow"; } } // no track of its own yet
        public override bool Hostile { get { return true; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }
        public override MapPortal[] Portals { get { return PortalList; } }
        public override MapProp[] Props { get { return PropList; } }
    }
}
