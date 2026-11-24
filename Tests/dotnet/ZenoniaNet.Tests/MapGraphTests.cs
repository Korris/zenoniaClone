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

    /// <summary>MapPortals.TryFind with `mapId` current: it reads the map height from MapRegistry.Current, so asking with another map current
    /// looks at the wrong rows (this check passed for Oakhaven's south gate while the game bounced the hero back, 16/11/2026).</summary>
    static bool OnTrigger(string mapId, float x, float y)
    {
        MapPortal found;
        try { MapRegistry.UseZoneMap(MapRegistry.Get(mapId)); return MapPortals.TryFind(MapRegistry.Get(mapId).PortalsFor(true), x, y, out found); }
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
    public void EveryLiveMapIsReachableFromTheStartMap_AndOnlyLiveMapsAreWhenLegacyIsOff()
    {
        var seen = new HashSet<string> { MapRegistry.StartMapId };
        var open = new Queue<IGameMap>();
        open.Enqueue(MapRegistry.Get(MapRegistry.StartMapId));
        while (open.Count > 0)
            foreach (MapPortal p in open.Dequeue().PortalsFor(true))
                if (seen.Add(p.TargetMapId)) open.Enqueue(MapRegistry.Get(p.TargetMapId));
        foreach (IGameMap map in MapRegistry.Live) Assert.True(seen.Contains(map.Id), map.Id + " cannot be reached from the start map");
        foreach (IGameMap map in MapRegistry.Live) Assert.DoesNotContain(map.Id, System.Linq.Enumerable.Select(MapRegistry.Legacy, m => m.Id)); // no id shared with a pre-Taurus map
        foreach (IGameMap map in MapRegistry.Live) Assert.All(map.Portals, p => Assert.Contains(MapRegistry.Live, m => m.Id == p.TargetMapId)); // a live map never leads into the old world
    }

    [Fact]
    public void EveryLegacyMapIsReachableFromFrostHollow()
    {
        var seen = new HashSet<string>();
        var open = new Queue<IGameMap>();
        open.Enqueue(MapRegistry.Get(FrostHollowLayout.MapId));
        seen.Add(FrostHollowLayout.MapId);
        while (open.Count > 0)
            foreach (MapPortal p in open.Dequeue().PortalsFor(true))
                if (seen.Add(p.TargetMapId)) open.Enqueue(MapRegistry.Get(p.TargetMapId));
        foreach (IGameMap map in MapRegistry.Legacy) Assert.True(seen.Contains(map.Id), map.Id + " cannot be reached through any portal");
    }

    [Fact]
    public void NeitherAStartTileNorAnArrivalTileIsATrigger()
    {
        // PortalGate would catch both, but a hero placed on a trigger could not leave the way it expects: respawn and arrival have to be clear of the map's portals.
        MapPortal found;
        foreach (IGameMap map in MapRegistry.All)
        {
            Assert.False(OnTrigger(map.Id, map.PlayerStart.x, map.PlayerStart.y), map.Id + " starts on a portal");
            foreach (MapPortal p in map.PortalsFor(true))
            {
                Vector2 feet = FeetOn(p.TargetMapId, p.TargetTx, p.TargetTy);
                Assert.False(OnTrigger(p.TargetMapId, feet.x, feet.y), map.Id + " -> " + p.TargetMapId + " arrives within reach of a portal (" + p.TargetTx + "," + p.TargetTy + ")");
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
        // Willowbrook's west gate goes to Oakhaven; World Map 2.1 added its south gate to Bramblewood Forest (BramblewoodRootburrowMapTests).
        Assert.All(willow.Portals, p => Assert.Contains(p.TargetMapId, new[] { OakhavenVillageMap.MapId, BramblewoodForestMap.MapId }));
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
    [InlineData(SunbreakFoothillsMap.MapId)]
    [InlineData(ScorpionGorgeMap.MapId)]
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
    public void WorldMap21RegionThreeRunsSunspireAmberDunesScorpionGorge()
    {
        // World Map 2.1 (13/11/2026): Scorpion Gorge is the second farm map of region 3, reached from Amber Dunes' dangling east track end. Until Mirewatch exists its east gap
        // leads straight on to Emberfen Marsh, and the old Amber Dunes -> Emberfen shortcut is closed, so region 4 is only reached through Scorpion Gorge.
        IGameMap amber = MapRegistry.Get(AmberDunesMap.MapId), gorge = MapRegistry.Get(ScorpionGorgeMap.MapId), ember = MapRegistry.Get(EmberfenMarshMap.MapId);
        Assert.Contains(amber.Portals, p => p.TargetMapId == ScorpionGorgeMap.MapId && p.TargetTx == ScorpionGorgeMap.WestArrivalTx && p.TargetTy == ScorpionGorgeMap.WestArrivalTy);
        Assert.DoesNotContain(amber.Portals, p => p.TargetMapId == EmberfenMarshMap.MapId);
        Assert.DoesNotContain(ember.Portals, p => p.TargetMapId == AmberDunesMap.MapId);
        foreach (MapPortal p in gorge.Portals)
        {
            if (p.Tx == 0) Assert.Equal((AmberDunesMap.MapId, AmberDunesMap.ScorpionGorgeArrivalTx), (p.TargetMapId, p.TargetTx));
            else Assert.Equal((EmberfenMarshMap.MapId, EmberfenMarshMap.WestArrivalTx, EmberfenMarshMap.WestArrivalTy), (p.TargetMapId, p.TargetTx, p.TargetTy));
        }
        Assert.Contains(ember.Portals, p => p.TargetMapId == ScorpionGorgeMap.MapId && p.TargetTx == ScorpionGorgeMap.EastArrivalTx && p.TargetTy == ScorpionGorgeMap.EastArrivalTy);
        Assert.False(gorge.IsSolidTile(ScorpionGorgeMap.EastArrivalTx, ScorpionGorgeMap.EastArrivalTy), "the east arrival tile is solid");
        foreach (IGameMap map in new[] { amber, gorge, ember })
            foreach (MapPortal p in map.Portals)
                Assert.Contains(MapRegistry.Get(p.TargetMapId).Portals, q => q.TargetMapId == map.Id);
    }

    [Fact]
    public void FeetJustInsideTheMapFromAnEdgePortalStillTriggerIt()
    {
        // 13/11/2026: portal tiles sit on the map edge and the hero's box stops before its feet reach them, so a portal reaches ReachUnits past its tile.
        IGameMap oak = MapRegistry.Get(OakhavenVillageMap.MapId);
        MapPortal east = System.Array.Find(oak.Portals, p => p.Tx == 35 && p.Ty == 10);
        float y = GameConfig.MapH - (east.Ty + 0.5f) * TileMapBase.Tile - PortalTile.FeetSampleUp;
        MapPortal found;
        Assert.True(MapPortals.TryFind(oak.Portals, 35 * TileMapBase.Tile - 10f, y, out found));
        Assert.Equal(ThatchVillageMap.MapId, found.TargetMapId);
        Assert.False(MapPortals.TryFind(oak.Portals, 35 * TileMapBase.Tile - 20f, y, out found), "a portal must not reach more than ReachUnits");
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
        // World Map 2.0 step 1 (13/11/2026): the user dropped the "kill the Warden first" gate, so the Den's far exit is open for every hero, killer or not.
        // World Map 2.1 (13/11/2026): the Den is a side dungeon of Sunbreak Foothills, so both its exits lead to Sunbreak: the entrance to the foot of the cave stairs, the one behind the Warden to the south road fork.
        IGameMap den = MapRegistry.Get(FrostfangDenMap.MapId);
        BossProgress killer = new BossProgress(), other = new BossProgress();
        killer.Mark(FrostfangDenMap.MapId, true);
        Vector2 gate = FrostHollowMap.TileFeet(34, 10, 2f), back = FrostHollowMap.TileFeet(0, 10, 2f);
        MapPortal found;

        Assert.True(MapPortals.TryFind(den.PortalsFor(killer.IsDefeatedOn(den.Id, true)), gate.x, gate.y, out found));
        Assert.Equal(SunbreakFoothillsMap.MapId, found.TargetMapId);
        Assert.True(MapPortals.TryFind(den.PortalsFor(other.IsDefeatedOn(den.Id, true)), gate.x, gate.y, out found));
        Assert.Equal(SunbreakFoothillsMap.MapId, found.TargetMapId);
        // Design answer 13/11/2026: the far door is a shortcut out to the fork of the south road, not back to the cave mouth.
        Assert.Equal(SunbreakFoothillsMap.DenExitArrivalTile.x, found.TargetTx);
        Assert.Equal(SunbreakFoothillsMap.DenExitArrivalTile.y, found.TargetTy);
        Assert.False(MapRegistry.Get(SunbreakFoothillsMap.MapId).IsSolidTile(found.TargetTx, found.TargetTy));

        // The way back is always open too.
        Assert.True(MapPortals.TryFind(den.PortalsFor(false), back.x, back.y, out found));
        Assert.Equal(SunbreakFoothillsMap.MapId, found.TargetMapId);
        Assert.Equal(SunbreakFoothillsMap.CaveArrivalTile.x, found.TargetTx);
        Assert.Equal(SunbreakFoothillsMap.CaveArrivalTile.y, found.TargetTy);
    }

    [Fact]
    public void WorldMap21RouteRunsFrostHollowSunbreakSunspireWithTheDenAsASideDungeon()
    {
        IGameMap frost = MapRegistry.Get(FrostHollowLayout.MapId), sunbreak = MapRegistry.Get(SunbreakFoothillsMap.MapId);
        IGameMap den = MapRegistry.Get(FrostfangDenMap.MapId), sunspire = MapRegistry.Get(SunspireOasisMap.MapId);
        Assert.Contains(frost.Portals, p => p.TargetMapId == SunbreakFoothillsMap.MapId);
        Assert.DoesNotContain(frost.Portals, p => p.TargetMapId == FrostfangDenMap.MapId);
        foreach (string id in new[] { FrostHollowLayout.MapId, FrostfangDenMap.MapId, SunspireOasisMap.MapId }) Assert.Contains(sunbreak.Portals, p => p.TargetMapId == id);
        Assert.All(den.Portals, p => Assert.Equal(SunbreakFoothillsMap.MapId, p.TargetMapId));
        Assert.Contains(sunspire.Portals, p => p.TargetMapId == SunbreakFoothillsMap.MapId && p.TargetTx == 45 && p.TargetTy == 15);
        Assert.Contains(sunspire.Portals, p => p.TargetMapId == SunbreakFoothillsMap.MapId && p.TargetTx == 45 && p.TargetTy == 16);
        Assert.DoesNotContain(sunspire.Portals, p => p.TargetMapId == FrostfangDenMap.MapId);
        // Standing on an arrival tile there is a portal back to the map we came from.
        foreach (IGameMap map in new[] { frost, sunbreak, den, sunspire })
            foreach (MapPortal p in map.Portals)
                Assert.Contains(MapRegistry.Get(p.TargetMapId).Portals, q => q.TargetMapId == map.Id);
    }

    [Fact]
    public void MapsAnswerTheSameEitherWayNowThatNoMapHasAGate()
    {
        foreach (IGameMap map in MapRegistry.All) Assert.Same(map.PortalsFor(true), map.PortalsFor(false));
    }
}
