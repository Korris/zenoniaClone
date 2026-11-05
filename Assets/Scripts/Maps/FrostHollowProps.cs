using System.Linq;

namespace ZenoniaProto
{
    /// <summary>
    /// Props of Frost Hollow (72x48), one table row per prop: [type, tx, ty] = bottom-left footprint tile (MapScenery y-sorts by it; PropDefs in FrostHollowPropDefs / VillagePropDefs).
    /// Three tables, concatenated in All: OldSection (cols 0-35, rows 0-23: the pines, dead trees, crystals, rune stones, fences, bones and stumps of the original painting, which the
    /// ground-only backdrop no longer has), Frame (forest belts on the cliff tops and the map border, the camp, the ruined towers around the north-east stone circle, crystals at the ice cave)
    /// and Scatter (rocks, stumps, saplings, bones, rune stones over the new fields). Trees block their trunk tile only. To add a prop: add a P(...) row to the table of its area
    /// (art in Sprites/village/props, def in FrostHollowPropDefs). Keep it off the stairs, the bridges, the village gate nook and the cave mouth; Tools/preview-frost-hollow-large-composite.py checks that.
    /// </summary>
    public static partial class FrostHollowProps
    {
        static MapProp P(string type, int tx, int ty) { return new MapProp(type, tx, ty); }

        // Where each thing stood in frost-hollow-ground-original.png (tile = painting pixel / 64), nudged onto walkable snow or onto the cliff tops the painting put pines on.
        static readonly MapProp[] OldSection =
        {
            // West forest wall: trunks on the solid edge column, canopies lean over cols 0-2 (the way down to the gate, rows 19-24, stays open)
            P("fhPineBig", 0, 5), P("fhPineMid", 1, 7), P("fhPineBig", 0, 9), P("fhPineMid", 0, 12), P("fhPineBig", 0, 15), P("fhPineMid", 1, 17), P("fhPineMid", 1, 10), P("fhPineMid", 1, 15), P("fhPineSlim", 2, 3), P("fhPineBig", 0, 18),
            // North-west: the cave front with its crystals, fence, bones and stumps
            P("fhCrystalMid", 2, 4), P("fhCrystalSmall", 1, 4), P("fhFence", 3, 4), P("fhBones", 6, 3), P("fhStump", 8, 3), P("fhRockSmall", 9, 4),
            P("fhFence", 7, 2), P("fhDeadTree", 12, 2), P("fhRock", 10, 4), P("fhFence", 14, 3),
            // West plain: rune stone and stumps, bones, rock, pine at the stairs, crystals, boulder
            P("fhRuneTall", 3, 6), P("fhStump", 6, 6), P("fhStump", 4, 7), P("fhBones", 6, 5), P("fhRock", 6, 7), P("fhPineMid", 4, 8),
            P("fhCrystalSmall", 4, 11), P("fhCrystalSmall", 8, 12), P("fhCrystalBig", 4, 13), P("fhBoulder", 2, 13),
            // Cliff wall above the road (row 9) and the rune stone at the bridge head
            P("fhFence", 11, 8), P("fhRuneTall", 13, 8), P("fhCrystalSmall", 13, 10), P("fhRuneTall", 14, 13),
            // West bank of the river: pines on the cliff tops, dead tree
            P("fhPineBig", 16, 5), P("fhDeadTree", 16, 7), P("fhPineBig", 15, 16), P("fhPineMid", 14, 14),
            // South-west rune corner around the start
            P("fhCrystalMid", 1, 19), P("fhRuneTall", 7, 18), P("fhRuneShort", 6, 20), P("fhCrystalBig", 8, 20), P("fhDeadTree", 9, 18), P("fhDeadTree", 8, 22),
            // East bank of the river: pines, dead tree, rocks, fences, rune stones, crystals, ribcage
            P("fhPineBig", 23, 8), P("fhPineMid", 23, 4), P("fhDeadTree", 24, 6), P("fhRock", 26, 8), P("fhRockSmall", 27, 9),
            P("fhFence", 26, 2), P("fhCrystalMid", 30, 2), P("fhFence", 25, 9), P("fhCrystalSmall", 28, 10), P("fhRuneTall", 33, 9),
            P("fhFence", 31, 11), P("fhCrystalMid", 26, 12), P("fhStump", 26, 13), P("fhStump", 28, 15),
            P("fhPineMid", 30, 15), P("fhRibcage", 32, 16), P("fhFence", 34, 16),
            P("fhPineMid", 23, 17), P("fhPineBig", 25, 19), P("fhPineMid", 22, 19),
        };

        static MapProp[] all;
        /// <summary>Every prop of the map. Built on first use: static fields of a partial class initialise file by file in no fixed order.</summary>
        public static MapProp[] All { get { return all ?? (all = OldSection.Concat(Frame).Concat(Scatter).ToArray()); } }
    }
}
