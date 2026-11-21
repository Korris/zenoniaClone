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
        /// Power Slash (8 hits: 90, 135 ... 315, 365, 440 ms), grand pass 21/11. Windup 0-90: a hot glow gathers on the blade (anticipation). Each
        /// small hit: a crimson crescent (WarriorGrandFx.Slash, 0.3 H) sweeping the 160 degree arc back and forth + a small burst star and sparks;
        /// the 7th: 0.38 H + a bigger star, short stop; the finisher: the widest crescent (0.95 r, 0.45 H), a burst at the target, a faint hollow
        /// shock to the real radius, a thick boundary arc on the ground, a small scorch, sparks fanning out, rocks, the 60 ms stop / shake.
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
            WarriorGrandFx.Charge(fx, 0f, Mathf.Max(60f, hits.Count > 0 ? hits[0] : 90f), blade, 0.9f * H, p); // anticipation on the blade
            for (int i = 0; i < hits.Count; i++)
            {
                bool last = i == hits.Count - 1, penult = i == hits.Count - 2;
                float t = hits[i], rad = last ? 0.95f : penult ? 0.82f : (i % 2 == 0 ? 0.55f : 0.7f);
                float sweep = last ? 90f : 60f, tail = last ? 240f : penult ? 180f : 140f, w = last ? 0.58f * H : penult ? 0.32f * H : 0.24f * H;
                float a0 = mid + half, a1 = mid - half;
                if (i % 2 == 1) { float s = a0; a0 = a1; a1 = s; }                                   // back and forth across the arc
                Slash(fx, ArcRibbonFx.Arc(chest, r * rad / Norm(mid, Sq), Sq, a0, a1), t - sweep * 0.5f, sweep, tail, w, p, 1f, RibbonSort.Over);
                Vector2 spot = chest + Dir(aim + Random.Range(-half, half) * 0.7f) * r * rad * 0.9f;
                if (!last) WarriorGrandFx.Impact(fx, t, spot, (penult ? 1.2f : 0.75f) * H, p, penult ? 10 : 4);
                if (penult) fx.At(t, f => WarriorLightLook.Punch(ctx.Body.IsLocal, 25f, 0.12f));
                SplitPulses(fx, ctx.Def.Id, t, fin[i], last ? chest + ctx.Dir * r * 0.55f : spot, p, last ? 14f : 9f);
            }
            // Finisher: white-hot impact, thick shockwave to the real radius, boundary arc, scorch, sparks fanning out, the stop / shake
            Vector2 hitAt = chest + ctx.Dir * r * 0.55f;
            WarriorGrandFx.Impact(fx, end, hitAt, 1.25f * H, p, 22);
            WarriorGrandFx.Shock(fx, end, 360f, ctx.Dir * r * 0.2f, r * 0.25f, r, p, 0.22f);
            WarriorGrandFx.Scorch(fx, end, 600f, ctx.Dir * r * 0.6f, r * 0.22f, WarriorLightLook.Flame);
            fx.At(end, f =>
            {
                float gm = WarriorLightLook.ParamDeg(aim, ClericLightFx.Squash);
                Slash(f, ArcRibbonFx.Arc(Vector2.zero, r, ClericLightFx.Squash, gm + half, gm - half), end, 70f, 320f, 0.25f * H, p, 0.95f, RibbonSort.Ground, 3);
                MechanicLightBursts.Sparks(f, f.Anchor + chest, 20, 150f, aim, half * 2f, p.Light, 0.35f, 2f);
                UltimateFx.Dust(f.Anchor + ctx.Dir * r * 0.5f, f.Own ? 12 : 3, r * 0.35f);
                UltimateFx.Debris(f.Anchor + ctx.Dir * r * 0.5f, f.Own ? 14 : 3, r * 0.3f, 90f);
                WarriorLightLook.Punch(ctx.Body.IsLocal, 60f, 0.4f, 0.4f, 0.09f);
            });
        }

        /// <summary>
        /// Whirlwind (hits 0 / 250 / 500 ms, radius 2.5 tiles, cast lock 750), grand pass 21/11: a faint boundary ring exactly at the radius with
        /// bright arcs running round; TWO opposing thick crimson crescent blades spinning flat round the hero (far half behind him, near half in
        /// front), 2 turns in 750 ms; each hit a hollow shock to the edge + sparks flung out; the last one a hit flash behind him, a scorch and
        /// the stop / shake.
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
            fx.Ground(WarriorLightLook.M(WarriorLightLook.Shock, p, ClericLightLook.Role.Arc), 0f, lockMs + 200f, 2f * r / WarriorLightLook.ShockBand * 0.92f, 2f * r / WarriorLightLook.ShockBand, 0.3f, sq, 0f, 4).Env(80f, 250f).Arc(-9f, 0f, 2);
            float start = Random.value * 360f;
            for (int k = 0; k < 2; k++)                                                                  // two opposing thick crescent blades (ChatGPT round 1 / concept)
            {
                float rr = r * (0.62f + 0.2f * k), a0 = start + k * 180f;
                Slash(fx, ArcRibbonFx.Arc(new Vector2(0f, 4f), rr, sq, a0, a0 - 720f), k * 30f, lockMs - 60f, 190f, (0.42f + 0.04f * k) * H, p, 1f, RibbonSort.Split, 0, hero, 4f, true);
            }
            for (int i = 0; i < hits.Count; i++)
            {
                bool last = i == hits.Count - 1;
                float t = hits[i];
                WarriorGrandFx.Shock(fx, t, last ? 360f : 260f, Vector2.zero, r * 0.45f, r * (last ? 1.1f : 1f), p, last ? 0.5f : 0.4f, 6);
                fx.At(t, f =>
                {
                    MechanicLightBursts.Sparks(f, f.Anchor + new Vector2(0f, 6f), last ? 28 : 12, last ? 170f : 120f, 90f, 360f, p.Light, 0.34f, 1.9f);
                    UltimateFx.Dust(f.Anchor, f.Own ? (last ? 14 : 6) : 2, r * 0.6f);
                    if (last) UltimateFx.Debris(f.Anchor, f.Own ? 16 : 4, r * 0.5f, 100f);
                    WarriorLightLook.Punch(ctx.Body.IsLocal, last ? 55f : 15f, last ? 0.38f : 0.1f, last ? 0.35f : 0f, last ? 0.08f : 0f);
                });
                SplitPulses(fx, ctx.Def.Id, t, fin[i], new Vector2(0f, 8f) + Dir(Random.value * 360f) * r * 0.5f, p, 10f);
            }
            float lastT = hits.Count > 0 ? hits[hits.Count - 1] : 500f;
            WarriorGrandFx.Impact(fx, lastT, new Vector2(0f, 8f), 2.2f * H, p, 0, ClericLightFx.Band.Behind);
            WarriorGrandFx.Scorch(fx, lastT, 800f, Vector2.zero, r * 0.45f, WarriorLightLook.Flame);
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

        /// <summary>A sword slash (WarriorGrandFx.Slash: glow, dark rim, opaque body, white-hot core). Returns the body ribbon.</summary>
        public static ArcRibbonFx Slash(ClericLightFx fx, ArcRibbonFx.PathFn path, float t0, float sweepMs, float tailMs, float width, ClericLightLook.Pal p, float alpha,
            RibbonSort sort = RibbonSort.Over, int order = 0, HeroController around = null, float splitY = 6f, bool linear = false)
        {
            return WarriorGrandFx.Slash(fx, path, t0, sweepMs, tailMs, width, p, alpha, sort, order, around, splitY, linear); // grand pass: 4 layers
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
