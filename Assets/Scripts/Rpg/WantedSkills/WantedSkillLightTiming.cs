using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Engine-free timing of the shader / particle FX of the Mire Hydra Queen skills (MireHydraLightFx, 2026-11-19), so the dotnet tests can hold it
    /// to the skill table (docs/sprite-rules.md VFX rule 1). Every time is in ms RELATIVE TO THE STRIKE INSTANT (the telegraph event's AtMs, which is
    /// also when the server deals the damage): negative = before the hit. The warning fills from -TelegraphMs to exactly 0; the impact pieces reach
    /// their full size exactly at 0 (venom streams full length, falling drops on the ground, shockwave out of the boss, geyser up) and are gone by
    /// ImpactEndMs, long before the skill comes off cooldown. No skill number lives here: radius, angle, count and telegraph come from WantedSkillTable.
    /// </summary>
    public static class WantedSkillLightTiming
    {
        public const string LightBoss = "mireHydraQueen";

        /// <summary>The last ChargeMs of the warning: bubbles rise inside the zone and the ground light gathers (the boss is about to strike).</summary>
        public const int ChargeMs = 450;
        /// <summary>Triple Venom Breath: the three streams grow from the mouths to the cone's rim over the last StreamGrowMs before the hit.</summary>
        public const int StreamGrowMs = 180;
        /// <summary>Queen's Toxic Reign: each drop falls for DropFallMs and lands at the strike instant.</summary>
        public const int DropFallMs = 380;
        /// <summary>Serpent Tail Crush: the shockwave ring runs from the boss to the rim in ShockwaveMs after the hit.</summary>
        public const int ShockwaveMs = 260;
        /// <summary>Ground light after the hit: full in GlowPeakMs, back to 0 at ImpactEndMs (same curve shape as the Ice Storm ground light).</summary>
        public const int GlowPeakMs = 120;

        /// <summary>When every impact piece of skill `skillId` is gone (ms after the strike).</summary>
        public static int ImpactEndMs(int skillId)
        {
            switch (skillId)
            {
                case 0: return 900;   // breath streams linger, then thin out
                case 2: return 950;   // shockwave + dust of venom
                default: return 1000; // blooms / rain splashes
            }
        }

        /// <summary>Warning fill 0..1 at `msToStrike` before the hit (AtMs - now): 0 when the telegraph starts, exactly 1 at the strike instant.</summary>
        public static float WarningFill(WantedSkillDef skill, float msToStrike)
        {
            return Clamp01(1f - msToStrike / Math.Max(1, skill.TelegraphMs));
        }

        /// <summary>Charge 0..1 over the last ChargeMs before the hit (0 earlier, 1 at the strike instant).</summary>
        public static float Charge(float msToStrike) { return Clamp01(1f - msToStrike / ChargeMs); }

        /// <summary>Venom stream length 0..1 at `t` ms relative to the strike (reaches 1 exactly at 0, holds afterwards).</summary>
        public static float StreamLength(float t) { return Clamp01(1f + t / StreamGrowMs); }

        /// <summary>Fall progress 0..1 of a Toxic Reign drop at `t` ms relative to the strike (1 = on the ground, exactly at 0).</summary>
        public static float DropProgress(float t) { return Clamp01(1f + t / DropFallMs); }

        /// <summary>Shockwave radius 0..1 (x the skill radius) at `t` ms after the hit, ease-out; 0 before the hit.</summary>
        public static float Shockwave(float t)
        {
            if (t <= 0f) return 0f;
            float x = Clamp01(t / ShockwaveMs);
            return 1f - (1f - x) * (1f - x);
        }

        /// <summary>Ground light / impact intensity 0..1 at `t` ms after the hit of skill `skillId`: 0 before, full at GlowPeakMs, 0 at ImpactEndMs.</summary>
        public static float Glow(int skillId, float t)
        {
            int end = ImpactEndMs(skillId);
            if (t <= 0f || t >= end) return 0f;
            if (t < GlowPeakMs) { float x = t / GlowPeakMs; return 1f - (1f - x) * (1f - x); }
            float y = 1f - (t - GlowPeakMs) / (end - GlowPeakMs);
            return y * y;
        }

        /// <summary>The light FX starts this long before the strike: the telegraph's own length (never later than a falling drop or a growing stream).</summary>
        public static int LeadMs(WantedSkillDef skill) { return Math.Max(skill.TelegraphMs, Math.Max(DropFallMs, StreamGrowMs)); }

        static float Clamp01(float v) { return v < 0f ? 0f : v > 1f ? 1f : v; }
    }
}
