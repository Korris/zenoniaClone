using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Shader-driven picture of the wizard skill Ice Storm, in the Ragnarok Online 3 style "light pillars / meteor shower" (replaces the painted ice-storm sheet when Enabled).
    /// Only the art changes: IceStormUltimate keeps its damage waves, chill, shake, hit-stop and sound, and starts this as a child (so it ends with the storm) only where NetMode.RendersFx.
    ///  - cast 0-650 ms: a light circle opens on the ground over the storm area (ray burst LightBeamScroll polar + ring), dissolving after the last wave;
    ///  - 12 light meteors (LightMeteor: glowing head + white -> ice-blue TrailRenderer), 3 per wave, each landing exactly on its damage wave (LightSkillTiming.LandMs);
    ///  - each landing explodes (LightImpact: ground light 0 -> peak in 0.15 s -> 0, ring, ray burst, light shaft); each wave also pulses a ground light over the whole circle.
    /// Spear spots are IceStormUltimate.SpearImpact (same spiral as the old spears). Toggle: Enabled (default on); `--light-fx off` brings back the painted sheet.
    /// </summary>
    public sealed class IceStormLightFx : MonoBehaviour
    {
        const float SkyRise = 120f, SkySide = -48f, SpearBlast = 13f, CircleGrow = 1.15f, Squash = 0.8f;

        /// <summary>New light FX on (default) or the old painted Ice Storm (`--light-fx off`, or set false at runtime).</summary>
        public static bool Enabled = ReadFlag();

        Vector2 center;
        float radius, ageMs;
        Vector2[] spots;
        LightMeteor[] meteors;
        LightImpact[] impacts, pulses;
        bool[] meteorSpawned, impactSpawned, pulseSpawned;
        MeshRenderer circleRays, circleRing;

        /// <summary>True when the light version should draw this cast (flag on, this process renders FX, shaders present).</summary>
        public static bool Active { get { return Enabled && NetMode.RendersFx && LightSkillAssets.Ready; } }

        public static void Preload() { if (Active) LightSkillAssets.Load(); }

        static bool ReadFlag()
        {
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "--light-fx");
            return !(i >= 0 && i + 1 < a.Length && a[i + 1].Equals("off", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Starts the light picture under the storm object `storm` (destroyed with it), centred on the storm circle.</summary>
        public static void Begin(Transform storm, Vector2 center, float radius)
        {
            var go = new GameObject("IceStormLight");
            go.transform.SetParent(storm, false);
            go.AddComponent<IceStormLightFx>().Init(center, radius);
        }

        void Init(Vector2 c, float r)
        {
            center = c; radius = r;
            int n = LegacySkillNumbers.IceStormSpears, waves = LegacySkillNumbers.IceStormWaves;
            spots = new Vector2[n];
            for (int i = 0; i < n; i++) spots[i] = IceStormUltimate.SpearImpact(center, i);
            meteors = new LightMeteor[n]; impacts = new LightImpact[n]; pulses = new LightImpact[waves];
            meteorSpawned = new bool[n]; impactSpawned = new bool[n]; pulseSpawned = new bool[waves];
            circleRays = LightSkillAssets.Quad(transform, "CircleRays", LightSkillAssets.Burst, false, UltimateFx.GroundFxOrder + 4);
            circleRing = LightSkillAssets.Quad(transform, "CircleRing", LightSkillAssets.Ring, false, UltimateFx.GroundFxOrder + 5);
            circleRays.transform.position = circleRing.transform.position = new Vector3(c.x, c.y, 0f);
            Tick(0f);
        }

        void Update() { ageMs += GameClock.Dt * 1000f; Tick(ageMs); }

        void Tick(float ms)
        {
            TickCircle(ms);
            for (int i = 0; i < spots.Length; i++)
            {
                if (!meteorSpawned[i] && ms >= LightSkillTiming.MeteorStartMs(i))
                {
                    meteorSpawned[i] = true;
                    Vector2 sky = spots[i] + new Vector2(SkySide * (i % 2 == 0 ? 1f : 0.7f), SkyRise); // same lean as the old spears (they fell from the upper left)
                    meteors[i] = new LightMeteor(transform, sky, spots[i], LightSkillTiming.MeteorProgress(i, ms));
                }
                if (meteors[i] != null)
                {
                    meteors[i].Tick(LightSkillTiming.MeteorProgress(i, ms), ms / 1000f);
                    if (meteors[i].Done(ms / 1000f)) { meteors[i].Destroy(); meteors[i] = null; }
                }
                float since = ms - LightSkillTiming.LandMs(i);
                if (!impactSpawned[i] && since >= 0f) { impactSpawned[i] = true; impacts[i] = new LightImpact(transform, spots[i], SpearBlast); }
                if (impacts[i] != null && !impacts[i].Tick(since)) { impacts[i].Destroy(); impacts[i] = null; }
            }
            for (int w = 0; w < pulses.Length; w++)
            {
                float since = ms - LightSkillTiming.WaveMs(w);
                if (!pulseSpawned[w] && since >= 0f) { pulseSpawned[w] = true; pulses[w] = new LightImpact(transform, center, radius, false); }
                if (pulses[w] != null && !pulses[w].Tick(since)) { pulses[w].Destroy(); pulses[w] = null; }
            }
        }

        /// <summary>Ground circle: opens over the cast (0 -> CastMs), holds through the waves, dissolves over the storm's tail.</summary>
        void TickCircle(float ms)
        {
            if (circleRays == null) return;
            float open = Mathf.Clamp01(ms / LegacySkillNumbers.IceStormCastMs), ease = 1f - (1f - open) * (1f - open);
            float lastWave = LightSkillTiming.WaveMs(LegacySkillNumbers.IceStormWaves - 1);
            float tail = Mathf.Clamp01((ms - lastWave - 150f) / (LightSkillTiming.EndMs() - lastWave - 150f));
            float d = radius * 2f * CircleGrow * (0.4f + 0.6f * ease);
            circleRays.transform.localScale = new Vector3(d, d * Squash, 1f);
            circleRing.transform.localScale = new Vector3(d * 1.08f, d * 1.08f * Squash, 1f);
            circleRing.transform.localRotation = Quaternion.identity;
            LightSkillAssets.Set(circleRays, ease * (1f - tail) * 0.85f, tail, 0.55f + 0.3f * ease);
            LightSkillAssets.Set(circleRing, ease * (1f - tail) * 0.7f);
            if (tail >= 1f) { Destroy(circleRays.gameObject); Destroy(circleRing.gameObject); circleRays = circleRing = null; }
        }

        void OnDestroy()
        {
            if (meteors != null) foreach (LightMeteor m in meteors) if (m != null) m.Destroy(); // meteors live under this object anyway; trails stop with it
        }
    }
}
