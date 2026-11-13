using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// PropDefs of Bramblewood Forest (cut from the ChatGPT prop sheet by Tools/cut-bramblewood-props.py, art in Sprites/village/props/bw*.png). VillagePropDefs.TryGet falls back to this table.
    /// Trees block their trunk tile so the hero walks behind the canopy; the boulder and the stone blocks block a 2-wide base. Brambles, bushes, ferns, small rocks, the hollow log, the stump,
    /// mushrooms, flowers and reeds are walk-through (FootH 0, FootW kept so the sprite stays centred): they never block a path (docs/map-design-rules.md rule 3).
    /// </summary>
    public static class BramblewoodForestPropDefs
    {
        static readonly Dictionary<string, PropDef> Defs = new Dictionary<string, PropDef>
        {
            { "bwOakBig", new PropDef(5.0f, 5.0f, 1, 1) },
            { "bwOakVine", new PropDef(5.0f, 4.75f, 1, 1) },
            { "bwOakRoots", new PropDef(5.0f, 5.25f, 1, 1) },
            { "bwOakMid", new PropDef(4.0f, 4.25f, 1, 1) },
            { "bwOakSmall", new PropDef(3.0f, 3.25f, 1, 1) },
            { "bwBrambleBig", new PropDef(3.5f, 3.0f, 1, 0) },
            { "bwBrambleMid", new PropDef(2.5f, 2.25f, 1, 0) },
            { "bwBrambleSmall", new PropDef(2.5f, 1.75f, 1, 0) },
            { "bwBush", new PropDef(2.5f, 2.0f, 1, 0) },
            { "bwFern", new PropDef(3.0f, 2.0f, 1, 0) },
            { "bwBoulder", new PropDef(3.5f, 2.5f, 2, 1) },
            { "bwRockBlocks", new PropDef(3.0f, 2.25f, 2, 1) },
            { "bwRock", new PropDef(2.5f, 1.5f, 1, 0) },
            { "bwRockSmall", new PropDef(2.5f, 1.25f, 1, 0) },
            { "bwLogHollow", new PropDef(3.5f, 2.25f, 3, 0) },
            { "bwStump", new PropDef(2.5f, 1.5f, 1, 0) },
            { "bwMushrooms", new PropDef(1.5f, 1.5f, 1, 0) },
            { "bwLupine", new PropDef(2.5f, 2.25f, 1, 0) },
            { "bwFlowers", new PropDef(2.5f, 1.75f, 1, 0) },
            { "bwReeds", new PropDef(2.5f, 2.25f, 1, 0) },
        };

        public static bool TryGet(string type, out PropDef def) { return Defs.TryGetValue(type, out def); }
    }
}
