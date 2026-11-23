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

        public static void Show(int cardIndex, bool first)
        {
            CardDef card = CardTable.ByIndex(cardIndex);
            if (card != null) RpgHud.Toast(Text(card, first), ColorOf(card));
        }
    }
}
