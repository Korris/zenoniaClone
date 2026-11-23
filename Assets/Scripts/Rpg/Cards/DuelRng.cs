namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Small deterministic generator (xorshift64*) for the card duel. Not System.Random: the sequence must be the same on every runtime and version, so a match is replayable
    /// from its seed and the moves alone (tests, server log, offline host all agree).
    /// </summary>
    public sealed class DuelRng
    {
        ulong s;

        public DuelRng(ulong seed) { s = seed == 0UL ? 0x9E3779B97F4A7C15UL : seed; }

        public ulong NextU64()
        {
            unchecked
            {
                s ^= s >> 12; s ^= s << 25; s ^= s >> 27;
                return s * 0x2545F4914F6CDD1DUL;
            }
        }

        /// <summary>0 .. n-1 (n must be at least 1).</summary>
        public int Next(int n) { return n <= 1 ? 0 : (int)((NextU64() >> 33) % (ulong)n); }
    }
}
