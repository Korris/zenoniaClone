using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// PropDefs of Scorpion Gorge (cut from the ChatGPT prop sheet by Tools/cut-scorpion-gorge-props.py, art in Sprites/village/props/sg*.png). VillagePropDefs.TryGet falls back to this table.
    /// Cacti, the dead tree and the standing stones (pillar, arch, stele, scorpion statue, lantern post, crate) block their base; the rock piles block a 2-wide base.
    /// The tumbleweed, rubble, ribcage, skull, pot, campfire and thorn bush are walk-through (FootH 0) and lie under the hero like ground.
    /// </summary>
    public static class ScorpionGorgePropDefs
    {
        static readonly Dictionary<string, PropDef> Defs = new Dictionary<string, PropDef>
        {
            { "sgCactusTall", new PropDef(2.5f, 3.25f, 1, 1) },
            { "sgCactusSmall", new PropDef(1.5f, 1.75f, 1, 1) },
            { "sgBarrelCactus", new PropDef(1.5f, 1.5f, 1, 1) },
            { "sgDeadTree", new PropDef(3.5f, 3.25f, 1, 1) },
            { "sgTumbleweed", new PropDef(1.5f, 1.5f, 1, 0) },
            { "sgRockPileBig", new PropDef(3.5f, 3.25f, 2, 1) },
            { "sgRockPileMed", new PropDef(3.0f, 2.0f, 2, 1) },
            { "sgStonePillar", new PropDef(2.0f, 3.25f, 1, 1) },
            { "sgStoneArch", new PropDef(4.0f, 2.5f, 2, 1) },
            { "sgRubble", new PropDef(2.5f, 1.25f, 1, 0) },
            { "sgRibcage", new PropDef(2.5f, 2.0f, 2, 0) },
            { "sgBullSkull", new PropDef(2.5f, 2.0f, 1, 0) },
            { "sgStele", new PropDef(3.0f, 3.25f, 1, 1) },
            { "sgScorpionStatue", new PropDef(3.0f, 3.0f, 1, 1) },
            { "sgBrokenPot", new PropDef(2.5f, 1.75f, 1, 0) },
            { "sgTent", new PropDef(4.0f, 3.0f, 2, 1) },
            { "sgCampfire", new PropDef(2.5f, 1.5f, 1, 0) },
            { "sgLanternPost", new PropDef(2.5f, 3.25f, 1, 1) },
            { "sgCrateSacks", new PropDef(3.0f, 2.25f, 1, 1) },
            { "sgThornBush", new PropDef(2.5f, 2.0f, 1, 0) },
        };

        public static bool TryGet(string type, out PropDef def) { return Defs.TryGetValue(type, out def); }
    }
}
