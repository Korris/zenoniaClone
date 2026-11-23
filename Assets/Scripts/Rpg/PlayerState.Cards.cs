using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>PlayerState: the monster card collection (cards, Soul Gems, Album, deck). Saved by SaveSerializer, filled by CardDrops.</summary>
    public sealed partial class PlayerState
    {
        /// <summary>Gold stays inside the API's int range (server-api MAX_INT), so a save with it can always be written.</summary>
        public const int GoldCap = 2000000000;

        public readonly CardCollection Cards = new CardCollection();

        /// <summary>A card dropped (or was handed out): +1 copy and the Album mark. True when it is new to the Album. Stops at 999 copies: CardOps.Receive handles the full stack.</summary>
        public bool AddCard(string id) { return Cards.Add(id, 1); }

        /// <summary>Adds gold without ever passing GoldCap (a reward that does not fit is cut, never wrapped).</summary>
        public void AddGoldCapped(int amount) { Gold = (int)Math.Min((long)GoldCap, Math.Max(0L, (long)Gold + amount)); }

        /// <summary>A bag item or a worn one by uid; null when the character has neither.</summary>
        public Item FindItem(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            Item i = Inventory.Find(x => x.uid == uid);
            if (i != null) return i;
            foreach (string slot in Slots.All) { Item e = GetEquipped(slot); if (e != null && e.uid == uid) return e; }
            return null;
        }

        /// <summary>Throws a bag item away. Refused for an item that still holds Soul Gems: the gems would be lost and are locked for good (take them out first).</summary>
        public ActionResult Discard(string uid)
        {
            Item item = Inventory.Find(x => x.uid == uid);
            if (item == null) return ActionResult.Fail("No such item");
            if (item.sockets != null && item.sockets.Count > 0) return ActionResult.Fail("Remove its Soul Gems first");
            RemoveFromInventory(uid);
            return ActionResult.Success("Discarded");
        }
    }
}
