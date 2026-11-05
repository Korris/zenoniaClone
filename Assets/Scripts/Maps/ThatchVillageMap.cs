using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Thatchwick Village: a safe thatched-roof village east of Oakhaven, ONE painted backdrop (Sprites/village/tiles/thatch-village-ground.png). Houses, well, market stall, barn and bridge are all
    /// painted in, so there are no MapProps and no NPCs: the rows are the only collision. They were classified from the painting tile by tile (checked with Tools/build-map-backdrop.py check):
    /// T house / roof / tree / rock / fence (solid)  g grass  d road  p plaza  w stream (solid)  B bridge.
    /// West entrance road (rows 8-9) joins Oakhaven's east gate, the east road (rows 11-12) leads to Willowbrook Meadows. The painted north and south roads are blocked at the map edge.
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
        /// <summary>West road pair back to Oakhaven's east bridge tiles, east road pair on to Willowbrook's west gate. Arrivals stand two tiles in from the target map's portal.</summary>
        static readonly MapPortal[] PortalList =
        {
            new MapPortal(0, 8, OakhavenVillageMap.MapId, 33, 10), new MapPortal(0, 9, OakhavenVillageMap.MapId, 33, 11),
            new MapPortal(35, 11, WillowbrookMeadowsMap.MapId, 2, 11), new MapPortal(35, 12, WillowbrookMeadowsMap.MapId, 2, 12),
        };

        public ThatchVillageMap() : base(Rows, "Tw") { }

        public override string Id { get { return MapId; } }
        public override string Name { get { return "Thatchwick Village"; } }
        public override string GroundTexture { get { return "Sprites/village/tiles/thatch-village-ground"; } }
        public override string Music { get { return "oakhaven-village"; } } // reuses Oakhaven's track
        public override bool Hostile { get { return false; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }
        public override MapPortal[] Portals { get { return PortalList; } }
    }
}
