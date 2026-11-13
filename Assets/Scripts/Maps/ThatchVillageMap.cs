using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Thatchwick Village: a safe thatched-roof village east of Oakhaven, ONE painted backdrop (Sprites/village/tiles/thatch-village-ground.png). Houses, well, market stall, barn and bridge are all
    /// painted in, so there are no MapProps and no NPCs: the rows are the only collision. They were classified from the painting tile by tile (checked with Tools/build-map-backdrop.py check):
    /// T house / roof / tree / rock / fence (solid)  g grass  d road  p plaza  w stream (solid)  B bridge.
    /// West entrance road (rows 8-9) joins Oakhaven's east gate. The east road (rows 11-12) led to Willowbrook Meadows; World Map 2.0 step 1 (13/11/2026) re-aimed it at Frost Hollow's old
    /// village gate (2,21) instead (Willowbrook now connects to Oakhaven's south gate). The painted north and south roads are blocked at the map edge.
    /// Three NPCs stand on open plaza tiles (NpcDefs, world-map-2 step 1): herbalistWren and smithHolt flank the square, brokerPell (market broker, dialogue only) stands near its centre.
    /// </summary>
    public sealed class ThatchVillageMap : TileMapBase
    {
        public const string MapId = "thatchVillage";

        static readonly string[] Rows =
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTdddTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTdddgTTTTTTTTTTwwwTTTT",
            "TTTTTTTTTTTTTTTTddTTTTTTTTTTggBBBBgT",
            "TTTTTTTTTTTTTTgdddTTTTTTTTTTTggwwwww",
            "TggTTTTTTTTTTTgdddTTTTTTTTTTggTTTTww",
            "TggTTTTTTTggTTgdddggggTgTTgggTTTTTTT",
            "TTggggggggddppppppppppppppgTTTTTTTTT",
            "ddddddTTTTTppppppTTTppppppTTTTTTTTTT",
            "dddddTTTTTTppppppTTpppppppTTTTTTTTTT",
            "TTggTTTTTTTTppppTTTTppppppTTTTggTTTT",
            "TTgTTTTTTTTpppppTTTTppppppdddddddddd",
            "TTTTTTTTgggpppppppppppppppTddddddddd",
            "TTTTTTTTTggppppppppppppppTTTTTTTTTTT",
            "TTTTTTTTTTTppppppppppppTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTppppppppTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTpTppppppppTTTTTTTTTTTTTT",
            "TTTTTTggTTTTpppppppTTTTTTTTTTTTTTTTT",
            "TTTTTTgggggpddddddTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTgpddddddTTTTTTTTgggTTTTTTT",
            "TTTTTTTTTTTTdddddddggggggggggggTTTTT",
            // Rows 21-23: under the HUD bar, so blocked.
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        static readonly Vector2Int PlayerStartTile = new Vector2Int(17, 14);
        /// <summary>West road pair back to Oakhaven's east bridge tiles; east road pair on to Frost Hollow's old village gate (World Map 2.0 step 1, 13/11/2026: replaces the old link to Willowbrook).
        /// Arrivals stand two tiles in from the target map's portal.</summary>
        static readonly MapPortal[] PortalList =
        {
            new MapPortal(0, 8, OakhavenVillageMap.MapId, 33, 10), new MapPortal(0, 9, OakhavenVillageMap.MapId, 33, 11),
            new MapPortal(35, 11, FrostHollowLayout.MapId, FrostHollowLayout.PlayerStartTile.x, FrostHollowLayout.PlayerStartTile.y),
            new MapPortal(35, 12, FrostHollowLayout.MapId, FrostHollowLayout.PlayerStartTile.x, FrostHollowLayout.PlayerStartTile.y),
        };

        // Stable ids are NpcDefs keys. All three stand on open plaza ground, off every road and clear of both gates (World Map 2.0 step 1, 13/11/2026).
        static readonly MapNpc[] NpcList =
        {
            new MapNpc("herbalistWren", 13, 9), new MapNpc("smithHolt", 22, 9), new MapNpc("brokerPell", 18, 13),
        };

        public ThatchVillageMap() : base(Rows, "Tw")
        {
            foreach (MapNpc n in NpcList) BlockTiles(n.Tx, n.Ty, 1, 1);
        }

        public override string Id { get { return MapId; } }
        public override string Name { get { return "Thatchwick Village"; } }
        public override string GroundTexture { get { return "Sprites/village/tiles/thatch-village-ground"; } }
        public override string Music { get { return "oakhaven-village"; } } // reuses Oakhaven's track
        public override bool Hostile { get { return false; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }
        public override MapPortal[] Portals { get { return PortalList; } }
        public override MapNpc[] Npcs { get { return NpcList; } }
    }
}
