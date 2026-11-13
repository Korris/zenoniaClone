using System.Linq;

namespace ZenoniaProto
{
    /// <summary>
    /// Props of Willowbrook Meadows (72x48), one table row per prop: [type, tx, ty] = bottom-left footprint tile (MapScenery y-sorts by it; PropDefs in VillagePropDefs). Three tables,
    /// concatenated in All: OldSection (the 45 wb* props cut from the first 36x24 painting, shifted +12 rows), Frame (forest border, tree belts, ruins, pond reeds, east barricade) and Scatter
    /// (bushes, rocks, stumps, logs over the new meadows). Trees block their trunk tile only; small decoration (bushes, small rocks, stumps, logs, reeds) has FootH 0 so it never blocks. Keep solid props off the roads/trails (Tools/build-map-walking-lanes.py, layout/willowbrook-72x48-lanes.txt) and off the cluster rects. To add a prop: add a P(...) row to the table of its area (art in Sprites/village/props, def in VillagePropDefs).
    /// </summary>
    public static partial class WillowbrookMeadowsProps
    {
        static MapProp P(string type, int tx, int ty) { return new MapProp(type, tx, ty); }

        // Cut from the painting, so each sprite lands on its original pixels (Tools/build-willowbrook-props.py); the old map's row r is row r + 12 here. The 13 old props the first painting's edge had
        // cropped flat (top, right, bottom edges: oaks, cliffs, rocks) are replaced in place by wbx* props of the same kind.
        static readonly MapProp[] OldSection =
        {
            P("wbxOakBig", 1, 18), P("wbxBoulder", 5, 13), P("wbRockNW", 8, 13),
            P("wbRockNW2", 2, 15), P("wbxOakMid", 11, 13), P("wbxOakBig", 13, 16),
            P("wbOakBig", 16, 18), P("wbxPine", 18, 13), P("wbRockTop", 13, 15),
            P("wbCliffStreamN", 17, 17), P("wbCliffMid", 18, 20), P("wbRocksStreamE", 25, 14),
            P("wbxOakSmall", 26, 13), P("wbxPineSlim", 29, 13), P("wbxRock", 31, 13),
            P("wbRockNE", 26, 14), P("wbxOakSmall", 34, 17), P("wbxPine", 34, 20),
            P("wbRockEast", 33, 19), P("wbxOakMid", 35, 28), P("wbOakWest", 3, 19),
            P("wbRockWest", 10, 21), P("wbSignpost", 15, 22), P("wbOakMid", 17, 22),
            P("wbRockMid", 19, 21), P("wbRocksStreamS", 23, 26), P("wbRockStream1", 28, 24),
            P("wbRockStream2", 30, 26), P("wbRockStream3", 30, 29), P("wbPalisade", 1, 26),
            P("wbOakSWClump", 3, 31), P("wbOakSWTree", 8, 29), P("wbOakSWLow", 1, 33),
            P("wbRockSW", 9, 32), P("wbxBoulder", 2, 35), P("wbOakSouth", 12, 34),
            P("wbFenceW", 3, 34), P("wbFenceE", 14, 34), P("wbRockLog", 18, 34),
            P("wbRockSouth", 16, 35), P("wbLog", 19, 34), P("wbFenceHay", 20, 33),
            P("wbHaystack", 23, 33), P("wbShed", 26, 33), P("wbxOakBig", 32, 35),
            // The redrawn ground (13/11) has no bridge: the old one is a flat walk-through prop over the B tiles (cols 23-27, row 22), 14 px below its old pixels.
            P("wbBridge", 23, 23),
        };

        static MapProp[] all;
        /// <summary>Every prop of the map. Built on first use: static fields of a partial class initialise file by file in no fixed order.</summary>
        public static MapProp[] All { get { return all ?? (all = OldSection.Concat(Frame).Concat(Scatter).ToArray()); } }
    }
}
