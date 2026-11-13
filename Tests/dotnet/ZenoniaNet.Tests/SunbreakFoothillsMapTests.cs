using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Xunit;
using ZenoniaProto;

/// <summary>The 48x32 Sunbreak Foothills (World Map 2.1): size and ground chunks, the three exits, the two bridges and two staircases as the only crossings, the west dead end,
/// and the 8 spawn rects + boss tile (parsed from MapSpawnTable.cs, which needs Unity).</summary>
[Collection("MapRegistry")]
public class SunbreakFoothillsMapTests
{
    static IGameMap Sunbreak { get { return MapRegistry.Get(SunbreakFoothillsMap.MapId); } }

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

    /// <summary>Tiles reachable from the south arrival tile, with `closed` treated as solid.</summary>
    static HashSet<int> Reach(HashSet<int> closed)
    {
        IGameMap map = Sunbreak;
        var seen = new HashSet<int>();
        var open = new Stack<int>();
        open.Push(SunbreakFoothillsMap.SouthArrivalTile.y * map.Width + SunbreakFoothillsMap.SouthArrivalTile.x);
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
        var map = (TileMapBase)Sunbreak;
        var set = new HashSet<int>();
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (map.CharAt(x, y) == c) set.Add(y * map.Width + x);
        return set;
    }

    [Fact]
    public void IsAHostile48x32MapWithTwoByTwoGroundChunksAndAMinimap()
    {
        Assert.Equal(48, Sunbreak.Width);
        Assert.Equal(32, Sunbreak.Height);
        Assert.True(Sunbreak.Hostile);
        Assert.Equal("Sunbreak Foothills", Sunbreak.Name);
        Assert.Equal(2, Sunbreak.GroundChunkCols);
        Assert.Equal(2, Sunbreak.GroundChunkRows);
        for (int r = 0; r < 2; r++)
            for (int c = 0; c < 2; c++) AssertPngSize("Assets/Resources/" + Sunbreak.GroundTexture + "-r" + r + "c" + c + ".png", 1536, 1024);
        AssertPngSize("Assets/Resources/" + Sunbreak.GroundTexture + "-minimap.png", 288, 192);
    }

    [Fact]
    public void ThreeExitsLeadToFrostHollowTheDenAndSunspire()
    {
        Assert.Equal(7, Sunbreak.Portals.Length);
        for (int x = 20; x <= 22; x++)
        {
            int px = x;
            Assert.Contains(Sunbreak.Portals, p => p.Tx == px && p.Ty == 28 && p.TargetMapId == FrostHollowLayout.MapId && p.TargetTx == 68 && p.TargetTy == 22);
        }
        Assert.Contains(Sunbreak.Portals, p => p.Tx == 23 && p.Ty == 3 && p.TargetMapId == FrostfangDenMap.MapId && p.TargetTx == 2 && p.TargetTy == 10);
        Assert.Contains(Sunbreak.Portals, p => p.Tx == 24 && p.Ty == 3 && p.TargetMapId == FrostfangDenMap.MapId && p.TargetTx == 2 && p.TargetTy == 10);
        Assert.Contains(Sunbreak.Portals, p => p.Tx == 47 && p.Ty == 15 && p.TargetMapId == SunspireOasisMap.MapId && p.TargetTx == 2 && p.TargetTy == 10);
        Assert.Contains(Sunbreak.Portals, p => p.Tx == 47 && p.Ty == 16 && p.TargetMapId == SunspireOasisMap.MapId && p.TargetTx == 2 && p.TargetTy == 11);
        foreach (Vector2Int a in new[] { SunbreakFoothillsMap.SouthArrivalTile, SunbreakFoothillsMap.CaveArrivalTile, SunbreakFoothillsMap.DenExitArrivalTile, SunbreakFoothillsMap.EastArrivalTile })
            Assert.False(Sunbreak.IsSolidTile(a.x, a.y), "arrival " + a.x + "," + a.y + " is solid");
        // The west road is a dead end: nothing walkable on the west edge, no portal there.
        for (int y = 0; y < 32; y++) Assert.True(Sunbreak.IsSolidTile(0, y), "west edge row " + y);
        Assert.DoesNotContain(Sunbreak.Portals, p => p.Tx == 0);
    }

    [Fact]
    public void TheStreamThePlateauWallAndTheCaveCliffOnlyOpenAtTheirBridgesAndStairs()
    {
        IGameMap map = Sunbreak;
        Func<int, int, int> at = (x, y) => y * map.Width + x;
        HashSet<int> all = Reach(new HashSet<int>()), decks = Tiles('B'), treads = Tiles('s');
        Assert.Equal(8, decks.Count);   // two diagonal decks of four tiles
        Assert.Equal(8, treads.Count);  // two staircases, two tiles wide and two deep
        Assert.Contains(at(25, 13), all); Assert.Contains(at(38, 4), all); Assert.Contains(at(23, 3), all);
        Assert.DoesNotContain(at(25, 13), Reach(decks));                       // central field, north of the stream
        var plateauStairs = new HashSet<int>(); var caveStairs = new HashSet<int>();
        foreach (int i in treads) (i % map.Width > 30 ? plateauStairs : caveStairs).Add(i);
        Assert.DoesNotContain(at(38, 4), Reach(plateauStairs));                // boss plateau
        Assert.DoesNotContain(at(23, 3), Reach(caveStairs));                   // cave mouth
        // Either bridge alone still connects both banks.
        var west = new HashSet<int>(); var east = new HashSet<int>();
        foreach (int i in decks) (i % map.Width < 20 ? west : east).Add(i);
        Assert.Contains(at(25, 13), Reach(west)); Assert.Contains(at(25, 13), Reach(east));
    }

    [Fact]
    public void SpawnRectsHoldFiftyMonstersWithRoomAndStayClearOfArrivalsPortalsAndProps()
    {
        string src = File.ReadAllText(RepoFile("Assets/Scripts/MapSpawnTable.cs"));
        int a = src.IndexOf("SunbreakFoothills =", StringComparison.Ordinal);
        Assert.True(a > 0);
        string block = src.Substring(a, src.IndexOf("};", a, StringComparison.Ordinal) - a);
        MatchCollection zones = Regex.Matches(block, "new FrostHollowMap\\.Zone\\(\"(\\w+)\", (\\d+), (\\d+), (\\d+), (\\d+), (\\d+), \\d+f\\)");
        Assert.Equal(8, zones.Count);
        var spots = new List<Vector2Int> { SunbreakFoothillsMap.SouthArrivalTile, SunbreakFoothillsMap.CaveArrivalTile, SunbreakFoothillsMap.DenExitArrivalTile, SunbreakFoothillsMap.EastArrivalTile };
        foreach (MapPortal p in Sunbreak.Portals) spots.Add(new Vector2Int(p.Tx, p.Ty));
        int total = 0;
        foreach (Match z in zones)
        {
            int n = int.Parse(z.Groups[2].Value), zx = int.Parse(z.Groups[3].Value), zy = int.Parse(z.Groups[4].Value), zw = int.Parse(z.Groups[5].Value), zh = int.Parse(z.Groups[6].Value);
            total += n;
            int free = 0;
            for (int y = zy; y < zy + zh; y++)
                for (int x = zx; x < zx + zw; x++)
                {
                    if (Sunbreak.IsSolidTile(x, y)) continue;
                    free++;
                    foreach (Vector2Int s in spots)
                        Assert.True((x - s.x) * (x - s.x) + (y - s.y) * (y - s.y) >= 25, z.Groups[1].Value + " rect tile " + x + "," + y + " is under 5 tiles from " + s.x + "," + s.y);
                    char c = ((TileMapBase)Sunbreak).CharAt(x, y);
                    Assert.True(c != 'B' && c != 's', "a spawn rect covers a bridge or a stair at " + x + "," + y);
                }
            Assert.True(free >= 2 * n, z.Groups[1].Value + " rect has " + free + " free tiles for " + n);
            foreach (MapProp p in Sunbreak.Props)
                Assert.False(p.Tx >= zx && p.Tx < zx + zw && p.Ty >= zy && p.Ty < zy + zh, p.Type + " at " + p.Tx + "," + p.Ty + " stands inside the " + z.Groups[1].Value + " rect");
        }
        Assert.Equal(50, total);
        Match boss = Regex.Match(src, "SunbreakFoothillsBossTile = new Vector2Int\\((\\d+), (\\d+)\\)");
        Assert.True(boss.Success);
        Assert.False(Sunbreak.IsSolidTile(int.Parse(boss.Groups[1].Value), int.Parse(boss.Groups[2].Value)), "the boss tile is solid");
        Assert.Contains("case SunbreakFoothillsId: return MonsterDef.RidgehornBehemoth;", src);
    }

    [Fact]
    public void PortalTablesOfTheLinkedMapsOnlyUseConstantsOfOtherMaps()
    {
        // Two maps that read each other's static fields while building their portal tables form a static-initialiser cycle: in the Unity player one side read (0,0) and Sunbreak's
        // south road sent heroes to Frost Hollow's tile (0,0). This runtime initialises lazily and cannot show it, so the source is checked: no "<Map>.<Something>Tile.x" inside a MapPortal.
        foreach (string file in new[] { "SunbreakFoothillsMap.cs", "FrostHollowLayout.cs", "FrostfangDenMap.cs", "SunspireOasisMap.cs" })
            foreach (string line in File.ReadAllLines(RepoFile("Assets/Scripts/Maps/" + file)))
                if (line.Contains("new MapPortal(")) Assert.False(Regex.IsMatch(line, @"\w+\.\w+Tile\.[xy]"), file + " builds a portal from another class's static field: " + line.Trim());
    }

    [Fact]
    public void NoPropStandsOnABridgeAStairOrAPortalAndEveryPropHasArtAndADef()
    {
        var map = (TileMapBase)Sunbreak;
        foreach (MapProp p in Sunbreak.Props)
        {
            PropDef d;
            Assert.True(VillagePropDefs.TryGet(p.Type, out d), "no PropDef for " + p.Type);
            Assert.True(File.Exists(RepoFile("Assets/Resources/Sprites/village/props/" + p.Type + ".png")), "no art for " + p.Type);
            for (int x = p.Tx; x < p.Tx + d.FootW; x++)
            {
                char c = map.CharAt(x, p.Ty);
                Assert.True(c != 'B' && c != 's', p.Type + " stands on a bridge or a stair at " + x + "," + p.Ty);
                int px = x;
                Assert.DoesNotContain(Sunbreak.Portals, q => q.Tx == px && q.Ty == p.Ty);
            }
        }
    }
}
