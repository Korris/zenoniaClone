using System;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>PlayerRpg: monster card drops. Authority only (offline game, dedicated server, host); an online client only sees the result in RpgState.</summary>
    public sealed partial class PlayerRpg
    {
        /// <summary>A card was added to the collection: (card id, first time in the Album).</summary>
        public event Action<string, bool> CardDropped;

        /// <summary>A card dropped but 999 copies were already held: it was sold at the NPC price instead (card id, gold paid).</summary>
        public event Action<string, int> CardOverflowed;

        /// <summary>Gives one dropped card (CardOps.Receive): into the collection, or as gold when the stack is full, and tells the listeners (toast, save). False for an id that is not a card.</summary>
        public bool CardDrop(string cardId)
        {
            if (!CardTable.IsCardId(cardId)) return false;
            CardGain gain = CardOps.Receive(State, cardId);
            Touch();
            if (gain.Stored) { if (CardDropped != null) CardDropped(cardId, gain.First); }
            else if (CardOverflowed != null) CardOverflowed(cardId, gain.Gold); // Gold 0 = the Gold cap held the sale back: the copy was skipped
            return true;
        }
    }
}
