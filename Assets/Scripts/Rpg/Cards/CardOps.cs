using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>What happened to a card the character earned (CardOps.Receive).</summary>
    public struct CardGain
    {
        public bool Stored;   // the card went into the collection
        public bool First;    // ... and it is new to the Album
        public int Gold;      // > 0: the stack was full (999), the card was sold at the NPC price instead
        public bool GoldFull; // the stack was full and Gold at the cap: the card was skipped, nothing paid
    }

    /// <summary>
    /// Card and Soul Gem actions, pure rules on a PlayerState (offline, zone server and tests share them). The server runs them; a client only asks.
    /// Order is always: check everything, then change. A gem is never lost: a step that could not store its result is refused before anything is taken.
    /// </summary>
    public static class CardOps
    {
        /// <summary>Rollback switch: false = Convert / Socket / Remove refuse. Gems already socketed keep working.</summary>
        public static bool GemsEnabled = true;

        /// <summary>A card was earned: it joins the collection, or - when 999 copies are already held - is turned into Gold at its NPC sell price (never dropped silently).</summary>
        public static CardGain Receive(PlayerState p, string cardId)
        {
            CardDef card = CardTable.Get(cardId);
            if (card == null) return default(CardGain);
            if (p.Cards.IsFull(cardId))
            {
                int gold = CardFees.Sell(card);
                if ((long)p.Gold + gold > PlayerState.GoldCap) return new CardGain { GoldFull = true }; // Gold is at the cap: the surplus copy is skipped, the stack stays at 999 (never "sold" for nothing)
                p.AddGoldCapped(gold);
                return new CardGain { Gold = gold };
            }
            return new CardGain { Stored = true, First = p.AddCard(cardId) };
        }

        /// <summary>Card -> Soul Gem: costs the fee of the card's rarity and one spare copy (not locked, not in the deck). The Album keeps the card. The gem starts unlocked.</summary>
        public static ActionResult Convert(PlayerState p, string cardId, GemKind kind)
        {
            if (!GemsEnabled) return ActionResult.Fail("Soul Gems are not available");
            CardDef card = CardTable.Get(cardId);
            if (card == null) return ActionResult.Fail("No such card");
            if (p.Cards.Count(cardId) < 1) return ActionResult.Fail("You do not own this card");
            if (p.Cards.Spare(cardId) < 1) return ActionResult.Fail(p.Cards.IsLocked(cardId) ? "This card is locked" : "All copies are in your deck");
            int fee = CardFees.Convert(card);
            if (p.Gold < fee) return ActionResult.Fail("Need " + fee + " G");
            if (!p.Cards.CanAddGem(cardId, kind, false)) return ActionResult.Fail("No room for another Soul Gem of this kind");
            p.Gold -= fee;
            p.Cards.Remove(cardId, 1);
            p.Cards.AddGem(cardId, kind, false);
            return ActionResult.Success(card.Name + " " + kind + " Gem made (-" + fee + " G)");
        }

        /// <summary>Puts one owned gem into the next free socket of an item (bag or worn). Free. From now on the gem is trade-locked for good, and so is the item (it cannot be traded or thrown away until the gem is removed).</summary>
        public static ActionResult Socket(PlayerState p, string itemUid, string cardId, GemKind kind)
        {
            if (!GemsEnabled) return ActionResult.Fail("Soul Gems are not available");
            Item item = p.FindItem(itemUid);
            if (item == null) return ActionResult.Fail("No such item");
            int count = GemSockets.SlotCount(item.slot);
            if (count == 0) return ActionResult.Fail("This item has no sockets");
            if (item.sockets == null) item.sockets = new List<string>();
            if (item.sockets.Count >= count) return ActionResult.Fail("All sockets are full");
            if (!CardTable.IsCardId(cardId)) return ActionResult.Fail("No such gem");
            string gem = GemSockets.Encode(cardId, kind);
            if (item.sockets.Contains(gem)) return ActionResult.Fail("This item already holds that gem");
            // a gem that was socketed before (locked) is used first: the unlocked ones stay free to sell
            if (!p.Cards.RemoveGem(cardId, kind, true) && !p.Cards.RemoveGem(cardId, kind, false)) return ActionResult.Fail("You do not own this gem");
            item.sockets.Add(gem);
            if (p.GetEquipped(item.slot) == item) p.RecomputeStats();
            return ActionResult.Success("Socketed " + CardTable.Get(cardId).Name + " " + kind + " Gem");
        }

        /// <summary>Takes the gem `cardId` / `kind` out of an item (the wire form of InvOp.GemRemove: a repeated request finds no gem and is refused, it never takes the neighbour). Same fee and rules as Remove.</summary>
        public static ActionResult RemoveGem(PlayerState p, string itemUid, string cardId, GemKind kind)
        {
            Item item = p.FindItem(itemUid);
            if (item == null) return ActionResult.Fail("No such item");
            int socket = item.sockets == null ? -1 : item.sockets.IndexOf(GemSockets.Encode(cardId, kind));
            if (socket < 0) return ActionResult.Fail("No gem in that socket");
            return Remove(p, itemUid, socket);
        }

        /// <summary>Takes the gem out of socket `socket` of an item. Costs the fee of the gem's rarity; the gem goes back to the collection, locked for good. Refused (nothing taken) when the collection could not hold it.</summary>
        public static ActionResult Remove(PlayerState p, string itemUid, int socket)
        {
            if (!GemsEnabled) return ActionResult.Fail("Soul Gems are not available");
            Item item = p.FindItem(itemUid);
            if (item == null) return ActionResult.Fail("No such item");
            if (item.sockets == null || socket < 0 || socket >= item.sockets.Count) return ActionResult.Fail("No gem in that socket");
            string cardId; GemKind kind;
            if (!GemSockets.TryParse(item.sockets[socket], out cardId, out kind)) return ActionResult.Fail("No gem in that socket");
            CardDef card = CardTable.Get(cardId);
            int fee = CardFees.Remove(card);
            if (p.Gold < fee) return ActionResult.Fail("Need " + fee + " G");
            if (!p.Cards.CanAddGem(cardId, kind, true)) return ActionResult.Fail("No room to keep this Soul Gem"); // checked before anything is taken
            p.Gold -= fee;
            p.Cards.AddGem(cardId, kind, true);
            item.sockets.RemoveAt(socket);
            if (p.GetEquipped(item.slot) == item) p.RecomputeStats();
            return ActionResult.Success("Removed " + card.Name + " " + kind + " Gem (-" + fee + " G)");
        }

        /// <summary>Sells `n` spare copies of a card to an NPC at the fixed price of its rarity (CardFees.Sell). Locked copies and copies in the deck are never sold. Refused (nothing taken) when the Gold would pass the cap.</summary>
        public static ActionResult Sell(PlayerState p, string cardId, int n)
        {
            CardDef card = CardTable.Get(cardId);
            if (card == null) return ActionResult.Fail("No such card");
            if (n < 1) return ActionResult.Fail("Nothing to sell");
            if (p.Cards.Count(cardId) < 1) return ActionResult.Fail("You do not own this card");
            if (p.Cards.Spare(cardId) < n) return ActionResult.Fail(p.Cards.IsLocked(cardId) ? "This card is locked" : "Copies in your deck cannot be sold");
            long gold = (long)CardFees.Sell(card) * n;
            if ((long)p.Gold + gold > PlayerState.GoldCap) return ActionResult.Fail("Your Gold is full");
            p.Cards.Remove(cardId, n);
            p.AddGoldCapped((int)gold);
            return ActionResult.Success("Sold " + n + " x " + card.Name + " (+" + gold + " G)");
        }

        /// <summary>The wire form of InvOp.CardSell: Slot = copies, and 0 = every spare copy (a byte cannot carry a stack of 999). PlayerRpg calls exactly this.</summary>
        public static ActionResult SellBySlot(PlayerState p, string cardId, int slot) { return Sell(p, cardId, slot == 0 ? p.Cards.Spare(cardId) : slot); }

        /// <summary>The wire form of InvOp.CardLock: Slot 1 locks the kind, anything else unlocks it.</summary>
        public static ActionResult LockBySlot(PlayerState p, string cardId, int slot) { return SetLock(p, cardId, slot == 1); }

        /// <summary>Lock Card: locks the KIND (flag per card id, so copies earned later are locked too) or unlocks it. A locked kind cannot be sold, converted or wagered; the duel deck may still use it.</summary>
        public static ActionResult SetLock(PlayerState p, string cardId, bool locked)
        {
            CardDef card = CardTable.Get(cardId);
            if (card == null) return ActionResult.Fail("No such card");
            if (!p.Cards.SetLocked(cardId, locked)) return ActionResult.Fail("You do not own this card");
            return ActionResult.Success(locked ? card.Name + " locked" : card.Name + " unlocked");
        }
    }
}
