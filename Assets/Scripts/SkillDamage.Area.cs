using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Who strikes and with what: built once per cast by the skill executor.</summary>
    public struct SkillDamageCtx
    {
        public HeroController Hero;   // kill credit, status ownership, "behind" test
        public float Atk;             // caster ATK (already includes buffs)
        public float DamageMul;       // rank multiplier (SkillDef.DamageMul) x temporary buffs
        public float CritChance;      // base crit + hero CritBonus
        public ClassPalette Sparks;   // impact spark colour (null = neutral)

        public const float BaseCritChance = 0.08f; // same base as HeroController / ComboProjectile
        public static SkillDamageCtx For(HeroController hero, float damageMul, ClassPalette sparks)
        {
            return new SkillDamageCtx { Hero = hero, Atk = hero.AtkStat, DamageMul = damageMul, CritChance = BaseCritChance + hero.CritBonus, Sparks = sparks };
        }
    }

    /// <summary>Shape-based skill damage for the data-driven skills (authority only; clients play the picture, the server applies the hit).</summary>
    public static partial class SkillDamage
    {
        const float CritMul = 1.6f, KnockbackSeconds = 0.12f;
        static readonly List<Monster> Candidates = new List<Monster>(24);

        /// <summary>
        /// Strike: every live monster whose box overlaps the effect's shape (placed at `center`, facing `dir`) takes one hit and gets the effect's
        /// status. Single hits only the nearest monster; a Pct 0 status-only strike affects at most `Count` monsters (nearest first).
        /// Returns the reused list of monsters hit.
        /// </summary>
        public static List<Monster> Area(SkillDamageCtx ctx, SkillEffect e, Vector2 center, Vector2 dir)
        {
            Hits.Clear();
            if (!NetMode.IsAuthority) return Hits;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down; else dir.Normalize();
            float u = GameConfig.TileUnits;
            IReadOnlyList<Monster> all = Monster.All;
            Candidates.Clear();
            for (int i = 0; i < all.Count; i++)
            {
                Monster m = all[i];
                if (m.IsDying) continue;
                Rect b = m.Box;
                if (SkillGeometry.Hits(e.Shape, center.x, center.y, dir.x, dir.y, e.Radius * u, e.AngleDeg, e.Length * u, e.Width * u, b.xMin, b.yMin, b.xMax, b.yMax))
                    Candidates.Add(m);
            }
            int limit = e.Shape == AreaShape.Single ? 1 : e.Pct <= 0f && e.Status.Kind != StatusKind.None ? Mathf.Max(1, e.Count) : int.MaxValue;
            if (Candidates.Count > limit)
            {
                Candidates.Sort((a, c) => (a.Center - center).sqrMagnitude.CompareTo((c.Center - center).sqrMagnitude));
                Candidates.RemoveRange(limit, Candidates.Count - limit);
            }
            ClassPalette prev = ClassPalette.Current;
            ClassPalette.Current = ctx.Sparks ?? ClassPalette.Neutral;
            for (int i = 0; i < Candidates.Count; i++) { HitOne(ctx, e, Candidates[i], center, dir); Hits.Add(Candidates[i]); }
            ClassPalette.Current = prev;
            return Hits;
        }

        /// <summary>One monster takes the effect (projectile impact on its target). Same rules as Area; returns false off the authority or when dying.</summary>
        public static bool Single(SkillDamageCtx ctx, SkillEffect e, Monster target, Vector2 from, Vector2 dir)
        {
            if (!NetMode.IsAuthority || target == null || target.IsDying) return false;
            ClassPalette prev = ClassPalette.Current;
            ClassPalette.Current = ctx.Sparks ?? ClassPalette.Neutral;
            HitOne(ctx, e, target, from, dir);
            ClassPalette.Current = prev;
            return true;
        }

        static void HitOne(SkillDamageCtx ctx, SkillEffect e, Monster m, Vector2 from, Vector2 dir)
        {
            StatusSpec st = e.Status;
            Vector2 away = m.Center - from;
            Vector2 push = away.sqrMagnitude > 0.01f ? away.normalized : dir;
            Vector2 knock = st.Kind == StatusKind.Knockback ? push * (st.Value * GameConfig.TileUnits / KnockbackSeconds) : Vector2.zero;
            if (e.Pct > 0f)
            {
                Vector2 attacker = ctx.Hero != null ? ctx.Hero.Position : from;
                bool behind = e.BehindBonusPct > 0f && SkillGeometry.IsBehind(m.FacingCode, attacker.x - m.Feet.x, attacker.y - m.Feet.y);
                float mul = e.Pct / 100f * ctx.DamageMul * StatusRules.ConditionalMultiplier(e, m.Status.Mask, behind); // bonus reads the status BEFORE this strike adds its own
                bool crit = ctx.CritChance > 0f && Random.value < ctx.CritChance;
                m.TakeHit(CombatMath.Damage(ctx.Atk, mul * (crit ? CritMul : 1f), m.Def), knock * (crit ? 2f : 1f), crit, false, false, ctx.Hero);
            }
            if (m.IsDying) return;
            if (st.Kind == StatusKind.Launch) m.Launch();
            else if (st.Kind != StatusKind.None && st.Kind != StatusKind.Knockback) m.ApplyStatus(st, ctx.Hero);
        }
    }
}
