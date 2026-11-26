using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Wind Step (SkillTable.Archer.cs: dash 3 tiles in 220 ms from 0 ms, then the next hits +40 % for 5 s), RO3 Soft Fantasy V1 (2026-11-26,
    /// ArcherLightLook Wind / Leaf, no frames):
    ///  - 0 ms at the start point: a wind ring bursting on the ground, gusts and leaves thrown back against the dash, a dust puff dissolving;
    ///  - 0-220 ms on the hero: two soft mesh trails (chest wind, low leaf) drawing the dash path, wind wisps (standing streaks) left behind at
    ///    70 / 140 / 210 ms like afterimages, each with a little dust;
    ///  - 230 ms where the dash ends (the hero's real position: works for remote casters moved by snapshots): landing ring, gusts forward, dust,
    ///    leaves, own small screen shake;
    ///  - then a pale wind ring spinning at the knees while the bonus hits wait (ArcherBuffAura, gone when they are spent or the 5 s run out).
    /// </summary>
    public static class ArcherWindStepLightFx
    {
        public static void Cast(SkillContext ctx)
        {
            HeroController h = ctx.Body.Hero;
            SkillEffect move = ArcherLightSkills.Effect(ctx.Def, EffectKind.Motion);
            float dashMs = move != null ? move.AtMs + move.DurationMs : 220f;
            float dim = ArcherLightLook.Dim(h);
            Vector2 dir = ctx.Dir, from = ctx.Feet;
            float a = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Burst(from, -dir, a + 180f, dim, 1f, "ArcherWindStepStart");
            var trail = ClericLightFx.Create("ArcherWindStepTrail", h.Position, h, dashMs + 120f, dim);
            trail.Upright(ArcherLightLook.Flash(ArcherLightLook.Wind), ClericLightFx.Band.Over, false, 0f, dashMs + 20f, 4f, 4f, 4f, 4f, 0.35f, new Vector2(0f, 11f)).Trail(ArcherLightLook.Wind, 8f, 0.26f)
                 .Upright(ArcherLightLook.Flash(ArcherLightLook.Leaf), ClericLightFx.Band.Over, false, 0f, dashMs + 20f, 3f, 3f, 3f, 3f, 0.25f, new Vector2(0f, 3f)).Trail(ArcherLightLook.Leaf, 4.5f, 0.22f);
            for (int i = 1; i <= 3; i++)
            {
                float t = dashMs * i / 3.3f;
                trail.At(t, f => Wisp(f.Anchor, a, f.Dim));
            }
            trail.At(dashMs + 10f, f => { Burst(f.Anchor, dir, a, f.Dim, 0.85f, "ArcherWindStepLand"); });
            if (ctx.Body.IsLocal) trail.At(dashMs + 10f, f => CameraFollow.SkillShake(0.08f));
            SkillEffect buff = ArcherLightSkills.Effect(ctx.Def, EffectKind.Buff);
            if (buff != null) ArcherBuffAura.Begin(h, ctx.Def, ArcherBuffAura.Look.WindStep, buff.Buff.Kind, buff.Buff.Ms, dashMs + 150f);
        }

        /// <summary>Wind burst at `at`: ground ring, 3 gusts pushing along `push` (angle `pushDeg`), dust puff, leaves, motes (`k` = size).</summary>
        static void Burst(Vector2 at, Vector2 push, float pushDeg, float dim, float k, string name)
        {
            var fx = ClericLightFx.Create(name, at, null, 520f, dim);
            fx.Ground(ArcherLightLook.Line(ArcherLightLook.Band, ArcherLightLook.Wind), 0f, 360f, 8f * k, 40f * k, 0.85f, ClericLightFx.Squash, 0f, 8)
              .Ground(ArcherLightLook.Line(ArcherLightLook.WindArc, ArcherLightLook.Leaf), 0f, 320f, 14f * k, 30f * k, 0.75f, 1f, 0f, 9, push * 6f).Turn(pushDeg).Kind(LayerKind.Detail)
              .Ground(ArcherLightLook.Pool(ArcherLightLook.Wind), 0f, 380f, 14f * k, 30f * k, 1f).Kind(LayerKind.Glow)
              .Upright(ArcherLightLook.DustMat, ClericLightFx.Band.Over, false, 0f, 480f, 10f * k, 22f * k, 7f * k, 14f * k, 0.8f, new Vector2(0f, 4f), -3).Rise(4f).Dissolve();
            for (int i = 0; i < 3; i++)
            {
                float off = (i - 1) * 22f, rad = (pushDeg + off) * Mathf.Deg2Rad, t0 = i * 25f;
                var d = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                fx.Upright(ArcherLightLook.Line(ArcherLightLook.WindArc, ArcherLightLook.Wind), ClericLightFx.Band.Over, false, t0, t0 + 300f, 9f * k, 14f * k, 12f * k, 20f * k, i == 1 ? 0.85f : 0.65f, new Vector2(0f, 9f) + d * 5f, 1 + i)
                  .Turn(pushDeg + off).Move(d * 22f * k).Kind(i == 1 ? LayerKind.Main : LayerKind.Detail);
            }
            for (int i = 0; i < 3; i++)
            {
                float ang = pushDeg + Random.Range(-40f, 40f), rad = ang * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad) + 0.4f);
                fx.Upright(ArcherLightLook.Glyph(ArcherLightLook.LeafMask, ArcherLightLook.Leaf), ClericLightFx.Band.Over, false, 30f, 460f, 3.5f, 4.5f, 3.5f, 4.5f, 0.9f, new Vector2(0f, 6f), 4)
                  .Turn(ang - 90f + Random.Range(-30f, 30f)).Move(d * 20f * k).Kind(i == 0 ? LayerKind.Main : LayerKind.Detail);
            }
            Color mote = ArcherLightLook.Wind.Light;
            fx.At(0f, f =>
            {
                int n = f.Count(8);
                for (int i = 0; i < n; i++)
                    f.Emit(i % 2 == 0, at + new Vector2(0f, 4f), (Vector2)(Quaternion.Euler(0f, 0f, Random.Range(-50f, 50f)) * (push * Random.Range(35f, 60f))) + new Vector2(0f, 8f), Random.Range(1.4f, 2.2f), mote, 0.35f);
            });
        }

        /// <summary>A wind wisp standing where the hero just was (afterimage of the dash): a pale streak + low ring, fading in 280 ms.</summary>
        static void Wisp(Vector2 at, float dashDeg, float dim)
        {
            ClericLightFx.Create("ArcherWindWisp", at, null, 300f, dim)
                .Upright(ArcherLightLook.Glyph(ArcherLightLook.Streak, ArcherLightLook.Wind), ClericLightFx.Band.Over, false, 0f, 280f, 9f, 6f, 22f, 18f, 0.5f, new Vector2(0f, 10f), -4).Rise(3f)
                .Ground(ArcherLightLook.Line(ArcherLightLook.Band, ArcherLightLook.Wind), 0f, 240f, 6f, 18f, 0.55f).Kind(LayerKind.Detail)
                .Upright(ArcherLightLook.DustMat, ClericLightFx.Band.Over, false, 0f, 340f, 6f, 12f, 4f, 8f, 0.6f, new Vector2(0f, 2f), -5).Dissolve().Kind(LayerKind.Detail);
        }
    }
}
