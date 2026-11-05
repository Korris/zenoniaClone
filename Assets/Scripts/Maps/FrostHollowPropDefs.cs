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
            { "fhPineBig", new PropDef(3.5f, 4.25f, 1, 1) },
            { "fhPineMid", new PropDef(2.5f, 3.25f, 1, 1) },
            { "fhPineSlim", new PropDef(1.5f, 3.25f, 1, 1) },
            { "fhPineSapling", new PropDef(1.5f, 1.75f, 1, 0) },
            { "fhDeadTree", new PropDef(3.0f, 3.5f, 1, 1) },
            { "fhDeadTreeWide", new PropDef(3.5f, 3.25f, 1, 1) },
            { "fhCrystalBig", new PropDef(2.0f, 2.0f, 1, 1) },
            { "fhCrystalMid", new PropDef(1.0f, 1.5f, 1, 0) },
            { "fhCrystalSmall", new PropDef(1.0f, 1.0f, 1, 0) },
            { "fhRuneTall", new PropDef(1.0f, 2.5f, 1, 1) },
            { "fhRuneShort", new PropDef(1.0f, 1.75f, 1, 1) },
            { "fhBoulder", new PropDef(3.0f, 2.25f, 2, 1) },
            { "fhRock", new PropDef(1.5f, 1.5f, 1, 0) },
            { "fhRockSmall", new PropDef(1.0f, 1.0f, 1, 0) },
            { "fhStump", new PropDef(1.0f, 1.0f, 1, 0) },
            { "fhBones", new PropDef(1.5f, 1.25f, 1, 0) },
            { "fhRibcage", new PropDef(2.0f, 1.75f, 2, 0) },
            { "fhFence", new PropDef(2.0f, 1.5f, 2, 1) },
            { "fhTowerTall", new PropDef(2.0f, 2.75f, 2, 1) },
            { "fhTowerShort", new PropDef(1.5f, 1.75f, 1, 1) },
            { "fhTent", new PropDef(3.5f, 2.25f, 3, 2) },
            { "fhCampfire", new PropDef(2.0f, 1.5f, 1, 1) },
        };

        public static bool TryGet(string type, out PropDef def) { return Defs.TryGetValue(type, out def); }
    }
}
