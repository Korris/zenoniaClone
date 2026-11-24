namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// "The First Card Duel" (Taurus V4, BIBLE Q04): Card Master Orin lends 3 cards to the 5 starter cards for one scripted duel (DuelTutorialScript). The quest only moves when the player WINS
    /// (PlayerRpg.QuestCardDuel is called by the zone / offline host on a win, never on a loss, draw, give-up or dropped duel: those replay for free). At the turn-in the player picks ONE of Orin's 8 Common cards (RewardPickCards).
    /// Offered once "Orin's First Deck" is Done (TaurusQuests.FirstDeck).
    /// </summary>
    public static class CardDuelQuest
    {
        /// <summary>What the end banner of a duel with Orin says about the quest: HandIn = won, the quest is ready (choose the reward), CutShort = not won (lost, drawn, given up, dropped): play again for free, Practice = nothing at stake.</summary>
        public enum EndNote { Practice, HandIn, CutShort }

        public static EndNote NoteFor(QuestState first)
        {
            return first == QuestState.ReadyToTurnIn ? EndNote.HandIn : first == QuestState.Active ? EndNote.CutShort : EndNote.Practice;
        }

        public const string Id = "first-card-duel", GiverId = "cardMaster";

        public static readonly QuestDef First = new QuestDef
        {
            Id = Id, Title = "The First Card Duel", GiverNpcId = GiverId, GiverName = "Card Master Orin", Requires = TaurusQuests.FirstDeckId,
            Description = "Win a practice duel against Card Master Orin. He lends you 3 cards, and the winner picks one of his cards.",
            CardDuel = true, MonsterName = "Win a card duel against Orin", Count = 1,
            RewardPickCards = DuelTutorialScript.OrinDeck,
            ReadyNotice = "You won! Return to Card Master Orin and choose your reward card.",
            OfferLines = new[]
            {
                "You have your first deck now!",
                "But owning good cards is not enough. What matters is where you place them.",
                "Shall we play a game? It is only practice: you lose nothing, even if you lose the duel.",
            },
            ActiveLines = new[] { "Ready for our duel? Talk to me again and pick Duel when you are.\nIf you lose, you can try again as often as you like." },
            ReadyLines = new[]
            {
                "Well played! You understand how to place a card and win.",
                "Choose one card from my practice deck, it is yours to keep.",
            },
            DoneLines = new[] { "Come back any time for a practice duel. No cards are ever lost against me." },
        };
    }
}
