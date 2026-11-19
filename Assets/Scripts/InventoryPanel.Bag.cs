using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using W = ZenoniaProto.InventoryRoWidgets;
using B = ZenoniaProto.InventoryRoBoxes;

namespace ZenoniaProto
{
    /// <summary>
    /// Centre panel: 6 category tabs (All, Weapon, Armor, Accessory, Consumable, Material = BagFilter), "Bag (n/50)", the Sort dropdown
    /// (display orders of BagSort + "Tidy bag", the old Sort that reorders the bag itself, server-side online), and the 8 x 8 grid:
    /// potions and stones first, then the 50 bag slots; "+" = a free slot, faded "+" = past the bag's size. Coordinates are local to the panel.
    /// </summary>
    public partial class InventoryPanel
    {
        const int GridCols = 8, PageSize = 64;
        const float CellSize = 48f, CellStep = 51.5f, GridX = 9f, GridY = 118f, TabW = 68f, TabGapX = 1.5f, HeaderY = 78f;

        static readonly BagFilter[] TabFilters = { BagFilter.All, BagFilter.WeaponGroup, BagFilter.ArmorGroup, BagFilter.Accessory, BagFilter.Consumable, BagFilter.Material };
        static readonly string[] TabNames = { "All", "Weapon", "Armor", "Accessory", "Consumable", "Material" };
        static readonly string[] TabIcons = { "bag-cat-all", "bag-cat-weapon", "bag-cat-armor", "bag-cat-accessory", "bag-cat-consumable", "bag-cat-material" };
        static readonly BagSort[] SortOrders = { BagSort.BagOrder, BagSort.Rarity, BagSort.Level, BagSort.Type };
        static readonly string[] SortNames = { "Bag order", "Rarity", "Level", "Type" };

        readonly W.Cell[] gridSlots = new W.Cell[PageSize];
        readonly B.Tab[] tabs = new B.Tab[6];
        Text bagCountText, sortText;
        RectTransform sortList;

        void BuildBag(RectTransform centre)
        {
            for (int i = 0; i < TabFilters.Length; i++)
            {
                int tab = i;
                tabs[i] = new B.Tab(centre, "Tab" + TabNames[i], 6f + i * (TabW + TabGapX), 8f, TabW, TabIcons[i], TabNames[i],
                    () => { SetFilter(TabFilters[tab]); model.Worn = -1; lastClickIndex = -1; Refresh(); });
            }

            W.Img(centre, "BagIcon", 14f, HeaderY + 2f, 30f, 26f, "bag-icon");
            bagCountText = W.Txt(centre, "Count", 50f, HeaderY, 200f, 30f, "", 18, W.Ink);
            Image sort = W.Img(centre, "Sort", CentreW - 150f, HeaderY - 2f, 140f, 34f, "dropdown", true);
            W.Img(sort.transform, "Icon", 10f, 7f, 22f, 20f, "bag-ico-sort");
            sortText = W.Txt(sort.transform, "Label", 38f, 0f, 70f, 33f, "Sort", 16, W.Ink);
            W.Clickable(sort, ToggleSortList);

            for (int i = 0; i < PageSize; i++)
            {
                int cell = i;
                gridSlots[i] = new W.Cell(centre, "Cell" + i, GridX + (i % GridCols) * CellStep, GridY + (i / GridCols) * CellStep, CellSize, () => OnCellClick(cell));
            }
            BuildSortList(centre);
        }

        void BuildSortList(RectTransform centre)
        {
            int rows = SortNames.Length + 1;
            sortList = W.Img(centre, "SortList", CentreW - 160f, HeaderY + 34f, 150f, 12f + rows * 36f, "bag-window", true).rectTransform;
            for (int i = 0; i < rows; i++)
            {
                int row = i;
                Image b = W.Img(sortList, "Row" + i, 8f, 6f + i * 36f, 134f, 34f, i < SortNames.Length ? "bag-pill-white" : "btn-sky", true);
                W.Txt(b.transform, "Label", 0f, 0f, 134f, 33f, i < SortNames.Length ? SortNames[i] : "Tidy bag", 15, i < SortNames.Length ? W.Ink : Color.white, TextAnchor.MiddleCenter);
                W.Clickable(b, () => PickSort(row));
            }
            sortList.gameObject.SetActive(false);
        }

        void ToggleSortList() { sortList.gameObject.SetActive(!sortList.gameObject.activeSelf); sortList.SetAsLastSibling(); }

        void CloseSortList() { if (sortList != null) sortList.gameObject.SetActive(false); }

        /// <summary>A display order (the bag itself is untouched), or the last row: Tidy bag (BagActions.TidyBag, the server sorts online).</summary>
        void PickSort(int row)
        {
            CloseSortList();
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            lastClickIndex = -1;
            if (row < SortOrders.Length) { model.Sort = SortOrders[row]; Refresh(); return; }
            model.Sort = BagSort.BagOrder; // a tidied bag is shown as stored
            if (!BagActions.TidyBag(model, p).Pending) Refresh();
        }

        /// <summary>Capture runs: this bag item selected, no filter.</summary>
        public static void TestSelectBagItem(int bagIndex)
        {
            if (instance == null) return;
            instance.SetFilter(BagFilter.All);
            instance.model.Worn = -1;
            if (RpgSession.Player != null) instance.model.Rebuild(RpgSession.Player);
            instance.model.SelectCell(instance.model.Cells.FindAll(c => c.IsSupply).Count + bagIndex); // only stocked supplies are listed
            instance.Refresh();
        }

        void SetFilter(BagFilter filter)
        {
            if (model.SetFilter(filter)) confirmDiscard = false;
        }

        // ---- refresh ----

        void RefreshBag(PlayerState p)
        {
            model.Rebuild(p);
            for (int i = 0; i < TabFilters.Length; i++)
            {
                tabs[i].Set(model.Filter == TabFilters[i]);
            }
            bagCountText.text = "Bag  (" + p.Inventory.Count + "/" + PlayerState.InventorySize + ")";
            sortText.text = model.Sort == BagSort.BagOrder ? "Sort" : SortNames[System.Array.IndexOf(SortOrders, model.Sort)];

            for (int i = 0; i < PageSize; i++)
            {
                W.Cell v = gridSlots[i];
                bool selected = wornSel < 0 && i == model.Cell;
                BagSupply kind;
                if (model.SupplyAt(i, out kind))
                {
                    int count = BagModel.SupplyCount(p, kind);
                    v.SetState(selected, false, Color.white);
                    v.SetIcon(BagText.SupplySprite(kind), Color.white, 0.78f);
                    ItemEnhanceSlotFx.Show(v.Icon, null);
                    v.Count.text = count.ToString("N0", CultureInfo.InvariantCulture);
                    continue;
                }
                Item item = model.ItemAt(i);
                bool tooLow = item != null && p.CanEquip(item) && !p.MeetsLevel(item); // right class, hero level too low: grey + "Lv30"
                v.SetState(selected && item != null, item == null, item != null ? LootSprites.RarityColor(item) : Color.white, i >= model.CellCount);
                v.SetIcon(item != null ? LootSprites.Icon(item) : null, item == null || p.CanEquip(item) && !tooLow ? Color.white : tooLow ? new Color(0.55f, 0.55f, 0.6f, 0.6f) : new Color(1f, 0.6f, 0.6f, 0.55f));
                ItemEnhanceSlotFx.Show(v.Icon, item);
                int plus = EnhanceStats.Level(item);
                v.Count.text = tooLow ? "<color=#ff8a80>Lv" + EquipLevel.Required(item) + "</color>" : plus > 0 ? "+" + plus : "";
            }
        }
    }
}
