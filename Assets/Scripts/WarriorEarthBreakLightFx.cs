using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Picture of the Warrior's EARTH BREAK, "Golden Knight Judgment" (RO trailer style, 21/11 evening; concept
    /// docs/reference/warrior-fx-ro-trailer/concept-earth-break-chatgpt.png). The Warrior's ultimate standard: weight and force, not fire. The
    /// timeline and every hit stay in EarthBreakUltimate (slam 650, 8 small hits every 30 ms from it, 930, finisher 1030, burns 1130-1340, end 1400;
    /// 22/11 sped up from slam 1450 / end 2500, user: "gần như 2 s mới rớt kiếm"); this lays the matching shapes once at the cast, relative to the
    /// slam / finisher / burn times it is given. Earth Break palette (#887C5A / #C6B37D / #F3E1A8 / #FFFFFF) + Knight gold:
    ///  - 0-300 at the caster: a gold seal draws itself in under him, a soft glow gathers on the blade, two translucent gold sweeps (200 / 270);
    ///  - 180-650 at the target: a thin golden rune circle on the REAL radius draws itself in (telegraph), a faint gold ground light;
    ///  - 100-650: the giant 3D greatsword (EarthBreakGreatsword3D: procedural blade / fuller / guard / grip / pommel / gem wearing the
    ///    ChatGPT-painted steel-ivory + gold art, pulsing runes, lit by the scene on a 3D map). 22/11 evening (user: "rơi quá nhanh làm không thể
    ///    thấy cây kiếm đẹp"; was 350-650): summoned at 100 out of an ivory flash + gold star high above the target, hovers ~440 ms drifting
    ///    slowly down, turning and settling while the runes gather light, then the last 110 ms a fast plunge with a light streak, the point hits
    ///    the ground EXACTLY at the slam (650 ms, SkillTable unchanged);
    ///  - 650 slam: soft ivory contact flash, a 1.2 H star, a translucent golden light column behind the sword (5 H), a thin shock ring to the
    ///    radius, short earth cracks with gold cores (fade by the end), light dust; small hits: sparkles, a thin ring every third; 930: ring.
    ///    Flash / star / dust kept low round the blade so the planted sword stays readable;
    ///  - 1030 finisher: a 1.4 H white-gold star, thin rings to 1.2 R, 4 low gold-white light columns round the circle, the rune circle flares;
    ///  - 1130-1340 burns: the cracks pulse gold; the sword stays planted and dissolves into golden light over the last ~230 ms (1170-1400).
    /// No fire pillars, fireball, embers, scorch or red. Shake / hit-stop stay in EarthBreakUltimate (local caster only).
    /// 2.5D / 3D maps (22/11): every ground layer lies on the real ground as a circle and the columns / sword stand as camera-facing billboards
    /// (ClericLightFxView25D), so turning the camera no longer bends the skill.
    /// </summary>
    public static class WarriorEarthBreakLightFx
    {
        const float H = WarriorLightLook.H;
        // FX-only sword timing (the slam / damage time stays EarthBreakSlamMs): summoned at SwordInMs, final plunge PlungeMs, dissolve over the last SwordDissolveMs
        const float SwordInMs = 100f, PlungeMs = 110f, SwordDissolveMs = 230f;

        public static void Build(HeroController hero, Vector2 caster, Vector2 center, Vector2 dir, float radius, float slamMs, float smallStepMs, int smalls,
            float penultMs, float finishMs, float[] burnAt, float totalMs)
        {
            if (!WarriorLightLook.Ready || hero == null) return;
            ClericLightLook.Pal p = WarriorLightLook.Earth, gold = WarriorLightLook.Blade;
            float dim = ClericLightFx.DimFor(hero, null), sq = ClericLightFx.Squash, aim = WarriorLightSkills.AngleOf(dir);
            float seal = 2f * radius / WarriorLightLook.SigilBand;

            // Caster: charge seal, blade glow, two forward sweeps
            // 22/11 speed-up: the charge / sweep phase is ~0.45x the old 0-1050 (the sword lands at 650 instead of 1450)
            var me = ClericLightFx.Create("EarthBreakCharge", caster, hero, 500f, dim);
            me.Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, gold), 0f, 300f, 1.6f * H / WarriorLightLook.SigilBand, 1.6f * H / WarriorLightLook.SigilBand, 0.8f, sq, 30f, 4)
                .Reveal(170f).Phases(170f, 210f, 0.5f, 0.7f).Env(0f, 80f);
            WarriorGrandFx.Charge(me, 50f, 200f, new Vector2(0f, 13f) + dir * 7f, 1f * H, gold);
            const float Sq = 0.6f;
            float mid = WarriorLightLook.ParamDeg(aim, Sq), rr = Mathf.Min(radius, 40f) / WarriorLightSkills.Norm(mid, Sq);
            WarriorLightSkills.Slash(me, ArcRibbonFx.Arc(new Vector2(0f, 9f), rr, Sq, mid + 75f, mid - 75f), 200f, 100f, 170f, 0.36f * H, gold, 0.9f, RibbonSort.Over, 1);
            WarriorLightSkills.Slash(me, ArcRibbonFx.Arc(new Vector2(0f, 9f), rr * 0.8f, Sq, mid - 70f, mid + 70f), 270f, 95f, 160f, 0.28f * H, gold, 0.85f, RibbonSort.Over, 0);

            // Target: rune circle + faint ground light, the falling sword
            var fx = ClericLightFx.Create("EarthBreak", center, null, totalMs, dim);
            // sword from SwordInMs (hover ~440 ms, then a PlungeMs plunge to the slam); was 350-650, too fast to see the sword
            float swordH = 2.7f * H, dropFrom = 100f, swordFrom = Mathf.Min(SwordInMs, slamMs - PlungeMs - 120f), dropMs = (slamMs - swordFrom) / 0.37f; // mask fallback: PopCurve first reaches the ground at 37 % of the drop
            float telegraph = slamMs * 0.28f; // rune circle from ~180 (was 400 with the 1450 slam)
            fx.Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, p), telegraph, totalMs, seal, seal, 0.68f, sq, 10f, 4)
                .Reveal(270f).Phases(slamMs - 180f, slamMs + 90f, 0.6f, 0.75f).Env(0f, 240f);
            WarriorGrandFx.Sector(fx, telegraph, 270f, totalMs - telegraph, 260f, Vector2.zero, 90f, 360f, radius, p, 0.12f, 0.3f, false, 0.2f, 2);
            // summon: ivory flash in front of the sword, a gold star behind it, a burst of sparkles, where it appears high above the target
            Vector2 sky = new Vector2(0f, dropFrom + swordH * 0.45f);
            fx.Upright(WarriorLightLook.Flash(WarriorGrandFx.Hot), ClericLightFx.Band.Over, false, swordFrom, swordFrom + 170f, 0.9f * H, 1.7f * H, 0.9f * H, 1.7f * H, 0.6f, sky, 7).Env(0f, 120f)
              .Upright(WarriorLightLook.Spark(p), ClericLightFx.Band.Over, false, swordFrom, swordFrom + 280f, 1f * H, 2f * H, 1f * H, 2f * H, 0.7f, sky, 1).Turn(20f).Env(0f, 180f);
            fx.At(swordFrom, f => MechanicLightBursts.Sparks(f, f.Anchor + sky, f.Own ? 12 : 4, 140f, 90f, 360f, p.Light, 0.3f, 1.8f));
            if (!EarthBreakGreatsword3D.Lay(fx, swordFrom, totalMs, swordH, 8f, dropFrom, slamMs, SwordDissolveMs, gold, PlungeMs)) // planted until it dissolves at the end
                fx.Upright(WarriorLightLook.Glyph(WarriorLightLook.GreatSword, p), ClericLightFx.Band.Over, true, swordFrom, finishMs + 180f, 1.4f * H, 1.4f * H, swordH, swordH, 0.9f, new Vector2(0f, -8f), 2)
                    .Drop(dropFrom, dropMs).Env(70f, 190f);
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Fall(Vector2.zero, dropFrom * 0.72f + swordH + 20f, swordH * 0.6f), slamMs - PlungeMs - 10f, PlungeMs, 120f, 0.3f * H, gold, 0.8f, RibbonSort.Over, 0);

            // Slam (exactly at slamMs = 650)
            WarriorGrandFx.Impact(fx, slamMs, new Vector2(0f, 3f), 1.2f * H, p, 12); // 22/11: was 1.8 H, the star covered the planted blade
            WarriorGrandFx.Column(fx, slamMs, finishMs - slamMs + 180f, new Vector2(0f, -4f), 1.1f * H, 5f * H, gold, ClericLightFx.Band.Over, 1, LayerKind.Main, 0.2f); // r1: 0.5 / 1.5 H washed the slam white
            WarriorGrandFx.Shock(fx, slamMs, 290f, Vector2.zero, radius * 0.3f, radius, p, 0.75f);
            fx.Ground(WarriorLightLook.Line(WarriorLightLook.Crack, p), slamMs, totalMs, radius * 1.3f, radius * 1.4f, 0.85f, sq, 0f, 3).Env(40f, 280f).Kind(LayerKind.Main);
            fx.At(slamMs, f =>
            {
                UltimateFx.Debris(f.Anchor, f.Own ? 14 : 4, radius * 0.5f, 110f);
                UltimateFx.Dust(f.Anchor, f.Own ? 7 : 3, radius * 0.7f); // light dust: the blade stays readable
            });
            for (int i = 0; i < smalls; i++)
            {
                float t = slamMs + i * smallStepMs;
                fx.At(t, f => MechanicLightBursts.Sparks(f, f.Anchor + new Vector2(0f, 8f), 3, 120f, 90f, 360f, p.Light, 0.26f, 1.6f));
                if (i % 3 == 2) WarriorGrandFx.Shock(fx, t, 170f, Vector2.zero, radius * 0.25f, radius * 0.8f, p, 0.5f, 6);
            }
            WarriorGrandFx.Shock(fx, penultMs, 220f, Vector2.zero, radius * 0.5f, radius, p, 0.65f, 6);

            // Finisher (1030)
            WarriorGrandFx.Impact(fx, finishMs, new Vector2(0f, 4f), 1.4f * H, p, 16);
            WarriorGrandFx.Shock(fx, finishMs, 340f, Vector2.zero, radius * 0.4f, radius * 1.2f, p, 0.8f);
            WarriorGrandFx.Shock(fx, finishMs + 35f, 280f, Vector2.zero, radius * 0.2f, radius * 0.9f, gold, 0.6f, 6);
            WarriorGrandFx.Sector(fx, finishMs - 10f, 1f, 300f, 260f, Vector2.zero, 90f, 360f, radius, p, 0.14f, 0.8f, false, 0.3f, 3);
            for (int k = 0; k < 4; k++)
                WarriorGrandFx.Column(fx, finishMs + k * 18f, 370f, WarriorGrandFx.OnGround(k * 90f + 45f + aim, radius * 0.8f), 0.6f * H, 2.2f * H, gold,
                    ClericLightFx.Band.Over, 2, k % 2 == 0 ? LayerKind.Main : LayerKind.Detail, 0.45f);
            WarriorLightSkills.SplitPulses(fx, "warrior.earth-break", finishMs, true, new Vector2(0f, 8f), p, 16f); // the finisher's extra damage numbers
            fx.At(finishMs, f =>
            {
                UltimateFx.Debris(f.Anchor, f.Own ? 16 : 4, radius * 0.6f, 130f);
                UltimateFx.Dust(f.Anchor, f.Own ? 8 : 3, radius * 0.8f);
            });

            // Burns: the cracks pulse gold, a few sparkles
            foreach (float b in burnAt)
            {
                fx.Ground(WarriorLightLook.Line(WarriorLightLook.Crack, gold), b, b + 150f, radius * 1.3f, radius * 1.35f, 0.7f, sq, 0f, 4).Env(30f, 90f).Kind(LayerKind.Glow);
                fx.At(b, f => f.Puff(true, f.Anchor, f.Count(4), radius * 0.5f, 26f, 1.4f, 2.2f, p.Light, 0.5f));
            }
        }
    }
}
