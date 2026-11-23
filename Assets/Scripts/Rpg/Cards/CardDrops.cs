namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Card drop chances. Each hero that dealt a valid share of damage rolls once per kill (KillRewards), nothing falls on the ground.
    /// Numbers live here only: tuning them never needs a save migration.
    /// </summary>
    public static class CardDrops
    {
        /// <summary>Rollback switch: false = no card ever drops (cards already owned stay).</summary>
        public static bool Enabled = true;

        public const double ChanceN = 0.01, ChanceR = 0.005, ChanceSR = 0.002, ChanceBoss = 0.03;

        /// <summary>Drop chance of a card per kill of its monster.</summary>
        public static double Chance(CardDef card)
        {
            if (card == null) return 0.0;
            if (card.IsBoss) return ChanceBoss;
            switch (card.Rarity)
            {
                case CardRarity.N: return ChanceN;
                case CardRarity.R: return ChanceR;
                default: return ChanceSR;
            }
        }

        /// <summary>One roll for a kill of `monsterId`: the card id, or null (no card for this monster, disabled, or bad luck).</summary>
        public static string Roll(string monsterId, Rand rand)
        {
            if (!Enabled || rand == null) return null;
            CardDef card = CardTable.Get(monsterId);
            if (card == null) return null;
            return rand() < Chance(card) ? card.Id : null;
        }
    }
}
