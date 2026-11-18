using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Data of one layered hero (docs/character-appearance-layered-spec.md; formats in Tools/hero_layered_test_sheets.py):
    /// Data/hero-layered/&lt;set&gt;/class.json (hair styles, headgear, weapons, per-frame grips for all 8 Dir8 dirs) pointing at a gender body.json
    /// (cell, feet, anims, head anchors for the 5 painted dirs). Sheets: body/&lt;anim&gt;.png + &lt;anim&gt;-mask.png (+ hand/&lt;anim&gt;.png), rows = painted dirs
    /// down, down-right, right, up-right, up; left dirs are the mirrored right rows. Loaded once per class json and shared by every hero.
    /// </summary>
    public sealed class HeroLayeredSet
    {
        [Serializable] public class AnimSpec { public string name; public int frames = 1; }
        [Serializable] public class HeadAnchor { public string anim; public int dir, f; public float x, y, w, angle; }
        [Serializable] class BodyFile { public int cellW = 320, cellH = 256, feetY = 244, bodyPx = 200; public string sheets; public AnimSpec[] anims; public HeadAnchor[] head; }
        [Serializable] public class LayerEntry { public int style, id; public string sheet; public int cellW, cellH; public float ax, ay, refW = 1f, drop; public bool hidesHair; }
        [Serializable] public class WeaponEntry { public string part, sheet; public float gx, gy; }
        [Serializable] public class Grip { public string part, anim; public int dir, f; public float x, y, angle; public bool behind; }
        [Serializable] class ClassFile { public string body; public LayerEntry[] hair, headgear; public WeaponEntry[] weapons; public Grip[] grips; }

        /// <summary>World height of the body (the paper doll's figure height), as on the RO Soft path.</summary>
        public const float BodyUnits = 20.5f;
        public const int PaintedDirs = 5;

        static readonly Dictionary<string, HeroLayeredSet> Cache = new Dictionary<string, HeroLayeredSet>();

        public int CellW, CellH, FeetY;
        public float Ppu;
        string sheets;
        readonly Dictionary<string, int> frames = new Dictionary<string, int>();
        readonly Dictionary<string, HeadAnchor> heads = new Dictionary<string, HeadAnchor>();
        readonly Dictionary<string, Grip> grips = new Dictionary<string, Grip>();
        readonly Dictionary<int, LayerEntry> hair = new Dictionary<int, LayerEntry>(), headgear = new Dictionary<int, LayerEntry>();
        readonly Dictionary<string, Sprite> weapons = new Dictionary<string, Sprite>();
        readonly List<string> anims = new List<string>();

        /// <summary>The set behind a class json (resource path without extension), or null when it or its body / sheets are missing.</summary>
        public static HeroLayeredSet Load(string classJson)
        {
            HeroLayeredSet s;
            if (Cache.TryGetValue(classJson, out s)) return s;
            Cache[classJson] = s = TryRead(classJson);
            return s;
        }

        static HeroLayeredSet TryRead(string classJson)
        {
            try
            {
                TextAsset c = Resources.Load<TextAsset>(classJson);
                if (c == null) return null; // this class has no layered art (Fighter): the other renderer paths draw it, no warning
                ClassFile cf = c != null ? JsonUtility.FromJson<ClassFile>(c.text) : null;
                TextAsset b = cf != null && cf.body != null ? Resources.Load<TextAsset>(cf.body) : null;
                BodyFile bf = b != null ? JsonUtility.FromJson<BodyFile>(b.text) : null;
                if (bf == null || bf.anims == null || bf.bodyPx <= 0) { Debug.LogWarning("[ZenoniaProto] layered set " + classJson + " missing or incomplete"); return null; }
                var s = new HeroLayeredSet { CellW = bf.cellW, CellH = bf.cellH, FeetY = bf.feetY, Ppu = bf.bodyPx / BodyUnits, sheets = bf.sheets };
                foreach (AnimSpec a in bf.anims) { s.frames[a.name] = Mathf.Max(1, a.frames); s.anims.Add(a.name); }
                if (bf.head != null) foreach (HeadAnchor h in bf.head) s.heads[Key(h.anim, h.dir, h.f)] = h;
                if (cf.grips != null) foreach (Grip g in cf.grips) s.grips[g.part + "|" + Key(g.anim, g.dir, g.f)] = g;
                if (cf.hair != null) foreach (LayerEntry e in cf.hair) s.hair[e.style] = e;
                if (cf.headgear != null) foreach (LayerEntry e in cf.headgear) s.headgear[e.id] = e;
                if (cf.weapons != null)
                    foreach (WeaponEntry w in cf.weapons)
                    {
                        Texture2D t = SpriteSheet.TryLoad(w.sheet);
                        if (t != null) s.weapons[w.part] = SpriteSheet.Whole(w.sheet, s.Ppu, new Vector2(w.gx / t.width, 1f - w.gy / t.height));
                    }
                return s.BodyGrid(s.anims[0]) != null ? s : null;
            }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] layered set " + classJson + ": " + e.Message); return null; }
        }

        static string Key(string anim, int dir, int f) { return anim + "|" + dir + "|" + f; }

        public bool HasAnim(string anim) { return frames.ContainsKey(anim); }
        public int Frames(string anim) { int n; return frames.TryGetValue(anim, out n) ? n : 1; }

        /// <summary>The painted anim a game anim draws: its own sheet, else idle (idle / hurt / death), walk (walk / dash), else attack&lt;digit&gt; or the first attack.</summary>
        public string Resolve(string anim)
        {
            if (anim != null && frames.ContainsKey(anim)) return anim;
            if (anim == null || anim == "idle" || anim == "hurt" || anim == "death") return "idle";
            if (anim == "walk" || anim == "dash") return frames.ContainsKey("walk") ? "walk" : "idle";
            char last = anim[anim.Length - 1];
            if (char.IsDigit(last) && frames.ContainsKey("attack" + last)) return "attack" + last;
            foreach (string a in anims) if (a.StartsWith("attack", StringComparison.Ordinal)) return a;
            return "idle";
        }

        public Sprite[][] BodyGrid(string anim) { return Grid(sheets + anim); }
        public Sprite[][] HandGrid(string anim) { return SpriteSheet.TryLoad(sheets + "hand/" + anim) != null ? Grid(sheets + "hand/" + anim) : null; }
        public Texture2D Mask(string anim) { return SpriteSheet.TryLoad(sheets + anim + "-mask"); }

        Sprite[][] Grid(string path)
        {
            return SpriteSheet.TryLoad(path) != null ? SpriteSheet.Grid(path, CellW, CellH, Ppu, new Vector2(0.5f, (CellH - FeetY) / (float)CellH)) : null;
        }

        public HeadAnchor Head(string anim, int paintedDir, int f) { HeadAnchor h; return heads.TryGetValue(Key(anim, paintedDir, f), out h) ? h : null; }
        public Grip GripOf(string part, string anim, int dir8, int f) { Grip g; return grips.TryGetValue(part + "|" + Key(anim, dir8, f), out g) ? g : null; }
        public Sprite Weapon(string part) { Sprite s; return weapons.TryGetValue(part, out s) ? s : null; }

        public LayerEntry Hair(int style)
        {
            LayerEntry e;
            if (hair.TryGetValue(style, out e)) return e;
            return hair.TryGetValue(0, out e) ? e : null; // a style this set has no art for: the default head
        }

        /// <summary>The set's own entry for this hat id (the test set), else the shared hat art of Data/hero-layered/headgear.json once its sheet exists.</summary>
        public LayerEntry Headgear(int id) { LayerEntry e; return id > 0 && headgear.TryGetValue(id, out e) ? e : HeadgearArt.Layer(id); }

        /// <summary>Cell `dir` (painted dir) of a hair / headgear strip, pivot on the entry's anchor point.</summary>
        public Sprite LayerCell(LayerEntry e, int paintedDir)
        {
            if (e == null || SpriteSheet.TryLoad(e.sheet) == null) return null;
            Sprite[][] g = SpriteSheet.Grid(e.sheet, e.cellW, e.cellH, Ppu, new Vector2(e.ax / e.cellW, 1f - e.ay / e.cellH));
            Sprite[] row = SpriteSheet.Row(g, 0);
            return row != null && paintedDir < row.Length ? row[paintedDir] : null;
        }
    }
}
