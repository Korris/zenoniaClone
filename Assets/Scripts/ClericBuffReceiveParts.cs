using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Pieces shared by the Spirit Blessing (+ATK) and Divine Aegis (+DEF) receive bursts (ClericBuffReceiveFx), RO3 Soft Fantasy V1 "strong"
    /// buffs: 5-6 layers within ~1.8 H, four phases (lead-in 25-45 %, burst 100 %, hold ~60 %, fade). World units: hero H = 20.5.
    /// Crowd LOD: soft light = Glow, extra runes / rays / arrows = Detail (ClericLightFxLod).
    /// </summary>
    public static class ClericBuffReceiveParts
    {
        /// <summary>Emblem centre over the feet: name tag ~30, so the badge (13 units) and its arrows stay clear of it.</summary>
        public const float EmblemY = 46f;

        /// <summary>Ground: soft pool, the sigil drawing itself in (lead-in) then flashing at `burst` and turning, an inner rune ring turning
        /// the other way (detail), a wave running out to the sigil's edge at the burst.</summary>
        public static void Sigil(ClericLightFx fx, string mask, ClericLightLook.Pal p, float endMs, float burst, float d, float spin)
        {
            fx.Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Fill), 0f, endMs - 120f, d * 0.45f, d * 0.85f, 1f).Kind(LayerKind.Glow).Env(150f, 300f)
              .Ground(ClericLightLook.M(mask, p, ClericLightLook.Role.Line), 0f, endMs, d * 0.92f, d, 0.95f, ClericLightFx.Squash, spin, 5)
                  .Reveal(burst).Env(60f, 280f).Phases(burst, burst + 200f, 0.42f, 0.62f)
              .Ground(ClericLightLook.M(ClericLightLook.RuneRing, p, ClericLightLook.Role.Line), burst - 60f, endMs - 60f, d * 0.42f, d * 0.55f, 0.7f, ClericLightFx.Squash, -spin * 1.6f, 6)
                  .Kind(LayerKind.Detail).Env(90f, 250f)
              .Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Line), burst, burst + 320f, d * 0.4f, d * 1.02f, 0.7f, ClericLightFx.Squash, 0f, 7).Kind(LayerKind.Glow);
        }

        /// <summary>Short soft light column behind the hero (~1.8 H, not a tall 3D pillar), up for the burst and the hold.</summary>
        public static void Column(ClericLightFx fx, ClericLightLook.Pal p, float burst)
        {
            fx.Upright(ClericLightLook.Column(p), ClericLightFx.Band.Behind, true, burst - 50f, burst + 460f, 15f, 11f, 26f, 37f, 0.85f, new Vector2(0f, -1f), -3)
              .Kind(LayerKind.Glow).Env(70f, 260f);
        }

        /// <summary>Four thin light rays rising behind / beside the body from the lead-in into the burst (concept 1).</summary>
        public static void Rays(ClericLightFx fx, ClericLightLook.Pal p, float t0)
        {
            float[] xs = { -9f, 3.5f, -3.5f, 9f };
            Material m = ClericLightLook.M(ClericLightLook.Streak, p, ClericLightLook.Role.Glyph);
            for (int i = 0; i < xs.Length; i++)
            {
                float s = t0 + i * 45f, h = i % 3 == 0 ? 18f : 24f;
                fx.Upright(m, ClericLightFx.Band.Behind, true, s, s + 420f, 2.6f, 2f, h * 0.55f, h, 0.8f, new Vector2(xs[i], 1f), 2).Rise(9f).Kind(LayerKind.Detail);
            }
        }

        /// <summary>Badge popping in over the head at `t0` with a light flash behind it (`flashMask`) and two up-arrows rising beside it.</summary>
        public static void Emblem(ClericLightFx fx, string mask, string flashMask, ClericLightLook.Pal p, float t0, float endMs)
        {
            Vector2 at = new Vector2(0f, EmblemY);
            fx.Upright(ClericLightLook.M(flashMask, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, t0, t0 + 280f, 9f, 24f, 9f, 24f, 0.6f, at, 1)
              .Upright(ClericLightLook.M(mask, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, t0, endMs, 6f, 13f, 6f, 13f, 0.97f, at, 3)
                  .Pop(230f).Rise(3f).Env(30f, 230f);
            Material arrow = ClericLightLook.M(ClericLightLook.Arrow, p, ClericLightLook.Role.Glyph);
            for (int i = 0; i < 2; i++)
            {
                float s = t0 + 90f + i * 70f, x = i == 0 ? -9.5f : 9.5f;
                fx.Upright(arrow, ClericLightFx.Band.Over, false, s, endMs - 40f, 3f, 3.6f, 3.8f, 4.6f, 0.92f, at + new Vector2(x, -6f), 4)
                  .Rise(9f).Kind(LayerKind.Detail).Env(60f, 200f);
            }
        }

        /// <summary>Lead-in: `n` motes drawn from the sigil's rim towards the feet and up (light gathering before the burst).</summary>
        public static void Gather(ClericLightFx f, Color c, float d, int n)
        {
            n = f.Count(n);
            float a0 = Random.value * Mathf.PI * 2f;
            for (int i = 0; i < n; i++)
            {
                float a = a0 + i * Mathf.PI * 2f / n;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * ClericLightFx.Squash);
                f.Emit(i % 2 == 0, f.Anchor + dir * d * 0.48f, -dir * d * 1.3f + new Vector2(0f, 14f), Random.Range(1.5f, 2.2f), ClericLightLook.A(c, 0.95f), 0.32f);
            }
        }

        /// <summary>Burst: sparkles flying out from the chest and motes rising round the body through the hold.</summary>
        public static void Burst(ClericLightFx f, ClericLightLook.Pal p, int sparkles, int motes)
        {
            f.Ring(true, f.Anchor + new Vector2(0f, 9f), sparkles, 30f, 1.7f, 2.6f, ClericLightLook.A(p.Light, 0.95f), 0.45f);
            f.Puff(false, f.Anchor + new Vector2(0f, 3f), motes, 11f, 22f, 1.6f, 2.4f, ClericLightLook.A(p.Main, 0.9f), 0.7f);
        }
    }
}
