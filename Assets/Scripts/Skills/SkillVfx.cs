using System;
using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Painted frames of one skill: Rows[Cast / Loop / Impact] (null row = not painted), px size of the widest frame per row, fps per row.</summary>
    public sealed class SkillVfxSet
    {
        public const int Cast = 0, Loop = 1, Impact = 2;
        public Sprite[][] Rows = new Sprite[3][];
        public float[] Fps = { 12f, 12f, 12f }, WidestPx = new float[3];
    }

    /// <summary>
    /// Skill art loader + player (phase 06 layout): Resources/Data/skills/&lt;class&gt;-&lt;skill&gt;.json lists the frames (name "row_n", w, h, pivot px/py),
    /// Resources/Sprites/skills/&lt;class&gt;/&lt;skill&gt;/&lt;row&gt;_&lt;n&gt;.png holds them. Rows: cast (at the caster), loop (flying / lingering body), impact (at the hit).
    /// A missing set or row degrades to a tinted procedural ring so skills are playable before their art lands. Art is drawn only where RendersFx.
    /// </summary>
    public static class SkillVfx
    {
        const float SourceUnitsPerPx = 0.01f; // sprites are created at 100 ppu; scale is set from the wanted world width
        static readonly string[] RowNames = { "cast", "loop", "impact" };
        static readonly Dictionary<string, SkillVfxSet> Cache = new Dictionary<string, SkillVfxSet>();

        [Serializable] class Frame { public string name; public int w, h; public float px, py; }
        [Serializable] class Fps { public float cast, loop, impact; }
        [Serializable] class Meta { public Frame[] frames; public Fps fps; }

        /// <summary>Resource key of a skill: "wizard.fireball" -> ("wizard-fireball", "wizard/fireball").</summary>
        static void Keys(SkillDef def, out string data, out string folder) { data = def.Class + "-" + def.Id.Substring(def.Class.Length + 1); folder = def.Class + "/" + def.Id.Substring(def.Class.Length + 1); }

        /// <summary>Loads (once) the painted set of a skill; null when it has no art yet.</summary>
        public static SkillVfxSet Get(SkillDef def)
        {
            SkillVfxSet set;
            if (Cache.TryGetValue(def.Id, out set)) return set;
            set = Load(def);
            Cache[def.Id] = set;
            return set;
        }

        /// <summary>Loads the art of a class's skills ahead of the first cast so no cast hitches.</summary>
        public static void Preload(string classId) { if (NetMode.RendersFx) foreach (SkillDef d in SkillTable.ForClass(classId)) Get(d); }

        static SkillVfxSet Load(SkillDef def)
        {
            string data, folder;
            Keys(def, out data, out folder);
            try
            {
                TextAsset json = Resources.Load<TextAsset>("Data/skills/" + data);
                if (json == null) return null; // no art yet: procedural fallback, nothing to log
                Meta meta = JsonUtility.FromJson<Meta>(json.text);
                var set = new SkillVfxSet();
                if (meta.fps != null) set.Fps = new[] { meta.fps.cast > 0 ? meta.fps.cast : 12f, meta.fps.loop > 0 ? meta.fps.loop : 12f, meta.fps.impact > 0 ? meta.fps.impact : 12f };
                var rows = new List<Sprite>[3] { new List<Sprite>(), new List<Sprite>(), new List<Sprite>() };
                foreach (Frame f in meta.frames)
                {
                    string[] parts = f.name.Split('_');
                    int r = Array.IndexOf(RowNames, parts[0]), n;
                    if (r < 0 || parts.Length != 2 || !int.TryParse(parts[1], out n)) continue;
                    Texture2D tex = Resources.Load<Texture2D>("Sprites/skills/" + folder + "/" + f.name);
                    if (tex == null) continue;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    while (rows[r].Count <= n) rows[r].Add(null);
                    rows[r][n] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(f.px / tex.width, f.py / tex.height), 1f / SourceUnitsPerPx, 0, SpriteMeshType.FullRect);
                    set.WidestPx[r] = Mathf.Max(set.WidestPx[r], tex.width);
                }
                for (int r = 0; r < 3; r++)
                    if (rows[r].Count > 0 && !rows[r].Contains(null)) set.Rows[r] = rows[r].ToArray(); // a row with a hole is dropped whole
                return set.Rows[0] != null || set.Rows[1] != null || set.Rows[2] != null ? set : null;
            }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] skill VFX " + def.Id + " failed: " + e.Message); return null; }
        }

        static bool Painted(SkillDef def, int row) { SkillVfxSet s = Get(def); return s != null && s.Rows[row] != null; }

        /// <summary>Plays a row once, `widthUnits` wide (the widest frame maps to it). Cast / impact without art -> a tinted ring of that width.</summary>
        public static void Play(SkillContext ctx, int row, Vector2 at, float angleDeg, float widthUnits)
        {
            if (!ctx.View) return;
            SkillVfxSet s = Get(ctx.Def);
            if (s != null && s.Rows[row] != null)
            {
                int order = row == SkillVfxSet.Cast ? FeelConfig.FxOrder - 7 : FeelConfig.FxOrder + 2;
                SkillFxAnim.Play(s.Rows[row], s.Fps[row], at, angleDeg, widthUnits / (s.WidestPx[row] * SourceUnitsPerPx), order);
                return;
            }
            if (row == SkillVfxSet.Loop) return;
            ShockRing.Spawn(at, Mathf.Max(4f, widthUnits * 0.5f), row == SkillVfxSet.Cast ? 0.28f : 0.4f, Tint(ctx), FeelConfig.FxOrder - 8, 0.7f, 0.25f, true);
        }

        /// <summary>Starts the looping body (projectile / zone) or null when the skill has no loop art (callers draw the fallback).</summary>
        public static SkillFxAnim Loop(SkillContext ctx, Vector2 at, float angleDeg, float widthUnits, float seconds)
        {
            if (!ctx.View || !Painted(ctx.Def, SkillVfxSet.Loop)) return null;
            SkillVfxSet s = Get(ctx.Def);
            return SkillFxAnim.Play(s.Rows[SkillVfxSet.Loop], s.Fps[SkillVfxSet.Loop], at, angleDeg, widthUnits / (s.WidestPx[SkillVfxSet.Loop] * SourceUnitsPerPx), FeelConfig.FxOrder, true, seconds);
        }

        public static Color Tint(SkillContext ctx) { ClassPalette p = ctx.Palette ?? ClassPalette.Neutral; return p.Glow.a > 0f ? p.Glow : p.Tint; }
    }
}
