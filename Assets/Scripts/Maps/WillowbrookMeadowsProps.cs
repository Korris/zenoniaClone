using System.Linq;

namespace ZenoniaProto
{
    /// <summary>
    /// Props of Willowbrook Meadows (72x48), one table row per prop: [type, tx, ty] = bottom-left footprint tile (MapScenery y-sorts by it; PropDefs in VillagePropDefs). Three tables,
    /// concatenated in All: OldSection (the 45 wb* props cut from the first 36x24 painting, shifted +12 rows), Frame (forest border, tree belts, ruins, pond reeds, east barricade) and Scatter
    /// (bushes, rocks, stumps, logs over the new meadows). Trees block their trunk tile only. To add a prop: add a P(...) row to the table of its area (art in Sprites/village/props, def in VillagePropDefs).
    /// </summary>
    public static partial class WillowbrookMeadowsProps
    {
        static MapProp P(string type, int tx, int ty) { return new MapProp(type, tx, ty); }

        // Cut from the painting, so each sprite lands on its original pixels (Tools/build-willowbrook-props.py); the old map's row r is row r + 12 here. The 13 old props the first painting's edge had
        // cropped flat (top, right, bottom edges: oaks, cliffs, rocks) are replaced in place by wbx* props of the same kind.
        static readonly MapProp[] OldSection =
        {
            P("wbxOakBig", 1, 18), P("wbxBoulder", 5, 14), P("wbRockNW", 8, 14),
            P("wbRockNW2", 4, 15), P("wbxOakMid", 11, 14), P("wbxOakBig", 13, 17),
            P("wbOakBig", 16, 18), P("wbxPine", 18, 14), P("wbRockTop", 13, 17),
            P("wbCliffStreamN", 19, 15), P("wbCliffMid", 18, 20), P("wbRocksStreamE", 23, 16),
            P("wbxOakSmall", 26, 15), P("wbxPineSlim", 29, 13), P("wbxRock", 31, 13),
            P("wbRockNE", 26, 14), P("wbxOakSmall", 34, 17), P("wbxPine", 35, 21),
            P("wbRockEast", 33, 21), P("wbxOakMid", 35, 28), P("wbOakWest", 3, 22),
            P("wbRockWest", 10, 22), P("wbSignpost", 14, 22), P("wbOakMid", 17, 22),
            P("wbRockMid", 19, 22), P("wbRocksStreamS", 23, 26), P("wbRockStream1", 28, 24),
            P("wbRockStream2", 30, 26), P("wbRockStream3", 30, 29), P("wbPalisade", 1, 26),
            P("wbOakSWClump", 3, 31), P("wbOakSWTree", 9, 29), P("wbOakSWLow", 1, 33),
            P("wbRockSW", 8, 31), P("wbxBoulder", 2, 35), P("wbOakSouth", 12, 34),
            P("wbFenceW", 6, 34), P("wbFenceE", 14, 34), P("wbRockLog", 18, 34),
            P("wbRockSouth", 16, 35), P("wbLog", 19, 34), P("wbFenceHay", 20, 33),
            P("wbHaystack", 23, 33), P("wbShed", 26, 33), P("wbxOakBig", 32, 35),
        };

        static MapProp[] all;
        /// <summary>Every prop of the map. Built on first use: static fields of a partial class initialise file by file in no fixed order.</summary>
        public static MapProp[] All { get { return all ?? (all = OldSection.Concat(Frame).Concat(Scatter).ToArray()); } }
    }
}
