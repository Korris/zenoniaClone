using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Shared tile collision of the 36x24 maps (web tilemap.js): a solid-tile grid built from the row strings (top-down like the web, row 0 = top)
    /// plus footprints blocked by props / NPCs, a y-up RectBlocked and the 1-unit swept Move. Derived maps only supply data.
    /// </summary>
    public abstract class TileMapBase : IGameMap
    {
        public const int W = 36, H = 24;
        public const float Tile = 16f;
        static readonly MapProp[] NoProps = new MapProp[0];
        static readonly MapNpc[] NoNpcs = new MapNpc[0];

        readonly string[] rows;
        readonly bool[] solid = new bool[W * H];

        protected TileMapBase(string[] rows, string solidChars)
        {
            this.rows = rows;
            for (int ty = 0; ty < H; ty++)
                for (int tx = 0; tx < W; tx++) solid[ty * W + tx] = solidChars.IndexOf(rows[ty][tx]) >= 0;
        }

        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string GroundTexture { get; }
        public abstract string Music { get; }
        public abstract bool Hostile { get; }
        public abstract Vector2 PlayerStart { get; }
        public abstract MapPortal[] Portals { get; }
        public virtual MapProp[] Props { get { return NoProps; } }
        public virtual MapNpc[] Npcs { get { return NoNpcs; } }

        /// <summary>Tile char at (tx, ty); anything outside the map reads as 'C' (solid cliff).</summary>
        public char CharAt(int tx, int ty)
        {
            if (tx < 0 || ty < 0 || tx >= W || ty >= H) return 'C';
            return rows[ty][tx];
        }

        /// <summary>Blocks a w x h tile rectangle (top-left tile tx, ty) for props and NPCs, clipped to the map.</summary>
        protected void BlockTiles(int tx, int ty, int w, int h)
        {
            for (int y = ty; y < ty + h; y++)
                for (int x = tx; x < tx + w; x++)
                    if (x >= 0 && y >= 0 && x < W && y < H) solid[y * W + x] = true;
        }

        public bool IsSolidTile(int tx, int ty)
        {
            if (tx < 0 || ty < 0 || tx >= W || ty >= H) return true;
            return solid[ty * W + tx];
        }

        /// <summary>True when a y-up rect (bottom-left x,y; size w,h) touches a solid tile or leaves the map.</summary>
        public bool RectBlocked(float x, float y, float w, float h)
        {
            if (x < 0f || y < 0f || x + w > GameConfig.MapW || y + h > GameConfig.MapH) return true;
            int tx0 = Mathf.FloorToInt(x / Tile), tx1 = Mathf.FloorToInt((x + w - 0.001f) / Tile);
            int ty0 = Mathf.FloorToInt(y / Tile), ty1 = Mathf.FloorToInt((y + h - 0.001f) / Tile);
            for (int ty = ty0; ty <= ty1; ty++)
                for (int tx = tx0; tx <= tx1; tx++)
                    if (IsSolidTile(tx, H - 1 - ty)) return true;
            return false;
        }

        /// <summary>
        /// Moves a feet-anchored box (width w centred on feet, height h above feet) by (dx,dy), sweeping each axis in 1-unit steps
        /// like the web tilemap.moveBox so it slides along walls. Returns the new feet position.
        /// </summary>
        public Vector2 Move(Vector2 feet, float dx, float dy, float w, float h, out bool blockedX, out bool blockedY)
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
    }
}
