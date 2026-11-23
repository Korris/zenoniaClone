using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    public enum WagerError { None = 0, Level = 1, DailyLimit = 2, PairLimit = 3 }

    /// <summary>
    /// The card-stakes rules of a casual PvP duel as the ZONE checks them, only to hide or grey the UI and to refuse early; server-api checks every one again and is final (it also knows the account's age and the
    /// same-account rule). Every casual PvP duel stakes both whole 8-card decks and the winner takes 1 card of the loser's deck. Needs: Lv30+, at most 10 stakes duels a day, the same pair at most 2 a day.
    /// `Enabled` is the emergency switch (the API has CARD_WAGER_ENABLED for the same reason): off = PvP still plays, but no card moves and none of these limits apply.
    /// </summary>
    public static class WagerRules
    {
        public const int MinLevel = 30, MaxPerDay = 10, MaxPerPairPerDay = 2;
        public static bool Enabled = true;

        /// <summary>Can this player be in a stakes duel today? (level, daily count; always None while the switch is off). `dayKey` = WagerRules.DayKeyNow().</summary>
        public static WagerError CanEnter(PlayerState p, int dayKey)
        {
            if (!Enabled) return WagerError.None; // switched off: nothing is staked, so nothing to check
            if (p.Level < MinLevel) return WagerError.Level;
            CardDailyData d = p.Cards.Daily;
            if (d.day == dayKey && d.wagers >= MaxPerDay) return WagerError.DailyLimit;
            return WagerError.None;
        }

        /// <summary>WantedBoss day key for "now" (UTC ms): the daily duel counter restarts with it.</summary>
        public static int DayKeyNow() { return WantedBoss.DayKey(System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()); }

        public static string Text(WagerError e)
        {
            switch (e)
            {
                case WagerError.Level: return "Card Duel needs Lv " + MinLevel;
                case WagerError.DailyLimit: return MaxPerDay + " card duels a day is the limit";
                case WagerError.PairLimit: return "You two already dueled " + MaxPerPairPerDay + " times today";
                default: return "";
            }
        }

        /// <summary>Counts today's duels per pair of characters (this zone only; the API keeps the real count per account pair). Day-keyed: yesterday's counts are dropped on the first use of a new day.</summary>
        public sealed class PairLedger
        {
            readonly Dictionary<string, int> counts = new Dictionary<string, int>();
            int day = int.MinValue;

            static string Key(string a, string b) { return string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a; }

            void Roll(int dayKey) { if (dayKey != day) { counts.Clear(); day = dayKey; } }

            public bool Allowed(string a, string b, int dayKey)
            {
                Roll(dayKey);
                int n; counts.TryGetValue(Key(a, b), out n);
                return n < MaxPerPairPerDay;
            }

            public void Note(string a, string b, int dayKey)
            {
                Roll(dayKey);
                string k = Key(a, b); int n; counts.TryGetValue(k, out n); counts[k] = n + 1;
            }
        }
    }
}
