namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Monster Album milestones (Taurus V4: 3 / 6 / 9 / 12 different Taurus cards ever registered, 12 = the whole live collection; the Field Boss card sits OUTSIDE the 12, it is not IsLive). Each is claimed once per
    /// character (a bit in AlbumClaimed, kept even if the cards are sold or traded). Rewards (BIBLE Q06, ChatGPT 24/11): 150 G + 2 stones, 300 G + 3, 500 G + 5, 1000 G + 5 + the title "Taurus Card Collector".
    /// </summary>
    public static class AlbumRules
    {
        public static readonly int[] Milestones = { 3, 6, 9, 12 };
        public static readonly int[] Gold = { 150, 300, 500, 1000 };
        public static readonly int[] Stones = { 2, 3, 5, 5 };
        /// <summary>Milestone index whose claim bit is the title flag "Taurus Card Collector" (there is no title system yet: the claimed bit IS the flag; TODO show it when titles exist).</summary>
        public const int CollectorTitle = 3;
        public const string CollectorTitleName = "Taurus Card Collector";

        public static bool Reached(CardCollection c, int milestone) { return Valid(milestone) && c.AlbumCount >= Milestones[milestone]; }
        public static bool Claimed(CardCollection c, int milestone) { return Valid(milestone) && (c.AlbumClaimed & (1 << milestone)) != 0; }
        static bool Valid(int milestone) { return milestone >= 0 && milestone < Milestones.Length; }

        public static string RewardText(int milestone)
        {
            if (!Valid(milestone)) return "";
            return Gold[milestone] + " G + " + Stones[milestone] + " stones" + (milestone == CollectorTitle ? " + title " + CollectorTitleName : "");
        }

        /// <summary>Claims a milestone: it must be reached and not claimed yet. Gold and stones go to the hero in the same step; refused (the bit stays free) when either would be cut by its cap.</summary>
        public static ActionResult Claim(PlayerState p, int milestone)
        {
            CardCollection c = p.Cards;
            if (!Valid(milestone)) return ActionResult.Fail("No such milestone");
            if (Claimed(c, milestone)) return ActionResult.Fail("Already claimed");
            if (!Reached(c, milestone)) return ActionResult.Fail("Collect " + Milestones[milestone] + " different cards first");
            if ((long)p.Gold + Gold[milestone] > PlayerState.GoldCap) return ActionResult.Fail("Your Gold is full"); // refused before the bit is set, like Sell: the reward is never cut
            if (p.UpgradeStone + Stones[milestone] > PlayerState.MaxStones) return ActionResult.Fail("Your Upgrade Stones are full");
            c.AlbumClaimed |= 1 << milestone;
            p.AddGoldCapped(Gold[milestone]);
            p.UpgradeStone += Stones[milestone];
            return ActionResult.Success("Album reward: +" + RewardText(milestone));
        }
    }
}
