using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Frost Hollow layout copied from the web src/world/maps/frost-hollow-map.js: 36x24 tiles of 16 units, painted backdrop
    /// (Sprites/tiles/frost-hollow-ground.png) plus the tile classification that drives collision.
    /// T snowy pines (solid)  C cliff / rock / ruin (solid)  w frozen river (solid)  B bridge  s snow / stone ground.
    /// Rows are listed top-down like the web (row 0 = top of the map); world y points up, so the row of a point is TilesH-1-floor(y/16).
    /// The static FrostHollowMap keeps the Frost Hollow spawn data and forwards collision to whichever map is current.
    /// </summary>
    public sealed class FrostHollowLayout : TileMapBase
    {
        static readonly string[] Rows =
        {
            "CCCCCCCCCCCCCCCCCwwwCCCTTTTTTCCCCCCC",
            "CCCCCCCCCCCCCCCCCwwwCCCTTTTTTCCCCCCC",
            "CCCCCCCCCCsTTCTTTwwwCCCTTCCCCCCCssCC",
            "CCCCsssCCsssCCCTTwwwCCCTssCssCCCssCC",
            "TCCCsssssssssssTTTwwCCCTsssssCCsssCC",
            "TssCsssssssssssTTTwwCCCTssssssssssCC",
            "TCCCCsssssssssTTTCwwCCCTTsssssssssTT",
            "TTCCTCCssssssCCCCCwwwCCTTsCsssssssTT",
            "TTTTTsssssTssCCCCCwwwCCTTsCssssssCTT",
            "TTTCCssssCCCCCCCCCwwwCCsCCsssssssCTT",
            "TTTCCssCCCCssCCCCCwwwCCsssssssCCCCTT",
            "TTTTCsssCsssssssssCCCCCssssssssCCTTT",
            "TTCCCCsssssssssCCBBBBBsssssCCssCCCCC",
            "TTTTCCssssssCCTCCCwwwCCTsssCCssTCCCC",
            "TTTCCssssssssCTTCCwwCCTTsssCsssTCCCC",
            "TTTCCsssssssCCTTCCwwCCCTCssssssTCCCT",
            "TTTCCssssssssCCCCCwwCCCTssssssssCCCT",
            "TTTCCssCsCsssCCCCCwwCCCTssssssssssTT",
            "TCCCssCCCCTTTTTTTTwwCCCCCTssssssCCCT",
            "TCCsssCCCCTTTTTTTTwwCCCCTTsssCCCCCCT",
            "TTssssCCCTTTTTTTTTwwCCCCCCCCCCCCCCCC",
            "TTssssCsTTTTTTTTTTwwCCCCCCCCCCCCCCCC",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
        };

        /// <summary>Entrance circle (web 'P' at 5,19) back to the village gate; arrival is the tile north of the village's south portal.</summary>
        static readonly MapPortal[] PortalList = { new MapPortal(5, 19, OakhavenVillageMap.MapId, 16, 19) };

        public const string MapId = "frostHollow"; // the web / save id; the launch arg spells it frost-hollow
        public static readonly Vector2Int PlayerStartTile = new Vector2Int(4, 21);

        public FrostHollowLayout() : base(Rows, "TCw") { }

        public override string Id { get { return MapId; } }
        public override string Name { get { return "Frost Hollow"; } }
        public override string GroundTexture { get { return "Sprites/tiles/frost-hollow-ground"; } }
        public override string Music { get { return "frost-hollow"; } }
        public override bool Hostile { get { return true; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }
        public override MapPortal[] Portals { get { return PortalList; } }
    }
}
