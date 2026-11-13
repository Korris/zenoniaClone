using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// PropDefs of Sunbreak Foothills (cut from the ChatGPT prop sheet by Tools/cut-sunbreak-foothills-props.py, art in Sprites/village/props/sb*.png). VillagePropDefs.TryGet falls back to this table.
    /// Trees block their trunk tile so the hero walks behind the canopy; rock piles and sandstone outcrops block a 2-wide base, the cairn and the marker post their own tile.
    /// Grass, bushes, the skull, the log and the stream stones are walk-through (FootH 0, FootW kept so the sprite stays centred) and lie under the hero like ground.
    /// </summary>
    public static class SunbreakFoothillsPropDefs
    {
        static readonly Dictionary<string, PropDef> Defs = new Dictionary<string, PropDef>
        {
            { "sbPine", new PropDef(4.0f, 4.5f, 1, 1) },
            { "sbPineLean", new PropDef(3.5f, 4.5f, 1, 1) },
            { "sbPineSnow", new PropDef(3.5f, 4.5f, 1, 1) },
            { "sbPineSmall", new PropDef(2.5f, 3.5f, 1, 1) },
            { "sbJuniper", new PropDef(4.0f, 3.0f, 1, 1) },
            { "sbDeadTree", new PropDef(3.5f, 3.25f, 1, 1) },
            { "sbGrassTuft", new PropDef(2.5f, 2.0f, 1, 0) },
            { "sbBush", new PropDef(2.5f, 1.75f, 1, 0) },
            { "sbLupine", new PropDef(2.0f, 1.75f, 1, 0) },
            { "sbThornBush", new PropDef(2.0f, 1.5f, 1, 0) },
            { "sbRockMossy", new PropDef(3.5f, 2.5f, 2, 1) },
            { "sbRocks", new PropDef(3.0f, 1.75f, 2, 1) },
            { "sbRockSnow", new PropDef(3.0f, 1.75f, 2, 1) },
            { "sbSandstoneTall", new PropDef(3.5f, 3.0f, 2, 1) },
            { "sbSandstoneLow", new PropDef(3.5f, 1.75f, 2, 1) },
            { "sbCairn", new PropDef(2.5f, 2.0f, 1, 1) },
            { "sbMarkerPost", new PropDef(2.5f, 2.5f, 1, 1) },
            { "sbIbexSkull", new PropDef(2.0f, 1.5f, 1, 0) },
            { "sbLog", new PropDef(3.0f, 1.75f, 2, 0) },
            { "sbStreamStones", new PropDef(2.5f, 1.25f, 1, 0) },
        };

        public static bool TryGet(string type, out PropDef def) { return Defs.TryGetValue(type, out def); }
    }
}
