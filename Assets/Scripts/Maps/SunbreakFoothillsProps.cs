namespace ZenoniaProto
{
    /// <summary>
    /// Props of Sunbreak Foothills (48x32), one P(type, tx, ty) per prop: tx / ty = bottom-left footprint tile (MapScenery y-sorts by it; defs in SunbreakFoothillsPropDefs).
    /// Everything stands at a field edge, a cliff foot or a map corner: nothing on a road, a bridge, a stair or inside a spawn rect of MapSpawnTable.SunbreakFoothills
    /// (Tools/preview-sunbreak-foothills-composite.py prints PROBLEM when one does). Pines on the cold west side, sandstone on the east. Bushes, grass, skull, log and stream stones never block.
    /// </summary>
    public static class SunbreakFoothillsProps
    {
        static MapProp P(string type, int tx, int ty) { return new MapProp(type, tx, ty); }

        public static readonly MapProp[] All =
        {
            // West road dead end: two rock piles close the road two tiles before the map edge (col 0 is solid anyway).
            P("sbRockSnow", 1, 17), P("sbRockMossy", 1, 18),
            // North-west field (cliff foot, north of the harpy rect) and the cave approach
            P("sbPine", 10, 3), P("sbPineSnow", 13, 3), P("sbPineLean", 8, 5), P("sbPineSmall", 10, 9), P("sbPine", 19, 7), P("sbIbexSkull", 12, 9), P("sbLupine", 13, 10),
            P("sbPineSnow", 27, 4), P("sbPine", 30, 3), P("sbMarkerPost", 25, 6), P("sbBush", 27, 8), P("sbLupine", 29, 8), P("sbGrassTuft", 26, 9),
            // Field west of the stream
            P("sbPineSnow", 3, 13), P("sbCairn", 11, 14), P("sbStreamStones", 13, 11), P("sbLupine", 15, 11),
            // South-west field (snow side)
            P("sbPineSmall", 2, 20), P("sbGrassTuft", 3, 22), P("sbPine", 2, 23), P("sbPineSnow", 2, 25), P("sbPineSnow", 6, 27), P("sbRockSnow", 8, 28), P("sbPineSmall", 12, 27),
            P("sbPine", 15, 26), P("sbDeadTree", 17, 23), P("sbGrassTuft", 16, 22), P("sbLog", 7, 19), P("sbBush", 14, 19), P("sbBush", 18, 27),
            // South road and the wedge by the east bridge
            P("sbMarkerPost", 23, 25), P("sbBush", 24, 22), P("sbGrassTuft", 25, 22), P("sbStreamStones", 22, 18),
            // Central field: the strip between the two spawn rects and its corners
            P("sbJuniper", 24, 12), P("sbGrassTuft", 17, 11), P("sbRockMossy", 29, 14), P("sbPineSmall", 27, 15), P("sbBush", 34, 14),
            // Boss plateau: corners only, the arena stays clear
            P("sbCairn", 33, 2), P("sbSandstoneTall", 44, 2), P("sbThornBush", 34, 6), P("sbIbexSkull", 41, 6), P("sbCairn", 35, 10),
            // East side (sandstone): the pockets north of the east road, the edges of the east field, the sand by the end of the stream
            P("sbSandstoneLow", 40, 12), P("sbSandstoneTall", 43, 11), P("sbThornBush", 40, 9), P("sbThornBush", 42, 14), P("sbMarkerPost", 45, 14),
            P("sbGrassTuft", 31, 20), P("sbGrassTuft", 38, 18), P("sbThornBush", 39, 18), P("sbSandstoneTall", 44, 21), P("sbThornBush", 44, 24),
            P("sbSandstoneLow", 37, 25), P("sbDeadTree", 40, 27), P("sbStreamStones", 33, 26), P("sbStreamStones", 35, 28),
            // Trees on the cliff tops that frame the map (solid cliff tiles already)
            P("sbPineSnow", 1, 3), P("sbPineSnow", 7, 1), P("sbPine", 14, 1), P("sbPineSnow", 19, 2), P("sbPine", 28, 1), P("sbPineSnow", 1, 9), P("sbPine", 0, 22),
        };
    }
}
