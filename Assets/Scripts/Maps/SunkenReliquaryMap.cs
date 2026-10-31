using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Sunken Reliquary: the flooded ruins past Emberfen Marsh, ONE painted backdrop (Sprites/village/tiles/sunken-reliquary-ground.png) with the HD props y-sorted on top.
    /// The rows were classified from the painting and only drive collision: T ruin wall and flood (solid)  d stone causeway  p temple flagstones. The causeway enters at the west edge near the top and links
    /// two large rooms, a side room and a long hall to the Mirefang Hydra's round arena at the far end. A second causeway from the hall ends at (0,15-16) with nothing beyond it yet. There is no exit yet
    /// (Cinderwatch Hold is not built): add it behind a boss gate (MapSpawnTable.HasBossGate). MapSpawnTable keys on MapId.
    /// </summary>
    public sealed class SunkenReliquaryMap : TileMapBase
    {
        public const string MapId = "sunkenReliquary";

        static readonly string[] Rows =
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTppppppppppTTTTpppppppppTTTTppppTT",
            "dddppppppppppTTTTpppppppppTTTTppppTT",
            "dddppppppppppddddpppppppppddddppppTT",
            "TTTppppppppppTTTTpppppppppTTTTppppTT",
            "TTTppppppppppTTTTpppppppppTTTTTTTTTT",
            "TTTppppppppppTTTTpppppppppTTTTTTTTTT",
            "TTTppppppppppTTTTTTddTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTddTTTTTTTpppppTTT",
            "TTTTTTTTTTTTTTTTTTTddTTTTTTpppppppTT",
            "TTTTTTTTTTTTTTTTTTTddTTTTTppppppppTT",
            "TTTTTTTTTTTTTTTTTTTddTTTTTpppppppppT",
            "TTTpppppppppppppppppppTTTTpppppppppT",
            "TTTpppppppppppppppppppTTTTpppppppppT",
            "dddpppppppppppppppppppddddpppppppppT",
            "dddpppppppppppppppppppTTTTTpppppppTT",
            "TTTpppppppppppppppppppTTTTTpppppppTT",
            "TTTpppppppppppppppppppTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            // Rows 21-23: under the HUD bar, so they are blocked.
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        // [type, tx, ty]: tx / ty = bottom-left footprint tile. Everything stays off the causeways; the arena's middle is left clear for the fight.
        static readonly MapProp[] PropList =
        {
            // Pillars in the corners of the rooms and the hall
            new MapProp("mossyPillar", 4, 3), new MapProp("mossyPillar", 11, 3), new MapProp("mossyPillar", 4, 7), new MapProp("mossyPillar", 11, 7),
            new MapProp("mossyPillar", 18, 3), new MapProp("mossyPillar", 24, 3),
            new MapProp("mossyPillar", 5, 14), new MapProp("mossyPillar", 12, 14), new MapProp("mossyPillar", 5, 17), new MapProp("mossyPillar", 12, 17),
            new MapProp("swampLantern", 18, 7), new MapProp("swampLantern", 24, 7), new MapProp("brokenArch", 15, 18), new MapProp("sunkenShrine", 31, 5),
            // The arena: the hydra's nest behind it, pillars around the ring
            new MapProp("hydraNest", 31, 10), new MapProp("mossyPillar", 28, 10), new MapProp("mossyPillar", 33, 12), new MapProp("mossyPillar", 28, 17), new MapProp("mossyPillar", 33, 16),
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
        public override string Music { get { return "frost-hollow"; } } // no track of its own yet
        public override bool Hostile { get { return true; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }
        public override MapPortal[] Portals { get { return PortalList; } }
        public override MapProp[] Props { get { return PropList; } }
    }
}
