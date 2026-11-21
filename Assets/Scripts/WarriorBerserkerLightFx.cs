using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Cast burst of the Warrior's BERSERKER AWAKENING (21/11: first "RO3 Soft Fantasy V1" pass, then the GRAND pass: a transformation, not a
    /// status icon; spec + concept in docs/reference/warrior-fx-grand/). A self buff, so it stays round the hero (~2.5 H) and never covers the
    /// face; Rage palette (red-orange, gold, white-hot core, dark rim):
    ///  - 0-180 lead-in (the clenched fists): a war seal draws itself in under the hero, a red glow gathers on the body, motes drawn in;
    ///  - 180 roar (= BerserkerAwakeningFx.RoarMs): a red-white beam up behind him, a thick dark-red pressure wave to 2.6 H, the seal flares,
    ///    6 flame pillars licking up round the body (3 behind, 3 at the front sides), a flash behind the chest, sparks / embers, a short scorch;
    ///  - fades by ~1100 ms. The maintained part is the body ring in BerserkerAwakeningFx (rule 10 exception).
    /// </summary>
    public static class WarriorBerserkerLightFx
    {
        const float H = WarriorLightLook.H;

        public static void Burst(HeroController hero, float roarMs)
        {
            if (!WarriorLightLook.Ready || hero == null) return;
            ClericLightLook.Pal p = WarriorLightLook.Rage;
            float sq = ClericLightFx.Squash, seal = 1.8f * H / WarriorLightLook.SigilBand;
            var fx = ClericLightFx.Create("Berserker", hero.Position, hero, roarMs + 950f, ClericLightFx.DimFor(hero, null));
            fx.Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, p), 0f, roarMs + 800f, seal * 0.92f, seal, 0.95f, sq, -40f, 4)
                .Reveal(roarMs).Phases(roarMs, roarMs + 110f, 0.5f, 0.7f).Env(0f, 240f)
              .Ground(WarriorLightLook.Fill(p), 0f, roarMs + 700f, H * 1f, H * 1.8f, 1f).Env(120f, 280f).Kind(LayerKind.Glow)
              .Upright(WarriorLightLook.Fill(p), ClericLightFx.Band.Behind, false, 0f, roarMs + 80f, H * 0.6f, H * 1.4f, H * 0.8f, H * 1.7f, 1f, new Vector2(0f, 10f), 0).Env(roarMs * 0.9f, 80f)
              .Upright(WarriorLightLook.Glyph(ClericLightLook.Streak, p), ClericLightFx.Band.Behind, true, roarMs, roarMs + 420f, 0.9f * H, 1.3f * H, 2.6f * H, 3.6f * H, 1f, new Vector2(0f, 0f), 1).Pop(120f).Dissolve()
              .Upright(WarriorLightLook.Glyph(ClericLightLook.Streak, WarriorGrandFx.Hot), ClericLightFx.Band.Behind, true, roarMs, roarMs + 300f, 0.4f * H, 0.6f * H, 2.2f * H, 3.2f * H, 1f, new Vector2(0f, 0f), 2).Pop(110f).Dissolve();
            WarriorGrandFx.Shock(fx, roarMs, 380f, Vector2.zero, H * 0.4f, H * 1.25f, p, 0.75f);
            for (int k = 0; k < 3; k++)                                                                    // 3 flame ribbons spiralling up round the body (ChatGPT round 2)
            {
                float a0 = k * 120f;
                WarriorLightSkills.Slash(fx, u => new Vector2(Mathf.Cos((a0 + u * 540f) * Mathf.Deg2Rad) * 0.55f * H, Mathf.Sin((a0 + u * 540f) * Mathf.Deg2Rad) * 0.55f * H * sq + u * 1.4f * H),
                    roarMs + k * 30f, 320f, 200f, 0.22f * H, WarriorGrandFx.Pyre, 1f, RibbonSort.Behind, 3, hero, 6f, true);
            }
            WarriorGrandFx.Impact(fx, roarMs, new Vector2(0f, 10f), 1.3f * H, p, 18, ClericLightFx.Band.Behind);
            WarriorGrandFx.Scorch(fx, roarMs, 700f, Vector2.zero, H * 0.6f, WarriorLightLook.Flame);
            float[] xs = { -6f, 0f, 6f, -11f, 11f, 0f };
            for (int i = 0; i < xs.Length; i++)
            {
                bool back = i < 3;
                float t0 = roarMs + i * 22f;
                WarriorGrandFx.Pillar(fx, t0, 520f, new Vector2(xs[i], back ? 3f : (i == 5 ? -5f : -1f)), (back ? 0.55f : 0.42f) * H, (back ? 1.6f : 1f) * H, p,
                    back ? ClericLightFx.Band.Behind : ClericLightFx.Band.Front, 2, back ? LayerKind.Main : LayerKind.Detail);
            }
            fx.At(0f, f => In(f, 22f, 6, p.Light))
              .At(90f, f => In(f, 16f, 5, p.Light))
              .At(roarMs, f =>
              {
                  WarriorGrandFx.Embers(f, f.Anchor + new Vector2(0f, 6f), 18, 12f, p);
                  UltimateFx.Dust(f.Anchor, f.Own ? 10 : 3, 14f);
              });
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
