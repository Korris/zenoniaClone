using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>Save shapes of the card system (JsonUtility, field names = server-api stateSchema). Cards are stored as counts per id: V1 cards have no per-copy data.</summary>
    [Serializable] public class CardEntry { public string id; public int n; public int locked; } // locked = copies the player locked (Lock Card): not sold, converted, wagered or put in the duel deck; 0..n, missing in older v6 saves = 0

    /// <summary>A stack of Soul Gems of one card and kind (0 Power, 1 Guard). A gem that was ever socketed is locked (no trade) for good.</summary>
    [Serializable] public class GemEntry { public string id; public int kind; public bool locked; public int n; }

    /// <summary>Daily card counters; day = WantedBoss.DayKey, the counters restart when it changes. wagers is for the card-wager phase.</summary>
    [Serializable] public class CardDailyData { public int day, npcWins, pvpRewards, wagers; }

    /// <summary>Ranked season state (phase 07): season number, rating, league points and the win / loss record.</summary>
    [Serializable] public class CardRankData { public int season, mmr, lp, wins, losses, draws, best; }
}
