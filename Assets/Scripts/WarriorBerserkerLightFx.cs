using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Cast burst of the Warrior's BERSERKER AWAKENING, "RO trailer style" (21/11 evening): a buff, so it stays round the hero and never covers
    /// the face. Ivory-gold aura (RO Berserker = gold aura + only a little coral), 2 pulses, translucent:
    ///  - 0-180 lead-in (the clenched fists): a gold rune ring (~0.9 H radius) draws itself in under the hero, a soft glow gathers on the body;
    ///  - 180 roar (= BerserkerAwakeningFx.RoarMs): a translucent ivory-gold light column behind him (aura 3 H tall), pulse 1 = a thin ring to
    ///    2.5 H + a faint gold ground light, 3 ivory-gold ribbons rising round the body, 2-3 SHORT coral streaks (the only red-ish touch),
    ///    a star behind the chest; pulse 2 (+130 ms) = a second thin ring; fades by ~1000 ms. The maintained part is the body ring in
    ///    BerserkerAwakeningFx (WarriorLightLook.BodyRing, ivory gold with coral highlights).
    /// No flame pillars, scorch or red-orange wave.
    /// </summary>
    public static class WarriorBerserkerLightFx
    {
        const float H = WarriorLightLook.H;

        public static void Burst(HeroController hero, float roarMs)
        {
            if (!WarriorLightLook.Ready || hero == null) return;
            ClericLightLook.Pal p = WarriorLightLook.Rage, coral = WarriorLightLook.Coral;
            float sq = ClericLightFx.Squash, seal = 1.8f * H / WarriorLightLook.SigilBand;
            var fx = ClericLightFx.Create("Berserker", hero.Position, hero, roarMs + 900f, ClericLightFx.DimFor(hero, null));
            fx.Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, p), 0f, roarMs + 760f, seal * 0.92f, seal, 0.8f, sq, -40f, 4)
                .Reveal(roarMs).Phases(roarMs, roarMs + 110f, 0.5f, 0.7f).Env(0f, 240f)
              .Upright(WarriorLightLook.Fill(p), ClericLightFx.Band.Behind, false, 0f, roarMs + 80f, H * 0.6f, H * 1.3f, H * 0.8f, H * 1.6f, 1f, new Vector2(0f, 10f), 0).Env(roarMs * 0.9f, 80f);
            WarriorGrandFx.Sector(fx, roarMs, 1f, 620f, 380f, Vector2.zero, 90f, 360f, 2.5f * H, p, 0.16f, 0.7f, false, 0.3f, 2);
            WarriorGrandFx.Column(fx, roarMs, 520f, new Vector2(0f, -1f), 1.2f * H, 3.2f * H, p, ClericLightFx.Band.Behind, 1, LayerKind.Main, 0.5f);
            WarriorGrandFx.Shock(fx, roarMs, 360f, Vector2.zero, H * 0.4f, H * 2.5f, p, 0.55f);
            WarriorGrandFx.Shock(fx, roarMs + 130f, 380f, Vector2.zero, H * 0.5f, H * 2.2f, p, 0.5f, 6);
            for (int k = 0; k < 3; k++)                                                                    // 3 ivory-gold ribbons rising round the body
            {
                float a0 = k * 120f;
                WarriorLightSkills.Slash(fx, u => new Vector2(Mathf.Cos((a0 + u * 540f) * Mathf.Deg2Rad) * 0.55f * H, Mathf.Sin((a0 + u * 540f) * Mathf.Deg2Rad) * 0.55f * H * sq + u * 1.6f * H),
                    roarMs + k * 30f, 340f, 200f, 0.18f * H, p, 0.9f, RibbonSort.Behind, 3, hero, 6f, true);
            }
            for (int k = 0; k < 3; k++)                                                                    // the only coral: 3 short diagonal streaks
            {
                float sx = k == 1 ? 1f : -1f, y0 = (0.5f + 0.35f * k) * H;
                Vector2 a = new Vector2(sx * 0.75f * H, y0 + 0.35f * H), b = new Vector2(sx * 0.3f * H, y0 - 0.15f * H);
                WarriorLightSkills.Slash(fx, ArcRibbonFx.Line(a, b), roarMs + 40f + k * 45f, 70f, 120f, 0.12f * H, coral, 0.85f, RibbonSort.Over, 4);
            }
            WarriorGrandFx.Impact(fx, roarMs, new Vector2(0f, 10f), 1.2f * H, p, 8, ClericLightFx.Band.Behind);
            fx.At(0f, f => In(f, 22f, 5, p.Light))
              .At(90f, f => In(f, 16f, 4, p.Light))
              .At(roarMs, f => UltimateFx.Dust(f.Anchor, f.Own ? 6 : 2, 14f));
        }

        static void In(ClericLightFx f, float dist, int n, Color c)
        {
            n = f.Count(n);
            for (int i = 0; i < n; i++)
            {
                Vector2 d = WarriorLightSkills.Dir(Random.value * 360f), e = new Vector2(d.x, d.y * ClericLightFx.Squash);
                f.Emit(false, f.Anchor + new Vector2(0f, 8f) + e * dist, -e * dist * 3f, Random.Range(1.5f, 2.3f), c, 0.3f);
            }
        }
    }
}
