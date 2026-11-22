using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Level-up celebration in the skill style (user 22/11: "cái chữ Level Up làm giống như sprite skill đi, không xài hình nữa"; docs/sprite-rules.md:
    /// RO trailer style, Particle System + shader quads, no painted frames). Gold / ivory (ClericLightLook.Ascend), translucent and grand:
    ///  - lead-in 0-260 ms: the star sigil (3 H) draws itself in under the feet, a soft pool, gold motes drawn in from its rim;
    ///  - burst 260 ms: the sigil flashes and turns, a wave runs out, a tall light column (~5 H) rises behind the hero, a chest flash,
    ///    sparkles fly out; the glowing "LEVEL UP" word (LevelUpWordFx) pops over the head for the local player only;
    ///  - hold 260-1300 ms: two rune rings rise up round the body, sparkles stream up the column, an arc runs round the sigil;
    ///  - fade 1300-1800 ms.
    /// Another player's level-up plays the same light at the crowd tier (ClericLightFx.DimFor), without the word.
    /// Without the light assets (headless, missing textures) the old rings / embers play instead.
    /// </summary>
    public static class LevelUpLightFx
    {
        public const float BurstMs = 260f, EndMs = 1800f;
        const float Sigil = 62f;      // ground sigil diameter, world units (hero H = 20.5: ~3 H)
        const float ColumnH = 104f;   // light column height (~5 H, RO trailer "cột sáng 4,5-6 H")

        public static void Play(HeroController hero, bool own)
        {
            if (hero == null) return;
            if (!ClericLightLook.Drawable) { Fallback(hero.Position); if (own) LevelUpWordFx.Show(hero); return; }
            ClericLightLook.Pal p = ClericLightLook.Ascend;
            var fx = ClericLightFx.Create("LevelUp", hero.Position, hero, EndMs, own ? 1f : ClericLightFx.DimFor(hero, null));
            Material line = ClericLightLook.M(ClericLightLook.RuneRing, p, ClericLightLook.Role.Line);

            // Ground: pool, star sigil drawing in then flashing, arc round it, inner rune ring, wave out at the burst
            ClericBuffReceiveParts.Sigil(fx, ClericLightLook.SigilStar, p, EndMs - 100f, BurstMs, Sigil, 14f);
            fx.Ground(ClericLightLook.M(ClericLightLook.RuneRing, p, ClericLightLook.Role.Arc), BurstMs, EndMs - 150f, Sigil * 1.02f, Sigil * 1.08f, 0.75f, ClericLightFx.Squash, 0f, 8)
              .Arc(3.2f, 0f, 2).Env(90f, 420f);

            // Burst: light column behind the hero (+ a slim brighter core), chest flash
            fx.Upright(ClericLightLook.Column(p), ClericLightFx.Band.Behind, true, BurstMs - 60f, EndMs - 250f, 36f, 28f, ColumnH * 0.55f, ColumnH, 1f, new Vector2(0f, -1f), -3)
                  .Kind(LayerKind.Glow).Env(80f, 480f)
              .Upright(ClericLightLook.Column(p), ClericLightFx.Band.Behind, true, BurstMs, EndMs - 400f, 14f, 10f, ColumnH * 0.7f, ColumnH * 1.08f, 0.9f, new Vector2(0f, -1f), -2)
                  .Env(60f, 420f)
              .Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Behind, false, BurstMs, BurstMs + 260f, 10f, 30f, 10f, 30f, 0.75f, new Vector2(0f, 11f), 1);

            // Hold: two rune rings rising round the body
            fx.Ground(line, BurstMs + 40f, BurstMs + 900f, 24f, 30f, 0.8f, ClericLightFx.Squash, 40f, 9).Rise(34f).Env(80f, 320f).Kind(LayerKind.Detail)
              .Ground(line, BurstMs + 260f, BurstMs + 1100f, 20f, 26f, 0.7f, ClericLightFx.Squash, -46f, 9).Rise(40f).Env(80f, 320f).Kind(LayerKind.Detail);

            // Particles: gather, burst ring, sparkles streaming up the column through the hold
            Color gather = p.Light;
            fx.At(0f, f => ClericBuffReceiveParts.Gather(f, gather, Sigil, 10))
              .At(120f, f => ClericBuffReceiveParts.Gather(f, gather, Sigil * 0.75f, 8))
              .At(BurstMs, f => f.Ring(true, f.Anchor + new Vector2(0f, 8f), 16, 46f, 1.8f, 2.8f, ClericLightLook.A(p.Light, 0.95f), 0.5f))
              .At(BurstMs + 30f, f => f.Puff(false, f.Anchor, 12, Sigil * 0.35f, 26f, 1.8f, 2.8f, ClericLightLook.A(p.Main, 0.9f), 0.9f));
            for (int i = 0; i < 10; i++)
            {
                int k = i;
                fx.At(BurstMs + 60f + k * 90f, f => Stream(f, p, k));
            }
            if (own)
            {
                fx.At(BurstMs, f => { LevelUpWordFx.Show(hero); ScreenFlash.Trigger(new Color(1f, 0.95f, 0.65f, 0.07f), 0.3f); });
            }
        }

        /// <summary>Three sparkles / motes rising up the column, alternating sides, never across the face.</summary>
        static void Stream(ClericLightFx f, ClericLightLook.Pal p, int k)
        {
            int n = f.Count(3);
            for (int j = 0; j < n; j++)
            {
                float side = (k + j) % 2 == 0 ? 1f : -1f;
                Vector2 at = f.Anchor + new Vector2(side * Random.Range(5f, 11f), Random.Range(0f, 10f));
                bool spark = j != 1;
                f.Emit(spark, at, new Vector2(side * Random.Range(-1.5f, 1.5f), Random.Range(40f, 62f)), spark ? Random.Range(1.6f, 2.4f) : Random.Range(2f, 2.8f),
                    spark ? ClericLightLook.A(p.Core, 0.95f) : ClericLightLook.A(p.Main, 0.85f), Random.Range(0.6f, 0.9f));
            }
        }

        /// <summary>The pre-22/11 picture (procedural rings + embers) when the light assets are missing.</summary>
        static void Fallback(Vector2 feet)
        {
            ShockRing.Spawn(feet, 26f, 0.6f, new Color(1f, 0.85f, 0.35f, 0.95f), UltimateFx.GroundFxOrder + 6, 0.72f, 0.25f);
            ShockRing.Spawn(feet, 16f, 0.45f, new Color(1f, 1f, 0.7f, 0.85f), UltimateFx.GroundFxOrder + 7, 0.72f, 0.2f, true);
            UltimateFx.Embers(feet + new Vector2(0f, 6f), 36, 12f, 60f, new Color(1f, 0.9f, 0.45f, 1f));
        }
    }
}
