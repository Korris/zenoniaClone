namespace ZenoniaProto
{
    /// <summary>
    /// Props of Scorpion Gorge (48x32), one P(type, tx, ty) per prop: tx / ty = bottom-left footprint tile (MapScenery y-sorts by it; defs in ScorpionGorgePropDefs).
    /// Everything stands at a field edge, a cliff foot, a map corner or the gate buffer: nothing on a road, a bridge, the temple steps or inside a spawn rect of
    /// MapSpawnTable.ScorpionGorge (Tools/preview-scorpion-gorge-composite.py prints PROBLEM when one does). Cacti and a small camp by the west gate (Amber
    /// Dunes side); bones, pillars and the scorpion / sun stele around the crossroad and the temple; rock piles and the broken pot towards the dry riverbed;
    /// thorn bush and a pot near the Mirewatch wetland edge.
    /// </summary>
    public static class ScorpionGorgeProps
    {
        static MapProp P(string type, int tx, int ty) { return new MapProp(type, tx, ty); }

        public static readonly MapProp[] All =
        {
            // West gate buffer (Amber Dunes side): a small camp, clear of the road and of zones 1 / 2.
            P("sgCactusTall", 2, 12), P("sgCactusSmall", 5, 11), P("sgBarrelCactus", 6, 12), P("sgTent", 4, 17),
            P("sgCampfire", 6, 16), P("sgTumbleweed", 3, 16), P("sgDeadTree", 7, 17), P("sgRubble", 2, 16),
            // South-central field, between the south bridge and the boss approach (outside every zone).
            P("sgRockPileMed", 12, 24), P("sgStonePillar", 15, 26), P("sgThornBush", 13, 27), P("sgTumbleweed", 16, 23),
            P("sgRibcage", 11, 22), P("sgBullSkull", 10, 21),
            // Crossroad plaza around the temple approach (clear of the path forks).
            P("sgLanternPost", 27, 12), P("sgBrokenPot", 20, 13),
            // North-east field, past zones 5 / 6 towards the Mirewatch wetland.
            P("sgRockPileMed", 43, 4), P("sgStonePillar", 44, 7), P("sgBullSkull", 43, 9),
            // Boss arena approach, outside zones 7 / 8 and the brick path.
            P("sgScorpionStatue", 26, 21), P("sgRockPileBig", 43, 22), P("sgStele", 44, 26), P("sgBarrelCactus", 31, 20),
            // Mirewatch wetland edge (east side): keep clear of the puddle tiles.
            P("sgThornBush", 44, 16), P("sgTumbleweed", 47, 13), P("sgRubble", 44, 13),
        };
    }
}
