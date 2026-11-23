namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Gold prices of the card system, by card rarity (N, R, SR, SSR). One place to tune, never stored in a save.
    /// Convert = card -> Soul Gem (the card is used up), Remove = take a gem out of an item (the gem stays, locked for good), Sell = card to an NPC
    /// (also what a card that no longer fits in the 999 stack is turned into). Socketing is free.
    /// </summary>
    public static class CardFees
    {
        static readonly int[] ConvertGold = { 500, 2000, 8000, 25000 }, RemoveGold = { 200, 800, 3000, 10000 }, SellGold = { 50, 200, 800, 2500 };

        public static int Convert(CardDef card) { return card == null ? 0 : ConvertGold[(int)card.Rarity]; }
        public static int Remove(CardDef card) { return card == null ? 0 : RemoveGold[(int)card.Rarity]; }
        public static int Sell(CardDef card) { return card == null ? 0 : SellGold[(int)card.Rarity]; }
    }
}
