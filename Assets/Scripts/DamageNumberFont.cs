using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Sprite glyph font of the floating numbers (art: ChatGPT sheets cut by Tools/build-damage-number-font.py).
    /// Layout in Resources/Data/ui/damage-font-glyphs.json; atlases Resources/Sprites/ui/damage-font/base-&lt;style&gt;.png
    /// (one baked colour style per number kind, same layout) plus own-layout sets with their own atlas &lt;name&gt;.png: crit (red-orange,
    /// 22/11) and total (yellow multi-hit TOTAL, thick black outline). Quads go through DamageNumberBatch (one draw call per atlas).
    /// Draw returns false when the font or a glyph is missing, so the caller falls back to plain text.
    /// </summary>
    public static class DamageNumberFont
    {
        [Serializable] class GlyphRec { public string c; public int x, y, w, h; public float top, bottom; } // top / bottom: px above the baseline
        [Serializable] class GlyphSet { public string name; public float capHeight; public int atlasH; public GlyphRec[] glyphs; } // atlasH 0 = the file's
        [Serializable] class FontFile { public int atlasW, atlasH; public GlyphSet[] sets; }

        struct Glyph { public Rect Uv; public float W, Top, Bottom; }
        class Set { public float Cap; public Dictionary<string, Glyph> Glyphs = new Dictionary<string, Glyph>(); }

        static readonly string[] Words = { "CRITICAL", "MISS", "EXP", "HP", "MP" };
        const float Spacing = -0.07f, SpaceAdvance = 0.32f, WordScale = 0.78f;

        static bool loaded;
        static Set baseSet;
        static readonly Dictionary<string, Set> OwnSets = new Dictionary<string, Set>(); // crit, total: own layout + atlas <name>.png
        static readonly Dictionary<string, Texture2D> Atlases = new Dictionary<string, Texture2D>();
        static readonly List<Glyph> Line = new List<Glyph>(16);
        static readonly List<float> Scales = new List<float>(16);

        static void Load()
        {
            loaded = true;
            try
            {
                var json = Resources.Load<TextAsset>("Data/ui/damage-font-glyphs");
                if (json == null) { Debug.LogWarning("[ZenoniaProto] damage font: no Data/ui/damage-font-glyphs, plain text numbers"); return; }
                FontFile f = JsonUtility.FromJson<FontFile>(json.text);
                foreach (GlyphSet s in f.sets)
                {
                    var set = new Set { Cap = s.capHeight };
                    float ah = s.atlasH > 0 ? s.atlasH : f.atlasH;
                    foreach (GlyphRec g in s.glyphs)
                        set.Glyphs[g.c] = new Glyph { Uv = new Rect(g.x / (float)f.atlasW, 1f - (g.y + g.h) / ah, g.w / (float)f.atlasW, g.h / ah), W = g.w, Top = g.top, Bottom = g.bottom };
                    if (s.name == "base") baseSet = set; else OwnSets[s.name] = set;
                }
            }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] damage font: " + e.Message); baseSet = null; OwnSets.Clear(); }
        }

        static Texture2D Atlas(string name)
        {
            Texture2D t;
            if (Atlases.TryGetValue(name, out t)) return t;
            t = Resources.Load<Texture2D>("Sprites/ui/damage-font/" + name);
            if (t == null) Debug.LogWarning("[ZenoniaProto] damage font: missing atlas " + name);
            Atlases[name] = t;
            return t;
        }

        /// <summary>Splits the text into glyph keys (words like EXP first); false if a character has no glyph.</summary>
        static bool Layout(string text, Set set, bool crit)
        {
            Line.Clear(); Scales.Clear();
            for (int i = 0; i < text.Length;)
            {
                if (text[i] == ' ') { Line.Add(new Glyph { W = -1f }); Scales.Add(1f); i++; continue; }
                string key = null;
                foreach (string w in Words) if (string.CompareOrdinal(text, i, w, 0, w.Length) == 0) { key = w; break; }
                bool word = key != null;
                if (!word) key = text[i].ToString();
                Glyph g;
                if (!set.Glyphs.TryGetValue(key, out g)) return false;
                Line.Add(g);
                Scales.Add(word ? WordScale : crit && key == "!" ? 0.8f : 1f);
                i += key.Length;
            }
            return Line.Count > 0;
        }

        /// <summary>Draws the text centred on (cx, baseline y) in GUI space; capPx = digit height on screen. Style null = crit set; a style with
        /// its own set (crit, total) uses it and its atlas, else the base layout with base-&lt;style&gt;. `layer`: DamageNumberBatch paint layer.</summary>
        public static bool Draw(string text, string style, float cx, float baseY, float capPx, float alpha, Color tint, int layer = 0)
        {
            if (!loaded) Load();
            if (style == null) style = "crit";
            bool crit = style == "crit";
            Set set;
            bool own = OwnSets.TryGetValue(style, out set);
            if (!own) set = baseSet;
            if (set == null || !Layout(text, set, crit)) return false;
            Texture2D tex = Atlas(own ? style : "base-" + style);
            if (tex == null) return false;
            float k = capPx / set.Cap, total = 0f;
            for (int i = 0; i < Line.Count; i++)
                total += (Line[i].W < 0f ? SpaceAdvance * set.Cap : Line[i].W * Scales[i]) * k + (i > 0 ? Spacing * capPx : 0f);
            float x = cx - total * 0.5f;
            var col = new Color(tint.r, tint.g, tint.b, alpha);
            for (int i = 0; i < Line.Count; i++)
            {
                if (i > 0) x += Spacing * capPx;
                Glyph g = Line[i];
                if (g.W < 0f) { x += SpaceAdvance * capPx; continue; }
                float s = k * Scales[i];
                float lift = crit && Scales[i] < 1f ? 0.32f * capPx : 0f; // the crit "!" is smaller and pokes up above the digits
                float top = baseY - g.Top * s - lift, h = (g.Top - g.Bottom) * s;
                DamageNumberBatch.Add(tex, new Rect(x, top, g.W * s, h), g.Uv, col, layer);
                x += g.W * s;
            }
            return true;
        }
    }
}
