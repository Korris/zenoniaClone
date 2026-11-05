namespace ZenoniaProto
{
    /// <summary>
    /// Bushes, rocks, stumps and hollow logs spread over the new meadows (outside the old 36x24 section, off the roads and 1 tile away from the water), so the open fields are not bare.
    /// Placed by a seeded random pass with a minimum spacing, then frozen into this table: edit the rows freely. Only boulders block (3 tiles); bushes, rocks, stumps and hollow logs are walk-through (FootH 0). Nothing solid stands on a road or trail or inside a spawn cluster rect (WillowbrookLargeMapTests); the rows at the end sit one tile beside the walking lanes.
    /// </summary>
    public static partial class WillowbrookMeadowsProps
    {
        static readonly MapProp[] Scatter =
        {
            P("wbxBoulder", 11, 43), P("wbxBoulder", 58, 11), P("wbxBoulder", 59, 12),
            P("wbxBush", 5, 43), P("wbxBush", 39, 29), P("wbxBush", 45, 7),
            P("wbxBush", 50, 11), P("wbxBush", 57, 43), P("wbxBush", 59, 38), 
            P("wbxBush", 67, 7), P("wbxBush", 68, 40),
            P("wbxBushLilac", 11, 4), P("wbxBushLilac", 13, 7), P("wbxBushLilac", 27, 4), P("wbxBushLilac", 40, 23),
            P("wbxBushLilac", 43, 27), P("wbxBushLilac", 52, 17), P("wbxBushLilac", 56, 16), P("wbxBushLilac", 60, 25), P("wbxBushLilac", 61, 19),
            P("wbxBushWhite", 3, 6), P("wbxBushWhite", 20, 8), P("wbxBushWhite", 27, 8), 
            P("wbxBushWhite", 36, 24), P("wbxBushWhite", 36, 9), P("wbxBushWhite", 49, 35), P("wbxBushWhite", 53, 42),
            P("wbxLogHollow", 10, 39), P("wbxLogHollow", 50, 38),
            P("wbxRock", 46, 10), P("wbxRock", 47, 30), P("wbxRock", 51, 29), P("wbxRock", 54, 36),
            P("wbxRock", 58, 33), P("wbxRock", 66, 11),
            P("wbxRockTiny", 28, 12), P("wbxRockTiny", 40, 19), P("wbxRockTiny", 46, 14), P("wbxRockTiny", 53, 13), P("wbxRockTiny", 55, 32),
            P("wbxRockTiny", 63, 12),
            P("wbxRocks", 5, 39), P("wbxRocks", 13, 12), P("wbxRocks", 25, 40), P("wbxRocks", 30, 39),
            P("wbxRocks", 48, 24), P("wbxRocks", 64, 36),
            P("wbxStump", 17, 10), P("wbxStump", 49, 43), 
            // Beside the roads and trails (one tile off the walking lane): dressing that never blocks the way
            P("wbxRockTiny", 56, 3), P("wbxBoulder", 52, 4), P("wbxBushWhite", 18, 5), P("wbxLogHollow", 27, 8), P("wbxBushLilac", 34, 9),
            P("wbxBushLilac", 20, 10), P("wbxRockTiny", 37, 11), P("wbxRockTiny", 12, 13), P("wbxBushLilac", 50, 13), P("wbxStump", 56, 13),
            P("wbxStump", 33, 15), P("wbxRockTiny", 38, 18), P("wbxLogHollow", 6, 20), P("wbxRocks", 16, 20), P("wbxLogHollow", 37, 25),
            P("wbxRock", 26, 26), P("wbxRock", 33, 26), P("wbxBush", 43, 26), P("wbxRock", 48, 26), P("wbxBushLilac", 67, 26),
            P("wbxBush", 1, 27), P("wbxRock", 7, 27), P("wbxBushWhite", 63, 27), P("wbxBush", 58, 28), P("wbxRock", 51, 29),
            P("wbxBushLilac", 4, 30), P("wbxRocks", 55, 31), P("wbxBushWhite", 11, 33), P("wbxBushLilac", 10, 37), P("wbxBush", 45, 37),
            P("wbxRocks", 4, 38), P("wbxBushLilac", 53, 38), P("wbxLogHollow", 8, 41), P("wbxRocks", 51, 43),
        };
    }
}
