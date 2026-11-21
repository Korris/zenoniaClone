namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// "Secrets Beneath Rootburrow" (ChatGPT V5, docs/reference/rootburrow-dark-25d/floors-purpose-chatgpt.md section 4): the reason to go all the way down. Elara, a cave
    /// researcher outside Rootburrow's gate in Bramblewood Forest, sends the hero to the 2F warp crystal, then after the 2F-only Gloom Wolves, then to the
    /// Ambercap Warden on 3F (any hero who helped the kill gets the credit, KillRewards). One objective per step (the quest system counts one thing);
    /// rewards in gold / stones / potions (quest EXP is a follow-up). Listed in QuestCatalog.All.
    /// </summary>
    public static class RootburrowQuests
    {
        const string Giver = "caveResearcherElara", GiverName = "Elara";

        public static readonly QuestDef Hollow = new QuestDef
        {
            Id = "rootburrow-hollow", Title = "Into the Hollow", GiverNpcId = Giver, GiverName = GiverName,
            Description = "Elara needs someone to reach Rootburrow's second floor. Find the stairs down and attune the warp crystal on 2F.",
            WaypointId = "rootburrow2", MonsterName = "Attune the Rootburrow 2F warp crystal", Count = 1, RewardGold = 800, RewardHpPotions = 3,
            OfferLines = new[]
            {
                "You there! I am Elara, I study the old crystal caves. Rootburrow goes much deeper than the village folk think.",
                "Go down the stairs at the far end of the first floor and touch the warp crystal on 2F. Once it knows you, it will carry you back and forth.",
            },
            ActiveLines = new[] { "The stairs down are at the far north-west of the first floor.\nTouch the glowing crystal right past them." },
            ReadyLines = new[] { "You felt it hum, did you not? The crystals remember everyone who touches them.", "Here, for your trouble. Rest, then I have more for you." },
            DoneLines = new[] { "The crystals are linked floor to floor. Use them, the walk is long." },
        };

        public static readonly QuestDef Amethyst = new QuestDef
        {
            Id = "rootburrow-amethyst", Title = "Echoes of Amethyst", GiverNpcId = Giver, GiverName = GiverName, Requires = Hollow.Id,
            Description = "Gloom Wolves prowl Rootburrow 2F and nowhere else. Thin them out so Elara can reach the deeper crystals.",
            MonsterType = "gloomWolf", MonsterName = "Gloom Wolf", Count = 6, RewardGold = 1500, RewardStones = 2,
            OfferLines = new[]
            {
                "Something howls on the second floor. Gloom Wolves, they only live down there, drawn by the amethyst light.",
                "Defeat six of them. I want to see what they are guarding.",
            },
            ActiveLines = new[] { "The Gloom Wolves den in the caves of Rootburrow 2F." },
            ReadyLines = new[] { "Six! The deep path should be quieter now.", "Take these stones. You will need sharp steel for what lives on 3F." },
            DoneLines = new[] { "Thank you. Now, about the third floor..." },
        };

        public static readonly QuestDef Warden = new QuestDef
        {
            Id = "rootburrow-warden", Title = "The Mushroom Warden", GiverNpcId = Giver, GiverName = GiverName, Requires = Amethyst.Id,
            Description = "A giant amber mushroom, the Ambercap Warden, guards the heart of Rootburrow 3F. Defeat it (helping a party counts).",
            MonsterType = "ambercapWarden", MonsterName = "Ambercap Warden", Count = 1, RewardGold = 3000, RewardStones = 5,
            OfferLines = new[]
            {
                "The deepest floor is ruled by the Ambercap Warden, a mushroom as old as the roots themselves.",
                "Bring it down and the way to Thornheart will be safe. Friends are welcome, every hand that strikes it counts.",
            },
            ActiveLines = new[] { "The Warden waits in the great chamber in the middle of Rootburrow 3F.\nIt returns five minutes after it falls." },
            ReadyLines = new[] { "The Warden has fallen? Then Rootburrow is yours to explore.", "This is all my expedition fund. You have earned every coin." },
            DoneLines = new[] { "The deep crystals still hide chests nobody has opened. Keep looking!" },
        };
    }
}
