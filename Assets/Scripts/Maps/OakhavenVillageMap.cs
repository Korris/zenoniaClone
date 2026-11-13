using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Oakhaven Village: the safe starting hub, now ONE painted backdrop (Sprites/village/tiles/oakhaven-ground.png) like Thatchwick Village -
    /// no more MapProp buildings, the rows are the only collision. World Map 2.0 step 1 (13/11/2026) replaced the old web-sourced layout with the
    /// approved ChatGPT painting (docs/reference/new-oakhaven-village-concept-1.png / Assets/ArtLibrary/village/new-oakhaven/new-oakhaven-village-chatgpt-1.png),
    /// classified tile by tile with Tools/build-map-backdrop.py (grid + check): T house / roof / well / stall / forge / fence / tree (solid)
    /// g grass  d dirt road  p plaza apron  w stream (solid)  B bridge. Buildings, clockwise from the north: the Guild Hall (elderRowan, guardRook),
    /// the apothecary cottage west (healerNina) with the fruit stall just south of it (merchantDale), the forge east (smithBram) across the stream
    /// via the bridge that reaches the east edge (the Thatchwick gate), and two plain cottages south of the plaza flanking the road to the south
    /// gate (Willowbrook Meadows). Rows 22-23 are forced solid: the painted road there runs on under the HUD bar, same as the old layout, so the
    /// south gate sits one row above it, at row 21.
    /// </summary>
    public sealed class OakhavenVillageMap : TileMapBase
    {
        public const string MapId = "oakhavenVillage"; // the web / save id; the launch arg spells it oakhaven-village

        static readonly string[] Rows =
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTwwwT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTwwwT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTwwwT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTwwwT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTwwwT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTwwwT",
            "TTddddddddddddpppppppppTTTTTTTTTwwwT",
            "TTddddTTTTTTdddddddddddTTTTTTTTTwwwT",
            "TTTTTTTTTTTTddddddddddddddddddddBBBB",
            "TTTTTTTTTTTTdddddTTdddddddddddBBBBBB",
            "TTTTTTTTTdddddddddddddddddddddBBBBBB",
            "TTTTTTTTTdddddddddddddddddddddBBBBBB",
            "TTTTTTTTTTTTTTTTddddTTTTTTTTTTwwwwwT",
            "TTTTTTTTTTTTTTTTddddTTTTTTTTTTwwwwwT",
            "TTTTTTTTTTTTTTTTddddTTTTTTTTTTwwwwwT",
            "TTTTTTTTTTTTTTTTddddTTTTTTTTTTwwwwwT",
            "TTTTTTTTTTTTTTTTddddTTTTTTTTTTwwwwwT",
            "TTTTTTTTTTTTTTTTddddTTTTTTTTTTwwwwwT",
            "TTTTTTTTTTTTddddddddTTTTTTTTTTwwwwwT",
            "TTTTTTTTTdddddddddddddddddddddwwwwwT",
            "TTTTTTTTTTTTdddddTTTTTTTTTTTTTTTTTTT",
            // Rows 22-23: the painted road runs on under the HUD bar, so it is forced blocked (the south gate sits at row 21).
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        // Stable ids are NpcDefs keys. elderRowan (quest giver) and guardRook (Wanted board) flank the Guild Hall door; healerNina stands in front
        // of the apothecary cottage (west), merchantDale by the fruit stall just south of it; smithBram (world-map-2 step 1, B's catalog) stands in
        // the forge's open apron; villagerMia and childTim are flavour villagers in the open plaza.
        static readonly MapNpc[] NpcList =
        {
            new MapNpc("elderRowan", 16, 7), new MapNpc("guardRook", 20, 7),
            new MapNpc("healerNina", 5, 7), new MapNpc("merchantDale", 9, 11),
            new MapNpc("smithBram", 27, 9),
            new MapNpc("villagerMia", 22, 11), new MapNpc("childTim", 12, 12),
        };

        static readonly Vector2Int PlayerStartTile = new Vector2Int(20, 12);
        /// <summary>
        /// South gate (row 21, two tiles either side of the painted path's centre): to Willowbrook Meadows' west gate, landing two tiles in from
        /// its portal at (2,23) (World Map 2.0 step 1, 13/11/2026: agent A's WillowbrookMeadowsMap.cs placeholder arrival (16,19) in this map must
        /// be corrected to (13,20)/(14,20), one row north of this gate, now that the real tiles are known).
        /// East gate (the bridge, reaching the map edge at row 10-11): unchanged from the old layout, to Thatchwick Village's west road.
        /// </summary>
        static readonly MapPortal[] PortalList =
        {
            new MapPortal(13, 21, WillowbrookMeadowsMap.MapId, 2, 23), new MapPortal(14, 21, WillowbrookMeadowsMap.MapId, 2, 23),
            new MapPortal(35, 10, ThatchVillageMap.MapId, 2, 8), new MapPortal(35, 11, ThatchVillageMap.MapId, 2, 9),
        };

        public OakhavenVillageMap() : base(Rows, "Tw")
        {
            foreach (MapNpc n in NpcList) BlockTiles(n.Tx, n.Ty, 1, 1);
        }

        public override string Id { get { return MapId; } }
        public override string Name { get { return "Oakhaven Village"; } }
        public override string GroundTexture { get { return "Sprites/village/tiles/oakhaven-ground"; } }
        public override string Music { get { return "oakhaven-village"; } }
        public override bool Hostile { get { return false; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }
        public override MapPortal[] Portals { get { return PortalList; } }
        public override MapNpc[] Npcs { get { return NpcList; } }
    }
}
