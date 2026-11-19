using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Engine-free timing of the shader light FX of Astral Cataclysm (AstralLightFx), kept next to the skill data so the dotnet tests can check it:
    /// every meteor lands exactly on its rain drop's damage step (SkillSchedule), never on a hand-typed number (docs/sprite-rules.md, VFX rule 1).
    /// </summary>
    public static class LightSkillTiming
    {
        public const int MeteorFlightMs = 320;     // fall time before a drop lands (the meteor of drop 0 lands on the cast frame, so it is already down)
        public const int GroundPeakMs = 150;       // ground light: rises to full in 0.15 s ...
        public const int ExplosionMs = 560;        // ... then falls to 0 when the explosion ends
        public const int PillarTailMs = 380;       // the central light pillar dissolves for this long after the last drop

        /// <summary>Ms after the cast when drop `drop` of a Rain effect deals its damage (same formula as SkillSchedule.Build).</summary>
        public static int LandMs(SkillEffect rain, int drop) { return rain.AtMs + drop * Math.Max(0, rain.IntervalMs); }

        /// <summary>Ms after the cast when the meteor of `drop` appears (clamped to the cast frame).</summary>
        public static int MeteorStartMs(SkillEffect rain, int drop) { return Math.Max(0, LandMs(rain, drop) - MeteorFlightMs); }

        /// <summary>Fall progress 0..1 of a meteor `ms` after the cast (1 = on the ground). A meteor that starts late begins part-way down.</summary>
        public static float MeteorProgress(SkillEffect rain, int drop, float ms)
        {
            float land = LandMs(rain, drop);
            return Clamp01(1f - (land - ms) / MeteorFlightMs);
        }

        /// <summary>When the whole picture is over: last drop + explosion, and the pillar's dissolve.</summary>
        public static int EndMs(SkillEffect rain) { return LandMs(rain, Math.Max(1, rain.Count) - 1) + Math.Max(ExplosionMs, PillarTailMs); }

        /// <summary>
        /// Ground light intensity `ms` after a drop lands: 0 -> 1 over GroundPeakMs (ease-out), then 1 -> 0 at ExplosionMs (quadratic ease-in to 0).
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
