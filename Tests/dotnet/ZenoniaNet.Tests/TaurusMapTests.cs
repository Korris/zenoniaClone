using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Xunit;
using ZenoniaProto;
using ZenoniaProto.Rpg;

/// <summary>Taurus V4 phase 02 greybox: Willowbrook Village 44x32 (safe, five zones, six NPCs, south gate) and Clover Meadows 96x64 (Z1 Cloverfield, camps C01 / C02 from MapSpawnTable.cs).</summary>
[Collection("MapRegistry")]
public class TaurusMapTests
{
    static IGameMap Village { get { return MapRegistry.Get(WorldFlags.StartMapId); } }
    static IGameMap Meadows { get { return MapRegistry.Get(TauCloverMeadowsMap.MapId); } }

    internal static string RepoFile(string relative)
    {
        string dir = AppContext.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "Assets"))) dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return Path.Combine(dir, relative);
    }

    internal static double Dist(int ax, int ay, int bx, int by) { return Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by)); }

    internal static HashSet<int> Reach(IGameMap map, int sx, int sy)
    {
        var seen = new HashSet<int>();
        var open = new Stack<int>();
        open.Push(sy * map.Width + sx);
        while (open.Count > 0)
        {
            int i = open.Pop();
            int x = i % map.Width, y = i / map.Width;
            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height || map.IsSolidTile(x, y) || !seen.Add(i)) continue;
            open.Push(i + 1); open.Push(i - 1); open.Push(i + map.Width); open.Push(i - map.Width);
        }
        return seen;
    }

    /// <summary>PlayerStart is built with GameConfig.MapH of the CURRENT map (the sim runs one map), so the map has to be current while it is read.</summary>
    internal static Vector2Int StartTile(IGameMap map)
    {
        try
        {
            MapRegistry.UseZoneMap(map);
            Vector2 f = map.PlayerStart; // feet: x = (tx + .5) * 16, y(up) = H*16 - ((ty + 1) * 16 - 2)
            return new Vector2Int(Mathf.FloorToInt(f.x / 16f), map.Height - 1 - Mathf.FloorToInt(f.y / 16f));
        }
        finally { MapRegistry.ResetState(); }
    }

    static void AssertAllWalkableTilesReachable(IGameMap map)
    {
        Vector2Int s = StartTile(map);
        Assert.False(map.IsSolidTile(s.x, s.y), map.Id + " start tile is solid");
        HashSet<int> reach = Reach(map, s.x, s.y);
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (!map.IsSolidTile(x, y)) Assert.True(reach.Contains(y * map.Width + x), map.Id + " tile " + x + "," + y + " is open but unreachable");
    }

    [Fact]
    public void BothMapsAreLiveWithTheDesignedSizes()
    {
        Assert.Equal(44, Village.Width); Assert.Equal(32, Village.Height); Assert.False(Village.Hostile);
        Assert.Equal(96, Meadows.Width); Assert.Equal(64, Meadows.Height); Assert.True(Meadows.Hostile);
        Assert.True(Meadows.Width <= GameConfig.MaxTilesW && Meadows.Height <= GameConfig.MaxTilesH);
        Assert.Contains(MapRegistry.Live, m => m.Id == Village.Id);
        Assert.Equal("tauWillowbrookVillage", WorldFlags.StartMapId);
    }

    [Fact]
    public void EveryOpenTileOfBothMapsIsReachableFromTheStart()
    {
        AssertAllWalkableTilesReachable(Village);
        AssertAllWalkableTilesReachable(Meadows);
    }

    [Fact]
    public void TheBottomThreeRowsUnderTheHudAreBlocked()
    {
        foreach (IGameMap m in new[] { Village, Meadows })
            for (int y = m.Height - 3; y < m.Height; y++)
                for (int x = 0; x < m.Width; x++) Assert.True(m.IsSolidTile(x, y), m.Id + " row " + y);
    }

    [Fact]
    public void ThePortalsAreWalkableLeadBothWaysAndLandOnWalkableTilesOffTheirTriggers()
    {
        foreach (IGameMap m in new[] { Village, Meadows })
        {
            Assert.NotEmpty(m.Portals);
            foreach (MapPortal p in m.Portals)
            {
                Assert.False(m.IsSolidTile(p.Tx, p.Ty), m.Id + " portal tile solid");
                IGameMap target = MapRegistry.Get(p.TargetMapId);
                Assert.NotNull(target);
                Assert.False(target.IsSolidTile(p.TargetTx, p.TargetTy), "arrival tile solid in " + target.Id);
                Assert.DoesNotContain(target.Portals, q => q.Tx == p.TargetTx && q.Ty == p.TargetTy); // landing on a trigger would bounce straight back
                Assert.Contains(target.Portals, q => q.TargetMapId == m.Id); // the way back exists
                Assert.True(Reach(target, StartTile(target).x, StartTile(target).y).Contains(p.TargetTy * target.Width + p.TargetTx), "arrival not reachable in " + target.Id);
            }
        }
        Assert.All(Village.Portals, p => Assert.Equal(TauCloverMeadowsMap.MapId, p.TargetMapId));
        Assert.All(Meadows.Portals.Where(p => p.TargetMapId != TauAncientRootShrineMap.MapId), p => Assert.Equal(Village.Id, p.TargetMapId)); // the other four go to the shrine (phase 06)
    }

    [Fact]
    public void VillageHasTheSixNpcsOnOpenTilesWithRoomAroundThem()
    {
        string[] ids = { "elderRowan", "cardMaster", "tauBram", "tauLumi", "tauMira", "tauFinn" };
        Assert.Equal(ids.Length, Village.Npcs.Length);
        foreach (string id in ids) Assert.Contains(Village.Npcs, n => n.Id == id);
        var seen = new HashSet<string>();
        foreach (MapNpc n in Village.Npcs)
        {
            Assert.True(seen.Add(n.Id));
            Assert.True(Village.IsSolidTile(n.Tx, n.Ty), "an NPC blocks its own tile");
            int open = 0;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if (!Village.IsSolidTile(n.Tx + dx, n.Ty + dy)) open++;
            Assert.True(open >= 5, n.Id + " is boxed in");
        }
    }

    [Fact]
    public void TheVillageRoadsStayOpenAndCardCornerIsClearOfSpawnsAndTheOakIsSolid()
    {
        Assert.True(Village.IsSolidTile(22, 13)); // the old oak
        for (int y = 20; y <= 28; y++) for (int x = 20; x <= 23; x++) Assert.False(Village.IsSolidTile(x, y), "gate road " + x + "," + y);
        string spawn = File.ReadAllText(RepoFile("Assets/Scripts/MapSpawnTable.cs")); // MapSpawnTable needs Unity: read the source
        Assert.Contains("case TauWillowbrookVillageId: return NoZones;", spawn); // no monsters in the village
        Assert.Contains("case TauCloverMeadowsId: return TauCloverMeadows;", spawn);
        Assert.Contains("case TauWillowbrookVillageId: return null;", spawn); // no boss in the village, never the Frostfang King
        Assert.Contains("case TauCloverMeadowsId: return MonsterDef.TauBramblebackMatriarch;", spawn); // the field boss (phase 05)
    }

    internal static List<Match> CampRects()
    {
        string src = File.ReadAllText(RepoFile("Assets/Scripts/MapSpawnTable.cs"));
        int a = src.IndexOf("TauCloverMeadows =", StringComparison.Ordinal);
        Assert.True(a > 0);
        string block = src.Substring(a, src.IndexOf("};", a, StringComparison.Ordinal) - a);
        var list = new List<Match>();
        foreach (Match m in Regex.Matches(block, "new FrostHollowMap\\.Zone\\(\"(\\w+)\", (\\d+), (\\d+), (\\d+), (\\d+), (\\d+), (\\d+)f(?:, (true|false))?\\)")) list.Add(m);
        return list;
    }

    [Fact]
    public void CampsC01AndC02AreSlimeRectsOnOpenGrassAwayFromTheRoadAndTheGate()
    {
        List<Match> camps = CampRects().GetRange(0, 2); // the first two entries (the other camps: TaurusMeadowCampsTests)
        Assert.Equal("9", camps[0].Groups[2].Value); Assert.Equal("true", camps[0].Groups[8].Value);   // C01: 9, passive
        Assert.Equal("7", camps[1].Groups[2].Value); Assert.NotEqual("true", camps[1].Groups[8].Value); // C02: 7
        Assert.All(camps, c => { Assert.Equal("tauCloverSlime", c.Groups[1].Value); Assert.Equal("12000", c.Groups[7].Value); });
        var entrances = new List<Vector2Int> { StartTile(Meadows) };
        foreach (MapPortal p in Meadows.Portals) entrances.Add(new Vector2Int(p.Tx, p.Ty));
        var road = new List<Vector2Int>();
        for (int y = 0; y < Meadows.Height; y++) for (int x = 0; x < Meadows.Width; x++) if (((TileMapBase)Meadows).CharAt(x, y) == 'd') road.Add(new Vector2Int(x, y));
        foreach (Match c in camps)
        {
            int count = int.Parse(c.Groups[2].Value), tx = int.Parse(c.Groups[3].Value), ty = int.Parse(c.Groups[4].Value), w = int.Parse(c.Groups[5].Value), h = int.Parse(c.Groups[6].Value);
            int free = 0;
            for (int y = ty; y < ty + h; y++)
                for (int x = tx; x < tx + w; x++)
                {
                    Assert.False(Meadows.IsSolidTile(x, y), "camp tile solid " + x + "," + y);
                    char ch = ((TileMapBase)Meadows).CharAt(x, y);
                    Assert.True(ch == 'g', "camp tile " + x + "," + y + " is " + ch + " (road / plaza?)");
                    free++;
                    foreach (Vector2Int e in entrances) Assert.True(Dist(x, y, e.x, e.y) >= 5f, "camp tile " + x + "," + y + " too close to " + e);
                    foreach (Vector2Int r in road) Assert.True(Dist(x, y, r.x, r.y) >= 3f, "camp tile " + x + "," + y + " hugs the road");
                }
            Assert.True(free >= 2 * count, "room for the monsters");
        }
    }

    [Fact]
    public void TheMeadowRoadRunsFromTheGateNorthAndLevelsGrowWithDistance()
    {
        Vector2Int s = StartTile(Meadows);
        Assert.True(s.y >= 55); // the south: new heroes start near the village gate
        HashSet<int> reach = Reach(Meadows, s.x, s.y);
        List<Match> camps = CampRects();
        // C01 is nearer the gate than C02 (Slimes only here; the level steps up in later camps)
        Assert.True(int.Parse(camps[0].Groups[4].Value) > int.Parse(camps[1].Groups[4].Value));
        Assert.True(reach.Contains(40 * Meadows.Width + 50)); // the road reaches the north end of Z1
        Assert.False(Meadows.IsSolidTile(48, 20)); // phase 05: the road goes on north of the bridge
    }

    [Fact]
    public void GroundChunksMinimapAndBorderExistForBothMaps()
    {
        // phase 04 art: Tools/build-tau-25d-ground.py writes <ground>-r{r}c{c}.png at 64 px per tile, the border tile and the minimap (place-tau-25d-props.py --minimap)
        foreach (IGameMap m in new[] { Village, Meadows })
        {
            Assert.True(m.GroundChunkCols > 1 && m.GroundChunkRows > 1);
            for (int r = 0; r < m.GroundChunkRows; r++)
                for (int c = 0; c < m.GroundChunkCols; c++)
                {
                    byte[] b = File.ReadAllBytes(RepoFile("Assets/Resources/" + m.GroundTexture + "-r" + r + "c" + c + ".png"));
                    int pw = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], ph = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
                    Assert.Equal(m.Width * 64 / m.GroundChunkCols, pw); Assert.Equal(m.Height * 64 / m.GroundChunkRows, ph);
                    Assert.True(pw <= 2048 && ph <= 2048);
                }
            Assert.True(File.Exists(RepoFile("Assets/Resources/" + m.GroundTexture + "-border.png")));
            Assert.True(File.Exists(RepoFile("Assets/Resources/" + m.GroundTexture + "-minimap.png")));
        }
    }

    [Fact]
    public void PropsOnlyBlockOffTheLanesAndHaveArt()
    {
        // every prop has a def + PNG; a blocking footprint never lands on a road, plaza, bridge or portal tile (Tools/place-tau-25d-props.py checks the rest)
        foreach (IGameMap m in new[] { Village, Meadows })
            foreach (MapProp p in m.Props)
            {
                PropDef d;
                Assert.True(VillagePropDefs.TryGet(p.Type, out d), p.Type);
                Assert.True(File.Exists(RepoFile("Assets/Resources/Sprites/village/props/" + p.Type + ".png")), p.Type);
                for (int y = p.Ty - d.FootH + 1; y <= p.Ty; y++)
                    for (int x = p.Tx; x < p.Tx + d.FootW; x++)
                    {
                        char ch = ((TileMapBase)m).CharAt(x, y);
                        Assert.False(ch == 'd' || ch == 'p' || ch == 'B', p.Type + " blocks " + ch + " at " + x + "," + y);
                        foreach (MapPortal q in m.Portals) Assert.False(q.Tx == x && q.Ty == y, p.Type + " on a portal");
                    }
            }
    }
}
