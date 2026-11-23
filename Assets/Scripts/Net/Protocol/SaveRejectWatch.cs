namespace ZenoniaProto.Net
{
    /// <summary>
    /// Counts save refusals from the API (a state its validation does not accept) inside a sliding window. One refusal can be a bad character; a few in a row mean the API is older than
    /// this zone (a new save version or card id the API does not know yet). Deploy order is "server-api first, zone second" (docs/deploy.md): this is the alarm for when that was skipped.
    /// </summary>
    public sealed class SaveRejectWatch
    {
        public const int Threshold = 3;
        public const long WindowMs = 10 * 60 * 1000;

        int count;
        long windowStartMs;

        /// <summary>Records one refusal at `nowMs` and returns how many there were in the current window (the window restarts when the first one is older than WindowMs).</summary>
        public int Note(long nowMs)
        {
            if (count == 0 || nowMs - windowStartMs > WindowMs) { count = 0; windowStartMs = nowMs; }
            count++;
            return count;
        }

        /// <summary>True from the third refusal of a window on.</summary>
        public static bool IsRepeated(int refusals) { return refusals >= Threshold; }
    }
}
