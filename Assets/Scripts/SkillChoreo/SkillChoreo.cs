using System;
using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Entry point of the data-driven multi-phase skill VFX ("choreography"): Earth-Break-style layering (telegraph, additive glow, embers / debris / dust,
    /// rings, flash, hit-stop, shake, punch-zoom) per skill. Render only (RendersFx); damage and sim timings are untouched.
    /// Call Has() to decide between this and the one-sheet SkillVfx path, Play() once when the cast starts.
    /// </summary>
    public static class SkillChoreo
    {
        static Dictionary<string, Choreo> table;

        static Dictionary<string, Choreo> Table
        {
            get
            {
                if (table != null) return table;
                var list = new List<Choreo>();
                SkillChoreoDefsWizard.Register(list);
                table = new Dictionary<string, Choreo>();
                foreach (Choreo c in list) table[c.ClassId + "." + c.SkillId] = c;
                return table;
            }
        }

        /// <summary>"fireball" or "wizard.fireball" -> "fireball".</summary>
        static string Short(string skillId) { int i = skillId.IndexOf('.'); return i >= 0 ? skillId.Substring(i + 1) : skillId; }

        public static bool Has(string classId, string skillId) { return classId != null && skillId != null && Table.ContainsKey(classId + "." + Short(skillId)); }

        /// <summary>
        /// Starts the choreography (no-op + null where !RendersFx or the skill has none). origin = caster centre; target = aim point (area centre / projectile aim);
        /// follow = caster transform (shield, cast rings follow it; the choreography ends if it is destroyed); projectilePos = live bolt position (optional);
        /// hits = positions the bolt struck, nearest first (pierce: every pierced target); sfx = cue callback ("cast", "loop", "impact", "break").
        /// Keep the returned player for sustained skills: End(true) on shield break, End(false) on expiry.
        /// </summary>
        public static SkillChoreoPlayer Play(string classId, string skillId, Vector2 origin, Vector2 target, Transform follow = null,
            Func<Vector2> projectilePos = null, IList<Vector2> hits = null, Action<string> sfx = null)
        {
            if (!NetMode.RendersFx || !Has(classId, skillId)) return null;
            Transform root = Zone.Main != null ? Zone.Main.Root : null;
            if (root == null) return null;
            string id = Short(skillId);
            SkillDef def = null;
            foreach (SkillDef d in SkillTable.ForClass(classId)) if (d.Id == classId + "." + id) { def = d; break; }
            Vector2 to = target - origin;
            var env = new ChoreoEnv { Origin = origin, Dir = to.sqrMagnitude > 0.01f ? to.normalized : Vector2.right, Follow = follow, Head = projectilePos, Sfx = sfx, Art = def != null ? SkillVfx.Get(def) : null };
            if (env.Art == null) return null; // no painted art: caller keeps its fallback
            try { return SkillChoreoPlayer.Begin(Table[classId + "." + id], env, target, hits, root); }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] skill choreo " + id + " failed: " + e.Message); return null; }
        }

        /// <summary>Live monsters whose centre lies within `halfWidth` units of the segment origin -> origin + dir * lengthUnits, nearest first (pierce pops).</summary>
        public static List<Vector2> PierceHits(Vector2 origin, Vector2 dir, float lengthUnits, float halfWidth)
        {
            var hits = new List<Vector2>();
            foreach (Monster m in Monster.All)
            {
                if (m.IsDying) continue;
                Vector2 rel = m.Center - origin;
                float along = Vector2.Dot(rel, dir);
                if (along >= 0f && along <= lengthUnits && Mathf.Abs(rel.x * dir.y - rel.y * dir.x) <= halfWidth) hits.Add(m.Center);
            }
            hits.Sort((a, b) => (a - origin).sqrMagnitude.CompareTo((b - origin).sqrMagnitude));
            return hits;
        }
    }
}
