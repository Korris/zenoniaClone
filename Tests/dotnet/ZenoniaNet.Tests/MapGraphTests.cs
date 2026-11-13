using System.Collections.Generic;
using UnityEngine;
using Xunit;
using ZenoniaProto;
using ZenoniaProto.Net;

/// <summary>
/// The real map data (Assets/Scripts/Maps, compiled here against MapGraphStubs.cs): every portal of every registered map must work on the server, including the maps that get added
/// later, because the tests walk MapRegistry.All. The stubs stand in for the few engine types and game classes the maps touch.
/// </summary>
[Collection("MapRegistry")] // MapRegistry.Current is process-wide: tests that switch it must not run in parallel
public class MapGraphTests
{
    /// <summary>Feet of a tile of `mapId`, computed the way the server does after Zone.Enter: with that map current. The current map is restored.</summary>
    static Vector2 FeetOn(string mapId, int tx, int ty)
    {
        try { MapRegistry.UseZoneMap(MapRegistry.Get(mapId)); return FrostHollowMap.TileFeet(tx, ty, 2f); }
        finally { MapRegistry.ResetState(); }
    }

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
        Vector2 feet = FeetOn(targetId, targetTx, targetTy);
        int fx, fy;
        PortalTile.OfFeet(feet.x, feet.y, TileMapBase.Tile, MapRegistry.Get(targetId).Height, out fx, out fy);
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
                Vector2 feet = FeetOn(p.TargetMapId, p.TargetTx, p.TargetTy);
                Assert.False(MapPortals.TryFind(MapRegistry.Get(p.TargetMapId).PortalsFor(true), feet.x, feet.y, out found), map.Id + " -> " + p.TargetMapId + " arrives on a portal");
            }
        }
    }

    [Fact]
    public void WorldMapStep1RouteGoesBothWays()
    {
        // World Map 2.0 step 1 (13/11/2026, plans/261113-0106-world-map-step1): Oakhaven <-> Thatchwick is unchanged; Thatchwick's old east link to Willowbrook now goes to
        // Frost Hollow's old village gate instead; Willowbrook's west gate now goes to Oakhaven's new south gate instead of Thatchwick (agent C's new OakhavenVillageMap).
        IGameMap oak = MapRegistry.Get(OakhavenVillageMap.MapId), thatch = MapRegistry.Get(ThatchVillageMap.MapId);
        IGameMap willow = MapRegistry.Get(WillowbrookMeadowsMap.MapId), frost = MapRegistry.Get(FrostHollowLayout.MapId);
        Assert.Contains(oak.Portals, p => p.TargetMapId == ThatchVillageMap.MapId);
        Assert.Contains(oak.Portals, p => p.TargetMapId == WillowbrookMeadowsMap.MapId);
        Assert.Contains(thatch.Portals, p => p.TargetMapId == OakhavenVillageMap.MapId);
        Assert.Contains(thatch.Portals, p => p.TargetMapId == FrostHollowLayout.MapId);
        Assert.Contains(frost.Portals, p => p.TargetMapId == ThatchVillageMap.MapId);
        // Willowbrook is a dead end: its only portals go to Oakhaven.
        Assert.All(willow.Portals, p => Assert.Equal(OakhavenVillageMap.MapId, p.TargetMapId));
        Assert.False(thatch.Hostile);
        Assert.True(willow.Hostile);
        Assert.Contains(new MapNpc("herbalistWren", 13, 9), thatch.Npcs);
        Assert.Contains(new MapNpc("smithHolt", 22, 9), thatch.Npcs);
        Assert.Contains(new MapNpc("brokerPell", 18, 13), thatch.Npcs);
        // Standing on an arrival tile there is a portal back to the map we came from.
        foreach (IGameMap map in new[] { oak, thatch, willow, frost })
            foreach (MapPortal p in map.Portals)
            {
                IGameMap target = MapRegistry.Get(p.TargetMapId);
                Vector2 feet = FeetOn(p.TargetMapId, p.TargetTx, p.TargetTy);
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
        open.Push(first.Ty * map.Width + first.Tx);
        while (open.Count > 0)
        {
            int i = open.Pop();
            int x = i % map.Width, y = i / map.Width;
            if (map.IsSolidTile(x, y) || !seen.Add(i)) continue;
            open.Push(i + 1); open.Push(i - 1); open.Push(i + map.Width); open.Push(i - map.Width);
        }
        foreach (MapPortal p in map.Portals) Assert.Contains(p.Ty * map.Width + p.Tx, seen);
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                bool walk = !map.IsSolidTile(x, y);
                Assert.True(!walk || seen.Contains(y * map.Width + x), mapId + " walkable tile " + x + "," + y + " is cut off");
                Assert.True(!walk || y < map.Height - 3, mapId + " walkable tile " + x + "," + y + " is under the HUD");
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
    public void DenForwardPortalIsAlwaysOpenNow()
    {
        // World Map 2.0 step 1 (13/11/2026): the user dropped the "kill the Warden first" gate, so Frostfang Den -> Sunspire is open for every hero, killer or not.
        IGameMap den = MapRegistry.Get(FrostfangDenMap.MapId);
        BossProgress killer = new BossProgress(), other = new BossProgress();
        killer.Mark(FrostfangDenMap.MapId, true);
        Vector2 gate = FrostHollowMap.TileFeet(34, 10, 2f), back = FrostHollowMap.TileFeet(0, 10, 2f);
        MapPortal found;

        Assert.True(MapPortals.TryFind(den.PortalsFor(killer.IsDefeatedOn(den.Id, true)), gate.x, gate.y, out found));
        Assert.Equal(SunspireOasisMap.MapId, found.TargetMapId);
        Assert.True(MapPortals.TryFind(den.PortalsFor(other.IsDefeatedOn(den.Id, true)), gate.x, gate.y, out found));
        Assert.Equal(SunspireOasisMap.MapId, found.TargetMapId);

        // The way back is always open too.
        Assert.True(MapPortals.TryFind(den.PortalsFor(false), back.x, back.y, out found));
        Assert.Equal(FrostHollowLayout.MapId, found.TargetMapId);
    }

    [Fact]
    public void MapsAnswerTheSameEitherWayNowThatNoMapHasAGate()
    {
        foreach (IGameMap map in MapRegistry.All) Assert.Same(map.PortalsFor(true), map.PortalsFor(false));
    }
}
