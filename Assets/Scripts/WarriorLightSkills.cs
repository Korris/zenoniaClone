using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Warrior skills, "RO trailer style" since 21/11 evening (BIG, translucent, pale Knight gold; docs/reference/ro-trailer-skill-style/): Mesh Trail
    /// sword arcs (ArcRibbonFx) + ground sectors (RoSoftSector) + LightBlob shapes + particles on the Cleric light toolkit (ClericLightFx), no painted frames (the choreography sprites
    /// were removed). Render only: damage, pull, burn and the dash stay in the skill data (SkillRun on the authority). Every impact is timed on the
    /// strike steps of SkillTable (read through SkillSchedule, so the picture cannot drift from the numbers).
    ///  - Power Slash (here): the Bash fan on the ground + 8 thin arcs back and forth, the finisher a brighter second sweep + star;
    ///  - Whirlwind (here): faint full-circle ground light + 2 broken gold / steel air arcs spinning, a thin ring per hit (0 / 250 / 500 ms);
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
        /// Power Slash (8 hits: 90, 135 ... 315, 365, 440 ms; 160 degree arc, 3 tiles), RO trailer style 21/11 (concept
        /// docs/reference/warrior-fx-ro-trailer/concept-power-slash-chatgpt.png): the Knight Bash. Windup 0-90: a soft ivory glow on the blade.
        /// Hit 1: a big pale-gold ground fan (RoSoftSector, body ~0.22, ivory rim exactly on the real 3-tile reach) sweeps open in 70 ms and stays
        /// faintly through the barrage; each hit a thin translucent sword arc inside the fan, back and forth, + a small star; the 7th a bigger star;
        /// the finisher a second, brighter fan sweeping the other way, the widest arc, a 1.5 H white-gold star on the group, a thin shock ring to the
        /// reach, the 60 ms stop. No fire, scorch or embers (Neutral skill).
        /// </summary>
        static void PowerSlash(SkillContext ctx)
        {
            SkillEffect arc = ctx.Def.Effects[0];
            ClericLightLook.Pal p = WarriorLightLook.Blade;
            HeroController hero = ctx.Body.Hero;
            float r = arc.Radius * ctx.Units, aim = AngleOf(ctx.Dir), half = arc.AngleDeg * 0.5f;
            var fin = new List<bool>();
            List<float> hits = StrikeTimes(ctx.Def, fin);
            float first = hits.Count > 0 ? hits[0] : 90f, end = hits.Count > 0 ? hits[hits.Count - 1] : 440f;
            var fx = ClericLightFx.Create("PowerSlash", ctx.Feet, hero, end + 520f, ClericLightFx.DimFor(hero, null));
            Vector2 chest = new Vector2(0f, 9f), blade = chest + ctx.Dir * 7f;
            const float Sq = 0.6f;
            float mid = WarriorLightLook.ParamDeg(aim, Sq);
            WarriorGrandFx.Charge(fx, 0f, Mathf.Max(60f, first), blade, 0.8f * H, p);
            // The dominant shape: the Bash fan on the ground (opens with the first hit, faint through the barrage), then the finisher's brighter sweep
            WarriorGrandFx.Sector(fx, first - 40f, 70f, end - first + 300f, 280f, Vector2.zero, aim, arc.AngleDeg, r, p, 0.36f, 0.75f);
            WarriorGrandFx.Sector(fx, end - 35f, 60f, 330f, 250f, Vector2.zero, aim, arc.AngleDeg, r, p, 0.28f, 0.9f, true, 0.18f, 3);
            for (int i = 0; i < hits.Count; i++)
            {
                bool last = i == hits.Count - 1, penult = i == hits.Count - 2;
                float t = hits[i], rad = last ? 0.92f : penult ? 0.8f : (i % 2 == 0 ? 0.5f : 0.66f);
                float sweep = last ? 80f : 55f, tail = last ? 110f : 130f, w = last ? 0.3f * H : penult ? 0.2f * H : 0.15f * H;
                float a0 = mid + half, a1 = mid - half;
                if (i % 2 == 1) { float s = a0; a0 = a1; a1 = s; }                                   // back and forth across the arc
                Slash(fx, ArcRibbonFx.Arc(chest, r * rad / Norm(mid, Sq), Sq, a0, a1), t - sweep * 0.5f, sweep, tail, w, p, last ? 0.6f : 0.85f, RibbonSort.Over); // V3 review: the late finisher arc -40 %, gone ~100 ms after the hit
                Vector2 spot = chest + Dir(aim + Random.Range(-half, half) * 0.6f) * r * rad * 0.85f;
                if (!last) WarriorGrandFx.Impact(fx, t, spot, (penult ? 0.6f : 0.45f) * H, p, penult ? 5 : 2); // V3 review: barrage stars 0.45-0.6 H
                if (penult) fx.At(t, f => WarriorLightLook.Punch(ctx.Body.IsLocal, 25f, 0.12f));
                SplitPulses(fx, ctx.Def.Id, t, fin[i], last ? chest + ctx.Dir * r * 0.55f : spot, p, last ? 14f : 9f);
            }
            Vector2 hitAt = chest + ctx.Dir * r * 0.55f;
            WarriorGrandFx.Impact(fx, end, hitAt, 1.2f * H, p, 10);
            WarriorGrandFx.Shock(fx, end, 320f, Vector2.zero, r * 0.3f, r, p, 0.55f);
            fx.At(end, f =>
            {
                MechanicLightBursts.Sparks(f, f.Anchor + chest, 10, 140f, aim, half * 2f, p.Light, 0.3f, 1.8f);
                UltimateFx.Dust(f.Anchor + ctx.Dir * r * 0.5f, f.Own ? 6 : 2, r * 0.3f);
                WarriorLightLook.Punch(ctx.Body.IsLocal, 60f, 0.35f, 0.35f, 0.05f);
            });
        }

        /// <summary>
        /// Whirlwind (hits 0 / 250 / 500 ms, radius 2.5 tiles, cast lock 750), RO trailer style 21/11: a faint pale-gold full-circle ground light
        /// with an ivory rim exactly on the radius; TWO broken arcs of air spinning flat round the hero, offset in time (gold 0.82 r, steel 0.62 r,
        /// 160 ms later), hollow centre; each hit a thin steel shock ring to the edge + sparkles; the last a star behind him and the stop / shake.
        /// </summary>
        static void Whirlwind(SkillContext ctx)
        {
            SkillEffect e = ctx.Def.Effects[0];
            ClericLightLook.Pal p = WarriorLightLook.Blade, steel = WarriorLightLook.Steel;
            HeroController hero = ctx.Body.Hero;
            float r = e.Radius * ctx.Units, lockMs = Mathf.Max(600f, ctx.Def.CastLockMs);
            var fin = new List<bool>();
            List<float> hits = StrikeTimes(ctx.Def, fin);
            var fx = ClericLightFx.Create("Whirlwind", ctx.Feet, hero, lockMs + 400f, ClericLightFx.DimFor(hero, null));
            float sq = ClericLightFx.Squash;
            WarriorGrandFx.Sector(fx, 0f, 160f, lockMs + 150f, 260f, Vector2.zero, 90f, 360f, r, p, 0.13f, 0.5f, false, 0.3f, 2);
            float start = Random.value * 360f;
            Slash(fx, ArcRibbonFx.Arc(new Vector2(0f, 4f), r * 0.82f, sq, start, start - 640f), 0f, lockMs - 80f, 230f, 0.3f * H, p, 0.8f, RibbonSort.Split, 0, hero, 4f, true);
            Slash(fx, ArcRibbonFx.Arc(new Vector2(0f, 4f), r * 0.62f, sq, start + 180f, start - 400f), 160f, lockMs - 200f, 200f, 0.22f * H, steel, 0.9f, RibbonSort.Split, 1, hero, 4f, true);
            for (int i = 0; i < hits.Count; i++)
            {
                bool last = i == hits.Count - 1;
                float t = hits[i];
                WarriorGrandFx.Shock(fx, t, last ? 340f : 260f, Vector2.zero, r * 0.45f, r, steel, last ? 0.65f : 0.5f, 6);
                fx.At(t, f =>
                {
                    MechanicLightBursts.Sparks(f, f.Anchor + new Vector2(0f, 6f), last ? 14 : 6, last ? 150f : 110f, 90f, 360f, p.Light, 0.3f, 1.7f);
                    UltimateFx.Dust(f.Anchor, f.Own ? (last ? 8 : 3) : 1, r * 0.6f);
                    WarriorLightLook.Punch(ctx.Body.IsLocal, last ? 50f : 12f, last ? 0.3f : 0.08f, last ? 0.3f : 0f, last ? 0.05f : 0f);
                });
                SplitPulses(fx, ctx.Def.Id, t, fin[i], new Vector2(0f, 8f) + Dir(Random.value * 360f) * r * 0.5f, p, 10f);
            }
            float lastT = hits.Count > 0 ? hits[hits.Count - 1] : 500f;
            WarriorGrandFx.Impact(fx, lastT, new Vector2(0f, 8f), 0.65f * H, p, 4); // V4 review: one small ivory pulse at the final hit
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

        /// <summary>A sword slash (WarriorGrandFx.Slash: soft veil, translucent body, thin ivory edge). Returns the body ribbon.</summary>
        public static ArcRibbonFx Slash(ClericLightFx fx, ArcRibbonFx.PathFn path, float t0, float sweepMs, float tailMs, float width, ClericLightLook.Pal p, float alpha,
            RibbonSort sort = RibbonSort.Over, int order = 0, HeroController around = null, float splitY = 6f, bool linear = false)
        {
            return WarriorGrandFx.Slash(fx, path, t0, sweepMs, tailMs, width, p, alpha, sort, order, around, splitY, linear); // RO trailer style: 3 translucent layers
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
