using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>Size of a village prop in tiles (sprite) and its solid footprint (web prop-definitions.js def(w, h, footprintW, footprintH)).</summary>
    public struct PropDef
    {
        public float W, H;      // sprite box in tiles; the PNG is W*64 x H*64 px (4 px per world unit)
        public int FootW, FootH; // solid footprint in tiles, anchored on the bottom-left footprint tile
        public PropDef(float w, float h, int fw, int fh) { W = w; H = h; FootW = fw; FootH = fh; }
    }

    /// <summary>
    /// Oakhaven Village prop table, copied from the web PROP_DEFINITIONS. Buildings block a 2-row base so the painted doorstep in front stays
    /// walkable; trees block only their trunk tile so the canopy can be walked behind. All village props are solid (the web's rowboat sits on water).
    /// </summary>
    public static class VillagePropDefs
    {
        static readonly Dictionary<string, PropDef> Defs = new Dictionary<string, PropDef>
        {
            { "elderHouse", new PropDef(5.75f, 5f, 6, 2) }, { "cottageRed", new PropDef(3.5f, 3.75f, 3, 2) },
            { "hunterLodge", new PropDef(4.25f, 3.75f, 3, 2) }, { "healerCottage", new PropDef(4.75f, 3.75f, 5, 2) },
            { "carpenterWorkshop", new PropDef(6f, 3.5f, 6, 2) }, { "well", new PropDef(2.25f, 2.5f, 2, 1) },
            { "lanternPost", new PropDef(1.25f, 2.5f, 1, 1) }, { "marketStall", new PropDef(2.75f, 2.5f, 3, 1) },
            { "scarecrow", new PropDef(1.75f, 2f, 1, 1) }, { "woodpile", new PropDef(2.25f, 1.875f, 2, 1) },
            { "campfire", new PropDef(1.75f, 1.375f, 1, 1) }, { "barrel", new PropDef(1f, 1.25f, 1, 1) },
            { "crate", new PropDef(1.25f, 1.125f, 1, 1) }, { "rowboat", new PropDef(1.25f, 2.25f, 1, 2) },
            { "villageFence", new PropDef(2f, 1f, 2, 1) }, { "villageOak", new PropDef(3.5f, 3.75f, 1, 1) },
            { "villagePine", new PropDef(2.25f, 3.75f, 1, 1) }, { "villageBush", new PropDef(1.75f, 1.75f, 1, 1) },
            { "villageFlowerBush", new PropDef(2f, 1.75f, 1, 1) }, { "villageSignpost", new PropDef(1.5f, 2.25f, 1, 1) },
        };

        public static bool TryGet(string type, out PropDef def) { return Defs.TryGetValue(type, out def); }
    }
}
