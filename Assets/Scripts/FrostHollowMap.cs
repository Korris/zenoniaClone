using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Frost Hollow layout copied from the web src/world/maps/frost-hollow-map.js: 36x24 tiles of 16 units, painted backdrop
    /// (Sprites/tiles/frost-hollow-ground.png) plus the tile classification that drives collision.
    /// T snowy pines (solid)  C cliff / rock / ruin (solid)  w frozen river (solid)  B bridge  s snow / stone ground.
    /// Rows are listed top-down like the web (row 0 = top of the map); world y points up, so the row of a point is TilesH-1-floor(y/16).
    /// </summary>
    public static class FrostHollowMap
    {
        static readonly string[] Rows =
        {
            "CCCCCCCCCCCCCCCCCwwwCCCTTTTTTCCCCCCC",
            "CCCCCCCCCCCCCCCCCwwwCCCTTTTTTCCCCCCC",
            "CCCCCCCCCCsTTCTTTwwwCCCTTCCCCCCCssCC",
            "CCCCsssCCsssCCCTTwwwCCCTssCssCCCssCC",
            "TCCCsssssssssssTTTwwCCCTsssssCCsssCC",
            "TssCsssssssssssTTTwwCCCTssssssssssCC",
            "TCCCCsssssssssTTTCwwCCCTTsssssssssTT",
            "TTCCTCCssssssCCCCCwwwCCTTsCsssssssTT",
            "TTTTTsssssTssCCCCCwwwCCTTsCssssssCTT",
            "TTTCCssssCCCCCCCCCwwwCCsCCsssssssCTT",
            "TTTCCssCCCCssCCCCCwwwCCsssssssCCCCTT",
            "TTTTCsssCsssssssssCCCCCssssssssCCTTT",
            "TTCCCCsssssssssCCBBBBBsssssCCssCCCCC",
            "TTTTCCssssssCCTCCCwwwCCTsssCCssTCCCC",
            "TTTCCssssssssCTTCCwwCCTTsssCsssTCCCC",
            "TTTCCsssssssCCTTCCwwCCCTCssssssTCCCT",
            "TTTCCssssssssCCCCCwwCCCTssssssssCCCT",
            "TTTCCssCsCsssCCCCCwwCCCTssssssssssTT",
            "TCCCssCCCCTTTTTTTTwwCCCCCTssssssCCCT",
            "TCCsssCCCCTTTTTTTTwwCCCCTTsssCCCCCCT",
            "TTssssCCCTTTTTTTTTwwCCCCCCCCCCCCCCCC",
            "TTssssCsTTTTTTTTTTwwCCCCCCCCCCCCCCCC",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
        };

        public const int TilesW = 36, TilesH = 24;
        public const float Tile = 16f;
        public static readonly Vector2Int PlayerStartTile = new Vector2Int(4, 21);

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

        public static Vector2 PlayerStart { get { return TileFeet(PlayerStartTile.x, PlayerStartTile.y, 2f); } }

        public static char CharAt(int tx, int ty)
        {
            if (tx < 0 || ty < 0 || tx >= TilesW || ty >= TilesH) return 'C';
            return Rows[ty][tx];
        }

        public static bool IsSolidTile(int tx, int ty)
        {
            char c = CharAt(tx, ty);
            return c == 'T' || c == 'C' || c == 'w';
        }

        /// <summary>True when a y-up rect (bottom-left x,y; size w,h) touches a solid tile or leaves the map.</summary>
        public static bool RectBlocked(float x, float y, float w, float h)
        {
            if (x < 0f || y < 0f || x + w > GameConfig.MapW || y + h > GameConfig.MapH) return true;
            int tx0 = Mathf.FloorToInt(x / Tile), tx1 = Mathf.FloorToInt((x + w - 0.001f) / Tile);
            int ty0 = Mathf.FloorToInt(y / Tile), ty1 = Mathf.FloorToInt((y + h - 0.001f) / Tile);
            for (int ty = ty0; ty <= ty1; ty++)
                for (int tx = tx0; tx <= tx1; tx++)
                    if (IsSolidTile(tx, TilesH - 1 - ty)) return true;
            return false;
        }

        /// <summary>
        /// Moves a feet-anchored box (width w centred on feet, height h above feet) by (dx,dy), sweeping each axis in 1-unit steps
        /// like the web tilemap.moveBox so it slides along walls. Returns the new feet position.
        /// </summary>
        public static Vector2 Move(Vector2 feet, float dx, float dy, float w, float h, out bool blockedX, out bool blockedY)
        {
            float x = feet.x, y = feet.y, hw = w * 0.5f;
            blockedX = blockedY = false;
            float remaining = Mathf.Abs(dx), sign = Mathf.Sign(dx);
            while (remaining > 1e-4f)
            {
                float step = Mathf.Min(1f, remaining), next = x + step * sign;
                if (RectBlocked(next - hw, y, w, h)) { blockedX = true; break; }
                x = next; remaining -= step;
            }
            remaining = Mathf.Abs(dy); sign = Mathf.Sign(dy);
            while (remaining > 1e-4f)
            {
                float step = Mathf.Min(1f, remaining), next = y + step * sign;
                if (RectBlocked(x - hw, next, w, h)) { blockedY = true; break; }
                y = next; remaining -= step;
            }
            return new Vector2(x, y);
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
