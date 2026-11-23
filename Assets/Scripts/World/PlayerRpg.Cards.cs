using System;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>PlayerRpg: monster card drops. Authority only (offline game, dedicated server, host); an online client only sees the result in RpgState.</summary>
    public sealed partial class PlayerRpg
    {
        /// <summary>A card was added to the collection: (card id, first time in the Album).</summary>
        public event Action<string, bool> CardDropped;

        /// <summary>Adds one dropped card, marks the Album and tells the listeners (toast, save). False for an id that is not a card.</summary>
        public bool CardDrop(string cardId)
        {
            if (!CardTable.IsCardId(cardId)) return false;
            bool first = State.AddCard(cardId);
            Touch();
            if (CardDropped != null) CardDropped(cardId, first);
            return true;
        }
    }
}
