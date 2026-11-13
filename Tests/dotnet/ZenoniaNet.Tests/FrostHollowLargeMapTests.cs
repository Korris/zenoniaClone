using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Xunit;
using ZenoniaProto;

/// <summary>The 72x48 Frost Hollow: size, chunks, gate and ice-cave portals with their round trips, border/HUD rows, flood fill from the start, the old valley tiles and the 12 outer spawn clusters
/// (parsed from MapSpawnTable.cs, which needs Unity), plus the guard tying the row count to the save version.</summary>
[Collection("MapRegistry")]
public class FrostHollowLargeMapTests
{
    static IGameMap Frost { get { return MapRegistry.Get(FrostHollowLayout.MapId); } }

    static string RepoFile(string relative)
    {
        string dir = AppContext.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "Assets"))) dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return Path.Combine(dir, relative);
    }

    static HashSet<int> ReachableFromStart()
    {
        var seen = new HashSet<int>();
        var open = new Stack<int>();
        open.Push(21 * 72 + 4);
        while (open.Count > 0)
        {
            int i = open.Pop();
            int x = i % 72, y = i / 72;
            if (i < 0 || x >= 72 || y >= 48 || Frost.IsSolidTile(x, y) || !seen.Add(i)) continue;
            if (x < 71) open.Push(i + 1);
            if (x > 0) open.Push(i - 1);
            open.Push(i + 72); open.Push(i - 72);
        }
        return seen;
    }

    [Fact]
    public void IsA72x48MapWithTwoByTwoChunksAndTheChunkFilesExist()
    {
        Assert.Equal(72, Frost.Width);
        Assert.Equal(48, Frost.Height);
        Assert.Equal(48, FrostHollowLayout.RowCount);
        Assert.Equal(2, Frost.GroundChunkCols);
        Assert.Equal(2, Frost.GroundChunkRows);
        Assert.Equal("Sprites/tiles/frost-hollow-large-ground", Frost.GroundTexture);
        for (int r = 0; r < 2; r++)
            for (int c = 0; c < 2; c++) AssertPngSize("Assets/Resources/Sprites/tiles/frost-hollow-large-ground-r" + r + "c" + c + ".png", 2304, 1536);
        AssertPngSize("Assets/Resources/Sprites/tiles/frost-hollow-large-ground-minimap.png", 432, 288);
    }

    static void AssertPngSize(string relative, int w, int h)
    {
        byte[] b = File.ReadAllBytes(RepoFile(relative));
        int pw = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], ph = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
        Assert.Equal(w, pw); Assert.Equal(h, ph);
    }

    [Fact]
    public void VillageGateAndIceCavePortalsRoundTrip()
    {
        // World Map 2.0 step 1 (13/11/2026): the south-west gate at (2,21) now leads to Thatchwick (it used to lead to Oakhaven; see MapGraphTests.WorldMapStep1RouteGoesBothWays).
        // World Map 2.1 (13/11/2026): the ice cave (67-69,20) leads to the south road of Sunbreak Foothills instead of the Frostfang Den; Sunbreak's south portals come back to (68,22).
        IGameMap sunbreak = MapRegistry.Get(SunbreakFoothillsMap.MapId), thatch = MapRegistry.Get(ThatchVillageMap.MapId);
        Assert.Equal(4, Frost.Portals.Length);
        Assert.Contains(Frost.Portals, p => p.Tx == 2 && p.Ty == 21 && p.TargetMapId == ThatchVillageMap.MapId && p.TargetTx == 33 && p.TargetTy == 11);
        for (int x = 67; x <= 69; x++)
        {
            int px = x;
            Assert.Contains(Frost.Portals, p => p.Tx == px && p.Ty == 20 && p.TargetMapId == SunbreakFoothillsMap.MapId && p.TargetTx == 21 && p.TargetTy == 26);
        }
        foreach (MapPortal p in Frost.Portals) Assert.False(Frost.IsSolidTile(p.Tx, p.Ty), "portal tile " + p.Tx + "," + p.Ty + " is solid");
        Assert.Equal(new Vector2Int(4, 21), FrostHollowLayout.PlayerStartTile);
        Assert.Contains(thatch.Portals, p => p.TargetMapId == FrostHollowLayout.MapId && p.TargetTx == 4 && p.TargetTy == 21);
        // Sunbreak's three south triggers land on the same snow tile in front of the ice cave (the tile the Den's way back used before World Map 2.1).
        int back = 0;
        foreach (MapPortal p in sunbreak.PortalsFor(false))
        {
            if (p.TargetMapId != FrostHollowLayout.MapId) continue;
            back++;
            Assert.Equal(68, p.TargetTx); Assert.Equal(22, p.TargetTy);
            Assert.False(Frost.IsSolidTile(p.TargetTx, p.TargetTy), "arrival is solid");
            MapPortal arrival = p;
            Assert.DoesNotContain(Frost.Portals, q => q.Tx == arrival.TargetTx && q.Ty == arrival.TargetTy);
        }
        Assert.Equal(3, back);
        Assert.DoesNotContain(Frost.Portals, p => p.TargetMapId == FrostfangDenMap.MapId);
    }

    [Fact]
    public void StartTileIsOpenAndTheHudAndBorderRowsAreSolid()
    {
        Assert.False(Frost.IsSolidTile(4, 21));
        Assert.False(Frost.IsSolidTile(7, 5), "the King tile");
        for (int x = 0; x < 72; x++)
            foreach (int y in new[] { 0, 45, 46, 47 }) Assert.True(Frost.IsSolidTile(x, y), "row " + y + " col " + x);
        for (int y = 0; y < 48; y++) Assert.True(Frost.IsSolidTile(70, y) && Frost.IsSolidTile(71, y), "east border row " + y);
        for (int y = 22; y < 48; y++) Assert.True(Frost.IsSolidTile(0, y), "west border row " + y);
    }

    [Fact]
    public void EveryWalkableTileIsReachableFromTheStartAndTheSouthWestExitAndSouthEastPocketAreOpen()
    {
        HashSet<int> seen = ReachableFromStart();
        int walkable = 0;
        for (int y = 0; y < 48; y++)
            for (int x = 0; x < 72; x++)
                if (!Frost.IsSolidTile(x, y)) { walkable++; Assert.Contains(y * 72 + x, seen); }
        Assert.True(walkable > 1800, "walkable " + walkable);
        Assert.False(Frost.IsSolidTile(5, 23)); Assert.False(Frost.IsSolidTile(6, 24)); // stairs down the south rim of the old valley
        Assert.Contains(30 * 72 + 12, seen);   // south-west plaza
        Assert.Contains(22 * 72 + 30, seen);   // south-east pocket of the old valley
        foreach (MapPortal p in Frost.Portals) Assert.Contains(p.Ty * 72 + p.Tx, seen);
        Assert.True(Frost.IsSolidTile(20, 40), "river");
        Assert.True(Frost.IsSolidTile(18, 3), "river");
    }

    static List<int[]> ParseZones(string file, string field)
    {
        string src = File.ReadAllText(RepoFile(file));
        int a = src.IndexOf(field, StringComparison.Ordinal), b = src.IndexOf("};", a, StringComparison.Ordinal);
        var list = new List<int[]>();
        foreach (Match m in Regex.Matches(src.Substring(a, b - a), @"Zone\(""(\w+)"", (\d+), (\d+), (\d+), (\d+), (\d+), (\d+)f\)"))
            list.Add(new[] { int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value), int.Parse(m.Groups[5].Value), int.Parse(m.Groups[6].Value), int.Parse(m.Groups[7].Value) });
        return list;
    }

    [Fact]
    public void OuterClustersAreTwelveSixtySixMonstersWithRoomReachableAndClearOfEveryEntrance()
    {
        List<int[]> zones = ParseZones("Assets/Scripts/MapSpawnTable.cs", "FrostHollowOuter =");
        HashSet<int> seen = ReachableFromStart();
        Assert.Equal(12, zones.Count);
        var entrances = new List<int[]> { new[] { 4, 21 }, new[] { 2, 21 }, new[] { 68, 22 } };
        foreach (MapPortal p in Frost.Portals) entrances.Add(new[] { p.Tx, p.Ty });
        int total = 0;
        foreach (int[] z in zones)
        {
            int count = z[0], tx = z[1], ty = z[2], w = z[3], h = z[4], respawn = z[5], free = 0;
            total += count;
            Assert.InRange(respawn, 7000, 10500);
            Assert.True(tx >= 0 && ty >= 0 && tx + w <= 72 && ty + h <= 48, "rect outside the map");
            for (int y = ty; y < ty + h; y++)
                for (int x = tx; x < tx + w; x++)
                    if (!Frost.IsSolidTile(x, y)) { free++; Assert.Contains(y * 72 + x, seen); }
            Assert.True(free >= 2 * count, "cluster at " + tx + "," + ty + " has " + free + " free tiles for " + count + " monsters");
            foreach (int[] e in entrances)
            {
                int dx = Math.Max(Math.Max(tx - e[0], 0), e[0] - (tx + w - 1)), dy = Math.Max(Math.Max(ty - e[1], 0), e[1] - (ty + h - 1));
                Assert.True(Math.Sqrt(dx * dx + dy * dy) * 16.0 >= 80.0, "cluster at " + tx + "," + ty + " is inside the entrance clearance of " + e[0] + "," + e[1]);
            }
        }
        Assert.Equal(66, total);
    }

    [Fact]
    public void WatchtowerPlateauIsReachableByItsStairsAndHoldsTheSecondGolemCluster()
    {
        HashSet<int> seen = ReachableFromStart();
        // stairs (49-50, rows 5-6) open, flanked by solid balustrades (cols 48 and 51), snow below and the path above to the stone circle
        foreach (int x in new[] { 49, 50 })
            foreach (int y in new[] { 3, 4, 5, 6, 7 }) { Assert.False(Frost.IsSolidTile(x, y), "stairs path " + x + "," + y); Assert.Contains(y * 72 + x, seen); }
        Assert.True(Frost.IsSolidTile(48, 5) && Frost.IsSolidTile(51, 5), "balustrades");
        Assert.False(Frost.IsSolidTile(49, 2), "stone circle top"); Assert.Contains(2 * 72 + 49, seen);
        foreach (MapProp p in FrostHollowProps.All)
            Assert.False(p.Tx <= 50 && p.Tx + 2 > 49 && p.Ty >= 3 && p.Ty <= 7, "prop " + p.Type + " on the stairs path at " + p.Tx + "," + p.Ty);
        List<int[]> zones = ParseZones("Assets/Scripts/MapSpawnTable.cs", "FrostHollowOuter =");
        Assert.Contains(zones, z => z[0] == 5 && z[1] == 43 && z[2] == 1 && z[4] == 3 && z[5] == 10500);   // golems on the plateau (rows 1-3: clear of the stairs rows 4-7)
    }

    // Every stone staircase / cliff pass of the painting: step tiles (walkable), side walls (solid), the landing above and below. Rows were once classified by colour, which read grey steps as cliff.
    public static IEnumerable<object[]> Stairs()
    {
        yield return new object[] { "west valley stairs", "5,9 6,9 5,10 6,10", "5,8", "5,11", "4,10 7,10" };
        yield return new object[] { "north stairs", "32,3 33,3 32,4 33,4", "33,2", "33,5", "31,4" };
        yield return new object[] { "river-east stairs", "29,12 30,12 29,13 30,13", "29,11", "29,14", "28,12 31,12" };
        yield return new object[] { "south-west rim stairs", "5,23 6,23 5,24 6,24 5,25 6,25", "5,22", "5,26", "4,24 7,24" };
        yield return new object[] { "watchtower stairs (NE plateau)", "49,5 50,5 49,6 50,6", "49,4", "49,7", "48,5 51,5" };
        yield return new object[] { "E1 stone stairs", "45,17 46,17 44,18 45,18 46,18 44,19 45,19", "45,16", "45,20", "44,17 43,18 47,18 46,19" };
        yield return new object[] { "cobble pass south of the E1 stairs", "45,22 45,23", "45,21", "45,24", "44,22 46,22 44,23 46,23" };
        yield return new object[] { "SE ledge stairs", "49,38 49,39", "48,38", "49,40", "48,39 50,38" };
    }

    static int[][] Tiles(string list)
    {
        var r = new List<int[]>();
        foreach (string t in list.Split(' ')) { string[] xy = t.Split(','); r.Add(new[] { int.Parse(xy[0]), int.Parse(xy[1]) }); }
        return r.ToArray();
    }

    [Theory]
    [MemberData(nameof(Stairs))]
    public void StairsAreWalkableWallsSolidAndTheLandingsConnectThroughTheSteps(string name, string steps, string top, string bottom, string walls)
    {
        var allowed = new HashSet<int>();
        foreach (int[] t in Tiles(steps)) { Assert.False(Frost.IsSolidTile(t[0], t[1]), name + ": step " + t[0] + "," + t[1] + " is solid"); allowed.Add(t[1] * 72 + t[0]); }
        int[] a = Tiles(top)[0], b = Tiles(bottom)[0];
        Assert.False(Frost.IsSolidTile(a[0], a[1]), name + ": top landing solid");
        Assert.False(Frost.IsSolidTile(b[0], b[1]), name + ": bottom landing solid");
        allowed.Add(a[1] * 72 + a[0]); allowed.Add(b[1] * 72 + b[0]);
        foreach (int[] w in Tiles(walls)) Assert.True(Frost.IsSolidTile(w[0], w[1]), name + ": wall " + w[0] + "," + w[1] + " must stay solid");
        // flood only over steps + the two landings: the top must reach the bottom through the steps, not around the cliff
        var seen = new HashSet<int>(); var open = new Stack<int>(); open.Push(a[1] * 72 + a[0]);
        while (open.Count > 0)
        {
            int i = open.Pop();
            if (!allowed.Contains(i) || !seen.Add(i)) continue;
            open.Push(i + 1); open.Push(i - 1); open.Push(i + 72); open.Push(i - 72);
        }
        Assert.Contains(b[1] * 72 + b[0], seen);
        Assert.True(seen.Contains(a[1] * 72 + a[0]) && seen.Count == allowed.Count, name + ": a step is cut off from the landings");
        HashSet<int> reach = ReachableFromStart();
        Assert.Contains(a[1] * 72 + a[0], reach); Assert.Contains(b[1] * 72 + b[0], reach);
    }

    [Fact]
    public void TheFiveOldZonesStayWalkableOnTheNewRows()
    {
        List<int[]> old = ParseZones("Assets/Scripts/FrostHollowMap.cs", "Zone[] Zones");
        Assert.Equal(5, old.Count);
        foreach (int[] z in old)
        {
            int free = 0;
            for (int y = z[2]; y < z[2] + z[4]; y++)
                for (int x = z[1]; x < z[1] + z[3]; x++) if (!Frost.IsSolidTile(x, y)) free++;
            Assert.True(free >= 2 * z[0], "old zone at " + z[1] + "," + z[2]);
        }
    }

    [Fact]
    public void RowCountBeyondTheLegacy24NeedsSaveVersionThreeOrMore()
    {
        // SaveSerializer is not part of this test project, so read its constant. A bigger map on SaveVersion 2 would leave every old frostHollow save unshifted (hero in the HUD rows).
        string src = File.ReadAllText(RepoFile("Assets/Scripts/Rpg/SaveSerializer.cs"));
        Match m = Regex.Match(src, @"public const int SaveVersion = (\d+);");
        Assert.True(m.Success);
        if (FrostHollowLayout.RowCount > 24) Assert.True(int.Parse(m.Groups[1].Value) >= 3, "Frost Hollow has " + FrostHollowLayout.RowCount + " rows but SaveVersion is " + m.Groups[1].Value);
    }

    [Fact]
    public void NoSolidPropStandsOnAPathBridgeStairOrInsideAnyClusterRect()
    {
        // Lane tiles come from Tools/build-map-walking-lanes.py (stone paths, bridges, stairs, the camp plaza). Walk-through props (FootH 0: small crystals and rocks, saplings, stumps, bones) may stand anywhere.
        string[] lane = File.ReadAllLines(RepoFile("plans/261105-1240-frost-hollow-large-map/layout/frost-hollow-72x48-lanes.txt"));
        Assert.Equal(48, lane.Length);
        var zones = ParseZones("Assets/Scripts/MapSpawnTable.cs", "FrostHollowOuter =");
        zones.AddRange(ParseZones("Assets/Scripts/FrostHollowMap.cs", "Zone[] Zones"));
        Assert.Equal(17, zones.Count);
        int checkedTiles = 0;
        foreach (MapProp p in FrostHollowProps.All)
        {
            PropDef d;
            Assert.True(VillagePropDefs.TryGet(p.Type, out d), "unknown prop " + p.Type);
            for (int y = p.Ty - d.FootH + 1; y <= p.Ty; y++)
                for (int x = p.Tx; x < p.Tx + d.FootW; x++)
                {
                    checkedTiles++;
                    Assert.False(x >= 0 && x < 72 && y >= 0 && y < 48 && lane[y][x] == 'r', p.Type + " at " + p.Tx + "," + p.Ty + " blocks a path tile " + x + "," + y);
                    foreach (int[] z in zones)
                        Assert.False(x >= z[1] && x < z[1] + z[3] && y >= z[2] && y < z[2] + z[4], p.Type + " at " + p.Tx + "," + p.Ty + " stands inside the cluster at " + z[1] + "," + z[2]);
                }
        }
        Assert.True(checkedTiles > 100, "only " + checkedTiles + " solid tiles checked");
    }
}
