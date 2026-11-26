using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Eagle Eye and Hunter's Focus, the Archer's self buffs (SkillTable.Archer.cs, applied at 0 ms: Eagle Eye 10 s atk speed +20 % / atk +10 %,
    /// Hunter's Focus 12 s atk +22 % / atk speed +15 %), RO3 Soft Fantasy V1 (2026-11-26). One emblem per skill (rule 1), compact like every buff
    /// (rule 3: 0.8-1.8 H), translucent, never over the face (old VFX rule 3), peak in the first ~300 ms (rule 4), then the ring round the body
    /// while the buff runs (ArcherBuffAura). Leaf + gold (rule 8), the icons' look:
    ///  - Eagle Eye (icon: hawk eye over gold chevrons): compass seal drawing itself under the feet, a gold hawk eye popping open over the head,
    ///    a lime ring widening round it, two chevrons rising at the sides, flash behind the chest, leaf motes;
    ///  - Hunter's Focus (icon: gold crosshair): a big gold reticle closing in on the hero (behind him, only its frame reads round him), a gold
    ///    ring contracting on the ground onto a lime rune circle, sparks drawn inward, flash behind the chest.
    /// </summary>
    public static class ArcherBuffLightFx
    {
        const float CastMs = 900f;

        public static void Cast(SkillContext ctx)
        {
            HeroController h = ctx.Body.Hero;
            if (h == null) return;
            float dim = ClericLightFx.DimFor(h, h);
            var fx = ClericLightFx.Create(ctx.Def.Index == ArcherLightSkills.EagleEye ? "ArcherEagleEye" : "ArcherHuntersFocus", h.Position, h, CastMs + 100f, dim);
            bool eye = ctx.Def.Index == ArcherLightSkills.EagleEye;
            if (eye) EagleEye(fx); else HuntersFocus(fx);
            SkillEffect buff = ArcherLightSkills.Effect(ctx.Def, EffectKind.Buff);
            if (buff != null)
                ArcherBuffAura.Begin(h, ctx.Def, eye ? ArcherBuffAura.Look.EagleEye : ArcherBuffAura.Look.HuntersFocus, buff.Buff.Kind, buff.Buff.Ms, CastMs - 300f);
        }

        static void EagleEye(ClericLightFx fx)
        {
            ClericLightLook.Pal leaf = ArcherLightLook.Leaf, gold = ArcherLightLook.Gold;
            Vector2 chest = new Vector2(0f, 10f), head = new Vector2(0f, 50f); // over the name + HP bar (~30-40 units up), Sổ lỗi 2026-11-22 Level Up
            fx.Ground(ArcherLightLook.Line(ArcherLightLook.Compass, leaf), 0f, CastMs, 20f, 30f, 0.9f, ClericLightFx.Squash, 30f, 5).Reveal(200f).Env(30f, 320f).Phases(0f, 320f, 1f, 0.5f)
              .Ground(ArcherLightLook.Pool(leaf), 0f, 760f, 14f, 30f, 1f).Kind(LayerKind.Glow)
              .Upright(ArcherLightLook.Flash(leaf), ClericLightFx.Band.Behind, false, 0f, 220f, 6f, 16f, 6f, 16f, 0.7f, chest, 1)
              // the hawk eye over the head: pops open, holds, drifts up a little and fades
              .Upright(ArcherLightLook.Glyph(ArcherLightLook.Eye, gold), ClericLightFx.Band.Over, false, 60f, CastMs, 16f, 25f, 16f, 25f, 0.95f, head, 3).Pop(170f).Env(40f, 300f).Rise(4f)
              .Upright(ArcherLightLook.Flash(gold), ClericLightFx.Band.Over, false, 80f, 300f, 5f, 14f, 5f, 14f, 0.7f, head, 4)
              .Upright(ArcherLightLook.Line(ArcherLightLook.Band, leaf), ClericLightFx.Band.Over, false, 60f, 480f, 12f, 38f, 12f, 38f, 0.8f, head, 2);
            for (int i = 0; i < 2; i++)                                    // the icon's chevrons, rising at the sides
            {
                float side = i == 0 ? -1f : 1f, t0 = 120f + 80f * i;
                fx.Upright(ArcherLightLook.Glyph(ArcherLightLook.Chevron, leaf), ClericLightFx.Band.Over, false, t0, t0 + 520f, 5f, 6.5f, 5f, 6.5f, 0.9f, new Vector2(side * 12f, 6f), 2).Rise(14f).Env(60f, 220f);
            }
            Color mote = ArcherLightLook.Mote, glint = ArcherLightLook.Glint;
            fx.At(80f, f => ClericBlessingLightFx.SideSparks(f, 10, mote, 0.6f))
              .At(160f, f => f.Ring(true, f.Anchor + head, 6, 26f, 1.5f, 2.3f, glint, 0.35f));
        }

        static void HuntersFocus(ClericLightFx fx)
        {
            ClericLightLook.Pal leaf = ArcherLightLook.Leaf, gold = ArcherLightLook.Gold;
            Vector2 chest = new Vector2(0f, 10f);
            fx.Ground(ArcherLightLook.Line(ArcherLightLook.RuneRing, leaf), 0f, CastMs, 22f, 30f, 0.85f, ClericLightFx.Squash, -24f, 5).Reveal(220f).Env(30f, 320f).Phases(0f, 340f, 1f, 0.5f)
              .Ground(ArcherLightLook.Line(ArcherLightLook.Band, gold), 0f, 320f, 64f, 24f, 0.8f, ClericLightFx.Squash, 0f, 6)                  // contracting onto the hero
              .Ground(ArcherLightLook.Line(ArcherLightLook.Reticle, gold), 60f, CastMs - 100f, 30f, 26f, 0.8f, ClericLightFx.Squash, 40f, 7).Env(40f, 300f).Kind(LayerKind.Detail)
              .Ground(ArcherLightLook.Pool(gold), 0f, 700f, 14f, 30f, 1f).Kind(LayerKind.Glow)
              // the reticle closing in: big -> body size over 300 ms, behind the hero so only its frame reads round him
              .Upright(ArcherLightLook.Line(ArcherLightLook.Reticle, gold), ClericLightFx.Band.Behind, false, 0f, CastMs - 60f, 58f, 30f, 58f, 30f, 0.9f, chest, -1).Env(30f, 320f)
              .Upright(ArcherLightLook.Line(ArcherLightLook.Band, leaf), ClericLightFx.Band.Behind, false, 200f, 560f, 18f, 40f, 18f, 40f, 0.75f, chest, -2)
              .Upright(ArcherLightLook.Flash(gold), ClericLightFx.Band.Behind, false, 260f, 480f, 8f, 22f, 8f, 22f, 0.75f, chest, 1)
              .Upright(ArcherLightLook.Glyph(ArcherLightLook.Star, gold), ClericLightFx.Band.Over, false, 280f, 620f, 4f, 9f, 4f, 9f, 0.9f, new Vector2(12f, 22f), 3).Pop(140f);
            Color glint = ArcherLightLook.Glint, spark = ArcherLightLook.Spark;
            fx.At(0f, f =>                                                  // sparks drawn in from a wide circle to the chest
            {
                int n = f.Count(14);
                Vector2 c = f.Anchor + chest;
                for (int i = 0; i < n; i++)
                {
                    float a = i * Mathf.PI * 2f / n;
                    var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f);
                    f.Emit(true, c + d * 30f, -d * 95f, Random.Range(1.5f, 2.3f), i % 2 == 0 ? glint : spark, 0.3f);
                }
            }).At(300f, f => ClericBlessingLightFx.SideSparks(f, 8, glint, 0.55f));
        }
    }
}
