using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Frostfang Den: the frozen cavern behind the ice cave of Sunbreak Foothills (a side dungeon since World Map 2.1), ONE painted backdrop (Sprites/village/tiles/frostfang-den-ground.png) with the HD props y-sorted on top.
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

        /// <summary>
        /// World Map 2.1 (13/11/2026): the Den is a side dungeon of Sunbreak Foothills, no longer the road from Frost Hollow to Sunspire. West edge: back to the foot of the stairs under Sunbreak's
        /// ice cave (the painted path is three tiles wide here, so three triggers, one light). East edge, past the arena's far wall behind the Glacier Warden: it used to go on to Sunspire Oasis and
        /// is now a one-way shortcut out to the fork of the south road in Sunbreak (DenExitArrival, far from the cave mouth), so nobody has to walk the whole cave back after the fight. Always open (no boss gate since World Map 2.0 step 1;
        /// the kill itself is still saved per hero, for the boss's own rewards).
        /// </summary>
        static readonly MapPortal[] PortalList =
        {
            new MapPortal(0, 9, SunbreakFoothillsMap.MapId, SunbreakFoothillsMap.CaveArrivalTx, SunbreakFoothillsMap.CaveArrivalTy),
            new MapPortal(0, 10, SunbreakFoothillsMap.MapId, SunbreakFoothillsMap.CaveArrivalTx, SunbreakFoothillsMap.CaveArrivalTy),
            new MapPortal(0, 11, SunbreakFoothillsMap.MapId, SunbreakFoothillsMap.CaveArrivalTx, SunbreakFoothillsMap.CaveArrivalTy),
            new MapPortal(34, 10, SunbreakFoothillsMap.MapId, SunbreakFoothillsMap.DenExitArrivalTx, SunbreakFoothillsMap.DenExitArrivalTy),
        };

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
        public override MapPortal[] Portals { get { return PortalList; } }
        public override MapProp[] Props { get { return PropList; } }
    }
}
