using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Buff and Heal effects. Allies are every hero of the zone in the area until a party system exists (user decision 2026-10-30).
    /// The authority changes HP / buffs; every side that renders draws the sparkle on each target.
    /// </summary>
    public static class SkillSupport
    {
        static readonly List<HeroController> Targets = new List<HeroController>(8);

        /// <summary>Heroes an Ally effect reaches: heroes inside the circle, else the locked ally, else the caster. Non-ally effects: the caster only.</summary>
        public static List<HeroController> Collect(SkillContext ctx, SkillEffect e)
        {
            Targets.Clear();
            HeroController self = ctx.Body.Hero;
            if (!e.Ally) { Targets.Add(self); return Targets; }
            if (e.Shape == AreaShape.Circle && e.Radius > 0f)
            {
                Vector2 c = e.Range > 0f ? SkillAim.ClampToWalls(ctx.Feet, ctx.Feet + ctx.Dir * e.Range * ctx.Units) : ctx.Feet;
                float r = e.Radius * ctx.Units;
                foreach (HeroController h in SkillAim.Heroes(ctx.Body))
                    if (h != null && !h.IsDead && (h.Position - c).sqrMagnitude <= r * r) Targets.Add(h);
            }
            else Targets.Add(ctx.Ally != null ? ctx.Ally : self);
            return Targets;
        }

        public static void Buff(SkillContext ctx, SkillEffect e)
        {
            List<HeroController> to = Collect(ctx, e);
            float value = ctx.Def.BuffAt(e, ctx.Rank);
            if (e.Buff.Kind == BuffKind.Shield) value = value / 100f * ctx.Body.Atk; // shield = % of the CASTER's attack, stored as HP
            for (int i = 0; i < to.Count; i++)
            {
                if (ctx.Authority) HeroBuffs.For(to[i]).Add(e.Buff.Kind, value, e.Buff.Ms, e.Count);
                if (ctx.View) SkillVfx.Play(ctx, SkillVfxSet.Impact, to[i].Position, 0f, 2.4f * ctx.Units);
            }
            if (ctx.View) SkillSfx.Play(ctx, SkillSfx.Cast, 0.8f);
        }

        /// <summary>Heals every target for Pct % of the caster's ATK (rank scaled).</summary>
        public static void Heal(SkillContext ctx, SkillEffect e)
        {
            List<HeroController> to = Collect(ctx, e);
            float amount = ctx.Pct(e) / 100f * ctx.Body.Atk;
            for (int i = 0; i < to.Count; i++) HealOne(ctx, to[i], amount);
            if (ctx.View) SkillSfx.Play(ctx, SkillSfx.Impact, 0.8f);
        }

        /// <summary>Heals one hero; the authority changes HP, a replaying client only draws the number and sparkle.</summary>
        public static void HealOne(SkillContext ctx, HeroController h, float amount)
        {
            float healed = ctx.Authority ? new HeroSkillBody(h).Heal(amount) : Mathf.Min(amount, h.MaxHp - h.Hp);
            if (ctx.View && healed > 0.5f) { HeroFx.Heal(h.Position, Mathf.RoundToInt(healed)); SkillVfx.Play(ctx, SkillVfxSet.Impact, h.Position, 0f, 2.4f * ctx.Units); }
        }
    }
}
