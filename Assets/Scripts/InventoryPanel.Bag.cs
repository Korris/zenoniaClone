using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Centre panel: a 5 x 4 paged grid (20 cells a page). Unfiltered: gold, potions and stones first, then the PlayerState.InventorySize bag slots (8 + 50 = 3 pages). Filtered by a worn slot: only the items the hero can put in that slot.
    /// The cells, the filter and the page are BagModel's; this file only draws them. Coordinates are local to the panel.
    /// </summary>
    public partial class InventoryPanel
    {
        const int GridCols = 5, PageSize = 20;
        const float CellSize = 72f, CellStep = 76f, CellIcon = 68f;

        readonly BagWidgets.Slot[] gridSlots = new BagWidgets.Slot[PageSize];
        BagWidgets.BagButton filterChip;
        Text allItemsText, bagCountText, pageText;

        void BuildBag(RectTransform centre)
        {
            allItemsText = BagWidgets.Text(centre, "AllItems", 16f, 13f, "All items", 19, "#fff0c5", TextAnchor.UpperLeft, 200f, true);
            filterChip = new BagWidgets.BagButton(centre, "FilterChip", 12f, 10f, 150f, 34f, "", BagSprites.Tone.Blue, () => { SetFilter(BagFilter.All); lastClickIndex = -1; Refresh(); });
            filterChip.Label.fontSize = 14;
            bagCountText = BagWidgets.Text(centre, "Count", CentreW - 14f, 15f, "", 18, "#fff0c5", TextAnchor.UpperRight, 100f, true);
            BagWidgets.Divider(centre, "ListRule", 14f, 41f, CentreW - 28f);
            for (int i = 0; i < PageSize; i++)
            {
                int cell = i;
                gridSlots[i] = new BagWidgets.Slot(centre, "Cell" + i, 12f + (i % GridCols) * CellStep, 52f + (i / GridCols) * CellStep, CellSize, () => OnCellClick(model.Page * PageSize + cell));
            }
            new BagWidgets.BagButton(centre, "Sort", 12f, 388f, 90f, 44f, "Sort", BagSprites.Tone.Blue, SortBag);
            new BagWidgets.BagButton(centre, "Prev", CentreW - 212f, 388f, 50f, 40f, "<", BagSprites.Tone.Gold, () => TurnPage(-1));
            pageText = BagWidgets.Text(centre, "Page", CentreW - 116f, 400f, "", 17, "#ffffff", TextAnchor.UpperCenter, 70f, true);
            new BagWidgets.BagButton(centre, "Next", CentreW - 64f, 388f, 50f, 40f, ">", BagSprites.Tone.Gold, () => TurnPage(1));
            messageText = BagWidgets.Text(centre, "Message", CentreW * 0.5f, 420f, "", 14, "#ffe08a", TextAnchor.UpperCenter, CentreW - 24f, true);
        }

        /// <summary>Capture runs: this bag item selected, no filter.</summary>
        public static void TestSelectBagItem(int bagIndex)
        {
            if (instance == null) return;
            instance.SetFilter(BagFilter.All);
            instance.model.SelectCell(instance.model.SupplyOrder.Length + bagIndex);
            instance.Refresh();
        }

        void SetFilter(BagFilter filter)
        {
            if (model.SetFilter(filter)) confirmDiscard = false;
        }

        // ---- actions ----

        void TurnPage(int by)
        {
            if (RpgSession.Player == null) return;
            model.TurnPage(by);
            confirmDiscard = false;
            Refresh();
        }

        /// <summary>Gear by slot order then item level (highest first); supplies by count.</summary>
        void SortBag()
        {
            PlayerState p = RpgSession.Player;
            if (p == null || BagActions.TidyBag(model, p).Pending) return; // online: the server sorts and its reply redraws
            lastClickIndex = -1;
            Refresh();
        }

        // ---- refresh ----

        void RefreshBag(PlayerState p)
        {
            model.Rebuild(p);
            bool filtered = model.Filter != BagFilter.All;
            allItemsText.enabled = !filtered;
            filterChip.Set(filtered, filtered ? ItemInfo.SlotLabel(BagModel.SlotOf(model.Filter)) + "   X" : null);
            bagCountText.text = p.Inventory.Count + "/" + PlayerState.InventorySize;
            pageText.text = (model.Page + 1) + " / " + model.PageCount;

            for (int i = 0; i < PageSize; i++)
            {
                int cell = model.Page * PageSize + i;
                BagWidgets.Slot v = gridSlots[i];
                bool selected = wornSel < 0 && cell == model.Cell;
                BagSupply kind;
                if (model.SupplyAt(cell, out kind))
                {
                    int count = BagModel.SupplyCount(p, kind);
                    v.SetFrame(selected, BagWidgets.Hex("#70899d"), BagModel.IsPotion(kind) && count == 0);
                    v.SetIcon(BagText.SupplySprite(kind), CellIcon - 8f, new Color(1f, 1f, 1f, count > 0 ? 1f : 0.4f));
                    v.SetEnhance(null);
                    v.Count.text = count.ToString();
                    continue;
                }
                Item item = model.ItemAt(cell);
                bool beyond = !filtered && cell >= model.CellCount;
                v.SetFrame(selected, item != null ? LootSprites.RarityColor(item) : BagWidgets.Hex("#607b91"), beyond);
                bool tooLow = item != null && p.CanEquip(item) && !p.MeetsLevel(item); // right class, hero level too low: gray + "Lv30" in the corner
                v.SetIcon(item != null ? LootSprites.Icon(item) : null, CellIcon, item == null || p.CanEquip(item) && !tooLow ? Color.white : tooLow ? new Color(0.55f, 0.55f, 0.6f, 0.6f) : new Color(1f, 0.6f, 0.6f, 0.55f));
                v.SetEnhance(item);
                int plus = EnhanceStats.Level(item);
                v.Count.text = tooLow ? "<color=#ff7070>Lv" + EquipLevel.Required(item) + "</color>" : plus > 0 ? "+" + plus : "";
            }
        }
    }
}
