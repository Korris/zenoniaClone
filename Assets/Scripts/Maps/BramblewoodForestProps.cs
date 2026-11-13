namespace ZenoniaProto
{
    /// <summary>
    /// Props of Bramblewood Forest (36x24), one entry per prop: [type, tx, ty] = bottom-left footprint tile (MapScenery y-sorts by it; PropDefs in BramblewoodForestPropDefs). Placed after the
    /// concept painting (Assets/ArtLibrary/maps/bramblewood-forest/bramblewood-forest-chatgpt-1.png): trees on the rims of the three clearings, rocks and reeds along the stream (whose painted
    /// stone banks the ground-only version lost), brambles and flowers at the forest edge. Nothing solid stands on a road, the bridge or inside a spawn rect (MapSpawnTable.BramblewoodForest);
    /// Tools/preview-bramblewood-rootburrow-composite.py checks that. The brambles at (34,12)/(34,13) cover the east road end, which the rows block (no map there yet).
    /// </summary>
    public static class BramblewoodForestProps
    {
        static MapProp P(string type, int tx, int ty) { return new MapProp(type, tx, ty); }

        public static readonly MapProp[] All =
        {
            // Trees (trunk tile solid): beside the top road, between road and stream, on the rims of the clearings.
            P("bwOakSmall", 14, 10), P("bwOakMid", 19, 9), P("bwOakSmall", 20, 5), P("bwOakSmall", 23, 10), P("bwOakMid", 10, 16), P("bwOakSmall", 14, 19), P("bwOakMid", 29, 17),
            // Big rocks (2-wide base solid): clearing corners, the foot of the waterfall cliff.
            P("bwRockBlocks", 6, 3), P("bwRockBlocks", 4, 7), P("bwRockBlocks", 12, 11), P("bwBoulder", 27, 5), P("bwBoulder", 27, 19),
            // Stream banks, north arm (walk-through; most stand on the water tiles themselves).
            P("bwRock", 24, 4), P("bwRockSmall", 22, 5), P("bwRock", 21, 6), P("bwReeds", 23, 6), P("bwRockSmall", 20, 7), P("bwReeds", 20, 8), P("bwRock", 22, 9), P("bwRockSmall", 19, 10), P("bwRock", 22, 10),
            // Stream banks, south arm.
            P("bwRockSmall", 15, 14), P("bwReeds", 16, 14), P("bwRock", 17, 15), P("bwRock", 13, 15), P("bwReeds", 14, 16), P("bwRockSmall", 13, 17), P("bwRockSmall", 11, 17), P("bwRock", 9, 18),
            P("bwReeds", 10, 18), P("bwReeds", 12, 18), P("bwRockSmall", 11, 19), P("bwReeds", 8, 19), P("bwRock", 7, 20),
            // Brambles at the forest edge, and across the dead east road end.
            P("bwBrambleMid", 14, 2), P("bwBrambleBig", 5, 4), P("bwBrambleMid", 4, 8), P("bwBrambleMid", 10, 11), P("bwBrambleMid", 32, 6), P("bwBrambleSmall", 23, 14), P("bwBrambleBig", 6, 17),
            P("bwBrambleMid", 13, 20), P("bwBrambleSmall", 27, 20), P("bwBrambleMid", 33, 11), P("bwBrambleBig", 34, 12), P("bwBrambleBig", 34, 13),
            // Bushes, ferns, stumps, the hollow log, mushrooms, loose rocks.
            P("bwFern", 5, 6), P("bwFern", 33, 10), P("bwBush", 6, 11), P("bwBush", 19, 1), P("bwBush", 32, 8), P("bwBush", 8, 14), P("bwStump", 12, 19), P("bwStump", 8, 15),
            P("bwLogHollow", 30, 15), P("bwMushrooms", 13, 7), P("bwMushrooms", 5, 16), P("bwMushrooms", 31, 16), P("bwRock", 15, 3), P("bwRock", 8, 11),
            // Flowers.
            P("bwFlowers", 10, 3), P("bwFlowers", 16, 2), P("bwFlowers", 22, 3), P("bwFlowers", 29, 5), P("bwFlowers", 33, 9), P("bwFlowers", 18, 15), P("bwFlowers", 24, 14), P("bwFlowers", 28, 14),
            P("bwFlowers", 30, 16), P("bwFlowers", 9, 20), P("bwLupine", 5, 10), P("bwLupine", 13, 11), P("bwLupine", 19, 6), P("bwLupine", 23, 3), P("bwLupine", 31, 15), P("bwLupine", 8, 16),
            P("bwLupine", 5, 15), P("bwLupine", 28, 18),
        };
    }
}
