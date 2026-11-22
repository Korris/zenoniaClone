using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Warrior lock-target heavy slash force (2026-11-22, V2 Adventure redraw; ChatGPT "attack force" analysis,
    /// docs/reference/ro-soft-adventure-redesign/proportions-and-attack-force-chatgpt.md). VIEW ONLY: the sim, the server and the 400 ms swing
    /// (ComboConfigs.WarriorHits, blow at 120 ms) are unchanged, so nothing here needs a server rebuild.
    /// - Frame timing: the 8-frame art is not spread evenly over the swing; its impact frame f4 starts exactly at the blow (120 ms):
    ///   f0 0-30 crouch, f1 30-70 wind-up peak, f2 70-95 hips / shoulders turn, f3 95-120 smear, f4 120 IMPACT, f5 overshoot, f6 settle, f7 guard.
    /// - Hit-stop inside the cycle: when the blow connects the picture holds the impact frame (and the lunge) for HoldMs, then runs the rest a
    ///   little faster so the swing still ends at 400 ms (no extra cooldown, no global GameClock freeze).
    /// - Lunge: the drawn figure + its shadow slide along the facing (back on the wind-up, LungeUnits forward at the impact, eased back by the
    ///   guard). The art itself has no travel any more (Tools/ro_fantasy_attack8_cut.py TRAVEL = 0) so the two never stack.
    /// </summary>
    public static class WarriorSlashFeel
    {
        public const float BlowMs = 120f;   // ComboConfigs.WarriorHits windup: damage time (server)
        public const float HoldMs = 35f;    // visual hit-stop on a connecting blow
        public const float LungeUnits = 4.4f; // ~0.2 H (V2 hero ~21.5 u) = ~15 px at 76 px on screen (target 13-19 px)
        public const float PullBackUnits = 1.2f;
        public const float ShakePx = 1.8f, ShakeMs = 75f, CritShakePx = 2.2f, CritShakeMs = 85f;
        public const float FlashMs = 65f, FlashCap = 0.6f; // monster white flash on the Warrior's own hits (0.7 covered the face with the hit spark, ChatGPT score 22/11)

        // frame starts (ms) of the 8-frame art inside the 400 ms swing; f4 = the impact pose at the blow
        static readonly float[] FrameStartMs = { 0f, 30f, 70f, 95f, 120f, 175f, 245f, 320f };

        /// <summary>Visual time of the swing: real time with a HoldMs freeze at the blow (connected only), caught up by the end.</summary>
        public static float VisualMs(float elapsedMs, float totalMs, bool connected)
        {
            if (!connected || elapsedMs <= BlowMs) return elapsedMs;
            if (elapsedMs <= BlowMs + HoldMs) return BlowMs;
            float rest = totalMs - BlowMs - HoldMs;
            if (rest <= 1f) return totalMs;
            return BlowMs + (elapsedMs - BlowMs - HoldMs) * (totalMs - BlowMs) / rest;
        }

        /// <summary>Frame of an n-frame attack sheet at this visual time (8 frames: the impact-aligned table, else even).</summary>
        public static int Frame(int n, float visualMs, float totalMs)
        {
            if (n != FrameStartMs.Length || totalMs <= 0f) return Mathf.Clamp(Mathf.FloorToInt(visualMs / totalMs * n), 0, n - 1);
            float t = visualMs * (400f / totalMs); // the table is written for 400 ms
            int f = 0;
            for (int i = 1; i < FrameStartMs.Length; i++) if (t >= FrameStartMs[i]) f = i;
            return f;
        }

        /// <summary>Lunge along the facing (world units) at this visual time: pull back on the wind-up, forward through the impact, ease back.</summary>
        public static float Lunge(float visualMs, float totalMs)
        {
            float t = visualMs * (400f / Mathf.Max(1f, totalMs));
            if (t <= 70f) return -PullBackUnits * Smooth(t / 70f);
            if (t <= BlowMs) return Mathf.Lerp(-PullBackUnits, LungeUnits, Smooth((t - 70f) / (BlowMs - 70f)));
            if (t <= 175f) return LungeUnits;
            if (t >= 400f) return 0f;
            return LungeUnits * (1f - Smooth((t - 175f) / (400f - 175f)));
        }

        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
    }
}
