using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>One painted ranged-attack sheet already sliced by Tools/slice-vfx-sheets.py: Rows[row][frame], row 0 cast, 1 projectile, 2 impact.</summary>
    public sealed class VfxSet
    {
        public const int Cast = 0, Proj = 1, Impact = 2;
        public Sprite[][] Rows;
    }

    /// <summary>
    /// Loader for the hand-painted ranged FX frames (Resources/Sprites/fx/vfx/SET/ROW_N.png + Resources/Data/vfx-SET.json with per-frame pivots).
    /// Sets: cleric, wizard, mechanic-orange, mechanic-blue (C toggles). For() returns null when art is missing, callers then fall back to the procedural FX.
    /// </summary>
    public static class VfxSprites
    {
        public const float UnitsPerPx = 0.058f; // cast ~17, projectile ~20, impact ~19 world units wide (hero ~18 tall)
        static readonly string[] RowNames = { "cast", "proj", "impact" };
        static readonly Dictionary<string, VfxSet> Cache = new Dictionary<string, VfxSet>();

        [Serializable] class Frame { public string name; public int w, h; public float px, py; }
        [Serializable] class Meta { public Frame[] frames; }

        public static string IdFor(BoltStyle style)
        {
            switch (style)
            {
                case BoltStyle.HolyArrow: return "cleric";
                case BoltStyle.Arcane: return "wizard";
                case BoltStyle.Plasma: return ClassPalette.MechanicBlue ? "mechanic-blue" : "mechanic-orange";
                default: return null;
            }
        }

        public static VfxSet For(BoltStyle style) { string id = IdFor(style); return id == null ? null : Get(id); }

        /// <summary>Loads (once) all frames of a set; null when the JSON or a PNG is missing.</summary>
        public static VfxSet Get(string id)
        {
            VfxSet set;
            if (Cache.TryGetValue(id, out set)) return set;
            set = Load(id);
            Cache[id] = set; // a missing set is cached as null so the error logs once
            return set;
        }

        /// <summary>Loads the class's art ahead of its first cast (mechanic: both colours) so the first shot does not hitch.</summary>
        public static void Preload(HeroClass id)
        {
            if (id == HeroClass.Cleric) Get("cleric");
            else if (id == HeroClass.Wizard) Get("wizard");
            else if (id == HeroClass.Mechanic) { Get("mechanic-orange"); Get("mechanic-blue"); }
        }

        static VfxSet Load(string id)
        {
            try
            {
                TextAsset json = Resources.Load<TextAsset>("Data/vfx-" + id);
                if (json == null) { Debug.LogWarning("[ZenoniaProto] Missing Resources/Data/vfx-" + id + ".json, using procedural FX"); return null; }
                Meta meta = JsonUtility.FromJson<Meta>(json.text);
                var rows = new Sprite[RowNames.Length][];
                for (int r = 0; r < rows.Length; r++) rows[r] = new Sprite[4];
                foreach (Frame f in meta.frames)
                {
                    string[] parts = f.name.Split('_');
                    int r = Array.IndexOf(RowNames, parts[0]), n = int.Parse(parts[1]);
                    Texture2D tex = SpriteSheet.Load("Sprites/fx/vfx/" + id + "/" + f.name);
                    if (r < 0 || n < 0 || n > 3 || tex == null) return null;
                    // Sprite pivot is per frame (head / centre / bottom-centre set by the slicer); ppu = 1 / UnitsPerPx so one source pixel = UnitsPerPx world units
                    rows[r][n] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(f.px / tex.width, f.py / tex.height), 1f / UnitsPerPx, 0, SpriteMeshType.FullRect);
                }
                for (int r = 0; r < rows.Length; r++) foreach (Sprite s in rows[r]) if (s == null) return null;
                return new VfxSet { Rows = rows };
            }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] VFX set " + id + " failed: " + e.Message); return null; }
        }
    }
}
