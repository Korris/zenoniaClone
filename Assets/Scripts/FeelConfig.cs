using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// All "game feel" tuning in one static place plus the two runtime A/B toggles.
    /// FxOn = false restores plain hits (short hit-stop, no shake / white flash / trail / sparks / glow layers / damage pop).
    /// </summary>
    public static class FeelConfig
    {
        // ---- runtime toggles (B = bloom, F = effects) ----
        public static bool BloomOn = true;
        public static bool FxOn = true;

        // ---- hit-stop (ms of frozen combat simulation; UI and camera keep running) ----
        public const float HitStopNormalMs = 45f, HitStopFinisherMs = 80f;
        public const float PlainHitStopNormalMs = 25f, PlainHitStopFinisherMs = 60f; // FxOn = false
        public static float HitStopMs(bool finisher)
        {
            if (FxOn) return finisher ? HitStopFinisherMs : HitStopNormalMs;
            return finisher ? PlainHitStopFinisherMs : PlainHitStopNormalMs;
        }

        // ---- screen shake (trauma model: offset = trauma^2 * MaxShakeUnits, camera offset only) ----
        public const float TraumaNormal = 0.35f, TraumaCrit = 0.55f, TraumaFinisher = 0.8f, TraumaHurt = 0.45f;
        public const float MaxShakeUnits = 5f, ShakeFrequency = 42f, TraumaDecayPerSecond = 1.8f;

        // ---- monster reaction ----
        public const float FlashMs = 110f;               // white flash duration (frozen during hit-stop, so it reads through the freeze)
        public const float KnockbackMultiplier = 1.35f, KnockbackMs = 170f;
        public const float HitJitterUnits = 0.7f;

        // ---- sorting: every combat FX draws above all y-sorted entities (entity orders stay within about -16000..0) ----
        public const int FxOrder = 18000;

        // ---- additive glow ----
        public const float AdditiveIntensity = 1.7f;     // > 1 so bright pixels exceed the bloom threshold in the HDR buffer
        public const float TrailIntensity = 1.9f;
        public const float EnhancedGlowAlpha = 0.6f;

        // ---- sword trail ribbon ----
        public const float TrailSpanDeg = 72f;           // arc half-angle around the facing direction
        public const float TrailReachPad = 2f;           // ribbon radius = combo reach + pad
        public const float TrailWidthNormal = 0.42f, TrailWidthFinisher = 0.62f; // fraction of radius
        public const float TrailSweepSec = 0.11f, TrailSweepFinisherSec = 0.16f, TrailFadeSec = 0.14f;

        // ---- impact sparks ----
        public const int SparksNormal = 9, SparksStrong = 14;
        public const float SparkSpeedMin = 45f, SparkSpeedMax = 125f;
        public const int ImpactPool = 12;

        // ---- bloom (post-process on the camera) ----
        public const float BloomThreshold = 1.0f;        // only HDR-bright pixels (additive FX, glow layers) pass
        public const float BloomKnee = 0.2f;
        public const float BloomIntensity = 0.85f;

        // ---- damage numbers ----
        public const float PopPeak = 1.35f, PopPeakCrit = 1.75f, PopRiseMs = 70f, PopSettleMs = 200f;
    }
}
