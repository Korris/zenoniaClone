using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Engine-free timing of the shader light FX of Ice Storm (IceStormLightFx), kept next to the skill numbers so the dotnet tests can check it:
    /// every light meteor lands exactly on its damage wave (IceStormUltimate strikes at CastMs + wave x WaveMs), never on a hand-typed number (docs/sprite-rules.md, VFX rule 1).
    /// </summary>
    public static class LightSkillTiming
    {
        public const int MeteorFlightMs = 320;     // fall time of a light meteor before it lands
        public const int GroundPeakMs = 150;       // ground light: rises to full in 0.15 s ...
        public const int ExplosionMs = 560;        // ... then falls to 0 when the explosion ends
        public const int SpearsPerWave = LegacySkillNumbers.IceStormSpears / LegacySkillNumbers.IceStormWaves;

        /// <summary>Ms after the cast when Ice Storm wave `wave` deals its damage (same formula as IceStormUltimate.Update).</summary>
        public static int WaveMs(int wave) { return (int)(LegacySkillNumbers.IceStormCastMs + wave * LegacySkillNumbers.IceStormWaveMs); }

        /// <summary>Ms after the cast when meteor `i` (0..IceStormSpears-1, 3 per wave) lands: on its wave.</summary>
        public static int LandMs(int i) { return WaveMs(i / SpearsPerWave); }

        /// <summary>Ms after the cast when meteor `i` appears (clamped to the cast frame).</summary>
        public static int MeteorStartMs(int i) { return Math.Max(0, LandMs(i) - MeteorFlightMs); }

        /// <summary>Fall progress 0..1 of meteor `i` at `ms` after the cast (1 = on the ground).</summary>
        public static float MeteorProgress(int i, float ms) { return Clamp01(1f - (LandMs(i) - ms) / MeteorFlightMs); }

        /// <summary>When the whole picture is over: the circle's tail after the last wave (IceStormUltimate lives as long).</summary>
        public static int EndMs() { return WaveMs(LegacySkillNumbers.IceStormWaves - 1) + (int)LegacySkillNumbers.IceStormTailMs; }

        /// <summary>
        /// Ground light intensity `ms` after a landing: 0 -> 1 over GroundPeakMs (ease-out), then 1 -> 0 at ExplosionMs (quadratic ease-in to 0).
        /// </summary>
        public static float GroundLight(float ms)
        {
            if (ms <= 0f || ms >= ExplosionMs) return 0f;
            if (ms < GroundPeakMs) { float x = ms / GroundPeakMs; return 1f - (1f - x) * (1f - x); }
            float y = 1f - (ms - GroundPeakMs) / (ExplosionMs - GroundPeakMs);
            return y * y;
        }

        static float Clamp01(float v) { return v < 0f ? 0f : v > 1f ? 1f : v; }
    }
}
