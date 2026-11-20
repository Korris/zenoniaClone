using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Round boss portraits for the boss bar medallion, cut at run time from the art the game already has: the first walk frame facing
    /// down (top-left cell of Resources/Sprites/monsters/&lt;type&gt;.png), cropped to a square around the face. Where the face sits in that
    /// cell differs per boss, so each one has an entry below (centre and side of the square as fractions of the cell, y from the top;
    /// measured on the sheets on 2026-11-16). A boss without an entry gets the middle of its cell; a boss whose sheet is missing gets no
    /// portrait (the medallion shows its plain plate).
    /// </summary>
    public static class BossPortraits
    {
        struct Face { public float X, Y, Size; public Face(float x, float y, float size) { X = x; Y = y; Size = size; } }

        static readonly Face Default = new Face(0.5f, 0.5f, 0.62f);
        static readonly Dictionary<string, Face> Faces = new Dictionary<string, Face>
        {
            // map bosses (192 px cells)
            { "frostfangKing", new Face(0.47f, 0.63f, 0.44f) },
            { "glacierWarden", new Face(0.55f, 0.56f, 0.44f) },
            { "mirefangHydra", new Face(0.50f, 0.34f, 0.38f) },
            { "thornfangAlpha", new Face(0.50f, 0.60f, 0.52f) },
            { "thornheartTreant", new Face(0.50f, 0.42f, 0.44f) },
            { "ridgehornBehemoth", new Face(0.48f, 0.70f, 0.48f) },
            { "duneDevourer", new Face(0.52f, 0.72f, 0.52f) },
            // Wanted bosses (384 px cells)
            { "rimeclawTyrant", new Face(0.51f, 0.48f, 0.40f) },   // crowned ice wolf, 2026-11-20
            { "glacialColossus", new Face(0.52f, 0.44f, 0.38f) },
            { "mireHydraQueen", new Face(0.50f, 0.33f, 0.36f) },
        };

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>Portrait of a monster type (MonsterDef.Sheet) whose walk cells are `cellPx` wide; null when the sheet is missing.</summary>
        public static Sprite Get(string type, int cellPx)
        {
            Sprite s;
            if (Cache.TryGetValue(type, out s) && s != null) return s;
            Texture2D tex = Resources.Load<Texture2D>("Sprites/monsters/" + type);
            if (tex == null || tex.width < cellPx || tex.height < cellPx) return null;
            Face f;
            if (!Faces.TryGetValue(type, out f)) f = Default;
            float side = f.Size * cellPx;
            float x = Mathf.Clamp(f.X * cellPx - side * 0.5f, 0f, cellPx - side);
            float top = Mathf.Clamp(f.Y * cellPx - side * 0.5f, 0f, cellPx - side);
            // Texture space has y = 0 at the bottom; the cell is the top row of the PNG.
            s = Sprite.Create(tex, new Rect(x, tex.height - top - side, side, side), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Cache[type] = s;
            return s;
        }
    }
}
