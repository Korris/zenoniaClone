namespace ZenoniaProto.Rpg
{
    public sealed partial class PlayerState
    {
        public readonly QuestLog Quests = new QuestLog();

        /// <summary>Success has no text: the client tells the player from the state it receives (QuestTracker), online and offline alike.</summary>
        public ActionResult AcceptQuest(string questId)
        {
            QuestDef q = QuestCatalog.Get(questId);
            if (q == null) return ActionResult.Fail("No such quest");
            return Quests.Accept(q) ? ActionResult.Success() : ActionResult.Fail("Quest already taken");
        }

        /// <summary>Hands the quest in and pays its reward, in one step and only once.</summary>
        public ActionResult TurnInQuest(string questId)
        {
            QuestDef q = QuestCatalog.Get(questId);
            if (q == null) return ActionResult.Fail("No such quest");
            if (!Quests.Close(q)) return ActionResult.Fail(Quests.StateOf(q) == QuestState.Done ? "Quest already completed" : "Quest not finished yet");
            ApplyPickup(new LootEntry { Kind = LootKind.Coin, Value = q.RewardGold });
            ApplyPickup(new LootEntry { Kind = LootKind.UpgradeStone, Value = q.RewardStones });
            for (int i = 0; i < q.RewardHpPotions; i++) ApplyPickup(new LootEntry { Kind = LootKind.HpPotion, Value = 1 });
            return ActionResult.Success();
        }
    }
}
