using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Ice Storm in RO3 Soft Fantasy V1 (2026-11-21, replaces the 19/11 blue HDR "light pillars / meteor shower" version; icon: a cloud raining
    /// ice shards and snowflakes). Only the art: IceStormUltimate keeps its damage waves, chill, shake, hit-stop, screen tint and sound, and
    /// starts this as a child (so it ends with the storm) only where NetMode.RendersFx. Ice palette (ElementLightLook.Ice), timings from
    /// LightSkillTiming (dotnet-tested: every shard lands on its damage wave):
    ///  - 0-650 ms (cast, rule 4 lead-in ~45 %): the frost ring (crystal teeth) draws itself in exactly on the storm radius, snowflake seal inside,
    ///    soft pool, a pale cloud gathering over the circle;
    ///  - 650 / 1050 / 1450 / 1850 ms (waves): 3 ice shards per wave fall from the cloud (ElementMeteor: shard turned along its fall + short trail),
    ///    each landing = small frost ring on the 13-unit shard blast, snowflake pop, flash, a shard left standing that melts (dissolve); the
    ///    whole ring flashes once per damage number of the wave (SkillHitSplit), snow motes;
    ///  - the last wave is the peak: a snowflake burst 2.5 H over the centre, a flash and a frost wave out past the ring (user 21/11: grand);
    ///  - after it everything rests at ~50 % and fades over the storm's tail.
    /// Spear spots are IceStormUltimate.SpearImpact (same spiral as the old spears). `--light-fx off` brings back the painted sheet.
    /// </summary>
    public sealed class IceStormLightFx : MonoBehaviour
    {
        const float SkyRise = 104f, SkySide = -40f, SpearBlast = 13f;
        const string SkillId = "wizard.ice-storm";

        /// <summary>New light FX on (default) or the old painted Ice Storm (`--light-fx off`, or set false at runtime).</summary>
        public static bool Enabled = ReadFlag();

        Vector2 center;
        float radius, ageMs, dim;
        Vector2[] spots;
        ElementMeteor[] shards;
        bool[] shardSpawned, landed;
        ClericLightFx circle;

        /// <summary>True when the light version should draw this cast (flag on, this process renders FX, shaders and masks present).</summary>
        public static bool Active { get { return Enabled && ElementLightLook.Active; } }

        /// <summary>Class spawn: loads every Wizard light FX material ahead of the first cast (all Wizard skills, not only Ice Storm).</summary>
        public static void Preload() { if (Enabled) ElementLightLook.Load(); }

        static bool ReadFlag()
        {
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "--light-fx");
            return !(i >= 0 && i + 1 < a.Length && a[i + 1].Equals("off", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Starts the light picture under the storm object `storm` (destroyed with it), centred on the storm circle; `caster` sets the crowd LOD.</summary>
        public static void Begin(Transform storm, Vector2 center, float radius, HeroController caster)
        {
            var go = new GameObject("IceStormLight");
            go.transform.SetParent(storm, false);
            go.AddComponent<IceStormLightFx>().Init(center, radius, caster);
        }

        void Init(Vector2 c, float r, HeroController caster)
        {
            center = c; radius = r;
            dim = ClericLightFx.DimFor(caster, null);
            int n = LegacySkillNumbers.IceStormSpears;
            spots = new Vector2[n]; shards = new ElementMeteor[n]; shardSpawned = new bool[n]; landed = new bool[n];
            for (int i = 0; i < n; i++) spots[i] = IceStormUltimate.SpearImpact(center, i);
            circle = Circle();
            Tick(0f);
        }

        ClericLightFx Circle()
        {
            ClericLightLook.Pal p = ElementLightLook.Ice;
            float d = 2f * radius, ring = d / 0.955f, cast = LegacySkillNumbers.IceStormCastMs, end = LightSkillTiming.EndMs(), lastWave = LightSkillTiming.WaveMs(LegacySkillNumbers.IceStormWaves - 1);
            var fx = ClericLightFx.Create("IceStorm", center, null, end, dim);
            Material frost = ElementLightLook.M(ElementLightLook.FrostRing, p, ClericLightLook.Role.Arc);
            fx.Ground(frost, 0f, end, ring * 0.85f, ring, 0.9f, ClericLightFx.Squash, 4f, 6).Reveal(cast * 0.8f).Arc(2.2f, 0f, 3).Env(40f, 500f).Phases(cast, cast + 260f, 0.45f, 0.5f)
              .Ground(ElementLightLook.M(ElementLightLook.Snowflake, p, ClericLightLook.Role.Line), 120f, end - 100f, d * 0.22f, d * 0.32f, 0.75f, ClericLightFx.Squash, -10f, 7).Env(200f, 450f).Phases(cast, cast + 260f, 0.45f, 0.5f).Kind(LayerKind.Detail)
              .Ground(ElementLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Fill), 0f, end, d * 0.5f, d * 1.0f, 1f).Env(cast, 500f).Kind(LayerKind.Glow)
              .Upright(ElementLightLook.M(ElementLightLook.Cloud, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, 150f, lastWave + 450f, d * 0.55f, d * 0.8f, d * 0.27f, d * 0.4f, 0.5f, new Vector2(0f, SkyRise - 14f), -6).Env(400f, 450f);
            // Each wave: the whole ring flashes once per damage number (SkillHitSplit), snow motes over the circle
            HitSplit split = SkillHitSplit.For(SkillId);
            int pulses = fx.Own ? Mathf.Clamp(split.Hits, 1, SkillHitSplit.MaxHits) : 1;
            Color mote = ElementLightLook.A(p.Light, 0.9f);
            for (int w = 0; w < LegacySkillNumbers.IceStormWaves; w++)
            {
                float t = LightSkillTiming.WaveMs(w);
                for (int k = 0; k < pulses; k++)
                {
                    float tk = t + split.StartDelayMs + k * split.IntervalMs;
                    fx.Ground(frost, tk, tk + 160f, ring, ring, k == 0 ? 0.6f : 0.4f, ClericLightFx.Squash, 4f, 7).Env(30f, 120f).Kind(LayerKind.Glow);
                }
                fx.At(t + 40f, f => f.Puff(false, center, 5, radius * 0.8f, 10f, 1.6f, 2.4f, mote, 0.7f));
            }
            // Last wave = the storm's peak: a big snowflake burst over the centre, a flash, a frost wave out past the ring
            fx.Upright(ElementLightLook.M(ElementLightLook.Snowflake, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, lastWave, lastWave + 520f, 18f, 50f, 18f, 50f, 0.9f, new Vector2(0f, 14f), 9).Pop(200f).Env(20f, 260f)
              .Upright(ElementLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, lastWave, lastWave + 280f, 14f, 40f, 14f, 40f, 0.85f, new Vector2(0f, 14f), 10)
              .Ground(ElementLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Line), lastWave, lastWave + 420f, d * 0.2f, d / 0.78f * 1.1f, 0.8f, ClericLightFx.Squash, 0f, 8)
              .At(lastWave + 20f, f => f.Ring(true, center + new Vector2(0f, 14f), 18, radius * 1.3f, 2f, 3f, p.Light, 0.5f));
            return fx;
        }

        void Update() { ageMs += GameClock.Dt * 1000f; Tick(ageMs); }

        void Tick(float ms)
        {
            ClericLightLook.Pal p = ElementLightLook.Ice;
            for (int i = 0; i < spots.Length; i++)
            {
                if (!shardSpawned[i] && ms >= LightSkillTiming.MeteorStartMs(i))
                {
                    shardSpawned[i] = true;
                    Vector2 sky = spots[i] + new Vector2(SkySide * (i % 2 == 0 ? 1f : 0.7f), SkyRise);
                    shards[i] = new ElementMeteor(transform, sky, spots[i] + new Vector2(0f, 3f), ElementLightLook.M(ElementLightLook.IceShard, p, ClericLightLook.Role.Glyph), 7f, 21f,
                        ElementLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), 8f, p, dim >= 0.6f ? 3.6f : 0f, 0.18f, dim >= 0.99f ? 1f : 0.7f, true);
                }
                if (shards[i] != null)
                {
                    shards[i].Tick(LightSkillTiming.MeteorProgress(i, ms), ms / 1000f);
                    if (shards[i].Done(ms / 1000f)) { shards[i].Destroy(); shards[i] = null; }
                }
                if (!landed[i] && ms >= LightSkillTiming.LandMs(i)) { landed[i] = true; Land(spots[i]); }
            }
        }

        /// <summary>One shard landing: frost ring on the shard blast, snowflake pop, flash, a shard left standing that melts, snow sparks.</summary>
        void Land(Vector2 at)
        {
            ClericLightLook.Pal p = ElementLightLook.Ice;
            float d = 2f * SpearBlast;
            Vector2 lift = new Vector2(0f, 6f);
            ClericLightFx.Create("IceStormHit", at, null, 620f, dim)
                .Ground(ElementLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Line), 0f, 300f, 6f, d / 0.78f, 0.85f, ClericLightFx.Squash, 0f, 9)
                .Upright(ElementLightLook.M(ElementLightLook.IceShard, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, true, 0f, 600f, 5f, 6.2f, 12f, 16f, 0.9f, new Vector2(0f, -1f), -2).Env(0f, 250f).Dissolve().Kind(LayerKind.Detail)
                .Upright(ElementLightLook.M(ElementLightLook.Snowflake, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, 0f, 300f, 10f, 23f, 10f, 23f, 0.9f, lift, 1).Pop(130f)
                .Upright(ElementLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 0f, 160f, 7f, 19f, 7f, 19f, 0.8f, lift, 2)
                .At(0f, f => f.Ring(true, at + lift, 6, 34f, 1.8f, 2.6f, p.Light, 0.32f));
        }

        void OnDestroy()
        {
            if (shards != null) foreach (ElementMeteor m in shards) if (m != null) m.Destroy();
            if (circle != null && ageMs < LightSkillTiming.EndMs() - 50f) Destroy(circle.gameObject); // the storm ended early (caster fell): its circle goes with it
        }
    }
}
