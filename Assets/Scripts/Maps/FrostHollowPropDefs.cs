using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// PropDefs of Frost Hollow (cut from the ChatGPT prop sheet by Tools/cut-frost-hollow-props.py, art in Sprites/village/props/fh*.png). VillagePropDefs.TryGet falls back to this table.
    /// Trees block their trunk tile so the hero walks behind the canopy; big crystals, rune stones, the campfire and short towers block their base tile, the boulder, fence and tall tower a 2-wide base,
    /// the tent a 3x2 base. Small crystals, rocks, saplings, stumps, bones and the ribcage are walk-through (FootH 0, FootW kept so the sprite stays centred): they never block a path.
    /// </summary>
    public static class FrostHollowPropDefs
    {
        static readonly Dictionary<string, PropDef> Defs = new Dictionary<string, PropDef>
        {
            { "fhPineBig", new PropDef(3.75f, 5.75f, 1, 1) },
            { "fhPineMid", new PropDef(2.75f, 4f, 1, 1) },
            { "fhPineSlim", new PropDef(2f, 2.75f, 1, 1) },
            { "fhPineSapling", new PropDef(1.5f, 2.5f, 1, 0) },
            { "fhDeadTree", new PropDef(3.25f, 3.75f, 1, 1) },
            { "fhDeadTreeWide", new PropDef(3f, 3f, 1, 1) },
            { "fhCrystalBig", new PropDef(2.0f, 2.0f, 1, 1) },
            { "fhCrystalMid", new PropDef(1.5f, 1.5f, 1, 0) },
            { "fhCrystalSmall", new PropDef(1.25f, 1f, 1, 0) },
            { "fhRuneTall", new PropDef(1.5f, 1.5f, 1, 1) },
            { "fhRuneShort", new PropDef(1.5f, 1f, 1, 1) },
            { "fhBoulder", new PropDef(3.25f, 2.25f, 2, 1) },
            { "fhRock", new PropDef(1.75f, 1.5f, 1, 0) },
            { "fhRockSmall", new PropDef(1.5f, 1f, 1, 0) },
            { "fhStump", new PropDef(1.5f, 1f, 1, 0) },
            { "fhBones", new PropDef(1.5f, 1.25f, 1, 0) },
            { "fhRibcage", new PropDef(2.0f, 1.75f, 2, 0) },
            { "fhFence", new PropDef(2.0f, 1.5f, 2, 1) },
            { "fhTowerTall", new PropDef(2.0f, 2.75f, 2, 1) },
            { "fhTowerShort", new PropDef(1.5f, 1.75f, 1, 1) },
            { "fhTent", new PropDef(3.5f, 2.25f, 3, 2) },
            { "fhCampfire", new PropDef(2.0f, 1.5f, 1, 1) },
            // 2.5D (21/11/2026): standing ice-rock cliffs on solid tiles north of the snow (FrostHollowCliffProps, Tools/place-map-25d-dungeon-props.py frost-hollow).
            // They stand only on tiles the rows already block, so FootH 1 changes no collision; fhCliffLow lies flat south of the snow (walk-through).
            { "fhCliffA", new PropDef(2.5f, 2.75f, 2, 1) },
            { "fhCliffB", new PropDef(2.5f, 2.75f, 2, 1) },
            { "fhCliffWide", new PropDef(3.5f, 2.5f, 3, 1) },
            { "fhCliffTall", new PropDef(3f, 4f, 3, 1) },
            { "fhCliffEndL", new PropDef(2.25f, 2.75f, 1, 1) },
            { "fhCliffEndR", new PropDef(2.25f, 3f, 1, 1) },
            { "fhCliffPillar", new PropDef(1.5f, 2.5f, 1, 1) },
            { "fhCliffLow", new PropDef(3f, 1.75f, 3, 0) },
            { "fhIceCave", new PropDef(3.75f, 2.75f, 3, 1) },   // the ice-cave mouth on the east cliff, on the solid row north of the portal row (was painted in the old ground)
            { "fhRockSlab", new PropDef(2.25f, 1.25f, 2, 0) },
            { "fhLogSnow", new PropDef(2.75f, 1.5f, 2, 0) },
            { "fhBushSnow", new PropDef(1.5f, 1.25f, 1, 0) },
            // Frostfang Den 2.5D ice cave (21/11/2026, FrostfangDenProps from Tools/place-map-25d-dungeon-props.py frostfang-den): standing ice walls on solid tiles
            // north of the corridors (FootH 1 on blocked tiles: no collision change), crystals = DungeonLights by name, low pieces walk-through.
            { "fdIceWallA", new PropDef(3.25f, 3.25f, 3, 1) },
            { "fdIceWallB", new PropDef(3.25f, 3.25f, 3, 1) },
            { "fdCrystalWall", new PropDef(3.5f, 3.25f, 3, 1) },
            { "fdIcicleWall", new PropDef(2.5f, 3f, 2, 1) },
            { "fdIceWallEnd", new PropDef(1.75f, 2.75f, 1, 1) },
            { "fdIcePillar", new PropDef(1.5f, 3f, 1, 1) },
            { "fdCrystalTall", new PropDef(2f, 2.5f, 1, 1) },
            { "fdCrystalTeal", new PropDef(1.5f, 1.75f, 1, 1) },
            { "fdCrystalShards", new PropDef(1.25f, 1.25f, 1, 0) },
            { "fdIceBoulder", new PropDef(2.5f, 2f, 2, 1) },
            { "fdRockCluster", new PropDef(1.75f, 1.25f, 1, 0) },
            { "fdRuneMarker", new PropDef(1.5f, 1.5f, 1, 1) },
        };

        public static bool TryGet(string type, out PropDef def) { return Defs.TryGetValue(type, out def); }
    }
}
