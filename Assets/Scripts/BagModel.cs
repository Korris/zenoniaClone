using System.Collections.Generic;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>What the bag holds besides gear: gold, the six potion stacks and upgrade stones (PlayerState counters, not items).</summary>
    public enum BagSupply { Coin, HpSmall, HpMedium, HpLarge, MpSmall, MpMedium, MpLarge, Stone }

    /// <summary>
    /// What the bag lists: everything, one worn slot, the potions (Consumable) or the upgrade stones (Material); the three groups after Headgear
    /// are the RO Soft bag's category tabs (Weapon = weapon + offhand, Armor = armor + gloves + boots, Accessory = necklace + hat + pet).
    /// </summary>
    public enum BagFilter { All, Weapon, Offhand, Armor, Gloves, Boots, Necklace, Pet, Consumable, Material, Headgear, WeaponGroup, ArmorGroup, Accessory } // Headgear and the groups last: the Armory's chip row is indexed by the first ten

    /// <summary>One cell of the list: a supply, an item, or an empty bag slot (Item == null).</summary>
    public struct BagCell { public bool IsSupply; public BagSupply Supply; public Item Item; }

    /// <summary>
    /// The bag as a list of cells, without any view: what is listed (filter, sort, supplies first), pages, and what is selected
    /// (a cell, or a worn slot). Both looks of the bag read it: the old window (InventoryPanel: 5 x 4 pages, supplies + all 50 slots)
    /// and the Armory Carousel (ArmoryWindow: a shelf of cards or a grid). The views own keys and taps; BagActions changes the player.
    /// Call Rebuild whenever the player state, the filter or the sort changed, before reading cells.
    /// </summary>
    public sealed partial class BagModel
    {
        // ---- how the owning view lists things (set once, or when the view changes layout) ----
        public int PageSize = 20;
        /// <summary>Gold as a cell (the old window); the Armory shows gold in its top bar instead.</summary>
        public bool ListCoin = true;
        /// <summary>Supplies the player has none of still get a cell (the old window draws them locked).</summary>
        public bool ListEmptySupplies = true;
        /// <summary>A slot filter lists only what this hero's class can wear (the old window); otherwise every item of that slot.</summary>
        public bool OwnClassOnly = true;
        /// <summary>Empty bag slots are cells too: unfiltered = every slot of the bag, filtered = at least one full page.</summary>
        public bool PadToBag = true;

        // ---- state ----
        public BagFilter Filter { get; private set; }
        public BagSort Sort = BagSort.BagOrder;
        /// <summary>Selected cell (index in Cells, may point at an empty slot), the page shown, and the selected worn slot (index in Slots.All, -1 = a cell is selected).</summary>
        public int Cell, Page, Worn = -1;

        public readonly List<BagCell> Cells = new List<BagCell>();
        /// <summary>Order of the supply cells; TidyBag puts the largest stacks first.</summary>
        public readonly BagSupply[] SupplyOrder = { BagSupply.Coin, BagSupply.HpSmall, BagSupply.HpMedium, BagSupply.HpLarge, BagSupply.MpSmall, BagSupply.MpMedium, BagSupply.MpLarge, BagSupply.Stone };

        /// <summary>Cells including the padding (Cells.Count when PadToBag is off).</summary>
        public int CellCount { get; private set; }
        public int PageCount { get { return System.Math.Max(1, (CellCount + PageSize - 1) / PageSize); } }

        // ---- supplies ----

        public static bool IsPotion(BagSupply kind) { return kind >= BagSupply.HpSmall && kind <= BagSupply.MpLarge; }
        public static bool PotionIsHp(BagSupply kind) { return kind <= BagSupply.HpLarge; }
        public static int PotionSize(BagSupply kind) { return PotionIsHp(kind) ? kind - BagSupply.HpSmall : kind - BagSupply.MpSmall; }

        public static int SupplyCount(PlayerState p, BagSupply kind)
        {
            switch (kind)
            {
                case BagSupply.Coin: return p.Gold;
                case BagSupply.Stone: return p.UpgradeStone;
                default: return p.PotionCount(PotionIsHp(kind), PotionSize(kind));
            }
        }

        // ---- filter ----

        /// <summary>The worn slot a filter lists, null for All / Consumable / Material.</summary>
        public static string SlotOf(BagFilter f)
        {
            switch (f)
            {
                case BagFilter.Weapon: return Slots.Weapon;
                case BagFilter.Offhand: return Slots.Offhand;
                case BagFilter.Armor: return Slots.Armor;
                case BagFilter.Gloves: return Slots.Gloves;
                case BagFilter.Boots: return Slots.Boots;
                case BagFilter.Necklace: return Slots.Helmet; // the "helmet" slot id is shown as a Necklace
                case BagFilter.Pet: return Slots.Pet;
                case BagFilter.Headgear: return Slots.Headgear;
                default: return null;
            }
        }

        /// <summary>True when an item of `slot` belongs to category tab `group` (WeaponGroup, ArmorGroup, Accessory).</summary>
        public static bool InGroup(BagFilter group, string slot)
        {
            switch (group)
            {
                case BagFilter.WeaponGroup: return slot == Slots.Weapon || slot == Slots.Offhand;
                case BagFilter.ArmorGroup: return slot == Slots.Armor || slot == Slots.Gloves || slot == Slots.Boots;
                case BagFilter.Accessory: return slot == Slots.Helmet || slot == Slots.Headgear || slot == Slots.Pet;
                default: return false;
            }
        }

        public static BagFilter FilterOf(string slot)
        {
            for (BagFilter f = BagFilter.Weapon; f <= BagFilter.Pet; f++) if (SlotOf(f) == slot) return f;
            return slot == Slots.Headgear ? BagFilter.Headgear : BagFilter.All;
        }

        /// <summary>False when it already was that filter. Goes back to the first page; a selected cell goes back to the first cell.</summary>
        public bool SetFilter(BagFilter f)
        {
            if (Filter == f) return false;
            Filter = f;
            Page = 0;
            if (Worn < 0) Cell = 0;
            return true;
        }

        bool Lists(BagSupply kind)
        {
            if (Filter == BagFilter.All) return kind != BagSupply.Coin || ListCoin;
            return IsPotion(kind) ? Filter == BagFilter.Consumable : kind == BagSupply.Stone && Filter == BagFilter.Material;
        }

        // ---- cells ----

        /// <summary>Lists the cells for this player and keeps page / selection inside them.</summary>
        public void Rebuild(PlayerState p)
        {
            Cells.Clear();
            string slot = SlotOf(Filter);
            if (slot == null)
                foreach (BagSupply kind in SupplyOrder)
                    if (Lists(kind) && (ListEmptySupplies || SupplyCount(p, kind) > 0)) Cells.Add(new BagCell { IsSupply = true, Supply = kind });
            int supplies = Cells.Count;
            if (Filter == BagFilter.All) foreach (Item item in p.Inventory) Cells.Add(new BagCell { Item = item });
            else if (slot != null)
            {
                foreach (Item item in p.Inventory)
                    if (item.slot == slot && (!OwnClassOnly || p.CanEquip(item))) Cells.Add(new BagCell { Item = item });
            }
            else if (Filter >= BagFilter.WeaponGroup)
            {
                foreach (Item item in p.Inventory)
                    if (InGroup(Filter, item.slot)) Cells.Add(new BagCell { Item = item }); // a category tab lists every class's items, like All
            }
            SortItems(supplies);

            CellCount = !PadToBag ? Cells.Count : Filter == BagFilter.All ? supplies + PlayerState.InventorySize : System.Math.Max(PageSize, Cells.Count);
            if (Page >= PageCount) Page = PageCount - 1;
            if (Worn < 0 && Cell >= CellCount) Cell = System.Math.Max(0, CellCount - 1);
        }

        public bool SupplyAt(int cell, out BagSupply kind)
        {
            bool has = cell >= 0 && cell < Cells.Count && Cells[cell].IsSupply;
            kind = has ? Cells[cell].Supply : BagSupply.Coin;
            return has;
        }

        public Item ItemAt(int cell) { return cell >= 0 && cell < Cells.Count ? Cells[cell].Item : null; }

        /// <summary>Cell showing this bag item, -1 when the filter hides it.</summary>
        public int CellOf(Item item)
        {
            for (int i = 0; i < Cells.Count; i++) if (item != null && Cells[i].Item == item) return i;
            return -1;
        }

        /// <summary>The selected item: what the selected worn slot holds, else the item of the selected cell (null for a supply or an empty slot).</summary>
        public Item Selected(PlayerState p) { return Worn >= 0 ? p.GetEquipped(Slots.All[Worn]) : ItemAt(Cell); }

        /// <summary>Selects a cell and shows its page.</summary>
        public void SelectCell(int cell)
        {
            Worn = -1;
            Cell = cell;
            Page = System.Math.Max(0, cell) / PageSize;
        }

        /// <summary>Next / previous page, wrapping; a selected cell moves to the first cell of that page.</summary>
        public void TurnPage(int by)
        {
            int pages = PageCount;
            Page = (Page + by + pages) % pages;
            if (Worn < 0) Cell = Page * PageSize;
        }
    }
}
