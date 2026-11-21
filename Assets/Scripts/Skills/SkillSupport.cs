using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Buff and Heal effects. Allies are the caster and its party members in the area (PartyAllies).
    /// The authority changes HP / buffs; every side that renders draws the sparkle on each target.
    /// </summary>
    public static class SkillSupport
    {
        const float SparkleTiles = 2.4f;
        static readonly List<HeroController> Targets = new List<HeroController>(8);

        /// <summary>Heroes an Ally effect reaches: allies inside the circle, else the locked ally (plus the caster for a LowestAlly heal), else the caster. Non-ally effects: the caster only.</summary>
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
                    if (h != null && !h.IsDead && PartyAllies.Are(self, h) && (h.Position - c).sqrMagnitude <= r * r) Targets.Add(h);
            }
            else
            {
                HeroController target = ctx.Ally != null ? ctx.Ally : self;
                Targets.Add(target);
                if (ctx.Def.Targeting == SkillTargeting.LowestAlly && self != null && target != self) Targets.Add(self); // LowestAllyHealRule: the caster is healed too
            }
            return Targets;
        }

        public static void Buff(SkillContext ctx, SkillEffect e)
        {
            List<HeroController> to = Collect(ctx, e);
            float value = ctx.Def.BuffAt(e, ctx.Rank);
            if (e.Buff.Kind == BuffKind.Shield) value = value / 100f * ctx.Body.SupportAtk; // shield = % of the CASTER's attack, stored as HP
            for (int i = 0; i < to.Count; i++)
            {
                if (ctx.Authority)
                {
                    byte source = BuffSource.Pack(ctx.Def.Class, ctx.Def.Index);
                    BuffState s = HeroBuffs.For(to[i]);
                    s.Add(e.Buff.Kind, value, e.Buff.Ms, e.Count, source);
                    NetEventBus.AllyBuff(to[i], ctx.Body.Hero, source, s.LeftMs(e.Buff.Kind)); // other clients see the buff's real time (status FX)
                }
                if (ctx.View) Sparkle(ctx, to[i]);
            }
            if (ctx.View) SkillSfx.Play(ctx, SkillSfx.Cast, 0.8f);
        }

        /// <summary>Heals every target for Pct % of the caster's ATK (rank scaled).</summary>
        public static void Heal(SkillContext ctx, SkillEffect e)
        {
            List<HeroController> to = Collect(ctx, e);
            float amount = ctx.Pct(e) / 100f * ctx.Body.SupportAtk;
            for (int i = 0; i < to.Count; i++) HealOne(ctx, to[i], amount);
            if (ctx.View) SkillSfx.Play(ctx, SkillSfx.Impact, 0.8f);
        }

        /// <summary>Heals one hero; the authority changes HP, a replaying client only draws the number and sparkle.</summary>
        public static void HealOne(SkillContext ctx, HeroController h, float amount)
        {
            float healed = ctx.Authority ? new HeroSkillBody(h).Heal(amount) : Mathf.Min(amount, h.MaxHp - h.Hp);
            if (!ctx.View || healed <= 0.5f) return;
            if (ClericLightSkills.Handles(ctx.Def.Id)) HeroFx.HealNumber(h.Position, Mathf.RoundToInt(healed)); // RO3 look: the light FX is the picture, only the number here
            else HeroFx.Heal(h.Position, Mathf.RoundToInt(healed));
            Sparkle(ctx, h);
        }

        /// <summary>The target's sparkle. A choreography draws the caster and its one target itself, so only the other allies get the translucent one.</summary>
        static void Sparkle(SkillContext ctx, HeroController h)
        {
            if (ClericBuffFx.Handles(ctx.Def.Id)) return; // every target wears its own aura
            if (ClericLightSkills.Handles(ctx.Def.Id)) { ClericLightSkills.OnTarget(ctx, h); return; } // the Cleric light FX decides per skill (tick flash, shield burst, ally spark)
            if (!SkillChoreo.DrawsBody(ctx.Def.Class, ctx.Def.Id)) SkillVfx.Play(ctx, SkillVfxSet.Impact, h.Position, 0f, SparkleTiles * ctx.Units);
            else if (h != ctx.Body.Hero && h != ctx.Ally) SkillVfx.PlayOnAlly(ctx, h.Position, SparkleTiles * ctx.Units);
        }
    }
}
