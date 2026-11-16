using System.Collections.Generic;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// "Something new is in the bag" for the red dot on the HUD's Bag tag. Client side only: nothing is saved or sent.
    /// The flag goes up when an item the client has not seen before appears in the bag (picked up, bought, traded, pushed by the server),
    /// and comes down when the bag is opened. It works by comparing the bag with the item ids already seen, so it needs no hook in the
    /// pickup / shop / network code. Worn items count as seen: taking gear off is not "new".
    /// </summary>
    public static class NewItemFlags
    {
        static readonly HashSet<string> seen = new HashSet<string>();
        static string seenClass; // the character the ids belong to (online the PlayerState object is replaced on every server push)

        public static bool Any { get; private set; }

        /// <summary>Capture runs: force the dot (DebugCapture --badges-demo).</summary>
        public static void Raise() { Any = true; }

        /// <summary>Call a few times a second while a hero plays. `bagOpen` = the player is looking at the bag (old or rebuilt).</summary>
        public static void Tick(PlayerState p, bool bagOpen)
        {
            if (p == null) return;
            if (p.ClassId != seenClass)
            {
                // Another character: what it carries is where it starts from.
                seen.Clear();
                seenClass = p.ClassId;
                Learn(p);
                Any = false;
                return;
            }
            bool fresh = Learn(p);
            if (bagOpen) Any = false; // looking at the bag: everything in it has been seen
            else if (fresh) Any = true;
        }

        /// <summary>Adds every bag and worn item to the seen set. True when a BAG item was not in it yet.</summary>
        static bool Learn(PlayerState p)
        {
            bool fresh = false;
            List<Item> bag = p.Inventory;
            for (int i = 0; i < bag.Count; i++)
                if (bag[i] != null && bag[i].uid != null && seen.Add(bag[i].uid)) fresh = true;
            for (int i = 0; i < Slots.All.Length; i++)
            {
                Item worn = p.GetEquipped(Slots.All[i]);
                if (worn != null && worn.uid != null) seen.Add(worn.uid);
            }
            return fresh;
        }
    }
}
