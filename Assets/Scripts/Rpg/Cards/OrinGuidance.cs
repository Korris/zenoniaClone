namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Orin's Guidance (BIBLE Q04 follow-up, ChatGPT 24/11): after 2 duels with Orin in a row that were NOT won (loss, draw, give-up) he offers "Accept guidance / Try alone". Accepting stores the flag
    /// CardCollection.HelpGuidance with the character (InvOp.CardHelp, server-authoritative): from then on every free turn of a duel with Orin shows the existing Hint (DuelNpcAi, careful mode) on its own, in
    /// this and every later duel. Nothing else changes: no auto-win, same rules, same decks, same AI. The loss streak is a session counter (CardCollection.OrinLosses, not saved): it only decides WHEN Orin asks.
    /// </summary>
    public static class OrinGuidance
    {
        public const int LossesToOffer = 2;

        /// <summary>A duel with Orin ended: a win clears the streak, anything else adds one.</summary>
        public static void Record(CardCollection c, DuelResult result)
        {
            if (c == null || result == DuelResult.Playing) return;
            c.OrinLosses = result == DuelResult.Win ? 0 : System.Math.Min(c.OrinLosses + 1, 1000);
        }

        public static bool Accepted(CardCollection c) { return c != null && (c.Help & CardCollection.HelpGuidance) != 0; }

        /// <summary>Orin asks now: 2+ not-won duels in a row and the guidance is not accepted yet.</summary>
        public static bool ShouldOffer(CardCollection c) { return c != null && !Accepted(c) && c.OrinLosses >= LossesToOffer; }

        /// <summary>"Try alone": the streak starts again, so he asks once more only after 2 more duels not won.</summary>
        public static void Decline(CardCollection c) { if (c != null) c.OrinLosses = 0; }
    }
}
