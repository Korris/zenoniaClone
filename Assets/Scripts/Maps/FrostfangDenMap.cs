using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Frostfang Den: the frozen cavern between Frost Hollow and Sunspire Oasis, ONE painted backdrop (Sprites/village/tiles/frostfang-den-ground.png) with the HD props y-sorted on top.
    /// The rows were classified from the painting and only drive collision: T cave wall (solid)  g snow / ice floor  d worn path  p frozen arena floor. The path enters at the west edge on rows 9-11
    /// and the arena fills the east end, with the way on to Sunspire at its far wall behind the Glacier Warden. Props block their footprints (VillagePropDefs). MapSpawnTable keys on MapId.
    /// </summary>
    public sealed class FrostfangDenMap : TileMapBase
    {
        public const string MapId = "frostfangDen";

        static readonly string[] Rows =
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTgggggggggTTTTTTTTgggggTTTT",
            "TTTTTTTTTggggggggggTTTTTgggggggggTTT",
            "TTTTTTTTggggggggggTTTTggggggggggggTT",
            "TTTTTTTgggggggggggTTTTggggggggggggTT",
            "TTTTTTTTgggggggggTTTTpppppppppppppgT",
            "TTTTTTTTTggggggggTTTgpppppppppppppgT",
            "TTTTTTTTTTTTggggggTTgpppppppppppppgT",
            "ddddddddTTTTdddddddddpppppppppppppgT",
            "ddddddddTTTddTTTdddddpppppppppppppgT",
            "ddddddddgggdddTTdddddpppppppppppppgT",
            "TTgggggggggggTTTTggggpppppppppppppgT",
            "TTTgggggggggTTTTTggggpppppppppppppgT",
            "TTTTgggggggggTTTTTgggpppppppppppppgT",
            "TTTTTggggggggggTTTTggpppppppppppppgT",
            "TTTTgggggggggggTTTTTgpppppppppppppTT",
            "TTTTTggggggggggTTTTTgpppppppppppppTT",
            "TTTTTTggTTTggTTTTTTgggggggggggggTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTggggggTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            // Rows 21-23: under the HUD bar, so they are blocked.
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        // [type, tx, ty]: tx / ty = bottom-left footprint tile. Everything stays off the path; the arena's middle is left clear for the fight.
        static readonly MapProp[] PropList =
        {
            // Pillars at the four corners of the arena
            new MapProp("icePillar", 23, 7), new MapProp("icePillar", 32, 7), new MapProp("icePillar", 23, 16), new MapProp("icePillar", 32, 16),
            // Stalagmites through the cave
            new MapProp("frozenStalagmite", 10, 4), new MapProp("frozenStalagmite", 16, 4), new MapProp("frozenStalagmite", 6, 15), new MapProp("frozenStalagmite", 13, 16), new MapProp("frozenStalagmite", 30, 18),
            // Crystals and the bones of those who came before
            new MapProp("frostCrystal", 13, 2), new MapProp("frostCrystal", 8, 13), new MapProp("frostCrystal", 25, 18), new MapProp("frostCrystal", 29, 3),
            new MapProp("frozenBones", 14, 7), new MapProp("frozenBones", 10, 17), new MapProp("frozenBones", 30, 13), new MapProp("frozenBones", 25, 5),
        };

        static readonly Vector2Int PlayerStartTile = new Vector2Int(3, 10);

        /// <summary>West edge: back to the top of Frost Hollow's north-east stairs (the painted path is three tiles wide here, so three triggers, one light). Always open.</summary>
        static readonly MapPortal[] BackPortals = { new MapPortal(0, 9, FrostHollowLayout.MapId, 33, 4), new MapPortal(0, 10, FrostHollowLayout.MapId, 33, 4), new MapPortal(0, 11, FrostHollowLayout.MapId, 33, 4) };
        /// <summary>The same plus the light at the arena's far wall, on to Sunspire, which only exists once this save has killed the Warden: until then no light and no trigger.</summary>
        static readonly MapPortal[] AllPortals = { BackPortals[0], BackPortals[1], BackPortals[2], new MapPortal(34, 10, SunspireOasisMap.MapId, 2, 10) };

        public FrostfangDenMap() : base(Rows, "Tw")
        {
            foreach (MapProp p in PropList)
            {
                PropDef d;
                if (VillagePropDefs.TryGet(p.Type, out d)) BlockTiles(p.Tx, p.Ty - d.FootH + 1, d.FootW, d.FootH);
                else Debug.LogError("[ZenoniaProto] unknown village prop " + p.Type);
            }
        }

        public override string Id { get { return MapId; } }
        public override string Name { get { return "Frostfang Den"; } }
        public override string GroundTexture { get { return "Sprites/village/tiles/frostfang-den-ground"; } }
        public override string Music { get { return "frost-hollow"; } } // no track of its own yet
        public override bool Hostile { get { return true; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }
        public override MapPortal[] Portals { get { return PortalsFor(RpgSession.BossDefeatedOn(MapId)); } }
        public override MapPortal[] PortalsFor(bool gateOpen) { return gateOpen ? AllPortals : BackPortals; }
        public override MapProp[] Props { get { return PropList; } }
    }
}
