using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Emberfen Marsh: the hostile swamp east of Amber Dunes, ONE painted backdrop (Sprites/village/tiles/emberfen-marsh-ground.png) with the HD props y-sorted on top.
    /// The rows were classified from the painting and only drive collision: T bank and thicket (solid)  w deep water  g mud and moss  d boardwalk path  B plank bridge. Two channels split it into three wide
    /// fields joined by one bridge each, so the path winds from the west edge near the bottom to the east edge near the top. Two more path ends wait for training sub-maps: (0,10) west and (35,10) east.
    /// Props block their footprints (VillagePropDefs). MapSpawnTable keys on MapId.
    /// </summary>
    public sealed class EmberfenMarshMap : TileMapBase
    {
        public const string MapId = "emberfenMarsh";

        static readonly string[] Rows =
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTgggggggggTTggggggggggggggggT",
            "TTTwwwwggggggggggTdddddddddddddddddd",
            "TTTwwwwggwwwgddddddddddddddddddddddT",
            "TTTTTTgggwwwdddddddggwwwwggggggggTTT",
            "TTTTTTTTTTTTBBTTTTTTTTTTTTTTTTTTTTTT",
            "wwwwwwwwwwwwBBwwwwwwwwwwwwwwwwwwwwww",
            "wwwwwwwwwwwwBBwwwwwwwwwwwwwwwwwwwwww",
            "TTTTTTggggggddgTTgggggTTgggggggggTTT",
            "TgggggggggggddggggggggTTgggggggggTTT",
            "ddddddddddddddddddddddgggwwwwddddddd",
            "TTTTggwwwwwgggggggggdddggwwwwggggggT",
            "TTTTTTTTTTTTTTTTTTTTTddTTTTTTTTTTTTT",
            "wwwwwwwwwwwwwwwwwwwwwBBwwwwwwwwwwwww",
            "wwwwwwwwwwwwwwwwwwwwwBBwwwwwwwwwwwww",
            "TTTTTTTTTTTTTTTTTTTTTddTTTTTTTTTTTTT",
            "TgggggggggggggggggggdddggggggggTTTgT",
            "TgggggggdddddddddddddddggggwwwwwgggT",
            "ddddddddddwwwgggggTTTggggggwwwwwggTT",
            "TgggggggggwwwgggggTTTgggggggggggggTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            // Rows 21-23: under the HUD bar, so they are blocked.
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        // [type, tx, ty]: tx / ty = bottom-left footprint tile. The tall ones stand on the channel banks and the south thicket, where their canopy hides water rather than the path.
        static readonly MapProp[] PropList =
        {
            // Trees and roots on the banks
            new MapProp("swampTree", 4, 15), new MapProp("swampTree", 12, 15), new MapProp("swampTree", 16, 15), new MapProp("swampTree", 27, 15), new MapProp("swampTree", 31, 15),
            new MapProp("swampTree", 3, 5), new MapProp("swampTree", 8, 5), new MapProp("swampTree", 14, 20), new MapProp("swampTree", 26, 20), new MapProp("swampTree", 33, 20),
            new MapProp("mangroveRoots", 8, 12), new MapProp("mangroveRoots", 26, 12), new MapProp("mangroveRoots", 6, 20), new MapProp("mangroveRoots", 22, 20),
            // Mushrooms and reeds at the edges of the fields
            new MapProp("giantMushroom", 30, 9), new MapProp("giantMushroom", 4, 9), new MapProp("giantMushroom", 15, 16), new MapProp("giantMushroom", 25, 19),
            new MapProp("reedPatch", 8, 4), new MapProp("reedPatch", 26, 4), new MapProp("reedPatch", 33, 19), new MapProp("reedPatch", 19, 9),
            // Lanterns at the bridge ends
            new MapProp("swampLantern", 11, 8), new MapProp("swampLantern", 14, 8), new MapProp("swampLantern", 20, 12), new MapProp("swampLantern", 23, 12),
        };

        static readonly Vector2Int PlayerStartTile = new Vector2Int(3, 18);
        /// <summary>West edge near the bottom: back to Amber Dunes. East edge near the top: on to Sunken Reliquary.</summary>
        static readonly MapPortal[] PortalList =
        {
            new MapPortal(0, 18, AmberDunesMap.MapId, 33, 17),
            new MapPortal(35, 2, SunkenReliquaryMap.MapId, 2, 3),
        };

        public EmberfenMarshMap() : base(Rows, "Tw")
        {
            foreach (MapProp p in PropList)
            {
                PropDef d;
                if (VillagePropDefs.TryGet(p.Type, out d)) BlockTiles(p.Tx, p.Ty - d.FootH + 1, d.FootW, d.FootH);
                else Debug.LogError("[ZenoniaProto] unknown village prop " + p.Type);
            }
        }

        public override string Id { get { return MapId; } }
        public override string Name { get { return "Emberfen Marsh"; } }
        public override string GroundTexture { get { return "Sprites/village/tiles/emberfen-marsh-ground"; } }
        public override string Music { get { return "frost-hollow"; } } // no track of its own yet
        public override bool Hostile { get { return true; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }
        public override MapPortal[] Portals { get { return PortalList; } }
        public override MapProp[] Props { get { return PropList; } }
    }
}
