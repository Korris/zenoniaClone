using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>The "Card: Meadow Slime" toast shown for a card drop, same text offline (RpgSession) and online (NetEventType.CardDrop).</summary>
    public static class CardToast
    {
        public static string Text(CardDef card, bool first) { return "Card: " + card.Name + " [" + card.Rarity + "]" + (first ? " NEW" : ""); }

        public static Color ColorOf(CardDef card)
        {
            if (card.IsBoss || card.Rarity == CardRarity.SSR) return new Color(1f, 0.82f, 0.3f);
            switch (card.Rarity)
            {
                case CardRarity.SR: return new Color(0.85f, 0.6f, 1f);
                case CardRarity.R: return new Color(0.55f, 0.8f, 1f);
                default: return new Color(0.85f, 1f, 0.8f);
            }
        }

        /// <summary>The stack is full (999): the card was sold at the NPC price instead (CardOps.Receive).</summary>
        public static string FullText(CardDef card) { return "Card: " + card.Name + " (999 held) sold +" + CardFees.Sell(card) + " G"; }

        /// <summary>The stack is full and the Gold cap holds the sale back: the copy is skipped, nothing is paid.</summary>
        public static string SkippedText(CardDef card) { return "Card: " + card.Name + " (999 held, Gold full) skipped"; }

        public static void Show(int cardIndex, bool first, bool full = false, bool skipped = false)
        {
            CardDef card = CardTable.ByIndex(cardIndex);
            if (card == null) return;
            RpgHud.Toast(skipped ? SkippedText(card) : full ? FullText(card) : Text(card, first), ColorOf(card));
        }

        public static void ShowFull(int cardIndex) { Show(cardIndex, false, true); }
    }
}
