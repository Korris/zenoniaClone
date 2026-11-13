using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Authority-only skill AI of a Wanted Boss (docs/wanted-boss-skills-design.md): Monster.Ai.cs's Chase state calls TryBeginWantedSkill before
    /// its plain-attack windup check; once a skill is picked the boss stands still through State.Telegraph (shape locked, everyone warned) and
    /// State.Execute (the hit lands, then a few seconds of plain attacks follow before the next skill may start -- WantedSkillClock.SkillGapMs).
    /// Only wanted bosses carry this (Monster.Ai.cs gates every call on def.IsWanted); it never runs on a client proxy (Think is authority-only).
    /// </summary>
    public partial class Monster
    {
        const float ExecuteMs = 350f; // impact / follow-through window before the boss resumes chasing
        static readonly Vector2[] WantedFacingVec = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };

        readonly WantedSkillClock wantedClock = new WantedSkillClock();
        WantedSkillDef[] wantedTable;
        WantedSkillDef pendingSkill;
        readonly List<WantedShapeInstance> pendingShapes = new List<WantedShapeInstance>(5);

        /// <summary>True once below 30 % hp (WantedSkillClock.EnrageHpFraction): the warning-enraged icon and the client's own display hook read it.</summary>
        public bool WantedEnraged { get { return def.IsWanted && hp / def.Hp < WantedSkillClock.EnrageHpFraction; } }
        /// <summary>Debug capture only (Tools/wanted-boss-test.ps1): true while this wanted boss is telegraphing a skill.</summary>
        public bool IsWantedTelegraphing { get { return state == State.Telegraph; } }

        void TickWantedSkills(float dtMs) { wantedClock.Tick(dtMs); }

        /// <summary>Chase: picks a ready skill (none ready = the plain attack cycle keeps going), locks its shape(s) and enters Telegraph.</summary>
        bool TryBeginWantedSkill()
        {
            if (wantedTable == null) wantedTable = WantedSkillTable.For(def.Sheet);
            if (wantedTable == null) return false;
            int pick = WantedSkillPicker.Pick(wantedTable, wantedClock, hp / def.Hp, Random.value);
            if (pick < 0) return false;
            WantedSkillDef skill = wantedTable[pick];
            if (!LockWantedShapes(skill)) return false;
            pendingSkill = skill;
            wantedClock.OnSkillUsed(skill);
            Enter(State.Telegraph, skill.TelegraphMs);
            BroadcastWantedTelegraph(skill);
            return true;
        }

        /// <summary>Resolves the skill's shape(s) now (hero positions, boss facing): locked for the whole telegraph. False = skip this pick
        /// without spending its cooldown (an OnPlayers skill with nobody alive to target).</summary>
        bool LockWantedShapes(WantedSkillDef skill)
        {
            pendingShapes.Clear();
            if (skill.OnPlayers)
            {
                IReadOnlyList<HeroController> all = homeZone != null ? homeZone.Heroes.All : null;
                if (all == null) return false;
                var alive = new List<HeroController>(all.Count);
                for (int i = 0; i < all.Count; i++) if (all[i] != null && !all[i].IsDead) alive.Add(all[i]);
                if (alive.Count == 0) return false;
                int take = Mathf.Min(skill.Count, alive.Count);
                for (int i = 0; i < take; i++)
                {
                    int j = i + Random.Range(0, alive.Count - i);
                    (alive[i], alive[j]) = (alive[j], alive[i]);
                    Vector2 p = alive[i].Position;
                    pendingShapes.Add(new WantedShapeInstance { OriginX = p.x, OriginY = p.y });
                }
                return true;
            }
            Vector2 origin = Feet; // "tâm vòng chân boss": the gameplay centre, not the painted body centre
            float dirDeg;
            if (skill.Shape == WantedAreaShape.Cone || skill.Shape == WantedAreaShape.Line)
            {
                Vector2 toTarget = target != null && !target.IsDead ? target.Position - origin : Vector2.zero;
                Vector2 dir = toTarget.sqrMagnitude > 1f ? toTarget.normalized : WantedFacingVec[facing];
                FaceTowards(dir);
                dirDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            }
            else dirDeg = skill.SafeArcCount > 0 ? Random.Range(0f, 360f) : 0f; // the enrage circle's safe lane(s): a fresh random direction each cast
            pendingShapes.Add(new WantedShapeInstance { OriginX = origin.x, OriginY = origin.y, DirDeg = dirDeg });
            return true;
        }

        /// <summary>Tells every client the locked shape(s) and the server-clock instant the hit lands, so they can draw the countdown themselves.
        /// Same clock as ServerTimeMs / NetClient.ServerNowMs (ZoneHost.ClockMs, shared by every zone so a portal does not jump it).</summary>
        void BroadcastWantedTelegraph(WantedSkillDef skill)
        {
            float nowMs = ZoneHost.Active != null ? ZoneHost.Active.ClockMs : homeZone.TimeMs;
            uint atMs = (uint)nowMs + (uint)skill.TelegraphMs;
            for (int i = 0; i < pendingShapes.Count; i++)
            {
                WantedShapeInstance s = pendingShapes[i];
                NetEventBus.WantedSkillTelegraph(this, skill.Id, new Vector2(s.OriginX, s.OriginY), s.DirDeg, atMs);
            }
        }

        /// <summary>Telegraph ran out: hits every living hero inside any locked shape, once each.</summary>
        void FireWantedSkill()
        {
            WantedSkillDef skill = pendingSkill;
            pendingSkill = null;
            if (skill == null || homeZone == null) return;
            IReadOnlyList<HeroController> all = homeZone.Heroes.All;
            for (int i = 0; i < all.Count; i++)
            {
                HeroController h = all[i];
                if (h == null || h.IsDead) continue;
                Vector2 p = h.Position;
                bool hit = false;
                for (int s = 0; s < pendingShapes.Count && !hit; s++) hit = WantedSkillArea.Hits(skill, pendingShapes[s], p.x, p.y);
                if (hit) h.TakeWantedSkillHit(Feet, def.Atk, skill);
            }
        }
    }
}
