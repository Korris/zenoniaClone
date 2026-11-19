using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using ZenoniaProto;

/// <summary>Sunken Reliquary after the arena grew for 30-40 player Wanted fights (19/11/2026): 58x24 in 2 ground chunks, every open tile reachable from the
/// player start, the 2-tile causeway from the hall, an arena of well over twice the old 70 tiles, and the boss tiles (MapSpawnTable / WantedBoss, parsed: they need Unity / Rpg) inside it.</summary>
[Collection("MapRegistry")]
public class SunkenReliquaryMapTests
{
    static TileMapBase Sunken { get { return (TileMapBase)MapRegistry.Get(SunkenReliquaryMap.MapId); } }
    const int ArenaX0 = 35, ArenaX1 = 53, ArenaY0 = 5, ArenaY1 = 19;

    static string RepoFile(string relative)
    {
        string dir = AppContext.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "Assets"))) dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return Path.Combine(dir, relative);
    }

    static (int w, int h) PngSize(string relative)
    {
        byte[] b = File.ReadAllBytes(RepoFile(relative));
        return ((b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23]);
    }

    static HashSet<int> ReachFromStart()
    {
        TileMapBase map = Sunken;
        var seen = new HashSet<int>();
        var open = new Stack<int>();
        int sx = 3, sy = 3; // SunkenReliquaryMap.PlayerStartTile (private); PlayerStart in feet depends on the current map's height
        open.Push(sy * map.Width + sx);
        while (open.Count > 0)
        {
            int i = open.Pop();
            int x = i % map.Width, y = i / map.Width;
            if (y < 0 || y >= map.Height || map.IsSolidTile(x, y) || !seen.Add(i)) continue;
            if (x + 1 < map.Width) open.Push(i + 1);
            if (x > 0) open.Push(i - 1);
            open.Push(i + map.Width); open.Push(i - map.Width);
        }
        return seen;
    }

    [Fact]
    public void Is58x24WithTwoGroundChunksAndAMinimapAndTheRowCountIsUnchanged()
    {
        Assert.Equal(58, Sunken.Width);
        Assert.Equal(24, Sunken.Height); // same height as before: world y and saved positions did not move
        Assert.Equal(2, Sunken.GroundChunkCols);
        Assert.Equal(1, Sunken.GroundChunkRows);
        for (int c = 0; c < 2; c++) Assert.Equal((29 * 64, 24 * 64), PngSize("Assets/Resources/" + Sunken.GroundTexture + "-r0c" + c + ".png"));
        Assert.Equal((58 * 6, 24 * 6), PngSize("Assets/Resources/" + Sunken.GroundTexture + "-minimap.png"));
        Assert.True(Sunken.Width <= ZenoniaProto.GameConfig.MaxTilesW);
    }

    [Fact]
    public void EveryOpenTileIsReachableFromTheStartAndTheBottomRowsAreBlocked()
    {
        TileMapBase map = Sunken;
        HashSet<int> reach = ReachFromStart();
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (!map.IsSolidTile(x, y)) Assert.True(reach.Contains(y * map.Width + x), "open tile " + x + "," + y + " cannot be reached from the start");
        for (int y = map.Height - 3; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++) Assert.True(map.IsSolidTile(x, y), "HUD row tile " + x + "," + y + " is open");
    }

    [Fact]
    public void TheCausewayFromTheHallIsTwoTilesWideAndTheArenaHoldsMoreThanTwiceTheOldRing()
    {
        TileMapBase map = Sunken;
        for (int x = 22; x < ArenaX0; x++)
        {
            Assert.False(map.IsSolidTile(x, 15), "causeway tile " + x + ",15 is blocked");
            Assert.False(map.IsSolidTile(x, 16), "causeway tile " + x + ",16 is blocked");
        }
        int open = 0;
        for (int y = ArenaY0; y <= ArenaY1; y++)
            for (int x = ArenaX0; x <= ArenaX1; x++) if (!map.IsSolidTile(x, y)) open++;
        Assert.True(open >= 2 * 70, "arena has only " + open + " open tiles");
        // the middle of the arena is clear (no prop footprint): a 9x5 block around the boss
        for (int y = 10; y <= 14; y++)
            for (int x = 40; x <= 48; x++) Assert.False(map.IsSolidTile(x, y), "arena middle tile " + x + "," + y + " is blocked");
    }

    [Fact]
    public void BothBossTilesStandInTheOpenMiddleOfTheArena()
    {
        Match spawn = Regex.Match(File.ReadAllText(RepoFile("Assets/Scripts/MapSpawnTable.cs")), "SunkenReliquaryBossTile = new Vector2Int\\((\\d+), (\\d+)\\)");
        Match wanted = Regex.Match(File.ReadAllText(RepoFile("Assets/Scripts/Rpg/WantedBoss.cs")), "\"mireHydraQueen\".*\"sunkenReliquary\"(?:, \\w+(?:\\.\\w+)?){6}, (\\d+), (\\d+)\\)");
        Assert.True(spawn.Success && wanted.Success);
        foreach (Match m in new[] { spawn, wanted })
        {
            int x = int.Parse(m.Groups[1].Value), y = int.Parse(m.Groups[2].Value);
            Assert.InRange(x, ArenaX0 + 4, ArenaX1 - 4);
            Assert.InRange(y, ArenaY0 + 4, ArenaY1 - 4);
            Assert.False(Sunken.IsSolidTile(x, y));
        }
    }
}
