using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Purification, RO3 Soft Fantasy V1 (spec 4.7 CLERIC_PURIFICATION_V1, Ruwach + Lex Divina light), replacing the painted choreography.
    /// The strike (SkillTable: 350 % around the caster, radius 3 tiles, at 0 ms) is skill data. 760 ms, white-teal #E3FAED / #B6E8D8, bad status lilac #A58CBD:
    ///  1. a white-teal rune ring under the feet (1.4 H) + a thin wave running out to the strike radius, 2. a thin Latin cross over the head,
    ///  3. lilac specks (the bad status) leaving the body and 4. breaking apart (LightBlob _Dissolve), 5. a white-teal flash at the end + sparks.
    /// The identity is "the bad status pulled off and dissolved", not another heal ring. Each monster in the radius gets a small white-teal hit flash.
    /// </summary>
    public static class ClericPurifyLightFx
    {
        const float Ms = 760f;
        const int MaxMonsterFlashes = 8;

        public static void Cast(SkillContext ctx)
        {
            SkillEffect strike = null;
            foreach (SkillEffect e in ctx.Def.Effects) if (e.Kind == EffectKind.Strike) { strike = e; break; }
            if (strike == null) return;
            ClericLightLook.Pal p = ClericLightLook.Purify, taint = ClericLightLook.Taint;
            float r = strike.Radius * ctx.Units;
            HeroController caster = ctx.Body.Hero;
            var fx = ClericLightFx.Create("Purification", caster.Position, caster, Ms, ClericLightFx.DimFor(caster, null));
            fx.Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Fill), 0f, 640f, 16f, 30f, 1f).Kind(LayerKind.Glow)
              .Ground(ClericLightLook.M(ClericLightLook.RuneRing, p, ClericLightLook.Role.Arc), 0f, 700f, 18f / 0.955f, 29f / 0.955f, 0.95f, ClericLightFx.Squash, 30f, 5).Arc(-6f).Env(60f, 300f)
              .Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Line), 60f, 520f, 12f, 2f * r / 0.78f, 0.6f, ClericLightFx.Squash, 0f, 4)
              .Upright(ClericLightLook.M(ClericLightLook.Latin, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, 40f, 700f, 6.5f, 8f, 9f, 11f, 0.95f, new Vector2(0f, 30f)).Rise(4f).Env(80f, 260f)
              .Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Behind, false, 470f, 700f, 8f, 20f, 8f, 20f, 0.7f, new Vector2(0f, 10f), 1)
              .At(500f, f => f.Ring(true, f.Anchor + new Vector2(0f, 8f), 6, 34f, 1.6f, 2.4f, p.Light, 0.3f));

            // The bad status: lilac specks start on the body, drift out and up, and crumble (dissolve) on the way
            int specks = fx.Lod == FxTier.Own ? 5 : fx.Lod == FxTier.Near ? 3 : 1;
            Material speck = ClericLightLook.M(ClericLightLook.Mote, taint, ClericLightLook.Role.Glyph);
            for (int i = 0; i < specks; i++)
            {
                float a = (i + 0.5f) / specks * Mathf.PI + Mathf.PI * 0.05f * (i % 2 == 0 ? 1f : -1f);  // fan over the upper half
                Vector2 from = new Vector2(Mathf.Cos(a) * 4f, 6f + Mathf.Sin(a) * 6f), dir = new Vector2(Mathf.Cos(a) * 14f, 6f + Mathf.Sin(a) * 8f);
                float t0 = 90f + i * 35f, s = 4.5f - i % 2;
                fx.Upright(speck, ClericLightFx.Band.Front, false, t0, t0 + 520f, s, s * 0.7f, s, s * 0.7f, 0.9f, from, 2).Move(dir).Dissolve().Env(60f, 0f);
            }

            // Monsters caught in the radius (found now: one killed at 0 ms is already dying, still drawn)
            if (fx.Lod == FxTier.Far) return;
            int n = 0;
            foreach (Monster m in Monster.All)
            {
                if (m == null || (m.Feet - ctx.Feet).sqrMagnitude > r * r) continue;
                if (n++ >= MaxMonsterFlashes) break;
                ClericLightFx.Create("PurifyHit", m.Center, null, 260f, fx.Dim)
                    .Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 0f, 200f, 5f, 13f, 5f, 13f, 0.75f, Vector2.zero)
                    .Upright(ClericLightLook.M(ClericLightLook.Latin, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, 0f, 240f, 5f, 7f, 7f, 10f, 0.8f, Vector2.zero, 1);
            }
        }
    }
}
