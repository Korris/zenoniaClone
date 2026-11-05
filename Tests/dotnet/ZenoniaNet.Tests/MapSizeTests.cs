using UnityEngine;
using Xunit;
using ZenoniaProto;
using ZenoniaProto.Net;

/// <summary>A map is as big as its rows: a stub 72x48 map must collide, convert feet <-> tiles and find portals like the 36x24 ones.</summary>
[Collection("MapRegistry")]
public class MapSizeTests
{
    const int BigW = 72, BigH = 48;

    sealed class BigMap : TileMapBase
    {
        static string[] MakeRows()
        {
            var rows = new string[BigH];
            for (int y = 0; y < BigH; y++)
            {
                var r = new char[BigW];
                for (int x = 0; x < BigW; x++) r[x] = x == 0 || y == 0 || x == BigW - 1 || y == BigH - 1 ? 'C' : '.';
                rows[y] = new string(r);
            }
            rows[10] = rows[10].Substring(0, 40) + "C" + rows[10].Substring(41); // one rock at (40,10)
            return rows;
        }

        public BigMap() : base(MakeRows(), "C") { }
        public override string Id { get { return "stub-big"; } }
        public override string Name { get { return "Stub big"; } }
        public override string GroundTexture { get { return "stub"; } }
        public override string Music { get { return ""; } }
        public override bool Hostile { get { return true; } }
        public override Vector2 PlayerStart { get { return FrostHollowMap.TileFeet(5, 5, 2f); } }
        public override MapPortal[] Portals { get { return new[] { new MapPortal(70, 46, "oakhaven-village", 1, 1) }; } }
        public override int GroundChunkCols { get { return 2; } }
        public override int GroundChunkRows { get { return 2; } }
    }

    static void WithCurrent(IGameMap map, System.Action body)
    {
        try { MapRegistry.UseZoneMap(map); body(); }
        finally { MapRegistry.ResetState(); }
    }

    [Fact]
    public void SizeComesFromTheRowsAndGameConfigFollowsTheCurrentMap()
    {
        var big = new BigMap();
        Assert.Equal(BigW, big.Width);
        Assert.Equal(BigH, big.Height);
        Assert.Equal(2, big.GroundChunkCols);
        WithCurrent(big, () => { Assert.Equal(BigW * 16f, GameConfig.MapW); Assert.Equal(BigH * 16f, GameConfig.MapH); });
        Assert.Equal(36, MapRegistry.Current.Width); // back to the default 36x24 Frost Hollow
        Assert.Equal(24, MapRegistry.Current.Height);
        Assert.True(BigW <= GameConfig.MaxTilesW && BigH <= GameConfig.MaxTilesH);
    }

    [Fact]
    public void CollisionUsesTheOwnSizeAtEveryEdge()
    {
        var big = new BigMap();
        Assert.False(big.RectBlocked(16f * 70f, 16f * 1f, 10f, 8f));   // east side, free (a 36-wide map would call this outside)
        Assert.True(big.RectBlocked(16f * 71f, 16f * 20f, 10f, 8f));   // the east border wall
        Assert.True(big.RectBlocked(BigW * 16f - 4f, 100f, 10f, 8f));  // pokes out of the map
        Assert.True(big.RectBlocked(100f, BigH * 16f - 4f, 10f, 8f));  // pokes out of the top
        Assert.True(big.IsSolidTile(40, 10));
        Assert.False(big.IsSolidTile(41, 10));
        Assert.True(big.IsSolidTile(BigW, 5));
        Assert.Equal('C', big.CharAt(BigW - 1, 0));
        // y-up world to rows: the rock at row 10 sits at world y = (48 - 1 - 10) * 16
        Assert.True(big.RectBlocked(40f * 16f + 2f, (BigH - 1 - 10) * 16f + 2f, 8f, 8f));
    }

    [Fact]
    public void MoveSlidesAndStopsAtTheFarWall()
    {
        var big = new BigMap();
        bool bx, by;
        Vector2 p = big.Move(new Vector2(60f * 16f, 20f * 16f), 400f, 0f, 10f, 8f, out bx, out by);
        Assert.True(bx);
        Assert.True(p.x < 71f * 16f && p.x > 69f * 16f);
    }

    [Theory]
    [InlineData(70, 46)]
    [InlineData(71, 1)]
    [InlineData(1, 46)]
    [InlineData(35, 24)]
    public void TileFeetAndPortalTileRoundTripOnTheBigMap(int tx, int ty)
    {
        var big = new BigMap();
        WithCurrent(big, () =>
        {
            Vector2 feet = FrostHollowMap.TileFeet(tx, ty, 2f);
            int fx, fy;
            PortalTile.OfFeet(feet.x, feet.y, TileMapBase.Tile, GameConfig.TilesH, out fx, out fy);
            Assert.Equal(tx, fx);
            Assert.Equal(ty, fy);
            MapPortal found;
            Assert.Equal(tx == 70 && ty == 46, MapPortals.TryFind(big.Portals, feet.x, feet.y, out found));
        });
    }

    [Fact]
    public void InterestGridCoversTheLargestMap()
    {
        var grid = new InterestGrid(GameConfig.MaxTilesW * GameConfig.TileUnits, GameConfig.MaxTilesH * GameConfig.TileUnits);
        Assert.NotNull(grid);
    }

    [Fact]
    public void RaggedRowsAreReported()
    {
        Assert.Throws<System.InvalidOperationException>(() => new Ragged()); // the stub Debug.LogError throws
    }

    sealed class Ragged : TileMapBase
    {
        public Ragged() : base(new[] { "....", "..." }, "C") { }
        public override string Id { get { return "r"; } }
        public override string Name { get { return "r"; } }
        public override string GroundTexture { get { return "r"; } }
        public override string Music { get { return ""; } }
        public override bool Hostile { get { return false; } }
        public override Vector2 PlayerStart { get { return new Vector2(0, 0); } }
        public override MapPortal[] Portals { get { return new MapPortal[0]; } }
    }
}
