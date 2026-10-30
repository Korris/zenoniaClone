using System;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// Chooses how far behind the server clock the client renders other entities: two snapshot intervals plus a margin for the measured arrival jitter
    /// (RFC 3550 style smoothed |transit difference|), clamped to 120..250 ms. The delay grows quickly when the network gets rougher and shrinks slowly,
    /// so a bad moment does not make the world speed up afterwards.
    /// </summary>
    public sealed class AdaptiveInterpolationDelay
    {
        public const double MinMs = 120.0, MaxMs = 250.0, JitterWeight = 3.0;
        readonly double intervalMs;
        double prevTransit, jitter, delay;
        bool has;

        public AdaptiveInterpolationDelay(double snapshotIntervalMs)
        {
            intervalMs = Math.Max(10.0, snapshotIntervalMs);
            delay = Clamp(intervalMs * 2.0);
        }

        public double DelayMs { get { return delay; } }
        public double JitterMs { get { return jitter; } }

        static double Clamp(double v) { return Math.Max(MinMs, Math.Min(MaxMs, v)); }

        /// <summary>`transitMs` = client receive time minus the snapshot's server time (any constant offset cancels).</summary>
        public void OnSnapshot(double transitMs)
        {
            if (has) jitter += (Math.Abs(transitMs - prevTransit) - jitter) / 8.0;
            prevTransit = transitMs; has = true;
            double target = Clamp(intervalMs * 2.0 + jitter * JitterWeight);
            delay += (target - delay) * (target > delay ? 0.25 : 0.03);
        }
    }
}
