using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Assassin Shadow Step and Cleric Heal (web shadowStep / heal skill handlers, rank 3 values) with their FX.</summary>
    public partial class HeroController
    {
        const float StepRange = 90f, StepBehind = 12f, StepMult = 2.2f * 1.25f, StepInvulnMs = 300f, HealFraction = 0.38f;
        static readonly Color Violet = new Color(0.62f, 0.34f, 1f), HolyGreen = new Color(0.62f, 1f, 0.5f);

        /// <summary>Blink behind the nearest foe within range and stab it for a guaranteed crit. False (no cost) when nothing is in range or no room.</summary>
        bool ShadowStep()
        {
            Monster target = null;
            float best = StepRange;
            var from = new Vector2(x, y);
            foreach (Monster m in Monster.All)
            {
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

            TrailFx(from, dest);
            x = dest.x; y = dest.y;
            Vector2 toTarget = feet - dest;
            facing = Mathf.Abs(toTarget.x) >= Mathf.Abs(toTarget.y) ? (toTarget.x >= 0f ? 3 : 2) : (toTarget.y >= 0f ? 1 : 0);
            invuln = Mathf.Max(invuln, StepInvulnMs);
            int dmg = CombatMath.Damage(AtkStat, StepMult * 1.6f, target.Def); // guaranteed crit
            target.TakeHit(dmg, toTarget.normalized * 40f * 2f, true, true);
            Vector2 hit = new Vector2(target.Center.x, target.Center.y + 4f);
            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            DaggerFx.Strike(hit, angle, 30f, true, false, 4);
            ShockRing.Spawn(dest, 16f, 0.3f, new Color(0.62f, 0.3f, 1f, 0.95f), UltimateFx.GroundFxOrder + 6, 0.72f, 0.15f);
            UltimateFx.Embers(hit, 26, 8f, 40f, new Color(0.7f, 0.45f, 1f, 1f));
            GameClock.HitStop(FeelConfig.HitStopFinisherMs);
            CameraFollow.AddTrauma(0.45f);
            DamageNumber.Spawn(x, y + 30f, "Shadow Step!", new Color(0.85f, 0.66f, 1f), false);
            return true;
        }

        /// <summary>Violet afterimage streak along the blink line plus smoke where the hero vanished.</summary>
        static void TrailFx(Vector2 from, Vector2 to)
        {
            int n = Mathf.Max(6, Mathf.RoundToInt(Vector2.Distance(from, to) / 4f));
            for (int i = 0; i <= n; i++)
                UltimateFx.Embers(Vector2.Lerp(from, to, i / (float)n) + new Vector2(0f, 8f), 2, 3f, 6f, new Color(0.6f, 0.35f, 1f, 0.9f));
            ShockRing.Spawn(from, 14f, 0.28f, new Color(0.5f, 0.25f, 0.9f, 0.9f), UltimateFx.GroundFxOrder + 6, 0.72f, 0.2f);
            UltimateFx.Dust(from, 4, 5f);
        }

        /// <summary>Tiny cosmetic heal pulse (cleric finisher): a few percent of max HP and a green sparkle around the hero.</summary>
        public void HealPulse(float fraction)
        {
            if (dead || hp >= MaxHp) return;
            hp = Mathf.Min(MaxHp, hp + MaxHp * fraction);
            UltimateFx.Embers(new Vector2(x, y + 6f), 8, 7f, 30f, new Color(0.7f, 1f, 0.5f, 1f));
        }

        /// <summary>Restores 38% of max HP with rising green-gold sparkles and a holy ring. False when already at full HP.</summary>
        bool Heal()
        {
            if (hp >= MaxHp - 0.5f) return false;
            float healed = Mathf.Min(MaxHp - hp, MaxHp * HealFraction);
            hp += healed;
            DamageNumber.Spawn(x, y + 30f, "+" + Mathf.RoundToInt(healed), new Color(0.55f, 1f, 0.55f), true);
            var feet = new Vector2(x, y);
            ShockRing.Spawn(feet, 24f, 0.55f, new Color(0.45f, 0.85f, 0.35f, 0.95f), UltimateFx.GroundFxOrder + 6, 0.72f, 0.3f);
            ShockRing.Spawn(feet, 15f, 0.4f, new Color(1f, 0.92f, 0.5f, 0.8f), UltimateFx.GroundFxOrder + 7, 0.72f, 0.2f, true);
            UltimateFx.Embers(feet + new Vector2(0f, 4f), 40, 14f, 48f, new Color(0.7f, 1f, 0.45f, 1f));
            UltimateFx.Embers(feet + new Vector2(0f, 8f), 14, 8f, 70f, new Color(1f, 0.95f, 0.55f, 1f));
            ScreenFlash.Trigger(new Color(0.5f, 1f, 0.5f, 0.07f), 0.25f);
            return true;
        }
    }
}
