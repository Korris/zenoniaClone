namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// "The First Card Duel": Card Master Orin (Oakhaven) lends the newcomer a deck for one scripted duel (DuelTutorialScript), then pays 300 G and 3 common cards once.
    /// Offered only after the character owns its first card (NeedsCard). Finished by the zone / offline host when the tutorial duel ends (PlayerRpg.QuestCardDuel); a duel given up counts like a loss.
    /// The 3 cards have different strong edges (Right / Bottom / Top). Listed last in QuestCatalog.All.
    /// </summary>
    public static class CardDuelQuest
    {
        /// <summary>What the end banner of a duel with Orin says about the quest: HandIn = tutorial played out / given up (the quest is ready), CutShort = the tutorial was dropped (death, hostile map, idle) and can be played again, Practice = nothing at stake.</summary>
        public enum EndNote { Practice, HandIn, CutShort }

        public static EndNote NoteFor(QuestState first)
        {
            return first == QuestState.ReadyToTurnIn ? EndNote.HandIn : first == QuestState.Active ? EndNote.CutShort : EndNote.Practice;
        }

        public const string Id = "first-card-duel", GiverId = "cardMaster";

        public static readonly QuestDef First = new QuestDef
        {
            Id = Id, Title = "The First Card Duel", GiverNpcId = GiverId, GiverName = "Card Master Orin",
            Description = "Card Master Orin will lend you a deck of common cards for a first duel. Play it, then bring the deck back.",
            CardDuel = true, NeedsCard = true, MonsterName = "Play a card duel with Orin", Count = 1, RewardGold = 300,
            RewardCards = new[] { "hornRabbit", "thornBoar", "forestWolf" },
            ReadyNotice = "Duel finished! Return the deck to Card Master Orin.",
            OfferLines = new[]
            {
                "A card in your pocket already? Then you are ready to duel.",
                "Let me lend you a deck of 8 common cards. We play one match on a 4x4 board, and I will show you each trick as we go.",
            },
            ActiveLines = new[] { "Ready for our duel? Talk to me again and pick Duel when you are." },
            ReadyLines = new[]
            {
                "Well played! Now give me back my deck.",
                "Remember the stakes in a real duel against another player: the winner takes 1 card from the loser's deck. It unlocks at Lv30, and a draw costs nobody a card.",
                "Here is a small reward, and three cards of your own to start your deck.",
            },
            DoneLines = new[] { "Come back any time for a practice duel. No cards are ever lost against me." },
        };
    }
}
