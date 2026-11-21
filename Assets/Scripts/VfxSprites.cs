using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// One painted ranged-attack sheet already sliced (Tools/slice-vfx-sheets.py, Tools/build-*-attack-fx*.py): Rows[row][frame], row 0 cast, 1 projectile,
    /// 2 impact; optional rows 3 big bolt and 4 blast (Mechanic v2: hit 3's blue bolt, hit 4's explosion), null when the set has none.
    /// </summary>
    public sealed class VfxSet
    {
        public const int Cast = 0, Proj = 1, Impact = 2, Bolt = 3, Blast = 4, Shard = 5, Lance = 6, Spikes = 7; // Shard: Wizard v3 hit 2 (the book's frost shard; v4: the frost crescent); Lance: Cleric v2 hit 3 (halo lance); Spikes: Wizard v4 hit 3 (glacier spikes, drawn upright)
        public Sprite[][] Rows;
    }

    /// <summary>
    /// Loader for the hand-painted ranged FX frames (Resources/Sprites/classes/CLASS/fx/attack-vN/ROW_N.png + Resources/Data/classes/CLASS/vfx-attack-vN.json with per-frame pivots).
    /// One set per class (the Cleric's painted cleric-v2 was replaced by ClericAttackLightFx on 2026-11-20, the Mechanic's mechanic-v3 by
    /// MechanicAttackLightFx on 2026-11-21): wizard-v4 (Tools/build-wizard-attack-fx-v4.py). The older sets (cleric, wizard, wizard-v2/v3, mechanic-v2/orange/blue) were removed 16/11. For() returns null
    /// when art is missing, callers then fall back to the procedural FX.
    /// </summary>
    public static class VfxSprites
    {
        public const float UnitsPerPx = 0.058f; // cast ~17, projectile ~20, impact ~19 world units wide (hero ~18 tall)
        static readonly string[] RowNames = { "cast", "proj", "impact", "bolt", "blast", "shard", "lance", "spikes" };
        const int RequiredRows = 3; // cast, proj, impact; bolt and blast are optional
        static readonly Dictionary<string, VfxSet> Cache = new Dictionary<string, VfxSet>();

        [Serializable] class Frame { public string name; public int w, h; public float px, py; }
        [Serializable] class Meta { public Frame[] frames; }

        public static string IdFor(BoltStyle style)
        {
            switch (style)
            {
                case BoltStyle.HolyArrow: return null;         // painted cleric-v2 set removed 2026-11-20: the light FX draws it (ClericAttackLightFx; legacy = procedural StreakBoltRig + HolySmite)
                case BoltStyle.Arcane: return WizardAttackLightFx.Active ? null : "wizard-v4"; // light FX since 2026-11-21 (WizardAttackLightFx); no shaders = the painted combo board 2026-11-10 set (Tools/build-wizard-attack-fx-v4.py)
                case BoltStyle.Plasma: return null;           // painted mechanic-v3 set removed 2026-11-21: the light FX draws it (MechanicAttackLightFx; no shaders = procedural StreakBoltRig + MechanicFx)
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
            if (id == HeroClass.Wizard) { if (!WizardAttackLightFx.Active) Get("wizard-v4"); }
            else if (id == HeroClass.Mechanic) MechanicCannonBlastFx.Preload(); // shader FX: materials only
        }

        /// <summary>"mechanic-v3" -> class "mechanic", set "attack-v3"; "wizard" -> "wizard", "attack".</summary>
        static void Split(string id, out string cls, out string set) { int d = id.IndexOf('-'); cls = d < 0 ? id : id.Substring(0, d); set = d < 0 ? "attack" : "attack" + id.Substring(d); }

        static VfxSet Load(string id)
        {
            try
            {
                string cls, set;
                Split(id, out cls, out set);
                TextAsset json = Resources.Load<TextAsset>("Data/classes/" + cls + "/vfx-" + set);
                if (json == null) { Debug.LogWarning("[ZenoniaProto] Missing Resources/Data/classes/" + cls + "/vfx-" + set + ".json, using procedural FX"); return null; }
                Meta meta = JsonUtility.FromJson<Meta>(json.text);
                var rows = new Sprite[RowNames.Length][];
                for (int r = 0; r < rows.Length; r++) rows[r] = new Sprite[4];
                foreach (Frame f in meta.frames)
                {
                    string[] parts = f.name.Split('_');
                    int r = Array.IndexOf(RowNames, parts[0]), n = int.Parse(parts[1]);
                    Texture2D tex = SpriteSheet.Load("Sprites/classes/" + cls + "/fx/" + set + "/" + f.name);
                    if (r < 0 || n < 0 || n > 3 || tex == null) return null;
                    // Sprite pivot is per frame (head / centre / bottom-centre set by the slicer); ppu = 1 / UnitsPerPx so one source pixel = UnitsPerPx world units
                    rows[r][n] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(f.px / tex.width, f.py / tex.height), 1f / UnitsPerPx, 0, SpriteMeshType.FullRect);
                }
                for (int r = 0; r < rows.Length; r++)
                {
                    bool complete = Array.TrueForAll(rows[r], s => s != null);
                    if (complete) continue;
                    if (r < RequiredRows) return null;
                    if (Array.Exists(rows[r], s => s != null)) Debug.LogWarning("[ZenoniaProto] VFX set " + id + ": row " + RowNames[r] + " incomplete, not used");
                    rows[r] = null; // optional row the set does not have
                }
                return new VfxSet { Rows = rows };
            }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] VFX set " + id + " failed: " + e.Message); return null; }
        }
    }
}
