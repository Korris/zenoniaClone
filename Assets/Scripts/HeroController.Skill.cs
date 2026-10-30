using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Class signature skill on L: MP + cooldown bookkeeping, cast root, and dispatch to the skill effect.
    /// Warrior = Earth Break, Wizard = Ice Storm (web ultimates), Assassin = Shadow Step, Cleric = Heal (HeroController.ClassSkills.cs); rank 3 values.
    /// </summary>
    public partial class HeroController
    {
        const float MpRegenPerSecond = 2.2f;
        float skillCd, castLeft, castTotal;
        ushort skillArg; // skill specific payload for the SkillCast event: shadow step target id / healed amount
        float mp { get { return rpg.Mp; } set { rpg.Mp = value; } }

        public float Mp { get { return mp; } }
        public float MaxMp { get { return rpg.MaxMp; } }
        public float SkillCooldownLeftMs { get { return skillCd; } }
        public bool IsCasting { get { return castLeft > 0f; } }

        void ResetSkillState() { skillCd = 0f; castLeft = 0f; }

        /// <summary>Starts the class skill when off cooldown and affordable. Returns true when cast.</summary>
        public bool TryCastSkill()
        {
            if (dead || cls.SkillMp <= 0f || skillCd > 0f || mp < cls.SkillMp || castLeft > 0f || dashLeft > 0f) return false;
            // The effect decides first (shadow step needs a target, heal needs missing HP) so a refused cast costs nothing.
            switch (cls.Id)
            {
                case HeroClass.Warrior: EarthBreakUltimate.Cast(this); break;
                case HeroClass.Wizard: IceStormUltimate.Cast(this); break;
                case HeroClass.Assassin: if (!ShadowStep()) return false; break;
                case HeroClass.Cleric: if (!Heal()) return false; break;
                default: return false;
            }
            NetEventBus.SkillCast(this, new Vector2(x, y), skillArg);
            skillArg = 0;
            mp -= cls.SkillMp;
            skillCd = cls.SkillCooldownMs;
            castLeft = castTotal = cls.SkillLockMs;
            combo.Reset();
            animTimer = 0f;
            return true;
        }

        /// <summary>Regen + cooldown ticks; returns true while the hero is rooted by a cast (no movement / attacks this step).</summary>
        bool TickSkill(float dt, float dtMs)
        {
            mp = Mathf.Min(MaxMp, mp + MpRegenPerSecond * dt);
            skillCd = Mathf.Max(0f, skillCd - dtMs);
            if (castLeft <= 0f) return false;
            castLeft = Mathf.Max(0f, castLeft - dtMs);
            moving = false;
            return true;
        }

        /// <summary>Cast pose: the class's overhead attack frames stretched over the root time.</summary>
        bool CastPose(out string anim, out int frame)
        {
            anim = "attack3";
            frame = 0;
            if (castLeft <= 0f) return false;
            frame = ByProgress(Mathf.Max(1, view.FrameCount(anim, facing)), 1f - castLeft / castTotal);
            return true;
        }
    }
}
