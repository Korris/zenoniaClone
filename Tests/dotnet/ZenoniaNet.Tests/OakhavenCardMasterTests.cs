using System.Collections.Generic;
using UnityEngine;
using Xunit;
using ZenoniaProto;

/// <summary>
/// Card Master Orin (Monster Card phase 08) stands on a grass tile beside the south gate road of Oakhaven Village (docs/map-design-rules.md: roads, gates and
/// bridges stay clear, every walkable tile stays reachable). The real map class is compiled here (MapGraphStubs.cs).
/// </summary>
[Collection("MapRegistry")]
public class OakhavenCardMasterTests
{
    static IGameMap Oak { get { return MapRegistry.Get(OakhavenVillageMap.MapId); } }

    static HashSet<int> Reachable(IGameMap map, int sx, int sy, int freeTile = -1)
    {
        var seen = new HashSet<int>();
        var q = new Queue<int>();
        int W = map.Width;
        q.Enqueue(sy * W + sx); seen.Add(sy * W + sx);
        int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
        while (q.Count > 0)
        {
            int c = q.Dequeue(), x = c % W, y = c / W;
            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i], ny = y + dy[i];
                if ((map.IsSolidTile(nx, ny) && ny * W + nx != freeTile) || !seen.Add(ny * W + nx)) continue;
                q.Enqueue(ny * W + nx);
            }
        }
        return seen;
    }

    [Fact]
    public void CardMasterStandsOnABlockedRoadsideTileAwayFromGatesAndNpcs()
    {
        MapNpc orin = System.Array.Find(Oak.Npcs, n => n.Id == "cardMaster");
        Assert.Equal("cardMaster", orin.Id);
        Assert.True(Oak.IsSolidTile(orin.Tx, orin.Ty), "the NPC tile blocks the hero like every other villager");
        foreach (MapPortal p in Oak.Portals) Assert.False(p.Tx == orin.Tx && p.Ty == orin.Ty, "the NPC must not stand on a gate tile");
        int others = 0;
        foreach (MapNpc n in Oak.Npcs) if (n.Tx == orin.Tx && n.Ty == orin.Ty) others++;
        Assert.Equal(1, others);
        // Near the south gate (Willowbrook): within 4 tiles of one of its trigger tiles.
        MapPortal gate = System.Array.Find(Oak.Portals, p => p.TargetMapId == WillowbrookMeadowsMap.MapId);
        Assert.InRange(Mathf.Abs(gate.Tx - orin.Tx) + Mathf.Abs(gate.Ty - orin.Ty), 1, 5);
    }

    [Fact]
    public void TheSouthGateRoadKeepsAtLeastFourFreeTilesAndEveryGateStaysReachable()
    {
        MapNpc orin = System.Array.Find(Oak.Npcs, n => n.Id == "cardMaster");
        int free = 0;
        for (int x = 0; x < Oak.Width; x++) if (!Oak.IsSolidTile(x, orin.Ty) && x >= 10 && x <= 18) free++;
        Assert.True(free >= 4, "the road row beside Orin has " + free + " free tiles");
        int sx = 20, sy = 12; // OakhavenVillageMap.PlayerStartTile (private): the plaza
        HashSet<int> reach = Reachable(Oak, sx, sy);
        foreach (MapPortal p in Oak.Portals) Assert.Contains(p.Ty * Oak.Width + p.Tx, reach);
        // Orin must not cut off any walkable tile: reachable tiles with him = reachable without him, minus his own tile (other villagers already leave west-road tiles
        // unreachable; that is older and not his doing).
        HashSet<int> without = Reachable(Oak, sx, sy, orin.Ty * Oak.Width + orin.Tx);
        Assert.Equal(without.Count - 1, reach.Count);
    }
}
