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
        /// <summary>Venom Drain: the tethers pulse bright at the strike and are gone DrainPulseMs later (ChatGPT: "tia hút về boss 350 ms").</summary>
        public const int DrainPulseMs = 350;
        /// <summary>Venom Drain: the green heal flash on the boss rises and fades over HealFlashMs after the strike.</summary>
        public const int HealFlashMs = 650;

        public static int ImpactEndMs(int skillId)
        {
            switch (skillId)
            {
                case 0: return 900;   // breath streams linger, then thin out
                case 2: return 950;   // shockwave + dust of venom
                case 4: return HealFlashMs; // Venom Drain: tether pulse (350) inside the heal flash
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

        /// <summary>Venom Drain tether opacity at `t` ms relative to the strike: a faint flow growing with the warning (0.3 -&gt; 0.8), full at the
        /// strike, back to 0 at DrainPulseMs.</summary>
        public static float TetherAlpha(WantedSkillDef skill, float t)
        {
            if (t < 0f) return 0.3f + 0.5f * WarningFill(skill, -t);
            if (t >= DrainPulseMs) return 0f;
            return t < GlowPeakMs ? 1f : 1f - (t - GlowPeakMs) / (DrainPulseMs - GlowPeakMs);
        }

        /// <summary>Venom Drain tether width multiplier: 1 during the warning, 1.6 at the strike pulse, easing back to 1 by DrainPulseMs.</summary>
        public static float TetherWidth(float t)
        {
            if (t < 0f || t >= DrainPulseMs) return 1f;
            return 1f + 0.6f * (1f - t / DrainPulseMs);
        }

        /// <summary>Venom Drain heal flash on the boss 0..1: 0 before the strike, full at GlowPeakMs, 0 at HealFlashMs.</summary>
        public static float HealFlash(float t)
        {
            if (t <= 0f || t >= HealFlashMs) return 0f;
            if (t < GlowPeakMs) return t / GlowPeakMs;
            float y = 1f - (t - GlowPeakMs) / (HealFlashMs - GlowPeakMs);
            return y * y;
        }

        /// <summary>The light FX starts this long before the strike: the telegraph's own length (never later than a falling drop or a growing stream).</summary>
        public static int LeadMs(WantedSkillDef skill) { return Math.Max(skill.TelegraphMs, Math.Max(DropFallMs, StreamGrowMs)); }

        static float Clamp01(float v) { return v < 0f ? 0f : v > 1f ? 1f : v; }
    }
}
