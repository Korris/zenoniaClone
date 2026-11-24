namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Taurus V4 phase 05: the mid-meadow main quests of Elder Rowan (BIBLE part2 VI). The BIBLE quests have several objectives; the quest system counts one thing, so each splits into a
    /// chain of one-objective quests (Requires): T-M03 "Trouble by the Brook" = 4 Reed Frogs -> 4 Moss Mushrooms -> touch the Brook Runestone; T-M04 "The Restless Forest" = 5 Bramble Boars ->
    /// 4 Leaf Mantises -> 3 Rootlings -> find the Ancient Roots. The two "touch" steps are WaypointId quests on quest-only touch points (DungeonPoints.TauRunestoneId / TauAncientRootsId).
    /// Rewards: Gold + Upgrade Stones scaled with the camp level (quest EXP has no system yet: pending-tasks). The camps follow one natural route south to north, one stop each.
    /// </summary>
    public static class TaurusMeadowQuests
    {
        const string Giver = "elderRowan", GiverName = "Elder Rowan";
        public const string FrogsId = "tau-brook-frogs", MushroomsId = "tau-brook-mushrooms", RunestoneId = "tau-brook-runestone";
        public const string BoarsId = "tau-forest-boars", MantisId = "tau-forest-mantis", RootlingsId = "tau-forest-rootlings", RootsId = "tau-forest-roots";

        public static readonly QuestDef Frogs = new QuestDef
        {
            Id = FrogsId, Title = "Trouble by the Brook", GiverNpcId = Giver, GiverName = GiverName, Requires = TaurusQuests.TrialId,
            Description = "Folk say the creatures by the brook north of the meadows act strangely. Defeat 4 Reed Frogs on the near bank.",
            MonsterType = "tauReedFrog", MonsterName = "Reed Frog", Count = 4, RewardGold = 150,
            ReadyNotice = "The frogs are quiet. Return to Elder Rowan in Willowbrook.",
            OfferLines = new[]
            {
                "Good, you are back on your feet. Now something else troubles me.",
                "Travellers say the creatures by the brook, north of the meadows, behave oddly. Frogs where there were none, mushrooms spitting at passers-by.",
                "Follow the road north to the brook and thin out the Reed Frogs first. Look at them closely.",
            },
            ActiveLines = new[] { "The Reed Frogs live on the near bank of the brook, north of the meadows.\nFour of them, then come back." },
            ReadyLines = new[] { "Four frogs, and nothing wrong with them that I can see. Hm. Take this for your trouble.", "Next, the mushroom grove on the far side of the brook." },
            DoneLines = new[] { "The brook is north of the meadows. The wooden bridge is on the road." },
        };

        public static readonly QuestDef Mushrooms = new QuestDef
        {
            Id = MushroomsId, Title = "Spores Across the Brook", GiverNpcId = Giver, GiverName = GiverName, Requires = FrogsId,
            Description = "Cross the wooden bridge and defeat 4 Moss Mushrooms in the grove beyond the brook.",
            MonsterType = "tauMossMushroom", MonsterName = "Moss Mushroom", Count = 4, RewardGold = 200,
            ReadyNotice = "The grove is calmer. Return to Elder Rowan in Willowbrook.",
            OfferLines = new[]
            {
                "The frogs were not the cause. Cross the wooden bridge, there is a grove of Moss Mushrooms on the far bank.",
                "They spit spores from a distance: do not stand still in front of them. Defeat four.",
            },
            ActiveLines = new[] { "Cross the brook on the wooden bridge. The Moss Mushrooms stand in the grove to the east of the road.\nStep aside when they spit." },
            ReadyLines = new[] { "Spores and moss, as always. Still, something about them was wrong, you say?", "There is an old stone by the water. Look at it, and tell me what you feel." },
            DoneLines = new[] { "The stone stands on the north bank, west of the road." },
        };

        public static readonly QuestDef Runestone = new QuestDef
        {
            Id = RunestoneId, Title = "The Brook Runestone", GiverNpcId = Giver, GiverName = GiverName, Requires = MushroomsId,
            Description = "An old runestone stands on the north bank of the brook, west of the road. Walk up to it.",
            WaypointId = DungeonPoints.TauRunestoneId, MonsterName = "Examine the Brook Runestone", Count = 1, RewardGold = 250, RewardStones = 2,
            ReadyNotice = "The runestone glows faintly. Return to Elder Rowan in Willowbrook.",
            OfferLines = new[]
            {
                "On the north bank, west of the road, an old stone leans over the water. Its runes are older than the village.",
                "Walk up to it. I want to know whether it still hums.",
            },
            ActiveLines = new[] { "The runestone is on the north bank of the brook, west of the road.\nWalk up to it." },
            ReadyLines = new[] { "It glows? Even for a moment? Then the old magic under these meadows is stirring.", "Take these stones. There is more to the forest in the north, and I fear it." },
            DoneLines = new[] { "The forest in the north has become restless. Rest first." },
        };

        public static readonly QuestDef Boars = new QuestDef
        {
            Id = BoarsId, Title = "The Restless Forest", GiverNpcId = Giver, GiverName = GiverName, Requires = RunestoneId,
            Description = "Merchants say the Bramble Boars of the thicket have turned vicious. Defeat 5 of them in the Bramble Thicket, north-west of the bridge.",
            MonsterType = "tauBrambleBoar", MonsterName = "Bramble Boar", Count = 5, RewardGold = 300,
            ReadyNotice = "The boars are thinned. Return to Elder Rowan in Willowbrook.",
            OfferLines = new[]
            {
                "Merchants on the north road say the Bramble Boars have turned vicious, and the Rootlings come out of the trees near the path.",
                "I suspect the old roots under the forest are out of balance. Begin with the boars in the thicket, north-west of the bridge. Defeat five.",
            },
            ActiveLines = new[] { "The Bramble Boars roam the thicket, north-west of the bridge. Keep to the road and take them one at a time." },
            ReadyLines = new[] { "Five boars. That is a relief, yet the thicket is still not safe.", "Mantises hide deeper in the forest. Take this and go carefully." },
            DoneLines = new[] { "Mind the thicket's far corner: the old sow of the brambles keeps her nest there." },
        };

        public static readonly QuestDef Mantis = new QuestDef
        {
            Id = MantisId, Title = "Blades in the Leaves", GiverNpcId = Giver, GiverName = GiverName, Requires = BoarsId,
            Description = "Leaf Mantises hide in the thicket and along the forest edge. Defeat 4 of them.",
            MonsterType = "tauLeafMantis", MonsterName = "Leaf Mantis", Count = 4, RewardGold = 350,
            ReadyNotice = "The mantises are gone. Return to Elder Rowan in Willowbrook.",
            OfferLines = new[]
            {
                "Leaf Mantises are quick and cut deep. They hide in the thicket and along the edge of the old forest.",
                "Defeat four of them. Strike while they wind up, and keep moving.",
            },
            ActiveLines = new[] { "Leaf Mantises: the deeper corner of the Bramble Thicket and the Elderwood Edge beyond." },
            ReadyLines = new[] { "Four mantises. The paths are safer already.", "Only the Rootlings remain, closer to the old trees." },
            DoneLines = new[] { "The Rootlings stand where the forest grows old." },
        };

        public static readonly QuestDef Rootlings = new QuestDef
        {
            Id = RootlingsId, Title = "Roots on the Path", GiverNpcId = Giver, GiverName = GiverName, Requires = MantisId,
            Description = "Rootlings, walking roots, have begun to appear near the road at the Elderwood Edge. Defeat 3 of them.",
            MonsterType = "tauRootling", MonsterName = "Rootling", Count = 3, RewardGold = 400,
            ReadyNotice = "The Rootlings are down. Return to Elder Rowan in Willowbrook.",
            OfferLines = new[]
            {
                "Rootlings: living roots with faces. They are slow but hit hard, and they should not be walking at all.",
                "Defeat three at the Elderwood Edge, at the north of the road.",
            },
            ActiveLines = new[] { "The Rootlings stand at the Elderwood Edge, north of the thicket.\nThey are slow: hit and step away." },
            ReadyLines = new[] { "Three Rootlings. Then the roots truly are stirring.", "One last thing: find the Ancient Roots at the end of the road, and tell me what you see." },
            DoneLines = new[] { "The Ancient Roots are at the very north of the meadows, beside the road to the old stone arch." },
        };

        public static readonly QuestDef Roots = new QuestDef
        {
            Id = RootsId, Title = "The Ancient Roots", GiverNpcId = Giver, GiverName = GiverName, Requires = RootlingsId,
            Description = "Follow the road to the far north of the meadows and find the Ancient Roots beside the old stone arch.",
            WaypointId = DungeonPoints.TauAncientRootsId, MonsterName = "Find the Ancient Roots", Count = 1, RewardGold = 500, RewardStones = 3,
            ReadyNotice = "You found the Ancient Roots. Return to Elder Rowan in Willowbrook.",
            OfferLines = new[]
            {
                "The road ends at the Ancient Roots, beside an arch of old stone nobody has opened in my lifetime.",
                "Go there and put your hand to the roots. Beware: the Thorn Sprites and the Barklings guard it.",
            },
            ActiveLines = new[] { "Follow the road north to the Ancient Roots. The old stone arch stands beyond them." },
            ReadyLines = new[] { "You touched the roots. And?", "...An arch that hums. Then it is time. Take this, you have earned it." },
            DoneLines = new[] { "Whatever sleeps under the old arch has waited a long time. We shall see." },
        };

        public static readonly QuestDef[] All = { Frogs, Mushrooms, Runestone, Boars, Mantis, Rootlings, Roots };
    }
}
