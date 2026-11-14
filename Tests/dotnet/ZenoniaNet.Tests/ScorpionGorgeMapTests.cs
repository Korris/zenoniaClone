using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Xunit;
using ZenoniaProto;

/// <summary>The 48x32 Scorpion Gorge (World Map 2.1, region 3): size and ground chunks, the west gate to Amber Dunes, the dry riverbed crossed only by its two bridges, the
/// blocked temple (Sunspire Vault has no map yet) and the 8 spawn rects + boss tile (parsed from MapSpawnTable.cs, which needs Unity).</summary>
[Collection("MapRegistry")]
public class ScorpionGorgeMapTests
{
    static IGameMap Gorge { get { return MapRegistry.Get(ScorpionGorgeMap.MapId); } }

    static string RepoFile(string relative)
    {
        string dir = AppContext.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "Assets"))) dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return Path.Combine(dir, relative);
    }

    static void AssertPngSize(string relative, int w, int h)
    {
        byte[] b = File.ReadAllBytes(RepoFile(relative));
        int pw = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], ph = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
        Assert.Equal(w, pw); Assert.Equal(h, ph);
    }

    /// <summary>Tiles reachable from the west arrival tile, with `closed` treated as solid.</summary>
    static HashSet<int> Reach(HashSet<int> closed)
    {
        IGameMap map = Gorge;
        var seen = new HashSet<int>();
        var open = new Stack<int>();
        open.Push(ScorpionGorgeMap.WestArrivalTile.y * map.Width + ScorpionGorgeMap.WestArrivalTile.x);
        while (open.Count > 0)
        {
            int i = open.Pop();
            int x = i % map.Width, y = i / map.Width;
            if (x < 0 || y < 0 || y >= map.Height || map.IsSolidTile(x, y) || closed.Contains(i) || !seen.Add(i)) continue;
            if (x + 1 < map.Width) open.Push(i + 1);
            if (x > 0) open.Push(i - 1);
            open.Push(i + map.Width); open.Push(i - map.Width);
        }
        return seen;
    }

    static HashSet<int> Tiles(char c)
    {
        var map = (TileMapBase)Gorge;
        var set = new HashSet<int>();
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (map.CharAt(x, y) == c) set.Add(y * map.Width + x);
        return set;
    }

    [Fact]
    public void IsAHostile48x32MapWithTwoByTwoGroundChunksAndAMinimap()
    {
        Assert.Equal(48, Gorge.Width);
        Assert.Equal(32, Gorge.Height);
        Assert.True(Gorge.Hostile);
        Assert.Equal("Scorpion Gorge", Gorge.Name);
        Assert.Equal(2, Gorge.GroundChunkCols);
        Assert.Equal(2, Gorge.GroundChunkRows);
        for (int r = 0; r < 2; r++)
            for (int c = 0; c < 2; c++) AssertPngSize("Assets/Resources/" + Gorge.GroundTexture + "-r" + r + "c" + c + ".png", 1536, 1024);
        AssertPngSize("Assets/Resources/" + Gorge.GroundTexture + "-minimap.png", 288, 192);
    }

    [Fact]
    public void TheExitsAreTheWestGateAndTheTemporaryEastGateAndTheTempleGateIsBlocked()
    {
        Assert.Equal(4, Gorge.Portals.Length);
        Assert.Contains(Gorge.Portals, p => p.Tx == 0 && p.Ty == 14 && p.TargetMapId == AmberDunesMap.MapId);
        Assert.Contains(Gorge.Portals, p => p.Tx == 0 && p.Ty == 15 && p.TargetMapId == AmberDunesMap.MapId);
        Assert.False(Gorge.IsSolidTile(ScorpionGorgeMap.WestArrivalTile.x, ScorpionGorgeMap.WestArrivalTile.y), "the west arrival tile is solid");
        // The temple facade (Sunspire Vault gate) has no destination map yet: solid, and no portal starts there.
        for (int x = 22; x <= 27; x++)
            for (int y = 0; y <= 4; y++) Assert.True(Gorge.IsSolidTile(x, y), "temple facade tile " + x + "," + y + " should be blocked");
        Assert.DoesNotContain(Gorge.Portals, p => p.Ty <= 6);
        // The east wall gap (Mirewatch later) is a temporary gate to Emberfen Marsh, two tiles wide.
        Assert.Contains(Gorge.Portals, p => p.Tx == 47 && p.Ty == 14 && p.TargetMapId == EmberfenMarshMap.MapId);
        Assert.Contains(Gorge.Portals, p => p.Tx == 47 && p.Ty == 15 && p.TargetMapId == EmberfenMarshMap.MapId);
        Assert.False(Gorge.IsSolidTile(47, 14) || Gorge.IsSolidTile(47, 15), "the east gate tiles are solid");
    }

    [Fact]
    public void TheDryRiverbedOnlyOpensAtItsTwoBridges()
    {
        IGameMap map = Gorge;
        Func<int, int, int> at = (x, y) => y * map.Width + x;
        HashSet<int> all = Reach(new HashSet<int>()), decks = Tiles('B');
        Assert.Equal(8, decks.Count);   // two bridges, four deck tiles each
        Assert.Contains(at(24, 10), all);   // crossroad, east bank
        Assert.Contains(at(38, 25), all);   // boss arena
        Assert.DoesNotContain(at(24, 10), Reach(decks));   // the whole riverbed closed: the east bank is unreachable from the west gate
        // Either bridge alone still connects both banks.
        var west = new HashSet<int>(); var east = new HashSet<int>();
        foreach (int i in decks) (i % map.Width < 17 ? west : east).Add(i);
        Assert.Contains(at(24, 10), Reach(west)); Assert.Contains(at(24, 10), Reach(east));
    }

    [Fact]
    public void SpawnRectsHoldFiftyMonstersWithRoomAndStayClearOfTheArrivalPortalsAndProps()
    {
        string src = File.ReadAllText(RepoFile("Assets/Scripts/MapSpawnTable.cs"));
        int a = src.IndexOf("ScorpionGorge =", StringComparison.Ordinal);
        Assert.True(a > 0);
        string block = src.Substring(a, src.IndexOf("};", a, StringComparison.Ordinal) - a);
        MatchCollection zones = Regex.Matches(block, "new FrostHollowMap\\.Zone\\(\"(\\w+)\", (\\d+), (\\d+), (\\d+), (\\d+), (\\d+), \\d+f\\)");
        Assert.Equal(8, zones.Count);
        var spots = new List<Vector2Int> { ScorpionGorgeMap.WestArrivalTile };
        foreach (MapPortal p in Gorge.Portals) spots.Add(new Vector2Int(p.Tx, p.Ty));
        int total = 0;
        foreach (Match z in zones)
        {
            int n = int.Parse(z.Groups[2].Value), zx = int.Parse(z.Groups[3].Value), zy = int.Parse(z.Groups[4].Value), zw = int.Parse(z.Groups[5].Value), zh = int.Parse(z.Groups[6].Value);
            total += n;
            int free = 0;
            for (int y = zy; y < zy + zh; y++)
                for (int x = zx; x < zx + zw; x++)
                {
                    if (Gorge.IsSolidTile(x, y)) continue;
                    free++;
                    foreach (Vector2Int s in spots)
                        Assert.True((x - s.x) * (x - s.x) + (y - s.y) * (y - s.y) >= 25, z.Groups[1].Value + " rect tile " + x + "," + y + " is under 5 tiles from " + s.x + "," + s.y);
                    Assert.NotEqual('B', ((TileMapBase)Gorge).CharAt(x, y));
                }
            Assert.True(free >= 2 * n, z.Groups[1].Value + " rect has " + free + " free tiles for " + n);
            foreach (MapProp p in Gorge.Props)
                Assert.False(p.Tx >= zx && p.Tx < zx + zw && p.Ty >= zy && p.Ty < zy + zh, p.Type + " at " + p.Tx + "," + p.Ty + " stands inside the " + z.Groups[1].Value + " rect");
        }
        Assert.Equal(50, total);
        Match boss = Regex.Match(src, "ScorpionGorgeBossTile = new Vector2Int\\((\\d+), (\\d+)\\)");
        Assert.True(boss.Success);
        Assert.False(Gorge.IsSolidTile(int.Parse(boss.Groups[1].Value), int.Parse(boss.Groups[2].Value)), "the boss tile is solid");
        Assert.Contains("case ScorpionGorgeId: return MonsterDef.DuneDevourer;", src);
    }

    [Fact]
    public void PortalTablesOfTheLinkedMapsOnlyUseConstantsOfOtherMaps()
    {
        // Two maps that read each other's static fields while building their portal tables form a static-initialiser cycle: in the Unity player one side read (0,0).
        // (See docs/map-design-rules.md / SunbreakFoothillsMap.SouthArrivalTx.) No "<Map>.<Something>Tile.x" inside a MapPortal of either linked class.
        foreach (string file in new[] { "ScorpionGorgeMap.cs", "AmberDunesMap.cs" })
            foreach (string line in File.ReadAllLines(RepoFile("Assets/Scripts/Maps/" + file)))
                if (line.Contains("new MapPortal(")) Assert.False(Regex.IsMatch(line, @"\w+\.\w+Tile\.[xy]"), file + " builds a portal from another class's static field: " + line.Trim());
    }

    [Fact]
    public void NoPropStandsOnABridgeOrOnAPortalAndEveryPropHasArtAndADef()
    {
        var map = (TileMapBase)Gorge;
        foreach (MapProp p in Gorge.Props)
        {
            PropDef d;
            Assert.True(VillagePropDefs.TryGet(p.Type, out d), "no PropDef for " + p.Type);
            Assert.True(File.Exists(RepoFile("Assets/Resources/Sprites/village/props/" + p.Type + ".png")), "no art for " + p.Type);
            for (int x = p.Tx; x < p.Tx + d.FootW; x++)
            {
                Assert.NotEqual('B', map.CharAt(x, p.Ty));
                int px = x;
                Assert.DoesNotContain(Gorge.Portals, q => q.Tx == px && q.Ty == p.Ty);
            }
        }
    }
}
