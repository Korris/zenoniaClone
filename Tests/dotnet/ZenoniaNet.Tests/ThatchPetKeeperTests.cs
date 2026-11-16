using System.Collections.Generic;
using Xunit;
using ZenoniaProto;

/// <summary>Thatchwick's pet keeper Lumi (16/11/2026): stands on the plaza edge, blocks only her tile, and every open tile still reaches the start (docs/map-design-rules.md 1, 6).</summary>
public class ThatchPetKeeperTests
{
    [Fact]
    public void LumiStandsOffTheRoadsAndEveryOpenTileStillReachesTheStart()
    {
        IGameMap map = MapRegistry.Get(ThatchVillageMap.MapId);
        Assert.Contains(map.Npcs, n => n.Id == "petKeeperLumi" && n.Tx == 22 && n.Ty == 14);
        Assert.True(map.IsSolidTile(22, 14)); // her own tile
        int w = map.Width, h = map.Height;
        var seen = new HashSet<int>();
        var open = new Stack<int>();
        open.Push(14 * w + 17); // PlayerStartTile
        while (open.Count > 0)
        {
            int i = open.Pop();
            int x = i % w, y = i / w;
            if (i < 0 || y >= h || map.IsSolidTile(x, y) || !seen.Add(i)) continue;
            if (x < w - 1) open.Push(i + 1);
            if (x > 0) open.Push(i - 1);
            open.Push(i + w); open.Push(i - w);
        }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (!map.IsSolidTile(x, y)) Assert.True(seen.Contains(y * w + x), "open tile " + x + "," + y + " cannot reach the start");
        foreach (MapPortal p in map.Portals) Assert.Contains(p.Ty * w + p.Tx, seen); // both gates
        Assert.False(map.IsSolidTile(21, 14)); Assert.False(map.IsSolidTile(22, 13)); // reachable from the plaza (west) and from the north; trees behind her (east, south)
    }
}
