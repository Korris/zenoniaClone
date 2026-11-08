using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Centre panel: a 5 x 4 paged grid (20 cells a page). Unfiltered: gold, potions and stones first, then the PlayerState.InventorySize bag slots (8 + 50 = 3 pages). Filtered by a worn slot: only the items the hero can put in that slot.
    /// Coordinates are local to the panel.
    /// </summary>
    public partial class InventoryPanel
    {
        enum Supply { Coin, HpSmall, HpMedium, HpLarge, MpSmall, MpMedium, MpLarge, Stone }
        const int GridCols = 5, PageSize = 20;
        const float CellSize = 72f, CellStep = 76f, CellIcon = 68f;

        static bool IsPotion(Supply kind) { return kind >= Supply.HpSmall && kind <= Supply.MpLarge; }
        static bool PotionIsHp(Supply kind) { return kind <= Supply.HpLarge; }
        static int PotionSize(Supply kind) { return PotionIsHp(kind) ? kind - Supply.HpSmall : kind - Supply.MpSmall; }

        readonly Supply[] supplyOrder = { Supply.Coin, Supply.HpSmall, Supply.HpMedium, Supply.HpLarge, Supply.MpSmall, Supply.MpMedium, Supply.MpLarge, Supply.Stone };
        readonly BagWidgets.Slot[] gridSlots = new BagWidgets.Slot[PageSize];
        readonly List<Item> filtered = new List<Item>();
        string filterSlot; // null = everything
        int cellSel, page;
        BagWidgets.BagButton filterChip;
        Text allItemsText, bagCountText, pageText;

        int CellCount(PlayerState p)
        {
            return filterSlot == null ? supplyOrder.Length + PlayerState.InventorySize : Mathf.Max(PageSize, filtered.Count);
        }

        int PageCount(PlayerState p) { return (CellCount(p) + PageSize - 1) / PageSize; }

        bool SupplyAtCell(int cell, out Supply kind)
        {
            bool has = filterSlot == null && cell >= 0 && cell < supplyOrder.Length;
            kind = has ? supplyOrder[cell] : Supply.Coin;
            return has;
        }

        Item ItemAtCell(PlayerState p, int cell)
        {
            if (filterSlot != null) return cell >= 0 && cell < filtered.Count ? filtered[cell] : null;
            int bag = cell - supplyOrder.Length;
            return bag >= 0 && bag < p.Inventory.Count ? p.Inventory[bag] : null;
        }

        static int SupplyCount(PlayerState p, Supply kind)
        {
            switch (kind)
            {
                case Supply.Coin: return p.Gold;
                case Supply.Stone: return p.UpgradeStone;
                default: return p.PotionCount(PotionIsHp(kind), PotionSize(kind));
            }
        }

        static Sprite SupplySprite(Supply kind)
        {
            return kind == Supply.Coin ? LootSprites.Coin() : kind == Supply.Stone ? LootSprites.Stone() : LootSprites.Potion(PotionIsHp(kind), PotionSize(kind));
        }

        void BuildBag(RectTransform centre)
        {
            allItemsText = BagWidgets.Text(centre, "AllItems", 16f, 13f, "All items", 19, "#fff0c5", TextAnchor.UpperLeft, 200f, true);
            filterChip = new BagWidgets.BagButton(centre, "FilterChip", 12f, 10f, 150f, 34f, "", BagSprites.Tone.Blue, () => { SetFilter(null); lastClickIndex = -1; Refresh(); });
            filterChip.Label.fontSize = 14;
            bagCountText = BagWidgets.Text(centre, "Count", CentreW - 14f, 15f, "", 18, "#fff0c5", TextAnchor.UpperRight, 100f, true);
            BagWidgets.Divider(centre, "ListRule", 14f, 41f, CentreW - 28f);
            for (int i = 0; i < PageSize; i++)
            {
                int cell = i;
                gridSlots[i] = new BagWidgets.Slot(centre, "Cell" + i, 12f + (i % GridCols) * CellStep, 52f + (i / GridCols) * CellStep, CellSize, () => OnCellClick(page * PageSize + cell));
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
            instance.SetFilter(null);
            instance.wornSel = -1;
            instance.cellSel = instance.supplyOrder.Length + bagIndex;
            instance.page = instance.cellSel / PageSize;
            instance.Refresh();
        }

        void SetFilter(string slot)
        {
            if (filterSlot == slot) return;
            filterSlot = slot;
            page = 0;
            cellSel = wornSel >= 0 ? cellSel : 0;
            confirmDiscard = false;
        }

        void RebuildFiltered(PlayerState p)
        {
            filtered.Clear();
            if (filterSlot == null) return;
            foreach (Item item in p.Inventory) if (item.slot == filterSlot && p.CanEquip(item)) filtered.Add(item);
        }

        // ---- actions ----

        void TurnPage(int by)
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            int pages = PageCount(p);
            page = (page + by + pages) % pages;
            if (wornSel < 0) cellSel = page * PageSize;
            confirmDiscard = false;
            Refresh();
        }

        /// <summary>Gear by slot order then item level (highest first); supplies by count.</summary>
        void SortBag()
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            if (NetMode.IsClient) { NetClient.SendInvAction(InvOp.Sort, "", 0); return; }
            p.Inventory.Sort((a, b) =>
            {
                int bySlot = Slots.Index(a.slot).CompareTo(Slots.Index(b.slot));
                return bySlot != 0 ? bySlot : b.itemLevel.CompareTo(a.itemLevel);
            });
            for (int i = 1; i < supplyOrder.Length; i++) // stable insertion sort, highest count first
                for (int j = i; j > 0 && SupplyCount(p, supplyOrder[j]) > SupplyCount(p, supplyOrder[j - 1]); j--)
                {
                    Supply t = supplyOrder[j]; supplyOrder[j] = supplyOrder[j - 1]; supplyOrder[j - 1] = t;
                }
            page = 0;
            if (wornSel < 0) cellSel = 0;
            lastClickIndex = -1;
            Refresh();
        }

        // ---- refresh ----

        void RefreshBag(PlayerState p)
        {
            RebuildFiltered(p);
            int pages = PageCount(p);
            if (page >= pages) page = pages - 1;
            if (wornSel < 0 && cellSel >= CellCount(p)) cellSel = CellCount(p) - 1;

            allItemsText.enabled = filterSlot == null;
            filterChip.Set(filterSlot != null, filterSlot != null ? ItemInfo.SlotLabel(filterSlot) + "   X" : null);
            bagCountText.text = p.Inventory.Count + "/" + PlayerState.InventorySize;
            pageText.text = (page + 1) + " / " + pages;

            for (int i = 0; i < PageSize; i++)
            {
                int cell = page * PageSize + i;
                BagWidgets.Slot v = gridSlots[i];
                Supply kind;
                if (SupplyAtCell(cell, out kind))
                {
                    int count = SupplyCount(p, kind);
                    v.SetFrame(wornSel < 0 && cell == cellSel, BagWidgets.Hex("#70899d"), IsPotion(kind) && count == 0);
                    v.SetIcon(SupplySprite(kind), CellIcon - 8f, new Color(1f, 1f, 1f, count > 0 ? 1f : 0.4f));
                    v.SetEnhance(null);
                    v.Count.text = count.ToString();
                    continue;
                }
                Item item = ItemAtCell(p, cell);
                bool beyond = filterSlot == null && cell >= CellCount(p);
                v.SetFrame(wornSel < 0 && cell == cellSel, item != null ? LootSprites.RarityColor(item) : BagWidgets.Hex("#607b91"), beyond);
                bool tooLow = item != null && p.CanEquip(item) && !p.MeetsLevel(item); // right class, hero level too low: gray + "Lv30" in the corner
                v.SetIcon(item != null ? LootSprites.Icon(item) : null, CellIcon, item == null || p.CanEquip(item) && !tooLow ? Color.white : tooLow ? new Color(0.55f, 0.55f, 0.6f, 0.6f) : new Color(1f, 0.6f, 0.6f, 0.55f));
                v.SetEnhance(item);
                int plus = EnhanceStats.Level(item);
                v.Count.text = tooLow ? "<color=#ff7070>Lv" + EquipLevel.Required(item) + "</color>" : PetDurabilityText(item) ?? (plus > 0 ? "+" + plus : "");
            }
        }
    }
}
