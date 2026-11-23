using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    public enum WagerError { None = 0, Disabled = 1, Level = 2, DailyLimit = 3, UnknownCard = 4, BossCard = 5, Rarity = 6, NotSpare = 7, Mismatch = 8, PairLimit = 9 }

    /// <summary>
    /// The Card Wager rules as the ZONE checks them, only to hide or grey the UI and to refuse early; server-api checks every one again and is final (it also knows the account's age). A wager is one N or R card
    /// (no boss) that is not locked and not needed by the deck, the two cards have the same rarity, the player is Lv30+ and has wagered fewer than 3 times today, and the same pair at most twice a day.
    /// Enabled is the emergency switch (the API has CARD_WAGER_ENABLED for the same reason): off = every wager is refused and PvP goes on without them.
    /// </summary>
    public static class WagerRules
    {
        public const int MinLevel = 30, MaxPerDay = 3, MaxPerPairPerDay = 2;
        public static bool Enabled = true;

        /// <summary>Can this player take part in a wager at all today? (level, daily count, switch). `dayKey` = DuelNpcRun.DayKeyNow().</summary>
        public static WagerError CanEnter(PlayerState p, int dayKey)
        {
            if (!Enabled) return WagerError.Disabled;
            if (p.Level < MinLevel) return WagerError.Level;
            CardDailyData d = p.Cards.Daily;
            if (d.day == dayKey && d.wagers >= MaxPerDay) return WagerError.DailyLimit;
            return WagerError.None;
        }

        /// <summary>Can this player put this card in? Unknown / boss / not N or R / not a spare copy (locked, or every copy is in the deck).</summary>
        public static WagerError CheckCard(PlayerState p, string cardId)
        {
            CardDef c = CardTable.Get(cardId);
            if (c == null) return WagerError.UnknownCard;
            if (c.IsBoss) return WagerError.BossCard;
            if (c.Rarity > CardRarity.R) return WagerError.Rarity;
            if (p.Cards.Spare(cardId) < 1) return WagerError.NotSpare;
            return WagerError.None;
        }

        /// <summary>The two cards may be wagered against each other: both known, same rarity.</summary>
        public static bool Matches(CardDef a, CardDef b) { return a != null && b != null && a.Rarity == b.Rarity && !a.IsBoss && !b.IsBoss; }

        public static string Text(WagerError e)
        {
            switch (e)
            {
                case WagerError.Disabled: return "Card Wager is switched off";
                case WagerError.Level: return "Card Wager needs Lv " + MinLevel;
                case WagerError.DailyLimit: return "3 wagers a day is the limit";
                case WagerError.UnknownCard: return "Unknown card";
                case WagerError.BossCard: return "Boss cards cannot be wagered";
                case WagerError.Rarity: return "Only N and R cards can be wagered";
                case WagerError.NotSpare: return "That card is locked or needed by your deck";
                case WagerError.Mismatch: return "Both cards must have the same rarity";
                case WagerError.PairLimit: return "You two already wagered twice today";
                default: return "";
            }
        }

        /// <summary>Counts today's wagers per pair of characters (this zone only; the API keeps the real count per account pair). Day-keyed: yesterday's counts are dropped on the first use of a new day.</summary>
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
