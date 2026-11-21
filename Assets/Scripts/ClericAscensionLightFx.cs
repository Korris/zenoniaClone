using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Divine Ascension (Cleric ultimate), RO3 Soft Fantasy V1 (spec 4.8 CLERIC_DIVINE_ASCENSION_V1: Prayer of the Holy Spirit + Sacred Lily + Holy
    /// Sigma), replacing the painted choreography. Gold #F3D47D + ivory #FFF9E1, sage #DCEECF as the one accent; 1450 ms.
    /// SkillTable: heal radius 5 tiles at 0 ms, strike radius 5 tiles at 100 ms (cast lock 900 ms), so the spec's beats are pulled forward onto
    /// the strike: 0-100 ms the seal appears under the feet (anticipation ~40 %), 100 ms the burst (holy wave out to the radius, light column
    /// 2.6 H behind the caster, core flash, a flash on every monster hit), 150-550 ms three rune rings open one after the other, 300-1100 ms light
    /// wings behind the caster, 1100-1450 ms everything fades and the motes drift off. The outer ring sits exactly on the 5-tile radius (rule 3:
    /// boundary = gameplay radius, the bright part stays near the centre). Healed allies: ClericLightSkills.AllySpark. Crowd: rule 12 tiers.
    /// </summary>
    public static class ClericAscensionLightFx
    {
        const float Ms = 1450f, StrikeMs = 100f;
        const int MaxMonsterFlashes = 10;

        public static void Cast(SkillContext ctx)
        {
            float rTiles = 5f;
            foreach (SkillEffect e in ctx.Def.Effects) if (e.Kind == EffectKind.Strike) { rTiles = e.Radius; break; }
            ClericLightLook.Pal p = ClericLightLook.Ascend, sage = ClericLightLook.Sage;
            HeroController caster = ctx.Body.Hero;
            float r = rTiles * ctx.Units, d = 2f * r, ring = d / 0.955f;
            Material rune = ClericLightLook.M(ClericLightLook.RuneRing, p, ClericLightLook.Role.Line), arcRing = ClericLightLook.M(ClericLightLook.RuneRing, p, ClericLightLook.Role.Arc);
            var fx = ClericLightFx.Create("Ascension", caster.Position, caster, Ms, ClericLightFx.DimFor(caster, null));

            // Seal under the feet (0-100 ms at ~40 %, then full), the soft pool
            fx.Ground(ClericLightLook.M(ClericLightLook.Compass, p, ClericLightLook.Role.Line), 0f, StrikeMs + 40f, 20f, 34f, 0.45f, ClericLightFx.Squash, 30f, 7)
              .Ground(ClericLightLook.M(ClericLightLook.Compass, p, ClericLightLook.Role.Arc), StrikeMs, Ms, 34f, 46f, 1f, ClericLightFx.Squash, 30f, 7).Arc(3f, 0f, 2).Env(30f, 380f)
              .Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Fill), 0f, 1300f, 30f, d * 0.75f, 1f).Env(120f, 450f).Kind(LayerKind.Glow);

            // The burst at the strike: holy wave out to the radius, core flash, light column behind the caster
            fx.Ground(ClericLightLook.M(ClericLightLook.Band, sage, ClericLightLook.Role.Line), StrikeMs, 620f, 16f, d / 0.78f, 0.8f, ClericLightFx.Squash, 0f, 8)
              .Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Behind, false, StrikeMs, 330f, 12f, 26f, 12f, 26f, 0.8f, new Vector2(0f, 11f), 2)
              .Upright(ClericLightLook.Column(p), ClericLightFx.Band.Behind, true, StrikeMs, 750f, 18f, 13f, 46f, 54f, 0.9f, new Vector2(0f, -1f), -3).Env(60f, 350f);

            // Three rune rings, one after the other: the boundary, a turning middle ring, an inner holy rune
            fx.Ground(arcRing, 150f, 1400f, ring * 0.8f, ring, 0.8f, ClericLightFx.Squash, 0f, 5).Arc(2f, 0f, 3).Env(150f, 400f)
              .Ground(rune, 250f, 1350f, ring * 0.45f, ring * 0.6f, 0.7f, ClericLightFx.Squash, -18f, 6).Env(150f, 400f).Kind(LayerKind.Detail)
              .Ground(ClericLightLook.M(ClericLightLook.Rune, p, ClericLightLook.Role.Line), 350f, 1300f, d * 0.24f, d * 0.34f, 0.75f, ClericLightFx.Squash, 22f, 6).Env(150f, 400f).Kind(LayerKind.Detail);

            // Light wings behind the caster (only in this moment)
            fx.Upright(ClericLightLook.M(ClericLightLook.Wings, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Behind, false, 300f, 1100f, 32f, 52f, 16f, 26f, 0.85f, new Vector2(0f, 18f), -2).Env(160f, 380f);

            // Particles: sparks out at the burst, gold motes rising over the circle, a last drift
            fx.At(StrikeMs, f => f.Ring(true, f.Anchor + new Vector2(0f, 6f), 16, r * 1.1f, 1.8f, 2.8f, p.Light, 0.45f))
              .At(StrikeMs + 50f, f => f.Puff(false, f.Anchor, 12, r * 0.6f, 22f, 1.8f, 2.8f, ClericLightLook.A(p.Main, 0.9f), 0.8f))
              .At(500f, f => f.Puff(false, f.Anchor, 10, r * 0.4f, 18f, 1.6f, 2.4f, ClericLightLook.A(sage.Light, 0.9f), 0.8f))
              .At(900f, f => f.Puff(true, f.Anchor + new Vector2(0f, 10f), 6, 14f, 14f, 1.6f, 2.2f, ClericLightLook.A(p.Core, 0.95f), 0.5f));

            // Monster hit flashes at the strike, own screen punch
            Vector2 centre = ctx.Feet;
            fx.At(StrikeMs, f => HitFlashes(centre, r, f.Dim, p));
            if (ctx.Body.IsLocal) fx.At(StrikeMs, f => { GameClock.HitStop(60f); CameraFollow.SkillShake(0.3f); });
        }

        static void HitFlashes(Vector2 centre, float r, float dim, ClericLightLook.Pal p)
        {
            if (ClericLightFx.TierOf(dim) == FxTier.Far) return;
            int n = 0;
            foreach (Monster m in Monster.All)
            {
                if (m == null || (m.Feet - centre).sqrMagnitude > r * r) continue;
                if (n++ >= MaxMonsterFlashes) break;
                ClericLightFx.Create("AscensionHit", m.Center, null, 280f, dim)
                    .Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 0f, 180f, 5f, 14f, 5f, 14f, 0.75f, Vector2.zero)
                    .Upright(ClericLightLook.M(ClericLightLook.Star, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, 0f, 260f, 8f, 15f, 8f, 15f, 0.9f, Vector2.zero, 1);
            }
        }
    }
}
