using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// All "game feel" tuning in one static place plus the two runtime A/B toggles.
    /// FxOn = false restores plain hits (short hit-stop, no shake / white flash / trail / sparks / glow layers / damage pop).
    /// </summary>
    public static class FeelConfig
    {
        // ---- runtime toggles (F6 = bloom, F = effects) ----
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

        // ---- ranged combo shots (wizard bolts / mechanic bullets): lighter than a sword hit because they come in quick bursts ----
        public const float HitStopBoltMs = 30f, HitStopBlastMs = 80f, PlainHitStopBoltMs = 20f;
        public static float BoltHitStopMs(bool blast) { return blast ? HitStopBlastMs : FxOn ? HitStopBoltMs : PlainHitStopBoltMs; }

        // ---- screen shake (trauma model: offset = trauma^2 * MaxShakeUnits, camera offset only) ----
        public const float TraumaNormal = 0.35f, TraumaCrit = 0.55f, TraumaFinisher = 0.8f, TraumaHurt = 0.45f, TraumaBolt = 0.18f, TraumaBlast = 0.7f;
        public const float MaxShakeUnits = 5f, ShakeFrequency = 42f, TraumaDecayPerSecond = 1.8f;

        // ---- monster reaction ----
        public const float FlashMaxAmount = 0.5f;          // cap: a full white silhouette read as a white blob on snow / white-furred monsters
        public const float FlashMs = 110f;               // white flash duration (frozen during hit-stop, so it reads through the freeze)
        public const float KnockbackMultiplier = 1.35f, KnockbackMs = 170f;
        public const float HitJitterUnits = 0.7f;

        // ---- sorting: every combat FX draws above all y-sorted entities (entity orders stay within about -16000..0) ----
        public const int FxOrder = 18000;

        // ---- additive glow ----
        public const float AdditiveIntensity = 1.7f;     // > 1 so bright pixels exceed the bloom threshold in the HDR buffer
        public const float TrailIntensity = 1.9f;
        public const float PaintedGlowAlpha = 0.22f;      // additive copy of the painted ranged-FX sprites (base sprite is normal alpha blend)
        public const float EnhancedGlowAlpha = 0.6f;

        // ---- held-item enhance effect (HeldEnhanceFx), index = EnhanceStats.VisualBand - 1 ----
        public struct EnhanceLook
        {
            public float SweepPeriod, SweepSec, SweepWidth, ShineAlpha, ShineLift; // light band: seconds between starts, seconds to cross the art, half width (art = 1), peak strength, constant surface lift
            public float HaloAlpha, HaloBreath, BreathHz;                          // 0 alpha = no halo
            public int Stars, Motes;                                               // live at once on a main weapon; offhands and companions get a share
            public float StarGapMin, StarGapMax, StarSize;                         // seconds between twinkles of one star slot, world units
        }
        public static readonly EnhanceLook[] EnhanceLooks =
        {
            new EnhanceLook { SweepPeriod = 5f, SweepSec = 2.4f, SweepWidth = 0.1f, ShineAlpha = 0.28f },                                                                                                                    // +7  faint slow shine
            new EnhanceLook { SweepPeriod = 3.4f, SweepSec = 1.6f, SweepWidth = 0.13f, ShineAlpha = 0.45f, ShineLift = 0.03f, HaloAlpha = 0.3f, HaloBreath = 0.05f, BreathHz = 0.35f },                                      // +9  stronger shine, soft halo
            new EnhanceLook { SweepPeriod = 3f, SweepSec = 1.4f, SweepWidth = 0.14f, ShineAlpha = 0.5f, ShineLift = 0.05f, HaloAlpha = 0.42f, HaloBreath = 0.08f, BreathHz = 0.4f, Stars = 1, StarGapMin = 1.2f, StarGapMax = 2.6f, StarSize = 3.5f }, // +11 occasional stars
            new EnhanceLook { SweepPeriod = 2.4f, SweepSec = 1.1f, SweepWidth = 0.16f, ShineAlpha = 0.6f, ShineLift = 0.07f, HaloAlpha = 0.7f, HaloBreath = 0.16f, BreathHz = 0.5f, Stars = 2, StarGapMin = 0.7f, StarGapMax = 1.6f, StarSize = 4f, Motes = 4 }, // +13 coloured halo, drifting motes
            new EnhanceLook { SweepPeriod = 1.2f, SweepSec = 0.55f, SweepWidth = 0.22f, ShineAlpha = 0.85f, ShineLift = 0.12f, HaloAlpha = 1f, HaloBreath = 0.2f, BreathHz = 0.8f, Stars = 5, StarGapMin = 0.1f, StarGapMax = 0.6f, StarSize = 5f, Motes = 10 }, // +15 fast bright sweeps, many stars, strong aura
        };
        public const int EnhanceSparkBudget = 60;         // stars + motes alive on screen across all heroes (APK frame budget)
        public const float EnhanceHaloIntensity = 1.2f;   // below AdditiveIntensity: only the top bands reach the bloom threshold

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
        public const float BloomThreshold = 1.25f;       // only HDR-bright pixels (additive FX, glow layers) pass
        public const float BloomKnee = 0.15f;
        public const float BloomIntensity = 0.85f;

        // ---- damage numbers ----
        public const float PopPeak = 1.35f, PopPeakCrit = 1.75f, PopRiseMs = 70f, PopSettleMs = 200f;
    }
}
