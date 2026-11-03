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
            public float RimAlpha, RimWidth, PulsePeriod, PulseSec;                // rim light; width = share of the 22 art px aura reach (0.16 ~ 1.3 screen px); pulse every PulsePeriod s (0 = none)
            public float InnerAlpha, FlowAlpha, FlowSec;                           // inner energy (the art's energy-coloured pixels), flow wave through them (0 s = none)
            public int Gems;                                                       // diamond gems beside the blade, live at once on a main weapon; offhands get a share
            public float GemGapMin, GemGapMax, GemSize;                            // seconds between gems of one slot, world units
            public bool Secondary, Crest;                                          // second colour in the flow; floating crest gem (main weapon only)
            public float AuraAlpha, AuraReach, AuraSpeed;                          // animated energy tongues around the silhouette (SpriteAura), 0 alpha = none; reach 1 = 22 art px
            public bool Afterimage;                                                // glowing copies trail the weapon while it moves
            public float SurgeEvery, SurgeSec;                                     // short bright surge of rim + inner energy (0 = none)
            public bool Tip;                                                       // +15 energy blade beyond the weapon tip (HeldEnhanceFx.Tip)
        }
        // Light aura as the base plus one bright moving feature per band (ChatGPT 2026-11-03: large transparent shape + small bright focal point + motion). One new visible feature per band, not just more alpha (2026-11-03, docs/pending-tasks.md): +7 rim, +9 inner energy, +11 flow,
        // +13 gems, +15 second colour + rim pulse + crest. The weapon's own drawing stays readable at every band.
        public static readonly EnhanceLook[] EnhanceLooks =
        {
            new EnhanceLook { SweepPeriod = 5f, SweepSec = 1f, SweepWidth = 0.07f, ShineAlpha = 0.25f, RimAlpha = 0.35f, RimWidth = 0.14f, InnerAlpha = 0.35f, AuraAlpha = 0.2f, AuraReach = 0.25f, AuraSpeed = 0.8f }, // +7  soft aura + thin rim

            new EnhanceLook { SweepPeriod = 4f, SweepSec = 0.9f, SweepWidth = 0.075f, ShineAlpha = 0.22f, RimAlpha = 0.5f, RimWidth = 0.19f, PulsePeriod = 2.4f, PulseSec = 2.4f, InnerAlpha = 0.7f, AuraAlpha = 0.26f, AuraReach = 0.38f, AuraSpeed = 1f }, // +9  bright energy line along the edge, flowing
            new EnhanceLook { SweepPeriod = 3.5f, SweepSec = 0.8f, SweepWidth = 0.08f, ShineAlpha = 0.26f, RimAlpha = 0.65f, RimWidth = 0.25f, PulsePeriod = 1.7f, PulseSec = 1.7f, SurgeEvery = 3f, SurgeSec = 0.3f, InnerAlpha = 0.8f, FlowAlpha = 1f, FlowSec = 1.5f, AuraAlpha = 0.3f, AuraReach = 0.5f, AuraSpeed = 1.3f }, // +11 faster line + surges
            new EnhanceLook { SweepPeriod = 3f, SweepSec = 0.7f, SweepWidth = 0.085f, ShineAlpha = 0.3f, RimAlpha = 0.8f, RimWidth = 0.31f, PulsePeriod = 1.2f, PulseSec = 1.2f, SurgeEvery = 2.2f, SurgeSec = 0.35f, InnerAlpha = 0.9f, FlowAlpha = 1.1f, FlowSec = 1.2f, Gems = 1, GemGapMin = 0.2f, GemGapMax = 0.8f, GemSize = 1.6f, AuraAlpha = 0.36f, AuraReach = 0.69f, AuraSpeed = 1.6f }, // +13 one detached flame
            new EnhanceLook { SweepPeriod = 2.5f, SweepSec = 0.6f, SweepWidth = 0.095f, ShineAlpha = 0.34f, RimAlpha = 0.65f, RimWidth = 0.375f, PulsePeriod = 0.9f, PulseSec = 0.9f, SurgeEvery = 1.5f, SurgeSec = 0.4f, InnerAlpha = 0.7f, FlowAlpha = 0.9f, FlowSec = 0.9f, Gems = 2, GemGapMin = 0.1f, GemGapMax = 0.6f, GemSize = 1.8f, Secondary = true, AuraAlpha = 0.28f, AuraReach = 1f, AuraSpeed = 1.8f, Afterimage = true, Tip = true }, // +15 two flames + energy blade past the tip + afterimages
        };
        public const int EnhanceSparkBudget = 60;         // gems alive on screen across all heroes (APK frame budget)
        public const float EnhanceHaloIntensity = 1f;     // below AdditiveIntensity: the weapon glow should barely feed the bloom
        public const bool EnhanceUsesBaseArt = true;      // held gear keeps its base art when enhanced: the painted enhanced art had a thick neon outline and a wider shape
        public const float EnhanceAuraInAction = 0.7f;
        public const float EnhanceSurgeBoost = 0.6f, EnhanceFlameAlpha = 0.5f;    // the +15 aura stays strong in combat: it is the item's signature
        public const float EnhancePulseWidth = 0.12f, EnhanceFlowWidth = 0.16f, EnhanceRimSaturation = 0.85f, EnhanceSecondaryHueShift = 0.07f, EnhanceFadeSec = 0.1f;
        // Per motion (idle, walk, action): rim, halo, shine, gems, inner, flow. Rim and inner energy carry the level and stay in combat;
        // the decorative layers ease down so the swing effect reads.
        public static readonly float[,] EnhanceMotionK = { { 1f, 1f, 1f, 1f, 1f, 1f }, { 1f, 0.7f, 0.7f, 0.6f, 1f, 0.85f }, { 1f, 0.35f, 0.25f, 0f, 1f, 0.75f } };

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
