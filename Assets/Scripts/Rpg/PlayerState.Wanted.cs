namespace ZenoniaProto.Rpg
{
    /// <summary>PlayerState: the daily Wanted Boss claim. Reward and flag change together, so one save carries both (a crash loses both or neither).</summary>
    public sealed partial class PlayerState
    {
        /// <summary>WantedBoss.DayKey of the last claim; 0 = never. Saved in SaveData.wantedDay. Offline never sets it.</summary>
        public int WantedDay;

        /// <summary>Pays today's reward once per day. Fails (and changes nothing) when `dayKey` was already claimed.</summary>
        public ActionResult ClaimWanted(int dayKey, WantedBossDef def)
        {
            if (def == null || dayKey <= 0) return ActionResult.Fail("No wanted boss");
            if (WantedDay >= dayKey) return ActionResult.Fail("Wanted reward already claimed today");
            ApplyPickup(new LootEntry { Kind = LootKind.Coin, Value = def.Gold });
            ApplyPickup(new LootEntry { Kind = LootKind.UpgradeStone, Value = def.Stones });
            AddPotions(true, def.PotionSize, def.HpPotions);
            AddPotions(false, def.PotionSize, def.MpPotions);
            WantedDay = dayKey;
            return ActionResult.Success(WantedBoss.RewardText(def));
        }
    }
}
