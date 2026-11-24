namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Taurus V4 phase 06: T-M05 "The Heart of the Ancient Roots" (BIBLE part2 VI), Elder Rowan's last chain of the chapter, after "The Ancient Roots" (TaurusMeadowQuests.RootsId). The quest system counts one
    /// objective, so it is three one-objective quests (Requires): step into the shrine (attune the entrance crystal) -> touch the Memory Amber (QuestOnly touch point in Area 02) -> defeat the Elder Root Guardian.
    /// The BIBLE's fourth step (report to Rowan) is the turn-in of the third: Rowan's ready lines carry the 13th-constellation scene and Orin's hint (no extra quest with nothing to do). The Memory Amber's
    /// short vision is the ready notice of step 2 (the chat line when the amber is touched); no cutscene (plan: a dialogue panel + one ChatGPT picture later). The Memory Amber Fragment is the quest flag
    /// (Done state), not an item. Boss kill credit is the usual shared one, the reward is paid once (a quest is Done once).
    /// </summary>
    public static class TaurusShrineQuests
    {
        const string Giver = "elderRowan", GiverName = "Elder Rowan";
        public const string EnterId = "tau-shrine-enter", AmberId = "tau-shrine-amber", GuardianId = "tau-shrine-guardian";

        public static readonly QuestDef Enter = new QuestDef
        {
            Id = EnterId, Title = "Under the Old Arch", GiverNpcId = Giver, GiverName = GiverName, Requires = TaurusMeadowQuests.RootsId,
            Description = "Go through the old stone arch at the far north of the Clover Meadows and step into the Ancient Root Shrine.",
            WaypointId = DungeonPoints.TauShrineGateId, MonsterName = "Enter the Ancient Root Shrine", Count = 1, RewardGold = 400, RewardStones = 2,
            ReadyNotice = "You are inside the shrine. Return to Elder Rowan in Willowbrook.",
            OfferLines = new[]
            {
                "The arch hums, you said. Then the shrine below the oak is awake, and the oak has guarded it since before our village.",
                "Go through the arch and step inside. Touch the crystal by the entrance, so the shrine knows you. Rootlings and Barklings roam the first hall.",
            },
            ActiveLines = new[] { "The old stone arch is at the north end of the Meadows road.\nTouch the crystal just inside." },
            ReadyLines = new[] { "You were inside? And you came back whole. Good.", "Take this. Deeper in there is a great amber the old folk spoke of. Go and look at it." },
            DoneLines = new[] { "The Amber Memory Hall lies beyond the first hall." },
        };

        public static readonly QuestDef Amber = new QuestDef
        {
            Id = AmberId, Title = "The Memory Amber", GiverNpcId = Giver, GiverName = GiverName, Requires = EnterId,
            Description = "Cross the first hall to the Amber Memory Hall and touch the great Memory Amber in its middle.",
            WaypointId = DungeonPoints.TauMemoryAmberId, MonsterName = "Touch the Memory Amber", Count = 1, RewardGold = 500, RewardStones = 3,
            ReadyNotice = "The Memory Amber showed you a vision. Return to Elder Rowan in Willowbrook.",
            OfferLines = new[]
            {
                "The Amber Memory Hall is the second room. Spore-casters and Thorn Sprites keep it, and slow bark guards. Mind the shooters first.",
                "In the middle of the hall stands the Memory Amber. Touch it and tell me what you see.",
            },
            ActiveLines = new[] { "The Memory Amber stands in the middle of the Amber Memory Hall, the second room of the shrine." },
            ReadyLines = new[] { "Thirteen stars... and a keeper that lost its way? So the amber remembers.", "The keeper sleeps beyond the Rune Bark Guardians. Rest, then go." },
            DoneLines = new[] { "The Heartroot Sanctuary lies beyond the three guardians." },
        };

        public static readonly QuestDef Guardian = new QuestDef
        {
            Id = GuardianId, Title = "The Elder Root Guardian", GiverNpcId = Giver, GiverName = GiverName, Requires = AmberId,
            Description = "The Elder Root Guardian has forgotten friend from foe. Defeat it in the Heartroot Sanctuary at the end of the shrine.",
            MonsterType = "tauElderRootGuardian", MonsterName = "Elder Root Guardian", Count = 1, RewardGold = 1200, RewardStones = 5,
            ReadyNotice = "The Elder Root Guardian is down. Return to Elder Rowan in Willowbrook.",
            OfferLines = new[]
            {
                "The Elder Root Guardian was made to protect the amber. Now the memories inside are unsettled and it no longer knows who comes to help.",
                "Pass the three Rune Bark Guardians into the Heartroot Sanctuary. It sweeps in a wide fan and pulses a ring round its body: watch the warnings on the ground and step out. When its core blazes it fights faster.",
                "There is a crystal before the boss hall: if you fall, you will wake at the entrance and can warp straight back.",
            },
            ActiveLines = new[] { "Beyond the three Rune Bark Guardians is the Heartroot Sanctuary. Read the warnings on the ground." },
            ReadyLines = new[]
            {
                "You did it. The keeper rests, and the amber is quiet again.",
                "That vision... thirteen stars, and the thirteenth hidden from our sky. The old oak knew. Card Master Orin keeps old records, he may tell you more. Take this, you have earned it.",
            },
            DoneLines = new[] { "Thirteen stars. We shall learn what the thirteenth means." },
        };

        public static readonly QuestDef[] All = { Enter, Amber, Guardian };
    }
}
