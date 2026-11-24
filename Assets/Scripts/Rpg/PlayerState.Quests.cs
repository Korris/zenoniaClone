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
            if (!Quests.Unlocked(q)) return ActionResult.Fail("Finish the quest before it first");
            if (!Quests.Accept(q)) return ActionResult.Fail("Quest already taken");
            if (q.WaypointId != null && HasWaypoint(q.WaypointId)) Quests.CountReach(q.WaypointId); // the crystal was attuned before the quest was taken
            if (q.Enhance && HoldsEnhancedPiece()) Quests.CountEnhance(); // enhanced before taking the quest: it counts at once (BIBLE Q03A)
            return ActionResult.Success();
        }

        /// <summary>A weapon or off-hand with at least +1, in the bag or worn.</summary>
        public bool HoldsEnhancedPiece()
        {
            foreach (Item i in Inventory) if (EnhanceStats.IsEnhanceable(i) && i.enhance >= 1) return true;
            foreach (string slot in Slots.All) { Item e = GetEquipped(slot); if (e != null && EnhanceStats.IsEnhanceable(e) && e.enhance >= 1) return true; }
            return false;
        }

        /// <summary>Hands the quest in and pays its reward, in one step and only once. `pick` = index into QuestDef.RewardPickCards for a quest that lets the player choose a card (ignored by the others).</summary>
        public ActionResult TurnInQuest(string questId, int pick = 0)
        {
            QuestDef q = QuestCatalog.Get(questId);
            if (q == null) return ActionResult.Fail("No such quest");
            if (q.RewardPickCards != null && (pick < 0 || pick >= q.RewardPickCards.Length)) return ActionResult.Fail("Choose a reward card"); // nothing closes: the index is checked against the server's own list
            if (q.RewardStarterHat && Quests.StateOf(q) == QuestState.ReadyToTurnIn && Inventory.Count >= InventorySize) return ActionResult.Fail("Inventory full"); // the gift needs a bag slot: nothing closes
            if (!Quests.Close(q)) return ActionResult.Fail(Quests.StateOf(q) == QuestState.Done ? "Quest already completed" : "Quest not finished yet");
            if (q.RewardGold > 0) ApplyPickup(new LootEntry { Kind = LootKind.Coin, Value = q.RewardGold });
            if (q.RewardStones > 0) ApplyPickup(new LootEntry { Kind = LootKind.UpgradeStone, Value = q.RewardStones }); // a 0 entry would still pay 1 stone
            for (int i = 0; i < q.RewardHpPotions; i++) ApplyPickup(new LootEntry { Kind = LootKind.HpPotion, Value = 1 });
            if (q.RewardCards != null) foreach (string id in q.RewardCards) AddCard(id); // a stack at the cap just stays there; Add makes the Album mark
            if (q.RewardPickCards != null) AddCard(q.RewardPickCards[pick]); // the quest closed just above, so the pick is paid exactly once
            if (q.RewardStarterHat) // fixed uid: the quest pays once, so it never collides
            {
                HeadgearDef hat = HeadgearCatalog.StarterFor(ClassId);
                Inventory.Add(HeadgearCatalog.CreateItem(hat, null, hat.TemplateId + "-gift"));
            }
            return ActionResult.Success();
        }
    }
}
