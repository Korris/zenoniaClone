using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Assassin Shadow Step and Cleric Heal (web shadowStep / heal skill handlers, rank 3 values) with their FX.</summary>
    public partial class HeroController
    {
        const float StepRange = 90f, StepBehind = 12f, StepMult = 2.2f * 1.25f, StepInvulnMs = 300f, HealFraction = 0.38f;

        /// <summary>Blink behind the nearest foe within range and stab it for a guaranteed crit. False (no cost) when nothing is in range or no room.</summary>
        bool ShadowStep()
        {
            Monster target = null;
            float best = StepRange;
            var from = new Vector2(x, y);
            IReadOnlyList<Monster> all = Monster.All;
            for (int i = 0; i < all.Count; i++)
            {
                Monster m = all[i];
                if (m.IsDying) continue;
                float d = Vector2.Distance(from, new Vector2(m.Center.x, m.Center.y - 6f));
                if (d < best) { best = d; target = m; }
            }
            if (target == null) return false;
            var feet = new Vector2(target.Center.x, target.Center.y - 6f);
            Vector2 dir = (feet - from).sqrMagnitude > 0.01f ? (feet - from).normalized : DirVec[facing];
            Vector2 dest = feet + dir * StepBehind;
            if (FrostHollowMap.RectBlocked(dest.x - BoxW * 0.5f, dest.y, BoxW, BoxH)) dest = feet - dir * StepBehind; // no room behind: strike from the front
            if (FrostHollowMap.RectBlocked(dest.x - BoxW * 0.5f, dest.y, BoxW, BoxH)) return false;

            HeroFx.ShadowTrail(from, dest);
            x = dest.x; y = dest.y;
            Vector2 toTarget = feet - dest;
            facing = Mathf.Abs(toTarget.x) >= Mathf.Abs(toTarget.y) ? (toTarget.x >= 0f ? 3 : 2) : (toTarget.y >= 0f ? 1 : 0);
            invuln = Mathf.Max(invuln, StepInvulnMs);
            int dmg = CombatMath.Damage(AtkStat, StepMult * 1.6f, target.Def); // guaranteed crit
            skillArg = target.NetId;
            target.TakeHit(dmg, toTarget.normalized * 40f * 2f, true, true, false, this);
            Vector2 hit = new Vector2(target.Center.x, target.Center.y + 4f);
            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            HeroFx.ShadowStrike(hit, angle, dest, new Vector2(x, y));
            GameClock.HitStop(FeelConfig.HitStopFinisherMs);
            return true;
        }

        /// <summary>Tiny cosmetic heal pulse (cleric finisher): a few percent of max HP and a green sparkle around the hero.</summary>
        public void HealPulse(float fraction)
        {
            if (dead || hp >= MaxHp) return;
            hp = Mathf.Min(MaxHp, hp + MaxHp * fraction);
            HeroFx.HealSparkle(new Vector2(x, y));
        }

        /// <summary>Restores 38% of max HP with rising green-gold sparkles and a holy ring. False when already at full HP.</summary>
        bool Heal()
        {
            if (hp >= MaxHp - 0.5f) return false;
            float healed = Mathf.Min(MaxHp - hp, MaxHp * HealFraction);
            hp += healed;
            skillArg = (ushort)Mathf.Clamp(Mathf.RoundToInt(healed), 0, 65535);
            HeroFx.Heal(new Vector2(x, y), Mathf.RoundToInt(healed));
            return true;
        }
    }
}
