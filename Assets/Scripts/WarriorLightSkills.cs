using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Warrior skills in "RO3 Soft Fantasy V1" (2026-11-21, user: "làm lại hết skill của warrior ... đổi qua kiểu mới"): Mesh Trail sword arcs
    /// (ArcRibbonFx) + LightBlob shapes + particles on the Cleric light toolkit (ClericLightFx), no painted frames (the choreography sprites
    /// were removed). Render only: damage, pull, burn and the dash stay in the skill data (SkillRun on the authority). Every impact is timed on the
    /// strike steps of SkillTable (read through SkillSchedule, so the picture cannot drift from the numbers).
    ///  - Power Slash (here): 8-hit barrage in a 160 degree, 3-tile arc: one ribbon per hit alternating direction, the finisher widest + boundary arc;
    ///  - Whirlwind (here): 3 spinning ribbons round the hero on a boundary ring of the 2.5-tile radius, a pulse per hit (0 / 250 / 500 ms);
    ///  - Flame Cleave, Charging Strike: WarriorLightStrikes. Earth Break, Blade Cyclone, Inferno Crash, Berserker Awakening keep their own
    ///    timeline classes, which now draw with WarriorEarthBreakLightFx / WarriorCycloneLightFx / WarriorInfernoLightFx / WarriorBerserkerLightFx.
    /// Shake / hit-stop / punch only for the local caster (sprite-rules VFX 12). Multi-hit numbers (SkillHitSplit, 21/11): a hit shown as N damage
    /// numbers gets N-1 small extra pulses (spark + 3 sparks) on the same beat as the numbers (SplitPulses).
    /// </summary>
    public static class WarriorLightSkills
    {
        public const string PowerSlashId = "warrior.power-slash", WhirlwindId = "warrior.whirlwind", FlameCleaveId = "warrior.flame-cleave", ChargingId = "warrior.charging-strike";
        const float H = WarriorLightLook.H;

        public static bool Handles(string skillId)
        {
            return (skillId == PowerSlashId || skillId == WhirlwindId || skillId == FlameCleaveId || skillId == ChargingId) && WarriorLightLook.Ready;
        }

        public static void Cast(SkillContext ctx)
        {
            if (!ctx.View || !Handles(ctx.Def.Id)) return;
            switch (ctx.Def.Id)
            {
                case PowerSlashId: PowerSlash(ctx); break;
                case WhirlwindId: Whirlwind(ctx); break;
                case FlameCleaveId: WarriorLightStrikes.FlameCleave(ctx); break;
                case ChargingId: WarriorLightStrikes.ChargingStrike(ctx); break;
            }
        }

        /// <summary>Times (ms) of every Strike step of the skill, in order (SkillSchedule = what SkillRun fires); `finishers` (optional) gets each
        /// step's Finisher flag (the closing blow, which SkillHitSplit may split into more numbers).</summary>
        public static List<float> StrikeTimes(SkillDef def, List<bool> finishers = null)
        {
            var t = new List<float>();
            foreach (SkillStep s in SkillSchedule.Build(def.Effects))
            {
                SkillEffect e = def.Effects[s.EffectIndex];
                if (e.Kind != EffectKind.Strike) continue;
                t.Add(s.AtMs);
                if (finishers != null) finishers.Add(e.Finisher);
            }
            return t;
        }

        /// <summary>
        /// Extra beats of a hit that SkillHitSplit shows as several damage numbers: for parts 2..N (the first is the impact itself) a small spark
        /// at `at` (offset from the host anchor) + 3 sparks, at the numbers' own times (t + StartDelayMs + i x IntervalMs).
        /// </summary>
        public static void SplitPulses(ClericLightFx fx, string skillId, float t, bool finisher, Vector2 at, ClericLightLook.Pal p, float size)
        {
            HitSplit split = SkillHitSplit.For(skillId);
            int parts = split.PartsFor(finisher);
            for (int i = 1; i < parts; i++)
            {
                float ti = t + split.StartDelayMs + i * split.IntervalMs;
                Vector2 jitter = Random.insideUnitCircle * size * 0.35f;
                fx.Upright(WarriorLightLook.Spark(p), ClericLightFx.Band.Over, false, ti, ti + 130f, size * 0.5f, size, size * 0.5f, size, 0.8f, at + jitter, 3).Kind(LayerKind.Detail);
                fx.At(ti, f => f.Ring(true, f.Anchor + at + jitter, 3, 32f, 1.3f, 2f, p.Light, 0.22f));
            }
        }

        public static float AngleOf(Vector2 dir) { return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; }

        /// <summary>
        /// Power Slash (8 hits: 90, 135 ... 315, 365, 440 ms). Windup 0-90: a glint at the blade + motes gathered toward the hero (the first hit
        /// pulls). Each small hit: a coral ribbon sweeping the 160 degree arc (alternating left / right, 0.5-0.75 of the 3-tile radius, 60 ms sweep,
        /// 120 ms tail) + 2 sparks; the 7th: wider ribbon, short stop; the finisher: the widest ribbon (0.95 r, 0.22 H, 220 ms tail), a thin boundary
        /// arc on the ground at the real radius, a 4-point flash and 10 sparks fanning out, the stop / shake / punch. Over in ~720 ms.
        /// </summary>
        static void PowerSlash(SkillContext ctx)
        {
            SkillEffect arc = ctx.Def.Effects[0];
            ClericLightLook.Pal p = WarriorLightLook.Blade;
            HeroController hero = ctx.Body.Hero;
            float r = arc.Radius * ctx.Units, aim = AngleOf(ctx.Dir), half = arc.AngleDeg * 0.5f;
            var fin = new List<bool>();
            List<float> hits = StrikeTimes(ctx.Def, fin);
            float end = hits.Count > 0 ? hits[hits.Count - 1] : 440f;
            var fx = ClericLightFx.Create("PowerSlash", ctx.Feet, hero, end + 520f, ClericLightFx.DimFor(hero, null));
            Vector2 chest = new Vector2(0f, 9f), blade = chest + ctx.Dir * 7f;
            const float Sq = 0.6f;
            float mid = WarriorLightLook.ParamDeg(aim, Sq);
            fx.Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, 0f, 140f, 3f, 9f, 3f, 9f, 0.75f, blade);
            fx.At(0f, f => Gather(f, ctx.Dir, r * 0.8f, p.Light, 6));
            for (int i = 0; i < hits.Count; i++)
            {
                bool last = i == hits.Count - 1, penult = i == hits.Count - 2;
                float t = hits[i], rad = last ? 0.95f : penult ? 0.82f : (i % 2 == 0 ? 0.55f : 0.7f);
                float sweep = last ? 90f : 60f, tail = last ? 230f : penult ? 170f : 130f, w = last ? 0.28f * H : penult ? 0.23f * H : 0.18f * H;
                float a0 = mid + half, a1 = mid - half;
                if (i % 2 == 1) { float s = a0; a0 = a1; a1 = s; }                                   // back and forth across the arc
                Slash(fx, ArcRibbonFx.Arc(chest, r * rad / Norm(mid, Sq), Sq, a0, a1), t - sweep * 0.5f, sweep, tail, w, p, last ? 1f : 0.9f, RibbonSort.Over);
                Vector2 spot = chest + Dir(aim + Random.Range(-half, half) * 0.7f) * r * rad * 0.9f;
                if (!last) fx.At(t, f => f.Ring(true, f.Anchor + spot, 2, 26f, 1.4f, 2.2f, p.Light, 0.22f));
                if (penult) fx.At(t, f => WarriorLightLook.Punch(ctx.Body.IsLocal, 15f, 0.06f));
                SplitPulses(fx, ctx.Def.Id, t, fin[i], last ? chest + ctx.Dir * r * 0.55f : spot, p, last ? 11f : 7f);
            }
            // Finisher: boundary arc at the real radius on the ground, flash, sparks fanning out
            fx.At(end, f =>
            {
                ArcRibbonFx.Add(f, ArcRibbonFx.Arc(Vector2.zero, r, ClericLightFx.Squash, WarriorLightLook.ParamDeg(aim, ClericLightFx.Squash) + half,
                    WarriorLightLook.ParamDeg(aim, ClericLightFx.Squash) - half), end, 70f, 300f, 2.6f, p, 0.85f, RibbonSort.Ground, 3);
                for (int k = 0; k < f.Count(16); k++)
                {
                    Vector2 d = Dir(aim + Random.Range(-half, half));
                    f.Emit(true, f.Anchor + chest + d * r * 0.35f, d * Random.Range(40f, 70f), Random.Range(1.6f, 2.6f), p.Light, Random.Range(0.25f, 0.4f));
                }
                UltimateFx.Dust(f.Anchor + ctx.Dir * r * 0.5f, f.Own ? 8 : 3, r * 0.3f);
                UltimateFx.Debris(f.Anchor + ctx.Dir * r * 0.5f, f.Own ? 6 : 2, r * 0.25f, 70f);
                WarriorLightLook.Punch(ctx.Body.IsLocal, 30f, 0.2f, 0.3f, 0.05f);
            });
            fx.Upright(WarriorLightLook.Spark(p), ClericLightFx.Band.Over, false, end, end + 220f, 12f, 26f, 12f, 26f, 0.95f, chest + ctx.Dir * r * 0.55f, 3)
              .Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, end, end + 180f, 10f, 28f, 8f, 22f, 0.75f, chest + ctx.Dir * r * 0.55f, 2)
              .Ground(WarriorLightLook.Wave(p), end, end + 320f, r * 0.3f, 2f * r / WarriorLightLook.BandRing, 0.75f, ClericLightFx.Squash, 0f, 6);                 // shockwave to the real radius
        }

        /// <summary>
        /// Whirlwind (hits 0 / 250 / 500 ms, radius 2.5 tiles, cast lock 750): boundary ring on the ground exactly at the radius with a bright
        /// arc running round (alpha 0.75, fades after 750), a soft pool; 3 coral ribbons spinning round the hero at 0.55 / 0.72 / 0.88 r (far half
        /// behind him, near half in front), 2 turns in 750 ms; each hit a wave ring running out to the edge + sparks flung out; the last one
        /// bigger with the stop / shake.
        /// </summary>
        static void Whirlwind(SkillContext ctx)
        {
            SkillEffect e = ctx.Def.Effects[0];
            ClericLightLook.Pal p = WarriorLightLook.Blade;
            HeroController hero = ctx.Body.Hero;
            float r = e.Radius * ctx.Units, lockMs = Mathf.Max(600f, ctx.Def.CastLockMs);
            var fin = new List<bool>();
            List<float> hits = StrikeTimes(ctx.Def, fin);
            var fx = ClericLightFx.Create("Whirlwind", ctx.Feet, hero, lockMs + 400f, ClericLightFx.DimFor(hero, null));
            float sq = ClericLightFx.Squash;
            fx.Ground(WarriorLightLook.Fill(p), 0f, lockMs + 200f, r * 1.2f, r * 1.8f, 0.9f).Env(80f, 250f).Kind(LayerKind.Glow)
              .Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Arc), 0f, lockMs + 200f, 2f * r / WarriorLightLook.BandRing * 0.9f, 2f * r / WarriorLightLook.BandRing, 0.75f, sq, 0f, 4).Env(80f, 250f).Arc(-9f, 0f, 2);
            float start = Random.value * 360f;
            for (int k = 0; k < 3; k++)
            {
                float rr = r * (0.55f + 0.165f * k), a0 = start + k * 120f;
                Slash(fx, ArcRibbonFx.Arc(new Vector2(0f, 4f), rr, sq, a0, a0 - 720f), k * 40f, lockMs - 60f, 150f, (0.16f + 0.03f * k) * H, p, 0.88f, RibbonSort.Split, 0, hero, 4f, true);
            }
            for (int i = 0; i < hits.Count; i++)
            {
                bool last = i == hits.Count - 1;
                float t = hits[i];
                fx.Ground(WarriorLightLook.Wave(p), t, t + (last ? 300f : 220f), r * 0.6f, 2f * r / WarriorLightLook.BandRing * (last ? 1.1f : 1f), last ? 0.85f : 0.65f, sq, 0f, 6).Kind(last ? LayerKind.Main : LayerKind.Detail);
                fx.At(t, f =>
                {
                    f.Ring(true, f.Anchor + new Vector2(0f, 6f), last ? 18 : 8, last ? 90f : 60f, 1.6f, 2.8f, p.Light, 0.34f);
                    UltimateFx.Dust(f.Anchor, f.Own ? (last ? 12 : 5) : 2, r * 0.6f);
                    if (last) UltimateFx.Debris(f.Anchor, f.Own ? 10 : 3, r * 0.5f, 90f);
                    if (last) WarriorLightLook.Punch(ctx.Body.IsLocal, 35f, 0.25f, 0.3f, 0.05f);
                });
                SplitPulses(fx, ctx.Def.Id, t, fin[i], new Vector2(0f, 8f) + Dir(Random.value * 360f) * r * 0.5f, p, 8f);
            }
            float lastT = hits.Count > 0 ? hits[hits.Count - 1] : 500f;
            fx.Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Behind, false, lastT, lastT + 200f, 14f, 32f, 10f, 22f, 0.7f, new Vector2(0f, 8f), 1);
        }

        /// <summary>`n` motes drifting from the arc ahead toward the hero (the gather of the first hit).</summary>
        public static void Gather(ClericLightFx f, Vector2 dir, float dist, Color c, int n)
        {
            n = f.Count(n);
            for (int i = 0; i < n; i++)
            {
                Vector2 d = Dir(AngleOf(dir) + Random.Range(-70f, 70f));
                Vector2 from = f.Anchor + new Vector2(0f, 8f) + d * dist * Random.Range(0.6f, 1f);
                f.Emit(false, from, -d * dist * 3f, Random.Range(1.6f, 2.4f), c, 0.25f);
            }
        }

        /// <summary>
        /// A sword slash: the bright main ribbon (WarriorLightLook.TrailBright) over a wide soft underlay (1.9x wider, ~35 %, the plain trail), so the
        /// cut reads thick and strong (user 21/11: "big and powerful") while the body stays one clean ribbon. Returns the main ribbon.
        /// </summary>
        public static ArcRibbonFx Slash(ClericLightFx fx, ArcRibbonFx.PathFn path, float t0, float sweepMs, float tailMs, float width, ClericLightLook.Pal p, float alpha,
            RibbonSort sort = RibbonSort.Over, int order = 0, HeroController around = null, float splitY = 6f, bool linear = false)
        {
            ArcRibbonFx under = ArcRibbonFx.Add(fx, path, t0, sweepMs, tailMs * 1.15f, width * 1.9f, p, alpha * 0.35f, sort, order - 1, around, splitY);
            ArcRibbonFx main = ArcRibbonFx.Add(fx, path, t0, sweepMs, tailMs, width, p, alpha, sort, order, around, splitY, WarriorLightLook.TrailBright(p));
            if (linear) { under.Linear(); main.Linear(); }
            return main;
        }

        public static Vector2 Dir(float deg) { float a = deg * Mathf.Deg2Rad; return new Vector2(Mathf.Cos(a), Mathf.Sin(a)); }

        /// <summary>Length of the unit ellipse point at parameter `paramDeg` (squash on y): dividing a reach by it puts the arc's middle at that reach.</summary>
        public static float Norm(float paramDeg, float squash)
        {
            float a = paramDeg * Mathf.Deg2Rad;
            return Mathf.Max(0.35f, new Vector2(Mathf.Cos(a), squash * Mathf.Sin(a)).magnitude);
        }
    }
}
