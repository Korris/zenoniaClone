namespace ZenoniaProto
{
    /// <summary>Rocks, stumps, saplings, bones, dead trees and rune stones scattered over the new fields (clear of the spawn rects' centres and the roads).</summary>
    public static partial class FrostHollowProps
    {
        static readonly MapProp[] Scatter =
        {
            // North-east fields
            P("fhRock", 44, 13), P("fhRockSmall", 52, 7), P("fhStump", 47, 13), P("fhPineSapling", 59, 12), P("fhPineSapling", 42, 10), P("fhPineSapling", 63, 8),
            P("fhBones", 54, 13), P("fhDeadTree", 60, 6), P("fhRock", 50, 14), P("fhBoulder", 36, 12), P("fhRockSmall", 66, 9), P("fhStump", 61, 14), P("fhPineSapling", 47, 6),
            P("fhDeadTreeWide", 57, 14),
            // Lake banks and the south-east field
            P("fhRock", 54, 26), P("fhBoulder", 54, 31), P("fhStump", 52, 34), P("fhRock", 57, 27), P("fhBoulder", 66, 33),
            P("fhStump", 58, 33), P("fhPineSapling", 56, 31), P("fhBones", 64, 38), P("fhDeadTree", 68, 40), P("fhRock", 53, 40),
            P("fhStump", 57, 40), P("fhRuneTall", 60, 29), P("fhRuneShort", 52, 43),
            // South of the lake and the south-middle fields
            P("fhRock", 30, 40), P("fhBoulder", 35, 42), P("fhStump", 28, 38), P("fhPineSapling", 37, 39), P("fhBones", 44, 41), P("fhDeadTree", 39, 38), P("fhRockSmall", 45, 43),
            P("fhPineSapling", 31, 35), P("fhRuneTall", 45, 35),
            // East bank of the river below the old section
            P("fhBones", 33, 30), P("fhRockSmall", 29, 35), P("fhDeadTree", 34, 25),
            // South-west plain
            P("fhRock", 6, 28), P("fhStump", 9, 32), P("fhPineSapling", 4, 34), P("fhBones", 11, 30), P("fhRockSmall", 3, 30), P("fhDeadTree", 12, 36), P("fhRuneTall", 7, 36), P("fhCrystalMid", 13, 34),
            // Old section extras
            P("fhPineSapling", 10, 6), P("fhRockSmall", 10, 15), P("fhPineSapling", 5, 16), P("fhStump", 12, 20), P("fhPineSapling", 28, 6), 
            // Beside the roads and trails (one tile off the walking lane): dressing that never blocks the way
            P("fhBoulder", 26, 4), P("fhPineSapling", 39, 4), P("fhPineSapling", 60, 5), P("fhPineSapling", 43, 6), P("fhRockSmall", 65, 6),
            P("fhRockSmall", 23, 7), P("fhPineSapling", 2, 8), P("fhRockSmall", 32, 10), P("fhRockSmall", 26, 13), P("fhBones", 59, 14),
            P("fhRuneShort", 3, 15), P("fhPineSapling", 23, 15), P("fhRuneShort", 32, 15), P("fhPineSapling", 42, 16), P("fhRock", 48, 16),
            P("fhStump", 1, 18), P("fhCrystalSmall", 8, 21), P("fhBones", 37, 23), P("fhCrystalSmall", 11, 24), P("fhCrystalMid", 2, 25),
            P("fhPineSapling", 63, 25), P("fhPineSapling", 13, 30), P("fhRockSmall", 15, 32), P("fhPineSapling", 8, 33), P("fhBones", 12, 34),
            P("fhRockSmall", 51, 36), P("fhRockSmall", 39, 37), P("fhStump", 54, 38), P("fhRockSmall", 1, 39), P("fhPineSapling", 35, 39),
            P("fhRock", 57, 40), P("fhRock", 15, 41), P("fhStump", 2, 44), P("fhCrystalSmall", 51, 44),
        };
    }
}
