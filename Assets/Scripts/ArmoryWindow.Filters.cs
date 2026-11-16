using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// ArmoryWindow: the filter row. "Type" chips (All, the six gear slots, Pet only while the bag still holds an old pet item,
    /// Consumable = potions, Material = upgrade stones), the Sort box and the Grid / Shelf switch. Sorting by Rarity, Level or Type only
    /// changes the order shown; "Tidy Bag" is the old window's Sort button: it reorders the bag itself (online: the server does).
    /// </summary>
    public sealed partial class ArmoryWindow
    {
        static readonly string[] FilterNames = { "All", "Weapon", "Offhand", "Armor", "Gloves", "Boots", "Necklace", "Pet", "Consumable", "Material" };
        static readonly float[] FilterWidths = { 44f, 68f, 70f, 60f, 62f, 58f, 78f, 44f, 96f, 74f };
        static readonly string[] SortNames = { "Bag Order", "Rarity (High to Low)", "Level (High to Low)", "Type", "Tidy Bag" }; // BagSort order, then the action
        const int TidyOption = 4;

        readonly PaperTab[] filterChips = new PaperTab[FilterNames.Length];
        RectTransform filters;
        PaperDropdown sortBox;
        PaperButton viewButton;

        void BuildFilters()
        {
            filters = UiKit.Rect(content, "Filters", 0f, 392f, PaperLayout.DesignWidth, 36f);
            PaperArt.Plate(UiKit.Rect(filters, "Strip", 14f, 0f, PaperLayout.DesignWidth - 28f, 36f), PaperTheme.Paper, PaperTheme.Wood, 12f);
            PaperTheme.Title(filters, "TypeLabel", 28f, 0f, 44f, 36f, "Type", PaperTheme.SectionSize, PaperTheme.Ink);
            for (int i = 0; i < filterChips.Length; i++)
            {
                BagFilter f = (BagFilter)i;
                filterChips[i] = new PaperTab(filters, "Chip" + FilterNames[i], 0f, 5f, FilterWidths[i], 26f, FilterNames[i], PaperTabStyle.Chip, () => PickFilter(f));
            }
            PaperTheme.Title(filters, "SortLabel", 806f, 0f, 40f, 36f, "Sort", PaperTheme.SectionSize, PaperTheme.Ink);
            viewButton = new PaperButton(filters, "View", 1052f, 3f, 64f, 30f, "Grid", PaperTone.Gold, () => SetGridView(!gridView));
            sortBox = new PaperDropdown(filters, "Sort", 846f, 4f, 198f, 28f, SortNames, PickSort); // last: its list opens over the row's other pieces
        }

        void PickFilter(BagFilter f)
        {
            confirmDiscard = false;
            model.Worn = -1;
            model.SetFilter(f);
            Refresh();
        }

        void PickSort(int option)
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            confirmDiscard = false;
            if (option == TidyOption)
            {
                model.Sort = BagSort.BagOrder;
                Show(BagActions.TidyBag(model, p));
                return;
            }
            Item keep = model.Worn < 0 ? model.ItemAt(model.Cell) : null; // the selected card stays selected in the new order
            model.Sort = (BagSort)option;
            model.Rebuild(p);
            int cell = model.CellOf(keep);
            if (cell >= 0) model.SelectCell(cell);
            Refresh();
        }

        void RefreshFilters(PlayerState p)
        {
            bool petItems = model.Filter == BagFilter.Pet || p.Inventory.Exists(i => i.slot == Slots.Pet);
            float x = 76f;
            for (int i = 0; i < filterChips.Length; i++)
            {
                bool shown = (BagFilter)i != BagFilter.Pet || petItems;
                filterChips[i].Root.gameObject.SetActive(shown);
                if (!shown) continue;
                filterChips[i].Root.anchoredPosition = new Vector2(x, -5f);
                filterChips[i].SetActive(model.Filter == (BagFilter)i);
                x += FilterWidths[i] + 4f;
            }
            sortBox.Show((int)model.Sort);
        }
    }
}
