using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Sanctuary, RO3 Soft Fantasy V1 (spec 4.5 CLERIC_SANCTUARY_V1, Sanctuary + Magnus Exorcismus geometry), replacing the painted choreography.
    /// The zone (SkillTable: radius 3 tiles, 8 pulses every 1000 ms from 0, 8000 ms) stays SkillGroundZone's; this draws on the ground only, at
    /// the cast point: outer white-gold rune ring exactly on the radius, a turning inner rune ring, a floor of square tiles, a green-ivory pool
    /// (12-18 %), a few healing motes rising. The ring opens in 450 ms at full strength, then everything rests at ~25 % (rule 10) and only flashes
    /// with each heal pulse (later pulses 40 %, 150-260 ms); each healed hero gets a small flash at their feet (Tick, from SkillSupport).
    /// Fades over the last 300 ms. Nothing upright, nothing over the heroes standing in it.
    /// </summary>
    public static class ClericSanctuaryLightFx
    {
        const float OpenMs = 450f, FadeOutMs = 300f;

        public static void Cast(SkillContext ctx)
        {
            SkillEffect zone = null;
            foreach (SkillEffect e in ctx.Def.Effects) if (e.Kind == EffectKind.Zone) { zone = e; break; }
            if (zone == null) return;
            ClericLightLook.Pal p = ClericLightLook.Sanct;
            float r = zone.Radius * ctx.Units, d = 2f * r, total = zone.DurationMs, ring = d / 0.955f;
            Material outer = ClericLightLook.M(ClericLightLook.RuneRing, p, ClericLightLook.Role.Arc), inner = ClericLightLook.M(ClericLightLook.RuneRing, p, ClericLightLook.Role.Line),
                grid = ClericLightLook.M(ClericLightLook.Grid, p, ClericLightLook.Role.Line), pool = ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Fill);
            var fx = ClericLightFx.Create("Sanctuary", ctx.Ground ?? ctx.Feet, null, total, // Ground skill (26/11): the zone sits on the chosen point (SkillGroundZone)
                ClericLightFx.DimFor(ctx.Body.Hero, null));

            // Opening (activation 85-100 %): ring grows to the radius, floor and inner ring come up, the pool spreads
            fx.Ground(outer, 0f, 900f, ring * 0.55f, ring, 1f, ClericLightFx.Squash, 0f, 6).Arc(3f, 0f, 2).Env(60f, 450f)
              .Ground(grid, 120f, 1000f, d * 0.75f, d * 0.92f, 0.85f, ClericLightFx.Squash, 0f, 4).Env(150f, 500f)
              .Ground(inner, 150f, 1000f, ring * 0.55f, ring * 0.7f, 0.8f, ClericLightFx.Squash, -14f, 5).Env(150f, 500f).Kind(LayerKind.Detail)
              .Ground(pool, 0f, 1100f, d * 0.6f, d * 1.25f, 1f).Env(OpenMs, 500f).Kind(LayerKind.Glow);

            // Resting state for the zone's life: ~25 % of the peak
            fx.Ground(outer, 500f, total, ring, ring, 0.32f, ClericLightFx.Squash, 0f, 6).Arc(1.2f, 0f, 2).Env(300f, FadeOutMs)
              .Ground(grid, 600f, total, d * 0.92f, d * 0.92f, 0.22f, ClericLightFx.Squash, 0f, 4).Env(300f, FadeOutMs)
              .Ground(inner, 600f, total, ring * 0.7f, ring * 0.7f, 0.22f, ClericLightFx.Squash, -14f, 5).Env(300f, FadeOutMs).Kind(LayerKind.Detail)
              .Ground(pool, 600f, total, d * 1.25f, d * 1.25f, 0.55f).Env(300f, FadeOutMs).Kind(LayerKind.Glow);

            // Heal pulses 2..8 (the first is the opening): the floor and the ring flash 40 % for a moment
            for (int n = 1; n < zone.Hits; n++)
            {
                float t = n * zone.IntervalMs;
                if (t >= total - 100f) break;
                fx.Ground(grid, t, t + 260f, d * 0.9f, d * 0.94f, 0.4f, ClericLightFx.Squash, 0f, 4).Env(40f, 200f)
                  .Ground(outer, t, t + 220f, ring, ring, 0.35f, ClericLightFx.Squash, 0f, 6).Env(40f, 170f).Kind(LayerKind.Glow)
                  .At(t, f => f.Puff(false, f.Anchor, 3, r * 0.7f, 14f, 1.6f, 2.4f, ClericLightLook.A(p.Light, 0.85f), 0.7f));
            }
            Color mote = Color.Lerp(ClericLightLook.Heal.Main, p.Core, 0.4f);
            fx.At(120f, f => f.Puff(false, f.Anchor, 10, r * 0.8f, 16f, 1.6f, 2.6f, ClericLightLook.A(mote, 0.9f), 0.8f))
              .At(240f, f => f.Puff(true, f.Anchor, 4, r * 0.6f, 12f, 1.6f, 2.2f, ClericLightLook.A(p.Light, 0.95f), 0.6f));
        }

        /// <summary>A hero healed by a pulse: a small green ring + soft flash at their feet, 260 ms (no extra circle per hero).</summary>
        public static void Tick(HeroController h, HeroController caster)
        {
            ClericLightLook.Pal p = ClericLightLook.Heal;
            ClericLightFx.Create("SanctuaryTick", h.Position, h, 280f, ClericLightFx.DimFor(caster, h))
                .Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Line), 0f, 260f, 8f, 20f, 0.75f)
                .Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Behind, false, 0f, 150f, 4f, 8f, 4f, 8f, 0.5f, new Vector2(0f, 8f))
                .At(0f, f => f.Puff(false, f.Anchor + new Vector2(0f, 3f), 2, 5f, 18f, 1.5f, 2.2f, ClericLightLook.A(p.Light, 0.9f), 0.4f));
        }
    }
}
