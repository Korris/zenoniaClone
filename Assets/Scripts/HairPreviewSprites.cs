using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Hair tiles and swatches of the Customize screen, built from the same art and dye code the hero uses in the world (HeroHairLayers.LoadCell + HairDye.Apply), so a tile shows exactly
    /// the head the hero will get. Layered classes (heads/face + heads/hair/N, dyeable) give four stacked cells per tile; full-head classes (heads/N, not dyeable) give one.
    /// A class without head art (the Fighter) has no Look at all. Cells are 192 px frames with the head wherever the body frame has it, so each Look also carries the head's bounds
    /// (normalised, y up) for the screen to crop the tile around the head.
    /// </summary>
    public static class HairPreviewSprites
    {
        public const int MaxStyles = 6; // the Customize grid is 2 x 3
        const byte SolidAlpha = 8;
        static readonly Dictionary<HeroClass, Look> Cache = new Dictionary<HeroClass, Look>();

        public sealed class Look
        {
            public string Root;          // Sprites/classes/<class>/heads/
            public bool Layered;
            public int StyleCount;       // 1..MaxStyles
            public Color32?[] StyleDyes; // default dye of each style (layered only)
            public Color32[] Presets;    // hair-dyes.json presets: the swatches (layered only)
            public Rect Head;            // where the head is inside a 192 cell, 0..1, y up
            public bool Dyeable { get { return Layered && Presets.Length > 0; } }
        }

        /// <summary>The look data of a class, or null when it has no head art (no styles to pick).</summary>
        public static Look For(HeroClass id)
        {
            Look look;
            if (Cache.TryGetValue(id, out look)) return look;
            look = Build(HeroClassDef.Get(id));
            Cache[id] = look;
            return look;
        }

        static Look Build(HeroClassDef cls)
        {
            if (cls == null || string.IsNullOrEmpty(cls.BareFolder) || !cls.BareFolder.EndsWith("/body/bare")) return null;
            string classRoot = cls.BareFolder.Substring(0, cls.BareFolder.Length - "/body/bare".Length);
            if (Resources.Load<TextAsset>("Data/" + classRoot + "/head-anchors") == null) return null; // the world would draw no head layer for it either
            var look = new Look { Root = "Sprites/" + classRoot + "/heads/" };
            look.Layered = SpriteSheet.TryLoad(look.Root + "face/down") != null && SpriteSheet.TryLoad(look.Root + "hair/0/down") != null;
            while (look.StyleCount < MaxStyles && HeroHairLayers.LoadCell(look.Root + (look.Layered ? "hair/" : "") + look.StyleCount + "/down") != null) look.StyleCount++;
            if (look.StyleCount == 0) return null;
            look.StyleDyes = new Color32?[0];
            look.Presets = new Color32[0];
            if (look.Layered) HeroHairLayers.LoadDyes(classRoot, out look.StyleDyes, out look.Presets);
            look.Head = HeadBounds(look);
            return look;
        }

        /// <summary>Sprites of a tile, back to front. Null entries are layers this style does not have. dye: -1 = the style's own colour, else a preset index.</summary>
        public static Sprite[] Layers(Look look, int style, int dye)
        {
            style = Mathf.Clamp(style, 0, look.StyleCount - 1);
            if (!look.Layered) return new[] { HeroHairLayers.LoadCell(look.Root + style + "/down") };
            Color32? tint = dye >= 0 && dye < look.Presets.Length ? (Color32?)look.Presets[dye] : style < look.StyleDyes.Length ? look.StyleDyes[style] : null; // same choice as HeroHairLayers.Show
            return new[]
            {
                HairDye.Apply(HeroHairLayers.LoadCell(look.Root + "hair-under/" + style + "/down"), tint),
                HairDye.Apply(HeroHairLayers.LoadCell(look.Root + "hair-back/" + style + "/down"), tint),
                HeroHairLayers.LoadCell(look.Root + "face/down"),
                HairDye.Apply(HeroHairLayers.LoadCell(look.Root + "hair/" + style + "/down"), tint),
            };
        }

        /// <summary>Colour of a swatch for a preset index (-1 = the style's own colour, shown as the style's default dye or light gray).</summary>
        public static Color SwatchColor(Look look, int style, int dye)
        {
            if (dye >= 0 && dye < look.Presets.Length) return look.Presets[dye];
            style = Mathf.Clamp(style, 0, look.StyleCount - 1);
            return style < look.StyleDyes.Length && look.StyleDyes[style].HasValue ? (Color)look.StyleDyes[style].Value : new Color(0.9f, 0.9f, 0.92f);
        }

        /// <summary>Union of the opaque areas of the face and every style's front hair (or of every full head), as a fraction of the cell.</summary>
        static Rect HeadBounds(Look look)
        {
            Rect all = new Rect(0.3f, 0.45f, 0.4f, 0.5f); // fallback when the art cannot be read: the upper middle of the cell
            bool any = false;
            var cells = new List<Sprite>();
            if (look.Layered) cells.Add(HeroHairLayers.LoadCell(look.Root + "face/down"));
            for (int s = 0; s < look.StyleCount; s++) cells.Add(HeroHairLayers.LoadCell(look.Root + (look.Layered ? "hair/" : "") + s + "/down"));
            foreach (Sprite sp in cells)
            {
                Rect r;
                if (!OpaqueBounds(sp, out r)) continue;
                all = any ? Union(all, r) : r;
                any = true;
            }
            return all;
        }

        static Rect Union(Rect a, Rect b)
        {
            float x0 = Mathf.Min(a.xMin, b.xMin), y0 = Mathf.Min(a.yMin, b.yMin), x1 = Mathf.Max(a.xMax, b.xMax), y1 = Mathf.Max(a.yMax, b.yMax);
            return new Rect(x0, y0, x1 - x0, y1 - y0);
        }

        static bool OpaqueBounds(Sprite sp, out Rect bounds)
        {
            bounds = default(Rect);
            if (sp == null) return false;
            Color32[] px = SpriteSheet.ReadPixels(sp.texture);
            int tw = sp.texture.width, x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
            int rx = (int)sp.rect.x, ry = (int)sp.rect.y, rw = (int)sp.rect.width, rh = (int)sp.rect.height;
            for (int y = 0; y < rh; y++)
                for (int x = 0; x < rw; x++)
                {
                    if (px[(ry + y) * tw + rx + x].a < SolidAlpha) continue;
                    if (x < x0) x0 = x; if (x > x1) x1 = x;
                    if (y < y0) y0 = y; if (y > y1) y1 = y;
                }
            if (x1 < 0) return false;
            bounds = new Rect(x0 / (float)rw, y0 / (float)rh, (x1 - x0 + 1) / (float)rw, (y1 - y0 + 1) / (float)rh);
            return true;
        }
    }
}
