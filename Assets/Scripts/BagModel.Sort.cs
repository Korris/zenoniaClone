using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Order the listed items are shown in. BagOrder = as stored in the bag; the others only change what the view shows, never the bag itself.</summary>
    public enum BagSort { BagOrder, Rarity, Level, Type }

    /// <summary>BagModel: the display sort of the listed items, and TidyBag, the old "Sort" button that reorders the bag itself (offline).</summary>
    public sealed partial class BagModel
    {
        static int RarityRank(Item item) { return System.Array.IndexOf(ItemDefs.RarityOrder, item.rarity); }

        /// <summary>Negative = a is shown before b. Rarity and Level run high to low; Type follows the worn slot order, then level.</summary>
        static int Compare(BagSort sort, Item a, Item b)
        {
            int byRarity = RarityRank(b).CompareTo(RarityRank(a)), byLevel = b.itemLevel.CompareTo(a.itemLevel), bySlot = Slots.Index(a.slot).CompareTo(Slots.Index(b.slot));
            switch (sort)
            {
                case BagSort.Rarity: return byRarity != 0 ? byRarity : byLevel != 0 ? byLevel : bySlot;
                case BagSort.Level: return byLevel != 0 ? byLevel : byRarity != 0 ? byRarity : bySlot;
                default: return bySlot != 0 ? bySlot : byLevel;
            }
        }

        /// <summary>Stable insertion sort of the item cells (from `first` on): equal items keep their bag order, so the list does not jump around.</summary>
        void SortItems(int first)
        {
            if (Sort == BagSort.BagOrder) return;
            for (int i = first + 1; i < Cells.Count; i++)
                for (int j = i; j > first && Compare(Sort, Cells[j].Item, Cells[j - 1].Item) < 0; j--)
                {
                    BagCell t = Cells[j]; Cells[j] = Cells[j - 1]; Cells[j - 1] = t;
                }
        }

        /// <summary>
        /// Reorders the bag itself: gear by slot order then item level (highest first), supply cells by count. Offline only: online the server
        /// sorts (BagActions.TidyBag sends InvOp.Sort). Selection goes back to the first cell.
        /// </summary>
        public void TidyBag(PlayerState p)
        {
            p.Inventory.Sort((a, b) =>
            {
                int bySlot = Slots.Index(a.slot).CompareTo(Slots.Index(b.slot));
                return bySlot != 0 ? bySlot : b.itemLevel.CompareTo(a.itemLevel);
            });
            for (int i = 1; i < SupplyOrder.Length; i++) // stable insertion sort, highest count first
                for (int j = i; j > 0 && SupplyCount(p, SupplyOrder[j]) > SupplyCount(p, SupplyOrder[j - 1]); j--)
                {
                    BagSupply t = SupplyOrder[j]; SupplyOrder[j] = SupplyOrder[j - 1]; SupplyOrder[j - 1] = t;
                }
            Page = 0;
            if (Worn < 0) Cell = 0;
        }
    }
}
