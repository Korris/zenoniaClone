using System.Collections.Generic;
using UnityEngine;
using Xunit;
using ZenoniaProto;
using ZenoniaProto.Net;

/// <summary>
/// The real map data (Assets/Scripts/Maps, compiled here against MapGraphStubs.cs): every portal of every registered map must work on the server, including the maps that get added
/// later, because the tests walk MapRegistry.All. The stubs stand in for the few engine types and game classes the maps touch.
/// </summary>
public class MapGraphTests
{
    public static IEnumerable<object[]> AllPortals()
    {
        foreach (IGameMap map in MapRegistry.All)
            foreach (MapPortal p in map.PortalsFor(true))
                yield return new object[] { map.Id, p.Tx, p.Ty, p.TargetMapId, p.TargetTx, p.TargetTy };
    }

    [Theory]
    [MemberData(nameof(AllPortals))]
    public void EveryPortalLeadsToAWalkableTileOfARegisteredMap(string mapId, int tx, int ty, string targetId, int targetTx, int targetTy)
    {
        IGameMap target = MapRegistry.Get(targetId);
        Assert.True(target != null, mapId + " has a portal to unknown map " + targetId);
        Assert.False(target.IsSolidTile(targetTx, targetTy), mapId + " -> " + targetId + " arrives on a solid tile");
        Assert.False(MapRegistry.Get(mapId).IsSolidTile(tx, ty), mapId + " has a portal on a solid tile " + tx + "," + ty);
        Assert.NotEqual(mapId, targetId);
    }

    [Theory]
    [MemberData(nameof(AllPortals))]
    public void TheArrivalSpotIsItsOwnTileForThePortalRule(string mapId, int tx, int ty, string targetId, int targetTx, int targetTy)
    {
        Vector2 feet = FrostHollowMap.TileFeet(targetTx, targetTy, 2f);
        int fx, fy;
        PortalTile.OfFeet(feet.x, feet.y, TileMapBase.Tile, TileMapBase.H, out fx, out fy);
        Assert.Equal(targetTx, fx);
        Assert.Equal(targetTy, fy);
    }

    [Fact]
    public void EveryMapIsReachableFromTheDefaultStart()
    {
        var seen = new HashSet<string>();
        var open = new Queue<IGameMap>();
        open.Enqueue(MapRegistry.Get(FrostHollowLayout.MapId));
        seen.Add(FrostHollowLayout.MapId);
        while (open.Count > 0)
            foreach (MapPortal p in open.Dequeue().PortalsFor(true))
                if (seen.Add(p.TargetMapId)) open.Enqueue(MapRegistry.Get(p.TargetMapId));
        foreach (IGameMap map in MapRegistry.All) Assert.True(seen.Contains(map.Id), map.Id + " cannot be reached through any portal");
    }

    [Fact]
    public void NeitherAStartTileNorAnArrivalTileIsATrigger()
    {
        // PortalGate would catch both, but a hero placed on a trigger could not leave the way it expects: respawn and arrival have to be clear of the map's portals.
        MapPortal found;
        foreach (IGameMap map in MapRegistry.All)
        {
            Assert.False(MapPortals.TryFind(map.PortalsFor(true), map.PlayerStart.x, map.PlayerStart.y, out found), map.Id + " starts on a portal");
            foreach (MapPortal p in map.PortalsFor(true))
            {
                Vector2 feet = FrostHollowMap.TileFeet(p.TargetTx, p.TargetTy, 2f);
                Assert.False(MapPortals.TryFind(MapRegistry.Get(p.TargetMapId).PortalsFor(true), feet.x, feet.y, out found), map.Id + " -> " + p.TargetMapId + " arrives on a portal");
            }
        }
    }

    [Fact]
    public void NewEastRouteGoesBothWays()
    {
        IGameMap oak = MapRegistry.Get(OakhavenVillageMap.MapId), thatch = MapRegistry.Get(ThatchVillageMap.MapId), willow = MapRegistry.Get(WillowbrookMeadowsMap.MapId);
        Assert.Contains(oak.Portals, p => p.TargetMapId == ThatchVillageMap.MapId);
        Assert.Contains(thatch.Portals, p => p.TargetMapId == OakhavenVillageMap.MapId);
        Assert.Contains(thatch.Portals, p => p.TargetMapId == WillowbrookMeadowsMap.MapId);
        // Willowbrook is a dead end: its only portals go back to Thatchwick.
        Assert.All(willow.Portals, p => Assert.Equal(ThatchVillageMap.MapId, p.TargetMapId));
        Assert.False(thatch.Hostile);
        Assert.True(willow.Hostile);
        Assert.Empty(thatch.Npcs);
        // Standing on an arrival tile there is a portal back to the map we came from.
        foreach (IGameMap map in new[] { oak, thatch, willow })
            foreach (MapPortal p in map.Portals)
            {
                IGameMap target = MapRegistry.Get(p.TargetMapId);
                Vector2 feet = FrostHollowMap.TileFeet(p.TargetTx, p.TargetTy, 2f);
                Assert.Contains(target.Portals, q => q.TargetMapId == map.Id);
                Assert.False(MapPortals.TryFind(target.Portals, feet.x, feet.y, out _), map.Id + " -> " + target.Id + " arrives on a portal");
            }
    }

    [Theory]
    [InlineData(ThatchVillageMap.MapId)]
    [InlineData(WillowbrookMeadowsMap.MapId)]
    public void EveryWalkableTileOfAPaintedMapConnectsToItsPortalsAndRowsUnderTheHudAreBlocked(string mapId)
    {
        IGameMap map = MapRegistry.Get(mapId);
        var seen = new HashSet<int>();
        var open = new Stack<int>();
        MapPortal first = map.Portals[0];
        open.Push(first.Ty * TileMapBase.W + first.Tx);
        while (open.Count > 0)
        {
            int i = open.Pop();
            int x = i % TileMapBase.W, y = i / TileMapBase.W;
            if (map.IsSolidTile(x, y) || !seen.Add(i)) continue;
            open.Push(i + 1); open.Push(i - 1); open.Push(i + TileMapBase.W); open.Push(i - TileMapBase.W);
        }
        foreach (MapPortal p in map.Portals) Assert.Contains(p.Ty * TileMapBase.W + p.Tx, seen);
        for (int y = 0; y < TileMapBase.H; y++)
            for (int x = 0; x < TileMapBase.W; x++)
            {
                bool walk = !map.IsSolidTile(x, y);
                Assert.True(!walk || seen.Contains(y * TileMapBase.W + x), mapId + " walkable tile " + x + "," + y + " is cut off");
                Assert.True(!walk || y < 21, mapId + " walkable tile " + x + "," + y + " is under the HUD");
            }
    }

    [Fact]
    public void TheDefaultMapIsFrostHollowAndIdsAreUniqueAndFitTheWire()
    {
        Assert.Equal(FrostHollowLayout.MapId, MapRegistry.Current.Id);
        var ids = new HashSet<string>();
        foreach (IGameMap map in MapRegistry.All)
        {
            Assert.True(ids.Add(map.Id), "two maps share the id " + map.Id);
            Assert.InRange(map.Id.Length, 1, NetProtocol.MaxMapIdLength);
        }
    }

    [Fact]
    public void DenForwardPortalOnlyExistsForAHeroWithTheKill()
    {
        IGameMap den = MapRegistry.Get(FrostfangDenMap.MapId);
        BossProgress killer = new BossProgress(), other = new BossProgress();
        killer.Mark(FrostfangDenMap.MapId, true);
        Vector2 gate = FrostHollowMap.TileFeet(34, 10, 2f), back = FrostHollowMap.TileFeet(0, 10, 2f);
        MapPortal found;

        Assert.True(MapPortals.TryFind(den.PortalsFor(killer.IsDefeatedOn(den.Id, true)), gate.x, gate.y, out found));
        Assert.Equal(SunspireOasisMap.MapId, found.TargetMapId);
        Assert.False(MapPortals.TryFind(den.PortalsFor(other.IsDefeatedOn(den.Id, true)), gate.x, gate.y, out found));

        // The way back is always open, for both.
        Assert.True(MapPortals.TryFind(den.PortalsFor(false), back.x, back.y, out found));
        Assert.Equal(FrostHollowLayout.MapId, found.TargetMapId);
    }

    [Fact]
    public void PortalsPropertyStillAsksTheLocalSave()
    {
        IGameMap den = MapRegistry.Get(FrostfangDenMap.MapId);
        RpgSession.StubDefeated = false;
        Assert.Equal(den.PortalsFor(false).Length, den.Portals.Length);
        RpgSession.StubDefeated = true;
        Assert.Equal(den.PortalsFor(true).Length, den.Portals.Length);
        RpgSession.StubDefeated = false;
    }

    [Fact]
    public void MapsWithoutAGateAnswerTheSameEitherWay()
    {
        foreach (IGameMap map in MapRegistry.All)
            if (map.Id != FrostfangDenMap.MapId) Assert.Same(map.PortalsFor(true), map.PortalsFor(false));
    }
}
