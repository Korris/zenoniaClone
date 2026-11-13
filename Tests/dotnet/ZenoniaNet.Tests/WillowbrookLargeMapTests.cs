using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Xunit;
using ZenoniaProto;

/// <summary>The 72x48 Willowbrook Meadows: size, west gate pair (World Map 2.0 step 1, 13/11/2026: now aimed at Oakhaven instead of Thatchwick), the HUD/border rows, the water, the east dead end
/// and the 11 spawn clusters (parsed from MapSpawnTable.cs, which needs Unity).</summary>
[Collection("MapRegistry")]
public class WillowbrookLargeMapTests
{
    static IGameMap Willow { get { return MapRegistry.Get(WillowbrookMeadowsMap.MapId); } }

    static string RepoFile(string relative)
    {
        string dir = AppContext.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "Assets"))) dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return Path.Combine(dir, relative);
    }

    [Fact]
    public void IsA72x48MapWithTwoByTwoGroundChunks()
    {
        Assert.Equal(72, Willow.Width);
        Assert.Equal(48, Willow.Height);
        Assert.Equal(2, Willow.GroundChunkCols);
        Assert.Equal(2, Willow.GroundChunkRows);
        Assert.Equal("Sprites/village/tiles/willowbrook-large-ground", Willow.GroundTexture);
    }

    [Fact]
    public void WestGateRowsTwentyThreeAndTwentyFourRoundTripWithOakhaven()
    {
        // World Map 2.0 step 1 (13/11/2026): Willowbrook's west gate now leads to Oakhaven's new south gate (13,21)/(14,21), not Thatchwick (plans/261113-0106-world-map-step1/plan.md).
        IGameMap oak = MapRegistry.Get(OakhavenVillageMap.MapId);
        Assert.Equal(4, Willow.Portals.Length); // the west gate pair + the south gate pair to Bramblewood Forest (World Map 2.1, BramblewoodRootburrowMapTests)
        Assert.Contains(Willow.Portals, p => p.Tx == 0 && p.Ty == 23 && p.TargetMapId == OakhavenVillageMap.MapId && p.TargetTx == 13 && p.TargetTy == 20);
        Assert.Contains(Willow.Portals, p => p.Tx == 0 && p.Ty == 24 && p.TargetMapId == OakhavenVillageMap.MapId && p.TargetTx == 14 && p.TargetTy == 20);
        Assert.Contains(oak.Portals, p => p.Tx == 13 && p.Ty == 21 && p.TargetMapId == WillowbrookMeadowsMap.MapId && p.TargetTx == 2 && p.TargetTy == 23);
        Assert.Contains(oak.Portals, p => p.Tx == 14 && p.Ty == 21 && p.TargetMapId == WillowbrookMeadowsMap.MapId && p.TargetTx == 2 && p.TargetTy == 23);
        foreach (MapPortal p in Willow.Portals) Assert.False(Willow.IsSolidTile(p.Tx, p.Ty), "gate tile " + p.Tx + "," + p.Ty + " is solid");
        foreach (MapPortal p in oak.Portals)
            if (p.TargetMapId == WillowbrookMeadowsMap.MapId) Assert.False(Willow.IsSolidTile(p.TargetTx, p.TargetTy), "arrival tile is solid");
    }

    [Fact]
    public void StartTileIsOpenAndTheBorderAndHudRowsAreSolid()
    {
        Assert.False(Willow.IsSolidTile(3, 23));
        for (int x = 0; x < 72; x++)
            foreach (int y in new[] { 0, 1, 45, 46, 47 }) Assert.True(Willow.IsSolidTile(x, y), "row " + y + " col " + x);
        for (int y = 0; y < 48; y++)
        {
            Assert.True(Willow.IsSolidTile(70, y) && Willow.IsSolidTile(71, y), "east border row " + y);
            if (y != 23 && y != 24) Assert.True(Willow.IsSolidTile(0, y), "west border row " + y);
        }
    }

    [Fact]
    public void PondStreamAreWaterBridgeAndMainRoadAreWalkableAndTheEastEndIsBarricaded()
    {
        Assert.True(Willow.IsSolidTile(40, 42), "pond");
        Assert.True(Willow.IsSolidTile(22, 18), "north stream");
        Assert.False(Willow.IsSolidTile(25, 22), "old bridge");
        // Walking from the start tile reaches the road end in front of the barricade.
        var seen = new HashSet<int>();
        var open = new Stack<int>();
        open.Push(23 * 72 + 3);
        while (open.Count > 0)
        {
            int i = open.Pop();
            if (Willow.IsSolidTile(i % 72, i / 72) || !seen.Add(i)) continue;
            open.Push(i + 1); open.Push(i - 1); open.Push(i + 72); open.Push(i - 72);
        }
        Assert.Contains(23 * 72 + 65, seen);
        for (int y = 22; y <= 24; y++) Assert.True(Willow.IsSolidTile(67, y), "barricade row " + y);
        Assert.False(Willow.IsSolidTile(64, 23));
    }

    static List<int[]> Zones()
    {
        string src = File.ReadAllText(RepoFile("Assets/Scripts/MapSpawnTable.cs"));
        int a = src.IndexOf("WillowbrookMeadows =", StringComparison.Ordinal), b = src.IndexOf("};", a, StringComparison.Ordinal);
        var list = new List<int[]>();
        foreach (Match m in Regex.Matches(src.Substring(a, b - a), @"Zone\(""(\w+)"", (\d+), (\d+), (\d+), (\d+), (\d+), (\d+)f\)"))
            list.Add(new[] { int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value), int.Parse(m.Groups[5].Value), int.Parse(m.Groups[6].Value), int.Parse(m.Groups[7].Value) });
        return list;
    }

    [Fact]
    public void SpawnClustersAreElevenAboutSeventyMonstersWithRoomAndKeepClearOfTheGate()
    {
        List<int[]> zones = Zones();
        int total = 0;
        Assert.InRange(zones.Count, 8, 12);
        foreach (int[] z in zones)
        {
            int count = z[0], tx = z[1], ty = z[2], w = z[3], h = z[4], respawn = z[5], free = 0;
            total += count;
            Assert.InRange(respawn, 6000, 10000);
            Assert.True(tx >= 0 && ty >= 0 && tx + w <= 72 && ty + h <= 48, "rect outside the map");
            for (int y = ty; y < ty + h; y++)
                for (int x = tx; x < tx + w; x++)
                    if (!Willow.IsSolidTile(x, y)) free++;
            Assert.True(free >= 2 * count, "cluster at " + tx + "," + ty + " has " + free + " free tiles for " + count + " monsters");
            // nearest rect tile to the start tile and the gate tiles must be outside the entrance clearance (80 units = 5 tiles)
            foreach (int[] e in new[] { new[] { 3, 23 }, new[] { 0, 23 }, new[] { 0, 24 }, new[] { 46, 44 }, new[] { 47, 44 }, new[] { 47, 42 } })
            {
                int dx = Math.Max(Math.Max(tx - e[0], 0), e[0] - (tx + w - 1)), dy = Math.Max(Math.Max(ty - e[1], 0), e[1] - (ty + h - 1));
                Assert.True(Math.Sqrt(dx * dx + dy * dy) * 16.0 >= 80.0, "cluster at " + tx + "," + ty + " is inside the entrance clearance");
            }
        }
        Assert.InRange(total, 50, 80);
    }

    [Fact]
    public void NoSolidPropStandsOnARoadTrailBridgeOrInsideASpawnClusterRect()
    {
        // Lane tiles come from Tools/build-map-walking-lanes.py (centre lines read off the painting). Walk-through props (FootH 0: bushes, small rocks, stumps, logs, reeds) may stand anywhere;
        // the barricade closes the road end on purpose and sits beyond the last lane tile.
        string[] lane = File.ReadAllLines(RepoFile("plans/261105-1052-willowbrook-large-hunting-map/layout/willowbrook-72x48-lanes.txt"));
        Assert.Equal(48, lane.Length);
        List<int[]> zones = Zones();
        int checkedProps = 0;
        foreach (MapProp p in WillowbrookMeadowsProps.All)
        {
            PropDef d;
            Assert.True(VillagePropDefs.TryGet(p.Type, out d), "unknown prop " + p.Type);
            if (p.Type == "wbxBarricade") continue; // the wall that ends the road at the east edge; its top row touches the corner of the east meadow cluster
            for (int y = p.Ty - d.FootH + 1; y <= p.Ty; y++)
                for (int x = p.Tx; x < p.Tx + d.FootW; x++)
                {
                    checkedProps++;
                    Assert.False(x >= 0 && x < 72 && y >= 0 && y < 48 && lane[y][x] == 'r', p.Type + " at " + p.Tx + "," + p.Ty + " blocks a road tile " + x + "," + y);
                    foreach (int[] z in zones)
                        Assert.False(x >= z[1] && x < z[1] + z[3] && y >= z[2] && y < z[2] + z[4], p.Type + " at " + p.Tx + "," + p.Ty + " stands inside the cluster at " + z[1] + "," + z[2]);
                }
        }
        Assert.True(checkedProps > 100, "only " + checkedProps + " solid tiles checked");
    }
}
