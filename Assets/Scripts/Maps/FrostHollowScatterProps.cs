namespace ZenoniaProto
{
    /// <summary>Rocks, stumps, saplings, bones, dead trees and rune stones scattered over the new fields (clear of the spawn rects' centres and the roads).</summary>
    public static partial class FrostHollowProps
    {
        static readonly MapProp[] Scatter =
        {
            // North-east fields
            P("fhRock", 44, 13), P("fhRockSmall", 52, 7), P("fhStump", 47, 13), P("fhPineSapling", 59, 12), P("fhPineSapling", 42, 10), P("fhPineSapling", 63, 8),
            P("fhBones", 54, 13), P("fhDeadTree", 60, 10), P("fhRock", 50, 14), P("fhBoulder", 38, 12), P("fhRockSmall", 66, 9), P("fhStump", 61, 14), P("fhPineSapling", 47, 6),
            P("fhDeadTreeWide", 57, 14),
            // Lake banks and the south-east field
            P("fhRock", 54, 26), P("fhBoulder", 54, 31), P("fhStump", 52, 34), P("fhPineSapling", 53, 29), P("fhRibcage", 62, 30), P("fhRock", 57, 27), P("fhBoulder", 66, 33),
            P("fhStump", 58, 33), P("fhPineSapling", 56, 31), P("fhBones", 64, 38), P("fhRockSmall", 60, 35), P("fhDeadTree", 66, 40), P("fhPineSapling", 63, 42), P("fhRock", 53, 40),
            P("fhStump", 57, 40), P("fhRuneTall", 60, 29), P("fhRuneShort", 52, 42),
            // South of the lake and the south-middle fields
            P("fhRock", 30, 40), P("fhBoulder", 34, 42), P("fhStump", 28, 38), P("fhPineSapling", 37, 39), P("fhBones", 44, 41), P("fhDeadTree", 41, 38), P("fhRockSmall", 45, 43),
            P("fhPineSapling", 31, 35), P("fhRuneTall", 44, 36),
            // East bank of the river below the old section
            P("fhRock", 28, 26), P("fhStump", 31, 27), P("fhPineSapling", 26, 33), P("fhBones", 33, 30), P("fhRockSmall", 29, 35), P("fhDeadTree", 34, 25),
            // South-west plain
            P("fhRock", 6, 28), P("fhStump", 9, 32), P("fhPineSapling", 4, 34), P("fhBones", 11, 30), P("fhRockSmall", 3, 30), P("fhDeadTree", 12, 36), P("fhRuneTall", 7, 37), P("fhCrystalMid", 13, 34),
            // Old section extras
            P("fhPineSapling", 10, 6), P("fhRockSmall", 10, 15), P("fhPineSapling", 5, 16), P("fhStump", 12, 20), P("fhPineSapling", 28, 6), P("fhRockSmall", 30, 18),
        };
    }
}
