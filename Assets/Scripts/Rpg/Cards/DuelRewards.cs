namespace ZenoniaProto.Rpg
{
    public struct DuelReward
    {
        public int FirstWinGold, DailyGold;
        /// <summary>First NPC win ever: the Ranked tutorial is now done.</summary>
        public bool TutorialDone;
        public int Total { get { return FirstWinGold + DailyGold; } }
    }

    /// <summary>
    /// Pays a finished NPC duel. Only a WIN pays: the first win against each NPC (that NPC's gold, once, kept in CardCollection.NpcFirstWins) and the daily task "win 3 NPC duels" (500 G the moment
    /// the third win of the day lands; the day is WantedBoss.DayKey, counters restart when it changes). A loss, a draw and a dropped match pay nothing. Gold stops at PlayerState.GoldCap.
    /// </summary>
    public static class DuelRewards
    {
        public static DuelReward ApplyNpcResult(PlayerState p, int npc, DuelResult result, int dayKey)
        {
            var reward = new DuelReward();
            DuelNpcDef def = DuelNpcTable.Get(npc);
            if (def == null || result != DuelResult.Win) return reward;
            CardCollection c = p.Cards;
            if (c.Daily.day != dayKey) c.Daily = new CardDailyData { day = dayKey };
            int bit = DuelNpcTable.FirstWinBit(npc);
            if ((c.NpcFirstWins & bit) == 0 && (long)p.Gold + def.FirstWinGold <= PlayerState.GoldCap) // near the Gold cap the reward is held, bit unset, and paid by a later win (like Album / Sell: never cut)
            {
                reward.TutorialDone = c.NpcFirstWins == 0;
                c.NpcFirstWins |= bit;
                reward.FirstWinGold = def.FirstWinGold;
            }
            c.Daily.npcWins++;
            if (c.Daily.npcWins == DuelNpcTable.DailyWinsNeeded) reward.DailyGold = DuelNpcTable.DailyBonusGold;
            p.AddGoldCapped(reward.Total);
            return reward;
        }

        public const int PvpWinGold = 300, PvpLoseGold = 100, PvpDrawGold = 150, PvpRewardedPerDay = 5;

        /// <summary>
        /// Pays a finished PvP duel: win 300 / lose 100 / draw 150 G, but only 5 matches a day pay at all (the counter is CardDailyData.pvpRewards, restarted with the day key). A side that gave up or was dropped
        /// (`left`) gets nothing and does not use up a paid match. Gold stops at the cap. Returns the gold paid (DailyGold of the reward).
        /// </summary>
        public static DuelReward ApplyPvpResult(PlayerState p, DuelResult result, bool left, int dayKey)
        {
            var reward = new DuelReward();
            if (left || result == DuelResult.Playing) return reward;
            CardCollection c = p.Cards;
            if (c.Daily.day != dayKey) c.Daily = new CardDailyData { day = dayKey };
            if (c.Daily.pvpRewards >= PvpRewardedPerDay) return reward;
            int gold = result == DuelResult.Win ? PvpWinGold : result == DuelResult.Draw ? PvpDrawGold : PvpLoseGold;
            c.Daily.pvpRewards++;
            reward.DailyGold = gold;
            p.AddGoldCapped(gold);
            return reward;
        }
    }
}
