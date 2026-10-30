using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Oakhaven Village (web src/world/maps/oakhaven-village-map.js v6): the safe starting hub, ONE painted backdrop
    /// (Sprites/village/tiles/oakhaven-ground.png) with the HD buildings and props y-sorted on top. The rows were classified from the painting
    /// tile by tile in the web and only drive collision: T forest canopy (solid)  g grass  d dirt road / market / farm  p plaza  w river  B bridge.
    /// Props block their footprints (VillagePropDefs) and every NPC blocks its tile, exactly like the web map-manager.
    /// </summary>
    public sealed class OakhavenVillageMap : TileMapBase
    {
        public const string MapId = "oakhavenVillage"; // the web / save id; the launch arg spells it oakhaven-village

        static readonly string[] Rows =
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTwwwTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTwwwTTT",
            "TTTggTgggggTgggggggggTgggggggTwwwTTT",
            "TTggggggggggggggggggggggggggggTwwTTT",
            "TTggggggggggggggddgggggggggggggwwTTT",
            "TTgggdgggggggggggdgggggggggdggTwwTgT",
            "TTTggdggggggggggddgggggggggdgggwwTgT",
            "TTggddddggggggggddgggggggggdgggwwTgT",
            "TTgddddddgggggggpppggggdgggdggTwwwgT",
            "TTgdddddddgggggpppppgggdgggdggTwwTgT",
            "TTddddddddddddppppppddddddddddBBBBdd",
            "TTgddddddggggdppppppddddddddddBBBBdd",
            "TTgggggggggggggpppppggggggggdgTwwTgT",
            "TTTgggggggggddggppgddgggggggdgTwwTgT",
            "TTgggggggggdgggggdggddgggggggdTwwggT",
            "TTddddddddgdgggggdggggdggggggdgwwTTT",
            "TTddddddddggggggddgggggddgggggBBwwTT",
            "TgddddddddggggggddggggggdggggggwwwTT",
            "TTddddddddggggggddgggggggggggggwwwTT",
            "TTddddddddggggggdggggggggggggggwwTTT",
            "TTggggggggggTgggdgggggggggggTgTwwwTT",
            "TTTTgTTgTTTTTTTgdggTTTTggggTTTTwwwTT",
            // Rows 22-23: the painted road runs on under the HUD bar, so it is blocked (the portal sits at row 21).
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTwwTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTwwTTT",
        };

        // Stable ids are NpcDefs keys. Each villager stands on open ground in front of their home / work.
        static readonly MapNpc[] NpcList =
        {
            new MapNpc("elderRowan", 17, 7), new MapNpc("villagerMia", 5, 7), new MapNpc("hunterLena", 27, 7), new MapNpc("healerNina", 23, 9),
            new MapNpc("merchantDale", 8, 10), new MapNpc("childTim", 19, 13), new MapNpc("farmerGuri", 8, 16), new MapNpc("fishermanRoy", 30, 15),
            new MapNpc("carpenterBrol", 24, 20), new MapNpc("guardRook", 18, 20),
        };

        // [type, tx, ty]: tx / ty = bottom-left footprint tile. Buildings sit over the end of their painted feeder road so it leads to the door.
        static readonly MapProp[] PropList =
        {
            // Homes and workshops
            new MapProp("elderHouse", 14, 5), new MapProp("cottageRed", 4, 5), new MapProp("hunterLodge", 26, 5),
            new MapProp("healerCottage", 21, 8), new MapProp("carpenterWorkshop", 22, 19),
            // Plaza: well at the centre, lanterns at the edges, signpost toward the south gate
            new MapProp("well", 16, 11), new MapProp("lanternPost", 14, 8), new MapProp("lanternPost", 20, 12), new MapProp("villageSignpost", 18, 15),
            // Market square (west)
            new MapProp("marketStall", 7, 8), new MapProp("barrel", 2, 9), new MapProp("crate", 2, 10),
            // Farm (southwest): fence along the north edge, scarecrow in the field
            new MapProp("villageFence", 2, 14), new MapProp("villageFence", 4, 14), new MapProp("villageFence", 6, 14), new MapProp("scarecrow", 5, 17),
            // Carpenter's yard
            new MapProp("woodpile", 28, 18), new MapProp("campfire", 21, 19), new MapProp("barrel", 21, 18),
            // River: lantern at the bridge head, rowboat moored by the dock
            new MapProp("lanternPost", 29, 9), new MapProp("rowboat", 32, 18),
            // Greenery
            new MapProp("villageOak", 11, 5), new MapProp("villageOak", 12, 14), new MapProp("villageOak", 25, 14),
            new MapProp("villagePine", 21, 3), new MapProp("villagePine", 11, 20), new MapProp("villagePine", 2, 4),
            new MapProp("villageFlowerBush", 13, 7), new MapProp("villageBush", 20, 7), new MapProp("villageFlowerBush", 9, 13), new MapProp("villageBush", 26, 12),
        };

        static readonly Vector2Int PlayerStartTile = new Vector2Int(16, 19);
        /// <summary>South gate: leads to the Frost Hollow entrance stairs (that map's start tile).</summary>
        static readonly MapPortal[] PortalList = { new MapPortal(16, 21, FrostHollowLayout.MapId, FrostHollowLayout.PlayerStartTile.x, FrostHollowLayout.PlayerStartTile.y) };

        public OakhavenVillageMap() : base(Rows, "Tw")
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
        public override string Name { get { return "Oakhaven Village"; } }
        public override string GroundTexture { get { return "Sprites/village/tiles/oakhaven-ground"; } }
        public override string Music { get { return "oakhaven-village"; } }
        public override bool Hostile { get { return false; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }
        public override MapPortal[] Portals { get { return PortalList; } }
        public override MapProp[] Props { get { return PropList; } }
        public override MapNpc[] Npcs { get { return NpcList; } }
    }
}
