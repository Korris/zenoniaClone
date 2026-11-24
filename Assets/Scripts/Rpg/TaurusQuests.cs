namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Taurus V4 newbie quests (BIBLE part 2 V). Q01 "go through the south gate" is folded into Rowan's offer (KISS). Q02 Elder Rowan: 5 Clover Slimes -> 5 Upgrade Stones + 300 G
    /// (stones feed Q03A: +1..+3 are sure successes at 1 stone each). Q03A Bram: the first successful enhance (QuestDef.Enhance), chained after Q02, 100 G.
    /// Rowan shows his "?" from the first second: the quest has no AutoStart and nothing before it.
    /// </summary>
    public static class TaurusQuests
    {
        public const string TrialId = "tau-meadow-trial", UpgradeId = "tau-first-upgrade", FirstDeckId = "tau-orin-first-deck";

        public static readonly QuestDef Trial = new QuestDef
        {
            Id = TrialId, Title = "A Trial in the Meadows", GiverNpcId = "elderRowan", GiverName = "Elder Rowan",
            Description = "Elder Rowan asks you to prove yourself on the meadows south of Willowbrook: defeat 5 Clover Slimes.",
            MonsterType = "tauCloverSlime", MonsterName = "Clover Slime", Count = 5, RewardGold = 300, RewardStones = 5,
            ReadyNotice = "Trial complete! Return to Elder Rowan in Willowbrook.",
            OfferLines = new[]
            {
                "Welcome to Willowbrook, young adventurer! Our little village sleeps under the old oak's protection.",
                "I see you already carry a weapon. Before you set out to explore Asteria, try your strength on the creatures of the meadows. Take the south gate.",
                "Do not worry, the Clover Slimes near the village are gentle. Defeat five of them and come back to me.",
            },
            ActiveLines = new[] { "The Clover Slimes wait beyond the south gate.\nDefeat five of them, then return to me." },
            ReadyLines = new[]
            {
                "Well done! You have found your feet in a fight.",
                "Here are some Upgrade Stones. Take them to Bram the Smith: a good adventurer tends to his gear.",
            },
            DoneLines = new[] { "Bram is on Smithy Lane, west of the square. Show him what you carry." },
        };

        public static readonly QuestDef FirstUpgrade = new QuestDef
        {
            Id = UpgradeId, Title = "First Upgrade", GiverNpcId = "tauBram", GiverName = "Bram the Smith", Requires = TrialId,
            Description = "Bram will show you how to strengthen your gear. Enhance your weapon or off-hand once.",
            Enhance = true, MonsterName = "Enhance a weapon or off-hand once", Count = 1, RewardGold = 100,
            ReadyNotice = "Upgrade done! Return to Bram the Smith.",
            OfferLines = new[]
            {
                "Ah, Upgrade Stones! Did Rowan hand those to you?",
                "Show me your gear. With a few stones and some gold, I can make it stronger.",
                "Open your bag, pick your weapon or off-hand and press Enhance. The first few levels never fail.",
            },
            ActiveLines = new[] { "Open your bag, choose your weapon or off-hand and press Enhance.\nThe first levels cannot fail." },
            ReadyLines = new[] { "Fine work! That edge will serve you well.", "Keep the stones you have left and enhance as you please. Here is a little gold for your trouble." },
            DoneLines = new[] { "Steel and leather, nothing fancy, but it holds an edge." },
        };

        /// <summary>The 5 Common cards of the starter deck (BIBLE Q03B): fixed, one copy each. The duel lends 3 more (DuelTutorialScript.PlayerDeck).</summary>
        public static readonly string[] StarterCards = { "tauCloverSlime", "tauHornRabbit", "tauReedFrog", "tauMossMushroom", "tauLeafMantis" };

        /// <summary>Q03B: after the meadow trial Orin's "!" appears; any 5 monsters -> the 5 starter cards. Not chained after Bram's upgrade quest (BIBLE: enhancing is not needed for Monster Cards).</summary>
        public static readonly QuestDef FirstDeck = new QuestDef
        {
            Id = FirstDeckId, Title = "Orin's First Deck", GiverNpcId = CardDuelQuest.GiverId, GiverName = "Card Master Orin", Requires = TrialId,
            Description = "Card Master Orin will give you a starter deck of Monster Cards if you defeat 5 monsters on the Clover Meadows.",
            MonsterType = QuestDef.AnyMonster, MonsterName = "any monster", Count = 5, RewardCards = StarterCards,
            ReadyNotice = "5 monsters down! Return to Card Master Orin for your starter deck.",
            OfferLines = new[]
            {
                "I heard from Rowan that you beat the slimes on the meadows. Well done!",
                "But strength is not the only thing that helps an adventurer. Have you heard of Monster Cards?",
                "Each card records the shape and strength of one creature. On the table, the four numbers on a card decide the result.",
                "Defeat 5 creatures on the Clover Meadows. Then I will give you a starter deck.",
            },
            ActiveLines = new[] { "Five creatures from the Clover Meadows, any kind will do.\nThen come back to me for your starter deck." },
            ReadyLines = new[]
            {
                "Excellent. The creatures of the meadows all have strong and weak sides. So do these cards.",
                "Here are five cards, yours to keep. Look them over in My Cards.",
            },
            DoneLines = new[] { "Look over your cards, then come back and we will play a game." },
        };

        /// <summary>Newbie quests + Elder Rowan mid-meadow chain (TaurusMeadowQuests, phase 05) + the shrine chain (TaurusShrineQuests, phase 06).</summary>
        public static readonly QuestDef[] All = Concat(Concat(new[] { Trial, FirstUpgrade, FirstDeck, CardDuelQuest.First }, TaurusMeadowQuests.All), TaurusShrineQuests.All);

        static QuestDef[] Concat(QuestDef[] a, QuestDef[] b)
        {
            var all = new QuestDef[a.Length + b.Length];
            a.CopyTo(all, 0); b.CopyTo(all, a.Length);
            return all;
        }
    }
}
