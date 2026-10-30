using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Lingering area (Blade Cyclone follows the caster, Sanctuary stays on the ground). Pulses `Hits` times, `IntervalMs` apart, for DurationMs:
    /// monsters in the circle take a strike, or (Ally zones) heroes in it are healed. At most 4 per caster.
    /// </summary>
    public sealed class SkillGroundZone : MonoBehaviour
    {
        SkillContext ctx;
        SkillEffect fx;
        Vector2 center;
        float ageMs;
        int pulses;
        SkillFxAnim body;
        SpriteRenderer ring;

        public static void Spawn(SkillContext ctx, SkillEffect e)
        {
            var go = new GameObject("SkillZone");
            go.transform.SetParent(ctx.Body.Hero.transform.parent, false);
            SkillGroundZone z = go.AddComponent<SkillGroundZone>();
            z.ctx = ctx; z.fx = e; z.center = ctx.Feet;
            SkillCaps.Add(ctx.Body.Hero, SkillCaps.MaxZones, z);
            if (!ctx.View) return;
            SkillSfx.Play(ctx, SkillSfx.Cast);
            float width = 2f * e.Radius * ctx.Units;
            z.body = SkillVfx.Loop(ctx, z.center, 0f, width, e.DurationMs / 1000f);
            if (z.body != null) return;
            z.ring = ProceduralSprites.Child(z.transform, "Ring", FxSprites.Ring()); // no painted loop yet: pulsing tinted ring on the ground
            z.ring.color = e.Ally ? new Color(0.9f, 0.95f, 0.5f, 0.8f) : SkillVfx.Tint(ctx);
            z.ring.sortingOrder = UltimateFx.GroundFxOrder;
            z.ring.transform.localScale = new Vector3(width, width * 0.72f, 1f); // sprite is 1 unit wide
        }

        void Update()
        {
            float dtMs = GameClock.Dt * 1000f;
            if (dtMs <= 0f) return;
            ageMs += dtMs;
            if (ageMs >= fx.DurationMs || ctx.Body.IsDead && fx.Attached) { if (body != null) body.Stop(); Destroy(gameObject); return; }
            if (fx.Attached) center = ctx.Body.Feet;
            while (pulses < fx.Hits && ageMs >= pulses * fx.IntervalMs) { Pulse(); pulses++; }
            if (body != null) body.MoveTo(center, 0f);
            if (ring != null) { ring.transform.position = center; ring.color = new Color(ring.color.r, ring.color.g, ring.color.b, 0.55f + 0.25f * Mathf.Sin(ageMs * 0.012f)); }
        }

        void Pulse()
        {
            if (fx.Ally) { HealPulse(); return; }
            SkillEffect e = SkillEffects.Derive(fx, AreaShape.Circle, fx.Radius);
            if (ctx.Authority) SkillDamage.Area(ctx.Dmg, e, center, ctx.Dir);
            if (ctx.View) SkillSfx.Play(ctx, SkillSfx.Impact, 0.5f);
        }

        void HealPulse()
        {
            float r = fx.Radius * ctx.Units, amount = ctx.Pct(fx) / 100f * ctx.Body.Atk;
            foreach (HeroController h in SkillAim.Heroes(ctx.Body))
                if (h != null && !h.IsDead && (h.Position - center).sqrMagnitude <= r * r) SkillSupport.HealOne(ctx, h, amount);
        }
    }
}
