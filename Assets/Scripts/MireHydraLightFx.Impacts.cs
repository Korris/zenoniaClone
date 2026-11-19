using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The impact half of MireHydraLightFx (t = ms after the strike instant; WantedSkillLightTiming holds every curve):
    ///  0 Triple Venom Breath (cone): three venom streams (purple sheet + moss core, LightBeamScroll lying on the ground) grow from the boss to the
    ///    cone's rim over the last StreamGrowMs, a bubble spray runs along them, a purple ground light fills the cone;
    ///  1 Poison Bloom / 3 Queen's Toxic Reign (circles on heroes): a poison geyser (upright LightBeamScroll, y-sorted), a moss shock ring, a
    ///    ground light and a burst of bubbles; Toxic Reign first drops a glowing venom drop from the sky that lands exactly at t = 0;
    ///  2 Serpent Tail Crush (circle round the boss): a moss shockwave ring runs out to the rim (ShockwaveMs), a purple ray burst rides it,
    ///    the ground light flashes and a ring of bubbles splashes outward.
    /// </summary>
    public sealed partial class MireHydraLightFx
    {
        const float StreamSpreadDeg = 38f, DropHeight = 90f;
        MeshRenderer[] streams;
        MeshRenderer glow, ring, burst, column, columnCore, drop;
        bool burstEmitted;

        void TickImpact(float t)
        {
            int id = skill.Id;
            if (skill.DrainPoisoned) { TickDrain(t); return; } // 4 Venom Drain: tethers + heal flash (MireHydraLightFx.Drain.cs)
            if (id == 3 && t < 0f) TickDrop(t);
            if (id == 0 && t >= -WantedSkillLightTiming.StreamGrowMs) TickStreams(t);
            if (t < 0f) return;
            if (drop != null) { Destroy(drop.gameObject); drop = null; }
            float g = WantedSkillLightTiming.Glow(id, t);
            TickGroundGlow(t, g);
            if (id == 2) TickShockwave(t, g);
            if (id == 1 || id == 3) TickGeyser(t, g);
            if (!burstEmitted) { burstEmitted = true; EmitBurst(); }
        }

        MeshRenderer Ground(string name, Material m, int above) { return LightSkillAssets.Quad(transform, name, m, false, UltimateFx.GroundFxOrder + above); }

        void TickStreams(float t)
        {
            if (streams == null)
            {
                streams = new MeshRenderer[6];
                for (int i = 0; i < 3; i++)
                {
                    float a = dirDeg + (i - 1) * StreamSpreadDeg * (skill.AngleDeg / 120f);
                    streams[i * 2] = LightSkillAssets.Quad(transform, "Stream", MireHydraFxLook.StreamOuter, true, UltimateFx.GroundFxOrder + 3);
                    streams[i * 2 + 1] = LightSkillAssets.Quad(transform, "StreamCore", MireHydraFxLook.StreamCore, true, UltimateFx.GroundFxOrder + 4);
                    streams[i * 2].transform.localRotation = streams[i * 2 + 1].transform.localRotation = Quaternion.Euler(0f, 0f, a - 90f); // quad "up" -> the stream's angle
                }
            }
            float len = radius * WantedSkillLightTiming.StreamLength(t);
            float end = WantedSkillLightTiming.ImpactEndMs(0);
            float fade = 1f - Smooth(end * 0.5f, end, t);
            float w = radius * 0.42f * (1f + 0.15f * Mathf.Clamp01(t / 300f));
            for (int i = 0; i < streams.Length; i++)
            {
                if (streams[i] == null) continue;
                bool core = (i & 1) == 1;
                streams[i].transform.localScale = new Vector3(core ? w * 0.42f : w, Mathf.Max(0.01f, len), 1f);
                LightSkillAssets.Set(streams[i], fade, Smooth(end * 0.45f, end, t));
                if (fade <= 0f) { Destroy(streams[i].gameObject); streams[i] = null; }
            }
            if (t < 0f || t > end * 0.6f) return;
            emitDebt += 90f * GameClock.Dt;                       // a spray of bubbles carried along the streams
            for (; emitDebt >= 1f; emitDebt -= 1f)
            {
                float a = (dirDeg + Random.Range(-skill.AngleDeg * 0.45f, skill.AngleDeg * 0.45f)) * Mathf.Deg2Rad;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                WantedFxParticles.Emit(bubbles, origin + d * radius * Random.Range(0.1f, 0.3f), d * radius * Random.Range(0.9f, 1.3f),
                    Random.Range(2f, 3.6f), WantedFxParticles.VenomColor(), Random.Range(0.55f, 0.8f));
            }
        }

        void TickGroundGlow(float t, float g)
        {
            if (glow == null)
            {
                glow = Ground("Glow", MireHydraFxLook.GroundGlow, 2);
                bool cone = skill.Shape == WantedAreaShape.Cone;
                float s = radius * (cone ? 1.5f : 2.5f);
                glow.transform.localScale = new Vector3(s, s, 1f);
                if (cone) { float a = dirDeg * Mathf.Deg2Rad; glow.transform.localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius * 0.55f; }
            }
            LightSkillAssets.Set(glow, g);
        }

        void TickShockwave(float t, float g)
        {
            if (ring == null) { ring = Ground("Shockwave", MireHydraFxLook.Ring, 5); burst = Ground("Burst", MireHydraFxLook.Burst, 4); burst.transform.localScale = new Vector3(radius * 2.1f, radius * 2.1f, 1f); }
            float k = WantedSkillLightTiming.Shockwave(t), end = WantedSkillLightTiming.ImpactEndMs(2);
            float d = radius * 2f * (0.25f + 0.85f * k);                               // burst-ring band sits at 78 % of the quad: rim reached a little after k = 1
            ring.transform.localScale = new Vector3(d, d, 1f);
            LightSkillAssets.Set(ring, 1f - Smooth(0.35f, 0.9f, t / end));
            LightSkillAssets.Set(burst, Mathf.Max(g, 0f) * 0.9f, Smooth(0.25f, 0.85f, t / end), 0.15f + 0.8f * k);
        }

        void TickGeyser(float t, float g)
        {
            float end = WantedSkillLightTiming.ImpactEndMs(skill.Id);
            if (column == null)
            {
                int order = GameConfig.OrderBase(origin.y) + 2;                         // y-sorted like a prop: a hero standing in front stays on top
                column = LightSkillAssets.Quad(transform, "Geyser", MireHydraFxLook.Column, true, order);
                columnCore = LightSkillAssets.Quad(transform, "GeyserCore", MireHydraFxLook.ColumnCore, true, order + 1);
                ring = Ground("Ring", MireHydraFxLook.Ring, 5);
            }
            float rise = Mathf.Min(1f, t / 140f), fall = 1f - Smooth(0.35f, 1f, t / end);
            float h = radius * 2.2f * (0.4f + 0.6f * rise), w = radius * 1.3f * (1f - 0.35f * Smooth(0.3f, 1f, t / end));
            column.transform.localScale = new Vector3(w, h, 1f);
            columnCore.transform.localScale = new Vector3(w * 0.3f, h * 0.92f, 1f);
            LightSkillAssets.Set(column, fall * 0.9f, Smooth(0.4f, 1f, t / end));
            LightSkillAssets.Set(columnCore, fall, Smooth(0.45f, 1f, t / end));
            float rs = radius * 2f * (0.45f + 0.7f * WantedSkillLightTiming.Shockwave(t));
            ring.transform.localScale = new Vector3(rs, rs, 1f);
            LightSkillAssets.Set(ring, 1f - Smooth(0.3f, 0.85f, t / end));
        }

        /// <summary>Toxic Reign: a glowing venom drop falls straight down onto the circle and lands at t = 0.</summary>
        void TickDrop(float t)
        {
            float p = WantedSkillLightTiming.DropProgress(t);
            if (p <= 0f) return;
            if (drop == null) drop = LightSkillAssets.Quad(transform, "Drop", MireHydraFxLook.Drop, false, FeelConfig.FxOrder - 18);
            drop.transform.localPosition = new Vector3(0f, DropHeight * (1f - p * p), 0f); // accelerating fall
            drop.transform.localScale = new Vector3(9f, 15f, 1f);
            LightSkillAssets.Set(drop, Mathf.Clamp01(p * 4f));
        }

        /// <summary>The strike-instant burst of bubbles: outward on the ground for the tail shockwave, up and out of the geysers.</summary>
        void EmitBurst()
        {
            if (skill.Id == 0) return; // the breath sprays continuously in TickStreams
            int n = skill.Id == 2 ? 48 : 22;
            for (int i = 0; i < n; i++)
            {
                float a = Random.value * Mathf.PI * 2f;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                if (skill.Id == 2)
                    WantedFxParticles.Emit(bubbles, origin + d * radius * 0.3f, d * radius * Random.Range(1.4f, 2.2f), Random.Range(2.4f, 4f), WantedFxParticles.VenomColor(), Random.Range(0.45f, 0.7f));
                else
                    WantedFxParticles.Emit(bubbles, origin + d * radius * Random.Range(0f, 0.5f), new Vector2(d.x * radius * 0.6f, Random.Range(20f, 42f)),
                        Random.Range(2.2f, 3.8f), WantedFxParticles.VenomColor(), Random.Range(0.6f, 0.95f));
            }
        }

        static float Smooth(float a, float b, float x) { float k = Mathf.Clamp01((x - a) / (b - a)); return k * k * (3f - 2f * k); }
    }
}
