using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Frost Hollow spawn data (web src/world/maps/frost-hollow-map.js) and the collision entry point used by heroes, monsters, loot and the net code.
    /// The tile layout itself lives in FrostHollowLayout (an IGameMap, like the village); IsSolidTile / RectBlocked / Move forward to
    /// MapRegistry.Current so every existing caller collides with the map the sim is running without knowing which one it is.
    /// </summary>
    public static class FrostHollowMap
    {
        public const int TilesW = TileMapBase.W, TilesH = TileMapBase.H;
        public const float Tile = TileMapBase.Tile;
        public static readonly Vector2Int PlayerStartTile = FrostHollowLayout.PlayerStartTile;

        public struct Zone
        {
            public string Type; public int Count, Tx, Ty, W, H; public float RespawnMs;
            public Zone(string type, int count, int tx, int ty, int w, int h, float respawnMs)
            { Type = type; Count = count; Tx = tx; Ty = ty; W = w; H = h; RespawnMs = respawnMs; }
        }

        /// <summary>Spawn zones of the web map (tile rect, count, respawn delay).</summary>
        public static readonly Zone[] Zones =
        {
            new Zone("frostSlime", 5, 5, 4, 9, 5, 8000f),
            new Zone("frostSlime", 3, 8, 13, 5, 4, 9000f),
            new Zone("iceGoblin", 4, 25, 5, 8, 5, 10000f),
            new Zone("frostSlime", 2, 25, 5, 8, 5, 8000f),
            new Zone("spectralWolf", 4, 25, 14, 6, 4, 11000f),
        };
        public static readonly Vector2Int BossTile = new Vector2Int(7, 5);

        /// <summary>Feet position of a tile centre-bottom (web: x = (tx+.5)*16, y = (ty+1)*16 - 2), converted to y-up world units.</summary>
        public static Vector2 TileFeet(int tx, int ty, float lift)
        {
            return new Vector2((tx + 0.5f) * Tile, GameConfig.MapH - ((ty + 1) * Tile - lift));
        }

        /// <summary>Frost Hollow's own start (GameConfig.HeroStart is the current map's).</summary>
        public static Vector2 PlayerStart { get { return TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }

        /// <summary>Tile char of the current map's rows (Frost Hollow's chars; the village has its own set).</summary>
        public static char CharAt(int tx, int ty)
        {
            var map = MapRegistry.Current as TileMapBase;
            return map != null ? map.CharAt(tx, ty) : 'C';
        }

        public static bool IsSolidTile(int tx, int ty) { return MapRegistry.Current.IsSolidTile(tx, ty); }

        /// <summary>True when a y-up rect (bottom-left x,y; size w,h) touches a solid tile or leaves the current map.</summary>
        public static bool RectBlocked(float x, float y, float w, float h) { return MapRegistry.Current.RectBlocked(x, y, w, h); }

        /// <summary>Moves a feet-anchored box on the current map, sliding along walls (see TileMapBase.Move). Returns the new feet position.</summary>
        public static Vector2 Move(Vector2 feet, float dx, float dy, float w, float h, out bool blockedX, out bool blockedY)
        {
            return MapRegistry.Current.Move(feet, dx, dy, w, h, out blockedX, out blockedY);
        }

        /// <summary>Random walkable spot inside a zone (web: random tile, feet 2 units above the tile bottom). False after 30 failed tries.</summary>
        public static bool TryZoneSpot(Zone z, Vector2 avoid, float minDist, out Vector2 feet)
        {
            for (int i = 0; i < 30; i++)
            {
                int tx = z.Tx + Random.Range(0, z.W), ty = z.Ty + Random.Range(0, z.H);
                if (IsSolidTile(tx, ty)) continue;
                feet = TileFeet(tx, ty, 2f);
                if (minDist > 0f && Vector2.Distance(feet, avoid) < minDist) continue;
                return true;
            }
            feet = Vector2.zero;
            return false;
        }
    }
}
