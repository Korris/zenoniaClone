namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Monster Album milestones (10 / 20 / 30 / 40 different cards ever owned). Each is claimed once per character: 10 pays gold now,
    /// 20 / 30 / 40 only set a flag (title Card Hunter, portrait frame, Monster Master look; the cosmetics are not in the game yet).
    /// </summary>
    public static class AlbumRules
    {
        public static readonly int[] Milestones = { 10, 20, 30, 40 };
        public const int Gold10 = 5000;
        public const int CardHunter = 1, PortraitFrame = 2, MonsterMaster = 3; // milestone indexes of the three cosmetic flags

        public static bool Reached(CardCollection c, int milestone) { return Valid(milestone) && c.AlbumCount >= Milestones[milestone]; }
        public static bool Claimed(CardCollection c, int milestone) { return Valid(milestone) && (c.AlbumClaimed & (1 << milestone)) != 0; }
        static bool Valid(int milestone) { return milestone >= 0 && milestone < Milestones.Length; }

        /// <summary>Claims a milestone: it must be reached and not claimed yet. The gold of milestone 10 goes to the hero in the same step.</summary>
        public static ActionResult Claim(PlayerState p, int milestone)
        {
            CardCollection c = p.Cards;
            if (!Valid(milestone)) return ActionResult.Fail("No such milestone");
            if (Claimed(c, milestone)) return ActionResult.Fail("Already claimed");
            if (!Reached(c, milestone)) return ActionResult.Fail("Collect " + Milestones[milestone] + " different cards first");
            if (milestone == 0 && (long)p.Gold + Gold10 > PlayerState.GoldCap) return ActionResult.Fail("Your Gold is full"); // refused before the bit is set, like Sell: the reward is never cut
            c.AlbumClaimed |= 1 << milestone;
            if (milestone == 0) { p.AddGoldCapped(Gold10); return ActionResult.Success("Album reward: +" + Gold10 + " G"); }
            return ActionResult.Success(milestone == CardHunter ? "Title unlocked: Card Hunter" : milestone == PortraitFrame ? "Portrait frame unlocked" : "Monster Master!");
        }
    }
}
