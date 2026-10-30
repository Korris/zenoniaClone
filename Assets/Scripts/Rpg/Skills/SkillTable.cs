using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Registry of the 40 skills (5 classes x 8). Data lives in the per-class partial files (SkillTable.Warrior.cs ...); values are rank 1 from
    /// plans/261030-1508-skill-tree-design/skill-tree-chatgpt.md. Distances in tiles, times in ms.
    /// </summary>
    public static partial class SkillTable
    {
        static readonly Dictionary<string, SkillDef[]> byClass = new Dictionary<string, SkillDef[]>
        {
            { "warrior", WarriorSkills() }, { "wizard", WizardSkills() }, { "mechanic", MechanicSkills() },
            { "cleric", ClericSkills() }, { "assassin", AssassinSkills() },
        };
        static readonly Dictionary<string, SkillDef> byId = BuildIndex();
        static readonly SkillDef[] none = new SkillDef[0];

        /// <summary>All skills, class order of RpgClasses.Ids then tree index.</summary>
        public static readonly IReadOnlyList<SkillDef> All = Flatten();

        /// <summary>The 8 skills of a class by tree index (empty for an unknown class).</summary>
        public static SkillDef[] ForClass(string classId) { SkillDef[] l; return classId != null && byClass.TryGetValue(classId, out l) ? l : none; }

        public static SkillDef Get(string classId, int index) { SkillDef[] l = ForClass(classId); return index >= 0 && index < l.Length ? l[index] : null; }

        public static SkillDef ById(string id) { SkillDef d; return id != null && byId.TryGetValue(id, out d) ? d : null; }

        static SkillDef[] Flatten()
        {
            var all = new List<SkillDef>();
            foreach (string c in RpgClasses.Ids) all.AddRange(byClass[c]);
            return all.ToArray();
        }

        static Dictionary<string, SkillDef> BuildIndex()
        {
            var d = new Dictionary<string, SkillDef>();
            foreach (SkillDef[] list in byClass.Values) foreach (SkillDef s in list) d[s.Id] = s;
            return d;
        }

        // ---- builders used by the data files ----

        /// <summary>MP added per rank: basic +1, advanced +2 (Lv 20-31) / +3, ultimates +5 (Lv 50).</summary>
        static int MpPerRankFor(int unlock) { return unlock < 20 ? 1 : unlock < 32 ? 2 : unlock < 50 ? 3 : 5; }

        static SkillDef Def(string cls, int index, string name, int unlock, int mp, float cdSec, SkillTargeting targeting, SkillEffect[] fx,
            SkillReq[] prereqs = null, float targetRange = 0f, int lockMs = 350, LegacySkill legacy = LegacySkill.None)
        {
            return new SkillDef
            {
                Id = cls + "." + name.ToLowerInvariant().Replace(' ', '-'), Class = cls, Index = index, Name = name, UnlockLevel = unlock,
                Prereqs = prereqs ?? new SkillReq[0], Mp = mp, MpPerRank = MpPerRankFor(unlock), CooldownMs = (int)(cdSec * 1000f), CastLockMs = lockMs,
                Targeting = targeting, TargetRange = targetRange, Effects = fx, Legacy = legacy,
            };
        }

        static SkillReq[] Req(int a, int ra) { return new[] { new SkillReq(a, ra) }; }
        static SkillReq[] Req(int a, int ra, int b, int rb) { return new[] { new SkillReq(a, ra), new SkillReq(b, rb) }; }
        static SkillEffect[] Fx(params SkillEffect[] e) { return e; }
        static SkillEffect[] NoFx() { return new SkillEffect[0]; }

        static SkillEffect Strike(AreaShape shape, float pct, float radius = 0f, float angle = 0f, float range = 0f, int hits = 1, int intervalMs = 0,
            float length = 0f, float width = 0f, int atMs = 0, StatusSpec status = default(StatusSpec))
        {
            return new SkillEffect { Kind = EffectKind.Strike, Shape = shape, Pct = pct, Radius = radius, AngleDeg = angle, Range = range, Hits = hits, IntervalMs = intervalMs,
                Length = length, Width = width, AtMs = atMs, Status = status };
        }

        static SkillEffect Proj(float pct, float range, float speed, float width = 1f, int count = 1, float spread = 0f, bool pierce = false,
            bool homing = false, float impact = 0f, StatusSpec status = default(StatusSpec))
        {
            return new SkillEffect { Kind = EffectKind.Projectile, Pct = pct, Range = range, Speed = speed, Width = width, Count = count, SpreadDeg = spread, Pierce = pierce,
                Homing = homing, ImpactRadius = impact, Status = status };
        }

        static SkillEffect Move(MotionKind kind, float tiles, int ms, int atMs = 0)
        {
            return new SkillEffect { Kind = EffectKind.Motion, Motion = kind, MoveTiles = tiles, DurationMs = ms, AtMs = atMs };
        }

        static SkillEffect Rain(float pct, int count, float range, float scatter, int intervalMs, float impact)
        {
            return new SkillEffect { Kind = EffectKind.Rain, Pct = pct, Count = count, Range = range, Radius = scatter, IntervalMs = intervalMs, ImpactRadius = impact };
        }

        static SkillEffect BuffFx(BuffKind kind, float value, int ms, bool ally = false, float radius = 0f)
        {
            return new SkillEffect { Kind = EffectKind.Buff, Buff = new BuffSpec(kind, value, ms), Ally = ally, Shape = radius > 0f ? AreaShape.Circle : AreaShape.Single, Radius = radius };
        }

        static SkillEffect HealFx(float pct, float radius = 0f, float range = 0f, int atMs = 0)
        {
            return new SkillEffect { Kind = EffectKind.Heal, Ally = true, Pct = pct, Shape = radius > 0f ? AreaShape.Circle : AreaShape.Single, Radius = radius, Range = range, AtMs = atMs };
        }
    }
}
