using System;
using System.Collections.Generic;
using Xunit;
using ZenoniaProto;

/// <summary>Placement and timing of the portal light (MapPortalExits, drawn by MapPortalLightFx) on every registered map.</summary>
[Collection("MapRegistry")]
public class MapPortalExitsTests
{
    public static IEnumerable<object[]> AllMaps()
    {
        foreach (IGameMap map in MapRegistry.All) yield return new object[] { map.Id };
    }

    [Theory]
    [MemberData(nameof(AllMaps))]
    public void EveryExitGetsOneLightInsideTheMapAndClearOfTheHud(string mapId)
    {
        IGameMap map = MapRegistry.Get(mapId);
        List<PortalExit> exits = MapPortalExits.Of(map);
        int tiles = 0;
        foreach (PortalExit e in exits)
        {
            tiles += e.Tiles;
            Assert.InRange(e.AnchorTx, 0.5f, map.Width - 0.5f);
            Assert.InRange(e.AnchorTy, 0.5f, map.Height - 0.5f);
            if (e.Edge == 3) Assert.True(map.Height - e.AnchorTy >= MapPortalExits.HudInsetTiles - 0.01f, mapId + " -> " + e.Target + ": bottom exit light under the HUD at row " + e.AnchorTy);
        }
        Assert.Equal(map.Portals.Length, tiles); // every portal tile belongs to exactly one light
    }

    [Theory]
    [MemberData(nameof(AllMaps))]
    public void EdgeLightsStandOnAWalkableTile(string mapId)
    {
        IGameMap map = MapRegistry.Get(mapId);
        foreach (PortalExit e in MapPortalExits.Of(map))
            if (e.Edge >= 0)
                Assert.False(map.IsSolidTile((int)Math.Floor(e.AnchorTx), (int)Math.Floor(e.AnchorTy)), mapId + " -> " + e.Target + " light on a solid tile " + e.AnchorTx + "," + e.AnchorTy);
    }

    [Fact]
    public void BreathIsSlowAndNeverBlinks()
    {
        float prev = MapPortalExits.Breath(0f, 0f), lo = 1f, hi = 0f;
        for (int i = 1; i <= 2400; i++) // 40 s at 60 fps
        {
            float k = MapPortalExits.Breath(i / 60f, 0f);
            Assert.True(Math.Abs(k - prev) < 0.01f, "brightness step " + (k - prev) + " at frame " + i);
            lo = Math.Min(lo, k); hi = Math.Max(hi, k); prev = k;
        }
        Assert.InRange(lo, MapPortalExits.BreathLow - 0.001f, MapPortalExits.BreathLow + 0.01f);
        Assert.InRange(hi, 0.99f, 1.0001f);
    }

    [Fact]
    public void SnowMapsGetTheBlueLight()
    {
        Assert.Equal(PortalLightRegion.Snow, MapPortalExits.RegionOf(FrostHollowLayout.MapId));
        Assert.Equal(PortalLightRegion.Village, MapPortalExits.RegionOf(OakhavenVillageMap.MapId));
        Assert.Equal(PortalLightRegion.Forest, MapPortalExits.RegionOf(RootburrowMap.MapId));
    }
}
