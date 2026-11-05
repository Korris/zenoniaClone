namespace ZenoniaProto
{
    /// <summary>
    /// The structure of the 72x48 map's new areas: the forest border (top, west, east, bottom), the two tree belts that split the hunting fields, the ruins on the paved north-east,
    /// reeds on the pond bank and the barricade that ends the main road at the east edge. Trees: trunk tile only (rows 0-1 and cols 70-71 are solid border anyway).
    /// </summary>
    public static partial class WillowbrookMeadowsProps
    {
        static readonly MapProp[] Frame =
        {
            // Top border: trunks on rows 1-3, canopies run off the map edge. Slim trees over the paved ruins so the paving stays visible.
            P("wbxOakBig", 3, 4), P("wbxPine", 7, 2), P("wbxOakMid", 11, 3), P("wbxPineSlim", 15, 2), P("wbxOakSmall", 18, 3), P("wbxPine", 21, 2),
            P("wbxOakMid", 28, 3), P("wbxPineSlim", 32, 2), P("wbxOakBig", 36, 3), P("wbxPine", 40, 2), P("wbxOakSmall", 44, 2), P("wbxPineSlim", 48, 1),
            P("wbxOakSlim", 52, 1), P("wbxPine", 56, 1), P("wbxPineSlim", 60, 1), P("wbxOakSlim", 64, 1), P("wbxOakMid", 68, 3),
            // West border above the old section (the old section carries its own trees below row 12).
            P("wbxOakMid", 1, 6), P("wbxPine", 2, 9), P("wbxOakSmall", 1, 12),
            // East border: trees behind the barricade road end and along the edge.
            P("wbxOakMid", 70, 7), P("wbxPine", 69, 11), P("wbxOakBig", 70, 16), P("wbxPineSlim", 69, 19), P("wbxPine", 70, 22), P("wbxOakSmall", 69, 27),
            P("wbxOakBig", 70, 32), P("wbxPine", 69, 36), P("wbxOakMid", 70, 41),
            // Bottom border, rows 45-46 sit under the HUD: low things and a few trees.
            P("wbxOakBig", 4, 46), P("wbxBush", 11, 45), P("wbxPine", 16, 46), P("wbxBushWhite", 23, 45), P("wbxOakMid", 29, 46), P("wbxBush", 38, 45),
            P("wbxPineSlim", 51, 46), P("wbxBushLilac", 58, 45), P("wbxOakBig", 64, 46), P("wbxPine", 69, 46),
            // Tree belt west (col 37-38): between the stream fields and the middle meadows, with a gap at rows 9-11.
            P("wbxOakSmall", 37, 5), P("wbxPine", 38, 7), P("wbxOakSlim", 37, 9),
            P("wbxOakMid", 37, 13), P("wbxPineSlim", 38, 15), P("wbxOakSmall", 37, 17), P("wbxPine", 38, 19),
            // Tree belt east (col 57-58): rows 12-20 and 27-38.
            P("wbxOakMid", 57, 13), P("wbxPine", 58, 15), P("wbxOakSlim", 57, 17), P("wbxOakSmall", 58, 20),
            P("wbxPine", 57, 28), P("wbxOakMid", 58, 30), P("wbxPineSlim", 57, 32), P("wbxOakSmall", 58, 34), P("wbxPine", 57, 36), P("wbxOakMid", 58, 38),
            // Ruins on the paving (cols 43-68, rows 2-9).
            P("wbxWallCorner", 46, 7), P("wbxPillarTall", 49, 5), P("wbxPillarShort", 51, 8), P("wbxWallLow", 53, 6), P("wbxPillarTall", 56, 4),
            P("wbxWallLow", 58, 8), P("wbxPillarShort", 60, 6), P("wbxPillarTall", 62, 4), P("wbxWallCorner", 64, 8), P("wbxPillarShort", 66, 5),
            P("wbxPillarShort", 44, 5), P("wbxWallLow", 47, 3),
            // Pond and stream reeds (decoration, no footprint).
            P("wbxReeds", 34, 40), P("wbxReeds", 33, 43), P("wbxReeds", 36, 38), P("wbxReeds", 45, 38), P("wbxReeds", 47, 41), P("wbxReeds", 46, 44),
            P("wbxReeds", 41, 44), P("wbxReeds", 38, 44), P("wbxReeds", 36, 31), P("wbxReeds", 40, 34), P("wbxReeds", 43, 36),
            // East dead end: the barricade across the main road (rows 22-24, cols 66-69) with rocks and bushes closing the rows around it.
            P("wbxBarricade", 66, 24), P("wbxBoulder", 67, 21), P("wbxBoulder", 66, 27), P("wbxRock", 68, 20), P("wbxRock", 68, 26),
        };
    }
}
