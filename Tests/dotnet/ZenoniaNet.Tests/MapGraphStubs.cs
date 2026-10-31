// Just enough of the engine and of the game for the real map files (Assets/Scripts/Maps) to compile without Unity. Only MapGraphTests uses them.
namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
    }

    public struct Vector2Int
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
    }

    public static class Mathf
    {
        public static int FloorToInt(float f) { return (int)System.Math.Floor(f); }
        public static float Abs(float f) { return System.Math.Abs(f); }
        public static float Sign(float f) { return f >= 0f ? 1f : -1f; }
        public static float Min(float a, float b) { return a < b ? a : b; }
    }

    public static class Debug
    {
        public static void Log(object m) { }
        public static void LogWarning(object m) { }
        public static void LogError(object m) { throw new System.InvalidOperationException(m.ToString()); } // a map that logs an error at construction must fail the tests
    }
}

namespace ZenoniaProto
{
    using UnityEngine;

    public static class GameConfig
    {
        public const float MapW = TileMapBase.W * TileMapBase.Tile, MapH = TileMapBase.H * TileMapBase.Tile;
    }

    public static class FrostHollowMap
    {
        public static Vector2 TileFeet(int tx, int ty, float lift) { return new Vector2((tx + 0.5f) * TileMapBase.Tile, GameConfig.MapH - ((ty + 1) * TileMapBase.Tile - lift)); }
    }

    public static class NetMode { public static bool IsOffline = true; }

    public static class RpgSession
    {
        public static bool StubDefeated;
        public static bool BossDefeatedOn(string mapId) { return StubDefeated; }
    }
}
