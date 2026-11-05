namespace ZenoniaProto
{
    /// <summary>The structure of the 72x48 map's new areas: forest on the cliff tops and the map border, the ruined towers around the north-east stone circle, the south-west camp,
    /// crystals at the ice cave and the golems' approach, rune stones along the road.</summary>
    public static partial class FrostHollowProps
    {
        static readonly MapProp[] Frame =
        {
            // Ruined towers around the north-east stone circle (solid plateau, decoration only; the circle stays visible in the middle)
            P("fhTowerTall", 42, 4), P("fhTowerShort", 46, 5), P("fhTowerShort", 52, 5), P("fhTowerTall", 55, 4), P("fhTowerShort", 44, 2), P("fhTowerShort", 54, 2),
            P("fhTowerTall", 47, 1), P("fhTowerShort", 51, 1), P("fhCrystalMid", 49, 6), P("fhRuneTall", 40, 5),
            // Top edge: pines on the cliff tops above the north-east fields
            P("fhPineMid", 38, 2), P("fhPineBig", 40, 3), P("fhPineBig", 58, 3), P("fhPineMid", 61, 2), P("fhPineBig", 64, 3), P("fhPineMid", 67, 2), P("fhPineBig", 69, 4), P("fhPineSlim", 62, 4),
            // East edge (solid cliff column 69-70): the cave mouth rows 16-26 stay clear
            P("fhPineBig", 70, 6), P("fhPineMid", 70, 9), P("fhPineBig", 70, 12), P("fhPineMid", 69, 14),
            P("fhPineMid", 70, 27), P("fhPineBig", 70, 30), P("fhPineMid", 70, 33), P("fhPineBig", 70, 36), P("fhPineMid", 70, 39), P("fhPineBig", 70, 42), P("fhPineMid", 70, 44),
            // South edge: under the HUD rows 45-47, canopies reach row 43
            P("fhPineBig", 3, 47), P("fhPineMid", 7, 46), P("fhPineSlim", 10, 46), P("fhPineBig", 13, 47), P("fhPineMid", 27, 46), P("fhPineBig", 30, 47), P("fhPineSlim", 33, 46),
            P("fhPineBig", 36, 47), P("fhPineMid", 40, 46), P("fhPineBig", 44, 47), P("fhPineSlim", 48, 46), P("fhPineBig", 52, 47), P("fhPineMid", 56, 46), P("fhPineBig", 60, 47),
            P("fhPineSlim", 64, 46), P("fhPineBig", 67, 47),
            // West edge below the gate: trunks on column 0
            P("fhPineBig", 0, 26), P("fhPineMid", 0, 29), P("fhPineBig", 0, 32), P("fhPineMid", 0, 35), P("fhPineBig", 0, 38), P("fhPineMid", 0, 41), P("fhPineBig", 0, 44),
            // Forest on the cliff tops: north field south rim, the lake's south cliffs, river banks
            P("fhPineMid", 40, 17), P("fhPineBig", 50, 19), P("fhPineBig", 53, 17), P("fhPineBig", 56, 36), P("fhPineMid", 53, 37), P("fhPineMid", 61, 38), P("fhPineBig", 57, 43),
            P("fhPineBig", 25, 30), P("fhPineBig", 16, 36), P("fhPineMid", 25, 44), P("fhPineMid", 15, 43),
            // Camp on the paved floor in the south-west: tent, cold fire, fence, bones
            P("fhTent", 5, 42), P("fhCampfire", 9, 42), P("fhFence", 3, 41), P("fhFence", 11, 44), P("fhFence", 13, 41), P("fhBones", 12, 42), P("fhStump", 2, 40), P("fhRock", 10, 40),
            // Crystals at the ice cave and on the golems' approach
            P("fhCrystalBig", 62, 17), P("fhCrystalMid", 64, 24), P("fhCrystalSmall", 62, 22), P("fhCrystalMid", 59, 21), P("fhCrystalSmall", 59, 17), P("fhCrystalMid", 55, 22),
            // Rune stones along the road
            P("fhRuneTall", 39, 8), P("fhRuneShort", 49, 11), P("fhRuneTall", 56, 7), P("fhRuneShort", 64, 11), P("fhRuneTall", 66, 16),
        };
    }
}
