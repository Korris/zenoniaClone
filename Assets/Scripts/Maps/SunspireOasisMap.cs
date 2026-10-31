using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Sunspire Oasis: the desert trade town, a safe hub like Oakhaven, ONE painted backdrop (Sprites/village/tiles/sunspire-oasis-ground.png) with the HD buildings
    /// and props y-sorted on top. The rows were classified from the painting and only drive collision: T canyon rock (solid)  g sand  d road  p plaza / stone pad  w oasis pool.
    /// The east-west road runs on rows 10-11 into the paved ring around the pool. Props block their footprints (VillagePropDefs) and every NPC blocks its tile.
    /// </summary>
    public sealed class SunspireOasisMap : TileMapBase
    {
        public const string MapId = "sunspireOasis";

        static readonly string[] Rows =
        {
            "TTTTTTTTTTTTTTTTdddTTTTTTTTTTTTTTTTT",
            "TTTTTTggggggggggdddgggggggggggggggTT",
            "TTggggggggggggggdddgggggggggggggggTT",
            "TTTgggggpppppppgdddggppppppppgggggTT",
            "TTggggggpppppppgdddggppppppppggggggT",
            "TTggggggpppppppgdddggppppppppgggggTT",
            "TTgggggggppppppgdddggppppppppggggTTT",
            "TTgggggggppppppgdddggpppppppgggggTTT",
            "TTgggggggggggpppppppppgggggggggggTTT",
            "TggggggggggggppwwwwwppggggggggggggTT",
            "dddddddddddddppwwwwwpppddddddddddddd",
            "dddddddddddddppwwwwwpppddddddddddddd",
            "TggggggggggggppwwwwwppggggggggggggTT",
            "TTgggggggggggppwwwwwppggggggggggggTT",
            "TTggggggpppppppgdddggppppppppgggggTT",
            "TTgggggpppppppggdddggppppppppgggggTT",
            "TTgggggpppppppggdddggppppppppgggggTT",
            "TTgggggpppppppggdddggppppppppggggTTT",
            "TTggggggppppppggdddggpppppppgggggTTT",
            "TgggggggggggggggdddggggggggggggggTTT",
            "TgggggggggggggggdddggggggggggggTTTTT",
            "TTTTTTgggggTTTggdddgTTTTTTTTTTTTTTTT",
            // Rows 22-23: the painted road runs on under the HUD bar, so it is blocked.
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        // Stable ids are NpcDefs keys. Each one stands on open ground in front of their work.
        static readonly MapNpc[] NpcList =
        {
            new MapNpc("elderSahir", 17, 8), new MapNpc("apothecaryNara", 25, 19), new MapNpc("blacksmithKhaled", 26, 8), new MapNpc("bountyLeila", 26, 12),
        };

        // [type, tx, ty]: tx / ty = bottom-left footprint tile. Buildings stand on the four painted stone pads with their doorstep free.
        static readonly MapProp[] PropList =
        {
            // Homes and forge on the north pads, inn and shop on the south pads
            new MapProp("residence", 8, 7), new MapProp("residence", 12, 7), new MapProp("blacksmithForge", 22, 7),
            new MapProp("caravanInn", 7, 18), new MapProp("merchantShop", 23, 18),
            // Plaza: palms on the south corners of the ring (north ones would hide the buildings), fountain toward the south road
            new MapProp("palmTree", 13, 14), new MapProp("palmTree", 22, 14), new MapProp("fountainOrnament", 20, 15),
            // Stalls along the road, bounty board by the east road, caravan wagon by the west gate
            new MapProp("sunspireStall", 7, 9), new MapProp("sunspireStall", 27, 9), new MapProp("noticeBoard", 24, 12), new MapProp("wagon", 3, 14),
            // Clutter outside the shops
            new MapProp("jarCluster", 14, 18), new MapProp("jarCluster", 22, 18), new MapProp("jarCluster", 28, 6), new MapProp("jarCluster", 11, 7),
            // Greenery
            new MapProp("palmTree", 4, 6), new MapProp("palmTree", 31, 5), new MapProp("palmTree", 4, 18), new MapProp("palmTree", 31, 15),
            new MapProp("palmTree", 15, 3), new MapProp("palmTree", 19, 3), new MapProp("palmTree", 15, 18), new MapProp("palmTree", 19, 18),
        };

        static readonly Vector2Int PlayerStartTile = new Vector2Int(17, 14);
        /// <summary>The road is two tiles wide at both edges, so each exit is a pair. Arrivals stand two tiles from the target map's portal, as on the existing maps.</summary>
        static readonly MapPortal[] PortalList =
        {
            new MapPortal(0, 10, FrostfangDenMap.MapId, 32, 10), new MapPortal(0, 11, FrostfangDenMap.MapId, 32, 11),
            new MapPortal(35, 10, AmberDunesMap.MapId, 2, 4), new MapPortal(35, 11, AmberDunesMap.MapId, 2, 5),
        };

        public SunspireOasisMap() : base(Rows, "Tw")
        {
            foreach (MapProp p in PropList)
            {
                PropDef d;
                if (VillagePropDefs.TryGet(p.Type, out d)) BlockTiles(p.Tx, p.Ty - d.FootH + 1, d.FootW, d.FootH);
                else Debug.LogError("[ZenoniaProto] unknown village prop " + p.Type);
            }
            foreach (MapNpc n in NpcList) BlockTiles(n.Tx, n.Ty, 1, 1);
        }

        public override string Id { get { return MapId; } }
        public override string Name { get { return "Sunspire Oasis"; } }
        public override string GroundTexture { get { return "Sprites/village/tiles/sunspire-oasis-ground"; } }
        public override string Music { get { return "oakhaven-village"; } } // no track of its own yet
        public override bool Hostile { get { return false; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }
        public override MapPortal[] Portals { get { return PortalList; } }
        public override MapProp[] Props { get { return PropList; } }
        public override MapNpc[] Npcs { get { return NpcList; } }
    }
}
