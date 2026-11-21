using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Cast burst of the Warrior's BERSERKER AWAKENING in "RO3 Soft Fantasy V1" (2026-11-21; replaces the painted fx/berserker burst frames). A strong
    /// buff (rule 1: 5-6 layers, within ~1.8 H, four clear phases), Rage palette (gold-orange: rule 8 ATK buff gold, pushed warm for the class):
    ///  - 0-180 lead-in (the clenched fists): a war seal draws itself in under the hero at ~40 %, motes drawn in;
    ///  - 180 roar (= BerserkerAwakeningFx.RoarMs): the seal flares to 100 % then holds ~55 %, a wave to 1.8 H, a soft flash behind the chest,
    ///    4 flame tongues licking up round the body (2 behind, 2 at the front sides, never over the face), 2 arrows rising at the sides, sparks;
    ///  - fades by ~900 ms. The maintained part is the body ring in BerserkerAwakeningFx (rule 10 exception).
    /// </summary>
    public static class WarriorBerserkerLightFx
    {
        const float H = WarriorLightLook.H;

        public static void Burst(HeroController hero, float roarMs)
        {
            if (!WarriorLightLook.Ready || hero == null) return;
            ClericLightLook.Pal p = WarriorLightLook.Rage;
            float sq = ClericLightFx.Squash, seal = 1.6f * H / WarriorLightLook.SigilBand;
            Material tongue = WarriorLightLook.Glyph(WarriorLightLook.FlameTongue, p), arrow = ClericLightLook.M(ClericLightLook.Arrow, p, ClericLightLook.Role.Glyph);
            var fx = ClericLightFx.Create("Berserker", hero.Position, hero, roarMs + 760f, ClericLightFx.DimFor(hero, null));
            fx.Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, p), 0f, roarMs + 700f, seal * 0.92f, seal, 0.8f, sq, -40f, 4)
                .Reveal(roarMs).Phases(roarMs, roarMs + 110f, 0.4f, 0.55f).Env(0f, 220f)
              .Ground(WarriorLightLook.Fill(p), 0f, roarMs + 600f, H * 0.9f, H * 1.6f, 0.85f).Env(120f, 260f).Kind(LayerKind.Glow)
              .Ground(WarriorLightLook.Wave(p), roarMs, roarMs + 280f, H * 0.5f, 1.8f * H / WarriorLightLook.BandRing, 0.8f, sq, 0f, 6)
              .Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Behind, false, roarMs, roarMs + 170f, 8f, 18f, 8f, 18f, 0.8f, new Vector2(0f, 10f), 1);
            float[] xs = { -5f, 5f, -9.5f, 9.5f };
            for (int i = 0; i < xs.Length; i++)
            {
                bool back = i < 2;
                float t0 = roarMs + i * 25f;
                fx.Upright(tongue, back ? ClericLightFx.Band.Behind : ClericLightFx.Band.Front, true, t0, t0 + 460f, 4f, 6.5f, 6f, back ? 22f : 15f, 0.85f, new Vector2(xs[i], back ? 2f : -1f), 2)
                  .Pop(150f).Dissolve().Kind(back ? LayerKind.Main : LayerKind.Detail);
            }
            for (int s = -1; s <= 1; s += 2)
                fx.Upright(arrow, ClericLightFx.Band.Over, false, roarMs + 40f, roarMs + 520f, 5f, 6f, 5f, 6f, 0.85f, new Vector2(s * 13f, 9f), 3).Pop(120f).Rise(10f);
            fx.At(0f, f => In(f, 20f, 4, p.Light))
              .At(90f, f => In(f, 16f, 3, p.Light))
              .At(roarMs, f =>
              {
                  f.Ring(true, f.Anchor + new Vector2(0f, 6f), 10, 70f, 1.6f, 2.6f, p.Light, 0.35f);
                  f.Puff(false, f.Anchor + new Vector2(0f, 2f), 6, 9f, 30f, 1.3f, 2f, p.Main, 0.7f);
                  UltimateFx.Dust(f.Anchor, f.Own ? 8 : 3, 12f);
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
