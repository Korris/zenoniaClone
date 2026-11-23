using System;

namespace ZenoniaProto.Rpg
{
    public enum RankTier { Bronze = 0, Silver = 1, Gold = 2, Platinum = 3, Diamond = 4 }

    public enum RankedError { None = 0, Level = 1, Disabled = 2 }

    /// <summary>
    /// Ranked Card Duel math (phase 07), pure and shared by the zone and the tests: the 30-day season, the five tiers by league points (LP), the hidden rating (MMR, Elo K 32) and what one result does to both.
    /// Ranked takes NO cards: a match only moves MMR / LP and the win-loss record. LP never goes below 0. A match that does not count (the same pair for the third time in a day) changes nothing.
    /// The state lives in CardCollection.Rank (CardRankData): season, mmr, lp, wins, losses, draws, best (highest LP this season), prevBest (the last finished season's best), rewards (bit per tier reached at a season end;
    /// the game has no cosmetic system yet, so a season-end reward is only this flag).
    /// </summary>
    public static class RankedRules
    {
        public const int MinLevel = 20, StartMmr = 1000, K = 32, SeasonDays = 30;
        /// <summary>Rollback switch: false = nobody can queue (the data stays). A server sets it from `--no-ranked` (NetLaunchArgs).</summary>
        public static bool Enabled = true;
        /// <summary>Season 1 starts here (2026-11-01 00:00 UTC); every SeasonDays days after it is the next season.</summary>
        public const long SeasonEpochMs = 1793491200000L;
        public const long SeasonMs = SeasonDays * 86400000L;
        /// <summary>LP needed for each tier (Bronze, Silver, Gold, Platinum, Diamond).</summary>
        public static readonly int[] TierLp = { 0, 300, 700, 1200, 1800 };
        public const int WinLpMin = 20, WinLpMax = 30, LossLpMin = 15, LossLpMax = 25, MaxMatchesPerPairPerDay = 2;
        /// <summary>A match that ends by a forfeit before this many moves were placed by the players (the server's auto-placements do not count) changes nothing for either side: nobody farms LP with a second character that gives up at once.</summary>
        public const int MinMovesForForfeit = 3;
        const int MaxSkippedSeasons = 10;
        /// <summary>The 5 tier bits of CardRankData.rewards.</summary>
        public const int RewardMask = 31;

        public struct Change { public int Mmr, Lp; }

        // ---- season ----

        /// <summary>The season number (1, 2, ...) a UTC time falls in; times before the epoch count as season 1.</summary>
        public static int Season(long utcMs) { return utcMs < SeasonEpochMs ? 1 : (int)((utcMs - SeasonEpochMs) / SeasonMs) + 1; }

        public static long SeasonEndMs(int season) { return SeasonEpochMs + season * SeasonMs; }

        /// <summary>Whole minutes until the current season ends (at least 0).</summary>
        public static int MinutesLeft(long utcMs) { return (int)Math.Max(0L, (SeasonEndMs(Season(utcMs)) - utcMs) / 60000L); }

        public static long NowUtcMs() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }

        // ---- tiers ----

        public static RankTier Tier(int lp)
        {
            RankTier t = RankTier.Bronze;
            for (int i = 1; i < TierLp.Length; i++) if (lp >= TierLp[i]) t = (RankTier)i;
            return t;
        }

        public static string TierName(RankTier t) { return t.ToString(); }

        /// <summary>LP at which the next tier starts, or -1 at Diamond.</summary>
        public static int NextTierLp(RankTier t) { return (int)t + 1 < TierLp.Length ? TierLp[(int)t + 1] : -1; }

        /// <summary>0..1 progress inside the tier (Diamond is always full).</summary>
        public static float TierProgress(int lp)
        {
            RankTier t = Tier(lp);
            int next = NextTierLp(t);
            if (next < 0) return 1f;
            return Math.Max(0f, Math.Min(1f, (lp - TierLp[(int)t]) / (float)(next - TierLp[(int)t])));
        }

        // ---- entry ----

        /// <summary>None when this character may queue (the deck and the map are checked by the zone).</summary>
        public static RankedError CanQueue(PlayerState p)
        {
            if (!Enabled) return RankedError.Disabled;
            return p.Level < MinLevel ? RankedError.Level : RankedError.None;
        }

        public static string Text(RankedError e)
        {
            switch (e)
            {
                case RankedError.Level: return "Ranked needs Lv " + MinLevel;
                case RankedError.Disabled: return "Ranked is switched off";
                default: return "";
            }
        }

        // ---- rating ----

        /// <summary>Elo expectation of a player rated `mmr` against `oppMmr` (0..1).</summary>
        public static double Expected(int mmr, int oppMmr) { return 1.0 / (1.0 + Math.Pow(10.0, (oppMmr - mmr) / 400.0)); }

        /// <summary>What a result does: MMR Elo K 32 (hidden); LP win +20..30 (20 + 10 x the surprise), loss -15..-25 (15 + 10 x how favoured you were), draw 0. Equal ratings: +25 / -20.</summary>
        public static Change ChangeFor(DuelResult result, int mmr, int oppMmr)
        {
            double e = Expected(mmr, oppMmr);
            double score = result == DuelResult.Win ? 1.0 : result == DuelResult.Draw ? 0.5 : 0.0;
            var c = new Change { Mmr = (int)Math.Round(K * (score - e), MidpointRounding.AwayFromZero) };
            if (result == DuelResult.Win) c.Lp = Math.Max(WinLpMin, Math.Min(WinLpMax, WinLpMin + (int)Math.Round(10.0 * (1.0 - e), MidpointRounding.AwayFromZero)));
            else if (result == DuelResult.Lose) c.Lp = -Math.Max(LossLpMin, Math.Min(LossLpMax, LossLpMin + (int)Math.Round(10.0 * e, MidpointRounding.AwayFromZero)));
            return c;
        }

        /// <summary>True when a match that ended by a forfeit (`forfeitedSide` >= 0) had fewer than MinMovesForForfeit placed by hand: it does not count for either side.</summary>
        public static bool EarlyForfeit(int forfeitedSide, int movesByHand) { return forfeitedSide >= 0 && movesByHand < MinMovesForForfeit; }

        /// <summary>Plays one result into a copy of `before` (the rank as the match started): MMR and LP move (LP stays >= 0), the record and `best` update. `counted` false = the match does not count: the copy equals `before`.
        /// Returns the new rank; `lpChange` is the LP actually gained or lost (a loss at 10 LP is -10, not -20).</summary>
        public static CardRankData Apply(CardRankData before, DuelResult result, int oppMmr, bool counted, out int lpChange)
        {
            CardRankData r = Copy(before);
            lpChange = 0;
            if (!counted || result == DuelResult.Playing) return r;
            if (r.mmr <= 0) r.mmr = StartMmr;
            Change c = ChangeFor(result, r.mmr, oppMmr <= 0 ? StartMmr : oppMmr);
            r.mmr = Math.Max(1, r.mmr + c.Mmr); // never 0: only season == 0 means "never played"
            int lp = Math.Max(0, r.lp + c.Lp);
            lpChange = lp - r.lp;
            r.lp = lp;
            if (result == DuelResult.Win) r.wins++; else if (result == DuelResult.Lose) r.losses++; else r.draws++;
            r.best = Math.Max(r.best, r.lp);
            return r;
        }

        // ---- season rollover ----

        /// <summary>Brings `r` to the season of `utcMs`. A character that never played gets the start rating; one from an older season keeps a half of its distance from 1000 MMR and half its LP (per skipped season, at most 10),
        /// the best LP of the season that just ended goes to prevBest (0 when seasons were skipped) and its tier into the rewards flags (only if it played that season); a never-played character is season == 0 and nothing else. Returns true when something changed.</summary>
        public static bool EnsureSeason(CardRankData r, long utcMs)
        {
            int now = Season(utcMs);
            if (r.season == 0) { r.season = now; r.mmr = StartMmr; r.lp = 0; r.best = 0; return true; } // the only sign of "never played"
            bool changed = false;
            if (r.mmr < 1) { r.mmr = 1; changed = true; } // a rating is never 0 (an old save, or a state that was stripped)
            if (r.season >= now) return changed; // same season, or a clock that went back: leave the rest alone
            bool played = r.wins + r.losses + r.draws > 0;
            if (played) r.rewards |= 1 << (int)Tier(r.best);
            r.prevBest = played && now - r.season == 1 ? r.best : 0; // the season that just ended; a skipped one has no best
            int gap = Math.Min(MaxSkippedSeasons, now - r.season);
            for (int i = 0; i < gap; i++) { r.mmr = StartMmr + (r.mmr - StartMmr) / 2; r.lp /= 2; }
            r.season = now; r.wins = r.losses = r.draws = 0; r.best = r.lp;
            return true;
        }

        /// <summary>Strips the hidden rating (mmr = 0) from a save that is about to be sent to a client. `d` must be a capture (SaveSerializer.Capture copies the rank), so the server's own state keeps its MMR.</summary>
        public static void HideRating(SaveData d) { if (d != null && d.cardRank != null) d.cardRank.mmr = 0; }

        public static CardRankData Copy(CardRankData r)
        {
            return new CardRankData { season = r.season, mmr = r.mmr, lp = r.lp, wins = r.wins, losses = r.losses, draws = r.draws, best = r.best, prevBest = r.prevBest, rewards = r.rewards };
        }
    }
}
