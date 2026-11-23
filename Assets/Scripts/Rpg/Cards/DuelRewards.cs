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
            if ((c.NpcFirstWins & bit) == 0)
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
    }
}
