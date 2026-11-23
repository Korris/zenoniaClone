using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Ranked results waiting for a player who was offline when their match ended (phase 07 fix H1), pure. A player who drops mid-match is saved with a provisional loss; if the match then ends in a win or draw for them
    /// (the opponent forfeits, the turn clock plays on for them), the real rank is parked here by character and applied the moment they log in again. Entries older than MaxAgeMs are dropped (the provisional loss stands).
    /// In memory only: a zone restart loses it, which leaves the provisional loss in place.
    /// </summary>
    public sealed class RankedPending
    {
        public const long MaxAgeMs = 24L * 3600L * 1000L;
        const int PurgeAbove = 256;

        sealed class Entry { public CardRankData Rank; public long At; }

        readonly Dictionary<string, Entry> byChar = new Dictionary<string, Entry>();

        public int Count { get { return byChar.Count; } }

        /// <summary>Parks the rank this character should have (replaces an older one).</summary>
        public void Put(string charKey, CardRankData rank, long nowMs)
        {
            if (string.IsNullOrEmpty(charKey) || rank == null) return;
            byChar[charKey] = new Entry { Rank = RankedRules.Copy(rank), At = nowMs };
            if (byChar.Count > PurgeAbove) Purge(nowMs);
        }

        /// <summary>The parked rank of a character that just came back (and forgets it); false when there is none or it is too old.</summary>
        public bool TryTake(string charKey, long nowMs, out CardRankData rank)
        {
            rank = null;
            Entry e;
            if (charKey == null || !byChar.TryGetValue(charKey, out e)) return false;
            byChar.Remove(charKey);
            if (nowMs - e.At > MaxAgeMs) return false;
            rank = e.Rank;
            return true;
        }

        public void Purge(long nowMs)
        {
            var old = new List<string>();
            foreach (KeyValuePair<string, Entry> kv in byChar) if (nowMs - kv.Value.At > MaxAgeMs) old.Add(kv.Key);
            foreach (string k in old) byChar.Remove(k);
        }
    }
}
