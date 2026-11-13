using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using ZenoniaProto;

/// <summary>World Map 2.1 (13/11/2026): Bramblewood Forest, Rootburrow and Thornheart Chamber: sizes, the portal chain Willowbrook - Bramblewood - Rootburrow - Chamber in both
/// directions, HUD rows, the bridge, the blocked east road end, reachability and the spawn rects (parsed from MapSpawnTable.cs, which needs Unity).</summary>
[Collection("MapRegistry")]
public class BramblewoodRootburrowMapTests
{
    static IGameMap Map(string id) { IGameMap m = MapRegistry.Get(id); Assert.True(m != null, id + " is not registered"); return m; }
    static IGameMap Bramble { get { return Map(BramblewoodForestMap.MapId); } }
    static IGameMap Burrow { get { return Map(RootburrowMap.MapId); } }
    static IGameMap Chamber { get { return Map(ThornheartChamberMap.MapId); } }

    static string RepoFile(string relative)
    {
        string dir = AppContext.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "Assets"))) dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return Path.Combine(dir, relative);
    }

    [Fact]
    public void IdsNamesAndSizes()
    {
        Assert.Equal("bramblewoodForest", Bramble.Id); Assert.Equal("Bramblewood Forest", Bramble.Name);
        Assert.Equal("rootburrow", Burrow.Id); Assert.Equal("Rootburrow", Burrow.Name);
        Assert.Equal("thornheartChamber", Chamber.Id); Assert.Equal("Thornheart Chamber", Chamber.Name);
        Assert.Equal(36, Bramble.Width); Assert.Equal(24, Bramble.Height);
        Assert.Equal(36, Burrow.Width); Assert.Equal(24, Burrow.Height);
        Assert.Equal(24, Chamber.Width); Assert.Equal(16, Chamber.Height);
        foreach (IGameMap m in new[] { Bramble, Burrow, Chamber }) Assert.True(m.Hostile, m.Id);
    }

    static void AssertLink(IGameMap from, int tx, int ty, IGameMap to, int ax, int ay)
    {
        Assert.Contains(from.Portals, p => p.Tx == tx && p.Ty == ty && p.TargetMapId == to.Id && p.TargetTx == ax && p.TargetTy == ay);
    }

    [Fact]
    public void ThePortalChainRunsBothWays()
    {
        IGameMap willow = Map(WillowbrookMeadowsMap.MapId);
        AssertLink(willow, 46, 44, Bramble, 17, 2); AssertLink(willow, 47, 44, Bramble, 18, 2);
        AssertLink(Bramble, 17, 0, willow, 47, 42); AssertLink(Bramble, 18, 0, willow, 47, 42);
        AssertLink(Bramble, 2, 13, Burrow, 32, 13);
        AssertLink(Burrow, 35, 12, Bramble, 4, 13); AssertLink(Burrow, 35, 13, Bramble, 4, 13);
        AssertLink(Burrow, 4, 3, Chamber, 11, 10); AssertLink(Burrow, 5, 3, Chamber, 12, 10);
        AssertLink(Chamber, 11, 11, Burrow, 4, 5); AssertLink(Chamber, 12, 11, Burrow, 5, 5);
        Assert.Equal(3, Bramble.Portals.Length); Assert.Equal(4, Burrow.Portals.Length); Assert.Equal(2, Chamber.Portals.Length);
    }

    static HashSet<int> Reach(IGameMap m, int sx, int sy)
    {
        var seen = new HashSet<int>();
        var open = new Stack<int>();
        open.Push(sy * m.Width + sx);
        while (open.Count > 0)
        {
            int i = open.Pop(), x = i % m.Width, y = i / m.Width;
            if (m.IsSolidTile(x, y) || !seen.Add(i)) continue;
            if (x + 1 < m.Width) open.Push(i + 1);
            if (x > 0) open.Push(i - 1);
            if (y + 1 < m.Height) open.Push(i + m.Width);
            if (y > 0) open.Push(i - m.Width);
        }
        return seen;
    }

    [Fact]
    public void HudRowsAreSolidAndEveryOpenTileIsReachableFromEveryPortal()
    {
        foreach (IGameMap m in new[] { Bramble, Burrow, Chamber })
        {
            for (int x = 0; x < m.Width; x++)
                for (int y = m.Height - 3; y < m.Height; y++) Assert.True(m.IsSolidTile(x, y), m.Id + " HUD row " + y + " col " + x);
            MapPortal first = m.Portals[0];
            HashSet<int> seen = Reach(m, first.Tx, first.Ty);
            for (int y = 0; y < m.Height; y++)
                for (int x = 0; x < m.Width; x++)
                    Assert.True(m.IsSolidTile(x, y) || seen.Contains(y * m.Width + x), m.Id + " open tile " + x + "," + y + " cannot be reached");
        }
    }

    [Fact]
    public void BramblewoodBridgeIsOpenStreamIsWaterAndTheEastRoadEndIsBlocked()
    {
        for (int x = 19; x <= 21; x++)
        {
            Assert.False(Bramble.IsSolidTile(x, 12), "bridge " + x + ",12");
            Assert.False(Bramble.IsSolidTile(x, 13), "bridge " + x + ",13");
            Assert.True(Bramble.IsSolidTile(x, 11) && Bramble.IsSolidTile(x, 14), "water beside the bridge, col " + x);
        }
        for (int x = 7; x <= 33; x++) Assert.False(Bramble.IsSolidTile(x, 12) && Bramble.IsSolidTile(x, 13), "road col " + x);
        for (int y = 0; y < 24; y++)
        {
            Assert.True(Bramble.IsSolidTile(34, y) && Bramble.IsSolidTile(35, y), "east edge row " + y); // no map east of Bramblewood in World Map 2.1
            Assert.True(Bramble.IsSolidTile(0, y) && Bramble.IsSolidTile(1, y), "west edge row " + y);
        }
    }

    static List<int[]> Zones(string table)
    {
        string src = File.ReadAllText(RepoFile("Assets/Scripts/MapSpawnTable.cs"));
        int a = src.IndexOf("Zone[] " + table + " =", StringComparison.Ordinal), b = src.IndexOf("};", a, StringComparison.Ordinal);
        Assert.True(a >= 0, table + " not found in MapSpawnTable.cs");
        var list = new List<int[]>();
        foreach (Match m in Regex.Matches(src.Substring(a, b - a), @"Zone\(""(\w+)"", (\d+), (\d+), (\d+), (\d+), (\d+), (\d+)f\)"))
            list.Add(new[] { int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value), int.Parse(m.Groups[5].Value), int.Parse(m.Groups[6].Value) });
        return list;
    }

    [Theory]
    [InlineData("bramblewoodForest", "BramblewoodForest", 30, 17, 3)]
    [InlineData("rootburrow", "Rootburrow", 16, 32, 13)]
    public void SpawnRectsHaveRoomAndKeepFiveTilesFromStartPortalsAndArrivals(string mapId, string table, int total, int startTx, int startTy)
    {
        IGameMap map = Map(mapId);
        var clear = new List<int[]>();
        clear.Add(new[] { startTx, startTy }); // PlayerStartTile of the map class
        foreach (MapPortal p in map.Portals) clear.Add(new[] { p.Tx, p.Ty });
        foreach (IGameMap other in MapRegistry.All)
            foreach (MapPortal p in other.Portals)
                if (p.TargetMapId == mapId) clear.Add(new[] { p.TargetTx, p.TargetTy });
        List<int[]> zones = Zones(table);
        int sum = 0;
        foreach (int[] z in zones)
        {
            int tx = z[1], ty = z[2], w = z[3], h = z[4], free = 0, sharing = 0;
            sum += z[0];
            foreach (int[] o in zones) if (o[1] == tx && o[2] == ty && o[3] == w && o[4] == h) sharing += o[0];
            Assert.True(tx >= 0 && ty >= 0 && tx + w <= map.Width && ty + h <= map.Height, "rect outside the map");
            for (int y = ty; y < ty + h; y++)
                for (int x = tx; x < tx + w; x++)
                    if (!map.IsSolidTile(x, y)) free++;
            Assert.True(free >= 2 * sharing, "rect at " + tx + "," + ty + " has " + free + " free tiles for " + sharing + " monsters");
            foreach (int[] e in clear)
            {
                int dx = Math.Max(Math.Max(tx - e[0], 0), e[0] - (tx + w - 1)), dy = Math.Max(Math.Max(ty - e[1], 0), e[1] - (ty + h - 1));
                Assert.True(Math.Sqrt(dx * dx + dy * dy) >= 5.0, "rect at " + tx + "," + ty + " is closer than 5 tiles to " + e[0] + "," + e[1]);
            }
        }
        Assert.Equal(total, sum);
    }

    [Fact]
    public void NoSolidBramblewoodPropStandsOnTheRoadTheBridgeOrInASpawnRect()
    {
        var bramble = (TileMapBase)Bramble;
        List<int[]> zones = Zones("BramblewoodForest");
        foreach (MapProp p in BramblewoodForestProps.All)
        {
            PropDef d;
            Assert.True(VillagePropDefs.TryGet(p.Type, out d), "unknown prop " + p.Type);
            for (int y = p.Ty - d.FootH + 1; y <= p.Ty; y++)
                for (int x = p.Tx; x < p.Tx + d.FootW; x++)
                {
                    char c = bramble.CharAt(x, y);
                    Assert.False(c == 'd' || c == 'B', p.Type + " at " + p.Tx + "," + p.Ty + " blocks road tile " + x + "," + y);
                    foreach (int[] z in zones)
                        Assert.False(x >= z[1] && x < z[1] + z[3] && y >= z[2] && y < z[2] + z[4], p.Type + " at " + p.Tx + "," + p.Ty + " stands inside the rect at " + z[1] + "," + z[2]);
                }
        }
    }

    [Fact]
    public void BossTilesAreOpenAndWillowbrookNoLongerSpawnsMantisOrTreant()
    {
        string src = File.ReadAllText(RepoFile("Assets/Scripts/MapSpawnTable.cs"));
        Match b = Regex.Match(src, @"BramblewoodForestBossTile = new Vector2Int\((\d+), (\d+)\), ThornheartChamberBossTile = new Vector2Int\((\d+), (\d+)\)");
        Assert.True(b.Success, "boss tiles not found");
        Assert.False(Bramble.IsSolidTile(int.Parse(b.Groups[1].Value), int.Parse(b.Groups[2].Value)), "Thornfang Alpha's tile is solid");
        Assert.False(Chamber.IsSolidTile(int.Parse(b.Groups[3].Value), int.Parse(b.Groups[4].Value)), "Thornheart Treant's tile is solid");
        int a = src.IndexOf("Zone[] WillowbrookMeadows =", StringComparison.Ordinal);
        string willow = src.Substring(a, src.IndexOf("};", a, StringComparison.Ordinal) - a);
        Assert.DoesNotContain("meadowMantis", willow);
        Assert.DoesNotContain("brambleTreant", willow);
    }
}
