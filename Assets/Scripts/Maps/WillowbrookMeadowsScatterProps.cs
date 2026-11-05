namespace ZenoniaProto
{
    /// <summary>
    /// Bushes, rocks, stumps and hollow logs spread over the new meadows (outside the old 36x24 section, off the roads and 1 tile away from the water), so the open fields are not bare.
    /// Placed by a seeded random pass with a minimum spacing, then frozen into this table: edit the rows freely. Boulders, rocks and logs block their base row, bushes and stumps one tile.
    /// </summary>
    public static partial class WillowbrookMeadowsProps
    {
        static readonly MapProp[] Scatter =
        {
            P("wbxBoulder", 11, 43), P("wbxBoulder", 58, 9), P("wbxBoulder", 59, 12),
            P("wbxBush", 5, 43), P("wbxBush", 22, 39), P("wbxBush", 39, 29), P("wbxBush", 40, 14), P("wbxBush", 45, 7),
            P("wbxBush", 50, 11), P("wbxBush", 57, 43), P("wbxBush", 59, 38), P("wbxBush", 60, 42), P("wbxBush", 62, 7),
            P("wbxBush", 62, 32), P("wbxBush", 63, 40), P("wbxBush", 67, 7), P("wbxBush", 68, 40),
            P("wbxBushLilac", 11, 4), P("wbxBushLilac", 13, 7), P("wbxBushLilac", 18, 40), P("wbxBushLilac", 27, 4), P("wbxBushLilac", 40, 23),
            P("wbxBushLilac", 43, 27), P("wbxBushLilac", 52, 17), P("wbxBushLilac", 56, 16), P("wbxBushLilac", 60, 25), P("wbxBushLilac", 61, 19),
            P("wbxBushWhite", 3, 6), P("wbxBushWhite", 7, 8), P("wbxBushWhite", 20, 8), P("wbxBushWhite", 27, 8), P("wbxBushWhite", 34, 37),
            P("wbxBushWhite", 36, 24), P("wbxBushWhite", 36, 9), P("wbxBushWhite", 43, 12), P("wbxBushWhite", 49, 35), P("wbxBushWhite", 53, 42),
            P("wbxLogHollow", 10, 39), P("wbxLogHollow", 37, 35), P("wbxLogHollow", 50, 38),
            P("wbxRock", 21, 43), P("wbxRock", 46, 10), P("wbxRock", 47, 30), P("wbxRock", 51, 29), P("wbxRock", 54, 36),
            P("wbxRock", 58, 33), P("wbxRock", 66, 11),
            P("wbxRockTiny", 28, 12), P("wbxRockTiny", 40, 19), P("wbxRockTiny", 46, 14), P("wbxRockTiny", 53, 13), P("wbxRockTiny", 55, 32),
            P("wbxRockTiny", 60, 29), P("wbxRockTiny", 62, 15), P("wbxRockTiny", 63, 12),
            P("wbxRocks", 5, 39), P("wbxRocks", 13, 12), P("wbxRocks", 14, 42), P("wbxRocks", 25, 40), P("wbxRocks", 30, 39),
            P("wbxRocks", 30, 6), P("wbxRocks", 48, 24), P("wbxRocks", 64, 36),
            P("wbxStump", 2, 11), P("wbxStump", 17, 10), P("wbxStump", 49, 43), P("wbxStump", 68, 4),
        };
    }
}
