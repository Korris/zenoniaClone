namespace ZenoniaProto
{
    /// <summary>
    /// The structure of the 72x48 map's new areas: the ruins on the paved north-east, reeds on the pond bank, the barricade that ends the main road at the east edge
    /// and low bushes on the bottom border. The trees live in WillowbrookMeadowsForestProps (2.5D forest, 21/11/2026).
    /// </summary>
    public static partial class WillowbrookMeadowsProps
    {
        static readonly MapProp[] Frame =
        {
            // Trees (forest belts, borders) moved to WillowbrookMeadowsForestProps (2.5D forest, 21/11/2026). Bottom border, rows 45-46 under the HUD: low bushes.
            P("wbxBush", 11, 45), P("wbxBushWhite", 23, 45), P("wbxBush", 38, 45), P("wbxBushLilac", 58, 45),
            P("wbxBush", 2, 22), // on the solid border tiles just north of the west gate (the old cliff there is gone)
            // Ruins on the paving (cols 43-68, rows 2-9).
            P("wbxWallCorner", 46, 11), P("wbxPillarTall", 49, 3), P("wbxPillarShort", 52, 6), P("wbxWallLow", 53, 5), P("wbxPillarTall", 56, 4),
            P("wbxWallLow", 55, 8), P("wbxPillarShort", 60, 2), P("wbxPillarTall", 62, 2), P("wbxWallCorner", 64, 11), P("wbxPillarShort", 68, 5),
            P("wbxPillarShort", 44, 3), P("wbxWallLow", 47, 3),
            // Pond and stream reeds (decoration, no footprint).
            P("wbxReeds", 34, 40), P("wbxReeds", 33, 43), P("wbxReeds", 36, 38), P("wbxReeds", 45, 38), P("wbxReeds", 47, 41), P("wbxReeds", 46, 44),
            P("wbxReeds", 41, 44), P("wbxReeds", 38, 44), P("wbxReeds", 36, 31), P("wbxReeds", 40, 34), P("wbxReeds", 43, 36),
            // East dead end: the barricade across the main road (rows 22-24, cols 66-69) with rocks and bushes closing the rows around it.
            P("wbxBarricade", 66, 24), P("wbxBoulder", 67, 25), P("wbxBoulder", 66, 27), P("wbxRock", 68, 20), P("wbxRock", 68, 26),
        };
    }
}
