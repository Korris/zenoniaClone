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
    /// Prop table of every map: Oakhaven copied from the web PROP_DEFINITIONS, then the desert props. Buildings block a 2-row base so the painted doorstep in front stays
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
            // Sunspire Oasis
            { "residence", new PropDef(3.5f, 3.75f, 3, 2) }, { "merchantShop", new PropDef(5f, 4.25f, 4, 2) },
            { "blacksmithForge", new PropDef(5.25f, 4.25f, 4, 2) }, { "caravanInn", new PropDef(6.25f, 5f, 6, 2) },
            { "palmTree", new PropDef(3f, 4.25f, 1, 1) }, { "sunspireStall", new PropDef(3f, 2.75f, 3, 1) },
            { "fountainOrnament", new PropDef(2.25f, 2.5f, 2, 1) }, { "jarCluster", new PropDef(1.5f, 1.5f, 1, 1) },
            { "noticeBoard", new PropDef(2f, 2.25f, 2, 1) }, { "wagon", new PropDef(3.5f, 2.5f, 3, 1) },
            // Amber Dunes
            { "duneRock", new PropDef(2.75f, 2f, 2, 1) }, { "deadAcacia", new PropDef(3f, 3.5f, 1, 1) },
            { "bleachedBones", new PropDef(2f, 1.25f, 1, 1) }, { "cactusCluster", new PropDef(1.75f, 2f, 1, 1) },
            { "brokenWagon", new PropDef(3.75f, 2.5f, 3, 1) }, { "ruinPillar", new PropDef(2.5f, 3.25f, 1, 1) },
            // Frostfang Den
            { "icePillar", new PropDef(2.25f, 3.5f, 1, 1) }, { "frozenStalagmite", new PropDef(2f, 2.5f, 1, 1) },
            { "frostCrystal", new PropDef(2f, 2.25f, 1, 1) }, { "frozenBones", new PropDef(2.5f, 1.75f, 1, 1) },
            // Emberfen Marsh
            { "swampTree", new PropDef(3.5f, 4.25f, 1, 1) }, { "mangroveRoots", new PropDef(2.5f, 3f, 2, 1) },
            { "giantMushroom", new PropDef(2.25f, 3f, 1, 1) }, { "reedPatch", new PropDef(1.75f, 2.25f, 1, 1) },
            // Sunken Reliquary (swampLantern stands in the marsh too)
            { "mossyPillar", new PropDef(2.25f, 3.25f, 1, 1) }, { "brokenArch", new PropDef(4.5f, 2.75f, 4, 1) },
            { "swampLantern", new PropDef(1.5f, 2.5f, 1, 1) }, { "sunkenShrine", new PropDef(3.75f, 4f, 3, 2) },
            { "hydraNest", new PropDef(2.75f, 3.25f, 2, 1) },
            // Willowbrook Meadows (cut from the painting by Tools/build-willowbrook-props.py; wbPalisade has no footprint, the map rows block it)
            { "wbOakNW", new PropDef(6.75f, 7f, 1, 1) }, { "wbCliffNW", new PropDef(5.5f, 3f, 2, 1) }, { "wbRockNW", new PropDef(3f, 1.75f, 2, 1) },
            { "wbRockNW2", new PropDef(2.5f, 2f, 2, 1) }, { "wbOakTop2", new PropDef(7.25f, 3f, 1, 1) }, { "wbOakClumpW", new PropDef(7.25f, 6f, 1, 1) },
            { "wbOakBig", new PropDef(6.5f, 6.5f, 1, 1) }, { "wbOakTopMid", new PropDef(4.5f, 3f, 1, 1) }, { "wbRockTop", new PropDef(3.5f, 2f, 2, 1) },
            { "wbCliffStreamN", new PropDef(3.5f, 2.75f, 2, 1) }, { "wbCliffMid", new PropDef(4.5f, 4.5f, 3, 1) }, { "wbRocksStreamE", new PropDef(5.5f, 2.25f, 3, 1) },
            { "wbOakTopRight", new PropDef(8f, 4f, 1, 1) }, { "wbOakTopFar", new PropDef(3.25f, 2f, 1, 1) }, { "wbRockTopEdge", new PropDef(4.25f, 2f, 3, 1) },
            { "wbRockNE", new PropDef(2f, 1.5f, 1, 0) }, { "wbOakEastA", new PropDef(4.75f, 6f, 1, 1) }, { "wbOakEastB", new PropDef(6.5f, 5f, 1, 1) },
            { "wbRockEast", new PropDef(2.75f, 2.25f, 2, 1) }, { "wbOakEastMid", new PropDef(6.75f, 5.75f, 1, 1) }, { "wbOakWest", new PropDef(4.5f, 4.5f, 1, 1) },
            { "wbRockWest", new PropDef(4f, 2.5f, 2, 1) }, { "wbSignpost", new PropDef(2.25f, 2.5f, 1, 1) }, { "wbOakMid", new PropDef(5.25f, 4f, 1, 1) },
            { "wbRockMid", new PropDef(3.5f, 2f, 2, 1) }, { "wbRocksStreamS", new PropDef(5.25f, 2.75f, 3, 1) }, { "wbRockStream1", new PropDef(2.5f, 2.25f, 1, 0) },
            { "wbRockStream2", new PropDef(3.25f, 2.75f, 2, 1) }, { "wbRockStream3", new PropDef(3.5f, 2f, 2, 1) }, { "wbPalisade", new PropDef(3.5f, 8f, 1, 0) },
            { "wbOakSWClump", new PropDef(7.25f, 7.25f, 3, 1) }, { "wbOakSWTree", new PropDef(5f, 4.75f, 1, 1) }, { "wbOakSWLow", new PropDef(5f, 7f, 1, 1) },
            { "wbRockSW", new PropDef(3.5f, 2.25f, 2, 1) }, { "wbRockCliffSW", new PropDef(5.25f, 2.75f, 3, 1) }, { "wbOakSouth", new PropDef(5.75f, 4.75f, 1, 1) },
            { "wbFenceW", new PropDef(6f, 2.5f, 4, 1) }, { "wbFenceE", new PropDef(4.25f, 2.5f, 3, 1) }, { "wbRockLog", new PropDef(2.5f, 1.75f, 1, 0) },
            { "wbRockSouth", new PropDef(2.5f, 1.5f, 1, 0) }, { "wbLog", new PropDef(5.75f, 2.25f, 3, 0) }, { "wbFenceHay", new PropDef(3.5f, 2.5f, 2, 1) },
            { "wbHaystack", new PropDef(3.5f, 3.25f, 2, 1) }, { "wbShed", new PropDef(7.75f, 6.25f, 4, 2) }, { "wbOakSE", new PropDef(6f, 5f, 2, 1) },
            // Willowbrook Meadows extras (cut from the ChatGPT prop sheet by Tools/cut-willowbrook-extra-props.py): trees block the trunk tile, rocks/walls/log/barricade their base, reeds nothing
            { "wbxOakBig", new PropDef(7.0f, 7.0f, 1, 1) },
            { "wbxOakMid", new PropDef(5.5f, 5.5f, 1, 1) },
            { "wbxOakSmall", new PropDef(4.5f, 5.0f, 1, 1) },
            { "wbxOakSlim", new PropDef(2.5f, 4.75f, 1, 1) },
            { "wbxPine", new PropDef(4.0f, 5.75f, 1, 1) },
            { "wbxPineSlim", new PropDef(2.5f, 5.25f, 1, 1) },
            { "wbxBush", new PropDef(3.0f, 2.25f, 1, 0) },
            { "wbxBushWhite", new PropDef(2.5f, 2.25f, 1, 0) },
            { "wbxBushLilac", new PropDef(2.5f, 2.25f, 1, 0) },
            { "wbxBoulder", new PropDef(5.0f, 3.25f, 3, 1) },
            { "wbxRock", new PropDef(3.0f, 2.25f, 2, 0) },
            { "wbxRocks", new PropDef(2.5f, 1.75f, 1, 0) },
            { "wbxRockTiny", new PropDef(2.0f, 1.25f, 1, 0) },
            { "wbxStump", new PropDef(3.5f, 2.5f, 1, 0) },
            { "wbxLogHollow", new PropDef(4.0f, 2.5f, 3, 0) },
            { "wbxPillarTall", new PropDef(2.0f, 3.75f, 1, 1) },
            { "wbxPillarShort", new PropDef(1.5f, 2.25f, 1, 1) },
            { "wbxWallLow", new PropDef(3.5f, 2.0f, 2, 1) },
            { "wbxWallCorner", new PropDef(3.5f, 2.75f, 2, 1) },
            { "wbxReeds", new PropDef(2.5f, 3.25f, 1, 0) },
            { "wbxBarricade", new PropDef(4.5f, 3.25f, 4, 3) },
            // The stream bridge, cut from the first painting (Tools/cut-willowbrook-bridge-prop.py): the redrawn ground has none. Walk-through and 3 tiles high, so it is drawn flat under the hero.
            { "wbBridge", new PropDef(5.5f, 3.0f, 5, 0) },
        };

        public static bool TryGet(string type, out PropDef def) { return Defs.TryGetValue(type, out def) || FrostHollowPropDefs.TryGet(type, out def) || BramblewoodForestPropDefs.TryGet(type, out def) || SunbreakFoothillsPropDefs.TryGet(type, out def); }
    }
}
