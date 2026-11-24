using System.Collections.Generic;
using Xunit;
using ZenoniaProto;

/// <summary>
/// WantedRoute on the real map data: from every registered map the guide arrow finds a way to each Wanted Boss map, by the fewest map changes.
/// The three boss maps are WantedBoss.Rotation's MapIds (Rpg is not compiled into this project, so they are spelled out; a renamed map fails the first test).
/// </summary>
[Collection("MapRegistry")]
public class WantedRouteTests
{
    static readonly string[] BossMaps = { FrostHollowLayout.MapId, FrostfangDenMap.MapId, SunkenReliquaryMap.MapId };

    /// <summary>Map changes from `from` to every map it reaches: the reference the route is checked against.</summary>
    static Dictionary<string, int> Hops(string from)
    {
        var dist = new Dictionary<string, int> { { from, 0 } };
        var open = new Queue<string>();
        open.Enqueue(from);
        while (open.Count > 0)
        {
            string at = open.Dequeue();
            foreach (MapPortal p in MapRegistry.Get(at).PortalsFor(true))
                if (!dist.ContainsKey(p.TargetMapId)) { dist[p.TargetMapId] = dist[at] + 1; open.Enqueue(p.TargetMapId); }
        }
        return dist;
    }

    [Fact]
    public void TheBossMapsAreTheOnesTheRotationNames()
    {
        Assert.Equal(new[] { "frostHollow", "frostfangDen", "sunkenReliquary" }, BossMaps);
        foreach (string id in BossMaps) Assert.NotNull(MapRegistry.Get(id));
    }

    [Fact]
    public void FromEveryMapTheRouteReachesEveryBossMapByTheFewestMapChanges()
    {
        foreach (string boss in BossMaps)
            foreach (IGameMap start in MapRegistry.Legacy)
            {
                if (start.Id == boss) continue;
                int expected = Hops(start.Id)[boss];
                string at = start.Id;
                int steps = 0;
                while (at != boss)
                {
                    MapPortal? p = WantedRoute.NextPortal(at, boss, true);
                    Assert.True(p.HasValue, "no way from " + at + " (started on " + start.Id + ") to " + boss);
                    Assert.Equal(WantedRoute.NextMap(at, boss, id => true), p.Value.TargetMapId);
                    Assert.Contains(MapRegistry.Get(at).PortalsFor(true), q => q.Tx == p.Value.Tx && q.Ty == p.Value.Ty && q.TargetMapId == p.Value.TargetMapId);
                    at = p.Value.TargetMapId;
                    Assert.True(++steps <= expected, start.Id + " -> " + boss + " takes more than the " + expected + " map changes needed");
                }
                Assert.Equal(expected, steps);
            }
    }

    [Fact]
    public void TheSameMapAnUnknownMapAndNoMapGiveNoPortal()
    {
        Assert.Null(WantedRoute.NextPortal(FrostHollowLayout.MapId, FrostHollowLayout.MapId, true));
        Assert.Null(WantedRoute.NextMap(FrostHollowLayout.MapId, FrostHollowLayout.MapId));
        Assert.Null(WantedRoute.NextPortal(FrostHollowLayout.MapId, "noSuchMap", true));
        Assert.Null(WantedRoute.NextPortal("noSuchMap", FrostHollowLayout.MapId, true));
        Assert.Null(WantedRoute.NextPortal(null, FrostHollowLayout.MapId, true));
        Assert.Null(WantedRoute.NextPortal(OakhavenVillageMap.MapId, null));
    }

    [Fact]
    public void OakhavenGoesThroughThatchwickToFrostHollowWithEveryGateShutToo()
    {
        Assert.Equal(ThatchVillageMap.MapId, WantedRoute.NextMap(OakhavenVillageMap.MapId, FrostHollowLayout.MapId));
        MapPortal? shut = WantedRoute.NextPortal(OakhavenVillageMap.MapId, FrostHollowLayout.MapId, false);
        Assert.True(shut.HasValue);
        Assert.Equal(ThatchVillageMap.MapId, shut.Value.TargetMapId);
        // The Den is a side dungeon of Sunbreak Foothills: from Frost Hollow the way in starts with Sunbreak.
        Assert.Equal(SunbreakFoothillsMap.MapId, WantedRoute.NextMap(FrostHollowLayout.MapId, FrostfangDenMap.MapId));
    }

    [Fact]
    public void OfSeveralDoorsToTheNextMapTheOneNearestTheHeroIsPicked()
    {
        // The Den has two exits, both to Sunbreak: the entrance on the west edge (0, 10) and the door behind the Warden (34, 10).
        MapPortal? fromWest = WantedRoute.NextPortal(FrostfangDenMap.MapId, FrostHollowLayout.MapId, null, 2, 10);
        MapPortal? fromEast = WantedRoute.NextPortal(FrostfangDenMap.MapId, FrostHollowLayout.MapId, null, 32, 10);
        Assert.Equal(0, fromWest.Value.Tx);
        Assert.Equal(34, fromEast.Value.Tx);
        Assert.Equal(SunbreakFoothillsMap.MapId, fromWest.Value.TargetMapId);
        Assert.Equal(SunbreakFoothillsMap.MapId, fromEast.Value.TargetMapId);
        // Without a hero tile: the first door in the map's own list.
        MapPortal first = System.Array.Find(MapRegistry.Get(FrostfangDenMap.MapId).PortalsFor(false), p => p.TargetMapId == SunbreakFoothillsMap.MapId);
        MapPortal? any = WantedRoute.NextPortal(FrostfangDenMap.MapId, FrostHollowLayout.MapId);
        Assert.Equal((first.Tx, first.Ty), (any.Value.Tx, any.Value.Ty));
    }
}
