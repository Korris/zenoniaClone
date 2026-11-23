using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>Save shapes of the card system (JsonUtility, field names = server-api stateSchema). Cards are stored as counts per id: V1 cards have no per-copy data.</summary>
    [Serializable] public class CardEntry { public string id; public int n; public int locked; } // locked = 1 when the player locked this KIND (Lock Card): its copies are not sold, converted or wagered (the duel deck may still use them); 0 / missing = open

    /// <summary>A stack of Soul Gems of one card and kind (0 Power, 1 Guard). A gem that was ever socketed is locked (no trade) for good.</summary>
    [Serializable] public class GemEntry { public string id; public int kind; public bool locked; public int n; }

    /// <summary>Daily card counters; day = WantedBoss.DayKey, the counters restart when it changes. wagers is for the card-wager phase.</summary>
    [Serializable] public class CardDailyData { public int day, npcWins, pvpRewards, wagers; }

    /// <summary>Ranked season state (phase 07, RankedRules): season number, hidden rating, league points, this season's record, best LP this season, best LP of the last finished season, and a bit per tier reached at a season end (season-end rewards are only this flag for now).</summary>
    [Serializable] public class CardRankData { public int season, mmr, lp, wins, losses, draws, best, prevBest, rewards; }
}
