using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Amber Dunes: the hostile desert field east of Sunspire Oasis, ONE painted backdrop (Sprites/village/tiles/amber-dunes-ground.png) with the HD props y-sorted on top.
    /// The rows were classified from the painting and only drive collision: T sandstone cliffs (solid)  g sand  d caravan track. Two cliff walls split it into three wide fields, so the track winds
    /// from the west edge near the top, under the first wall and over the second, to the east edge near the bottom. Two more track ends wait for training sub-maps: (18-19,0) north and (35,11) east.
    /// Props block their footprints (VillagePropDefs). MapSpawnTable keys on MapId.
    /// </summary>
    public sealed class AmberDunesMap : TileMapBase
    {
        public const string MapId = "amberDunes";

        static readonly string[] Rows =
        {
            "TTTTTTTTTTTTTTTTTTddTTTTTTTTTTTTTTTT",
            "TTTgggggggTTTTTTggddTTTTTTTTggggTTTT",
            "TTgggggggggTTTTTggddgggggggggggggTTT",
            "TTgggggggggTTTTTggddggggggggggggggTT",
            "ddddddgggggTTTTTggdddddddddgggggggTT",
            "dddddddddddTTTTTggddddddddddggggggTT",
            "TTgggdddddddTTTTggddgggTTTddggggggTT",
            "TTggggggggddTTTTggddgggTTTddggggggTT",
            "TTggggggggddTTTTggddggTTTTddggggggTT",
            "TTggggggggddTTTTggddggTTTTddggggggTT",
            "TTggggggggddTTTTggddggTTTTddddggggTT",
            "TTggggggggddTTTTggddggTTTTTddddddddd",
            "TTggggggggddTTTTggddggTTTTTdddddgTTT",
            "TTggggggggddTTTTgdddggTTTTTggdddgTTT",
            "TTggggggggddTTTggddgggTTTTTggddddTTT",
            "TTggggggggdddddddddgggTTTTTggggddTTT",
            "TTgggggggggdddddddggggTTTTTggggdddTT",
            "TTTgggggggggggggggggggTTTTTgggggdTTT",
            "TTTggggggggggggggTTgggTTTTTgggggggTT",
            "TTTggggggggggggggTTgggTTTTTggggggTTT",
            "TTTTggggggggggggggggggTTTTTggggggTTT",
            // Rows 21-23: under the HUD bar, so they are blocked.
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        // [type, tx, ty]: tx / ty = bottom-left footprint tile. Everything stays off the track so it reads as the way through, and the fields stay open for parties.
        static readonly MapProp[] PropList =
        {
            // Rocks and dead trees
            new MapProp("duneRock", 5, 9), new MapProp("duneRock", 30, 8), new MapProp("duneRock", 28, 19),
            new MapProp("deadAcacia", 4, 12), new MapProp("deadAcacia", 17, 4), new MapProp("deadAcacia", 30, 17), new MapProp("deadAcacia", 8, 19),
            // Cactus and bones
            new MapProp("cactusCluster", 7, 8), new MapProp("cactusCluster", 21, 19), new MapProp("cactusCluster", 29, 2), new MapProp("cactusCluster", 15, 17),
            new MapProp("bleachedBones", 3, 15), new MapProp("bleachedBones", 21, 7), new MapProp("bleachedBones", 33, 6), new MapProp("bleachedBones", 13, 18),
            // A lost caravan in the first field, old ruins in its far corner and in the last field
            new MapProp("brokenWagon", 4, 18),
            // Rockslide over the old Emberfen shortcut at the east road end
            new MapProp("duneRock", 33, 17), new MapProp("duneRock", 34, 16),
            new MapProp("ruinPillar", 5, 2), new MapProp("ruinPillar", 8, 2), new MapProp("ruinPillar", 28, 3), new MapProp("ruinPillar", 31, 3),
        };

        static readonly Vector2Int PlayerStartTile = new Vector2Int(3, 4);
        // Arrival tile is a const on purpose (see docs/map-design-rules.md): Scorpion Gorge aims its portal here and this map aims back at Scorpion Gorge.
        /// <summary>Where the Scorpion Gorge gate sends a hero: on the east track, two tiles in from the edge (the track end this row was already drawn with).</summary>
        public const int ScorpionGorgeArrivalTx = 33, ScorpionGorgeArrivalTy = 11;

        /// <summary>West edge near the top: back to Sunspire's east road (two tiles wide, so a pair). East edge near the bottom: the old shortcut to Emberfen Marsh,
        /// closed by a rockslide (13/11/2026: it skipped Scorpion Gorge; rows 16-17 x 33-35 solid). East edge, middle (the last column, x 35): the dangling caravan-track end (World Map 2.1, 13/11/2026) on to Scorpion Gorge, region 3.</summary>
        static readonly MapPortal[] PortalList =
        {
            new MapPortal(0, 4, SunspireOasisMap.MapId, 33, 10), new MapPortal(0, 5, SunspireOasisMap.MapId, 33, 11),
            new MapPortal(35, 11, ScorpionGorgeMap.MapId, ScorpionGorgeMap.WestArrivalTx, ScorpionGorgeMap.WestArrivalTy),
        };

        public AmberDunesMap() : base(Rows, "Tw")
        {
            foreach (MapProp p in PropList)
            {
                PropDef d;
                if (VillagePropDefs.TryGet(p.Type, out d)) BlockTiles(p.Tx, p.Ty - d.FootH + 1, d.FootW, d.FootH);
                else Debug.LogError("[ZenoniaProto] unknown village prop " + p.Type);
            }
        }

        public override string Id { get { return MapId; } }
        public override string Name { get { return "Amber Dunes"; } }
        public override string GroundTexture { get { return "Sprites/village/tiles/amber-dunes-ground"; } }
        public override string Music { get { return "frost-hollow"; } } // no track of its own yet
        public override bool Hostile { get { return true; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }
        public override MapPortal[] Portals { get { return PortalList; } }
        public override MapProp[] Props { get { return PropList; } }
    }
}
