using System;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Inventory tab (web drawEquipmentBag + inventory-supply-cells.js): a 3 x 4 paged grid whose first cells are gold, HP / MP potions and
    /// upgrade stones, then the 20 bag slots; Sort / page buttons and the bag count; on the right the selected item (icon, name, slot, level,
    /// stat lines compared with the worn item, enhance preview or set blurb) with Equip / Use, Enhance and Discard.
    /// </summary>
    public partial class InventoryPanel
    {
        enum Supply { Coin, HpPotion, MpPotion, Stone }
        const int GridCols = 3, PageSize = 12, MaxStatLines = 6;
        static readonly string[] SupplyNames = { "Gold", "HP Potion", "MP Potion", "Upgrade Stone" };
        static readonly string[] SupplyBlurbs = { "Gold used for trading.", "Restores 30% HP.", "Restores 30% MP.", "Used to enhance weapons. Success drops at higher +." };

        readonly Supply[] supplyOrder = { Supply.Coin, Supply.HpPotion, Supply.MpPotion, Supply.Stone };
        readonly BagWidgets.Slot[] gridSlots = new BagWidgets.Slot[PageSize];
        readonly Text[] statTexts = new Text[MaxStatLines], compareTexts = new Text[MaxStatLines], extraTexts = new Text[3];
        readonly Image[] statRules = new Image[MaxStatLines];
        RectTransform bagRoot, itemDetail, supplyDetail;
        int cellSel, page;
        BagWidgets.Slot detailSlot;
        Text bagCountText, selectHint, itemName, itemSlotText, itemLevelText, supplyName, supplyQuantity, supplyBlurb;
        BagWidgets.BagButton pageButton, equipButton, useButton, bagEnhanceButton, discardButton;

        int CellCount { get { return supplyOrder.Length + PlayerState.InventorySize; } }
        int PageCount { get { return (CellCount + PageSize - 1) / PageSize; } }

        static int SupplyCount(PlayerState p, Supply kind)
        {
            switch (kind)
            {
                case Supply.Coin: return p.Gold;
                case Supply.HpPotion: return p.HpPotions;
                case Supply.MpPotion: return p.MpPotions;
                default: return p.UpgradeStone;
            }
        }

        static Sprite SupplySprite(Supply kind)
        {
            return kind == Supply.Coin ? LootSprites.Coin() : kind == Supply.Stone ? LootSprites.Stone() : LootSprites.Potion(kind == Supply.HpPotion);
        }

        void BuildBag(RectTransform web)
        {
            bagRoot = UiKit.Rect(web, "Bag", 0f, 0f, UiKit.RefWidth, UiKit.RefHeight);
            BagWidgets.Panel(bagRoot, "Grid", 320f, 154f, 278f, 414f);
            BagWidgets.Panel(bagRoot, "Detail", 600f, 154f, 328f, 414f);
            for (int i = 0; i < PageSize; i++)
            {
                int cell = i;
                gridSlots[i] = new BagWidgets.Slot(bagRoot, "Cell" + i, 322f + (i % GridCols) * 90f, 164f + (i / GridCols) * 90f, 82f, () => OnCellClick(page * PageSize + cell));
            }
            new BagWidgets.BagButton(bagRoot, "Sort", 332f, 528f, 76f, 32f, "Sort", BagSprites.Tone.Steel, SortBag);
            pageButton = new BagWidgets.BagButton(bagRoot, "Page", 420f, 528f, 76f, 32f, "", BagSprites.Tone.Steel, NextPage);
            bagCountText = BagWidgets.Text(bagRoot, "Count", 581f, 538f, "", 13, "#bfd1dc", TextAnchor.UpperRight, 80f);
            selectHint = BagWidgets.Text(bagRoot, "Hint", 764f, 300f, "Select equipment", 18, "#b4d0e0", TextAnchor.UpperCenter, 300f);
            detailSlot = new BagWidgets.Slot(bagRoot, "DetailSlot", 615f, 169f, 76f, null);

            itemDetail = UiKit.Rect(bagRoot, "ItemDetail", 0f, 0f, UiKit.RefWidth, UiKit.RefHeight);
            itemName = BagWidgets.Text(itemDetail, "Name", 704f, 170f, "", 17, "#f2deae", TextAnchor.UpperLeft, 210f, true);
            itemSlotText = BagWidgets.Text(itemDetail, "Slot", 704f, 219f, "", 12, "#c7e2f2", TextAnchor.UpperLeft, 210f);
            itemLevelText = BagWidgets.Text(itemDetail, "Level", 704f, 239f, "", 13, "#ffffff", TextAnchor.UpperLeft, 120f, true);
            for (int i = 0; i < MaxStatLines; i++)
            {
                float y = 285f + i * 29f;
                statRules[i] = BagWidgets.Img(itemDetail, "Rule" + i, 616f, y - 9f, 295f, 1f, null, BagWidgets.Hex("#6f9ab355"));
                statTexts[i] = BagWidgets.Text(itemDetail, "Stat" + i, 616f, y, "", 14, "#c2e3f4", TextAnchor.UpperLeft, 220f);
                compareTexts[i] = BagWidgets.Text(itemDetail, "Compare" + i, 910f, y, "", 12, "#cfcfe0", TextAnchor.UpperRight, 90f, true);
            }
            for (int i = 0; i < extraTexts.Length; i++) extraTexts[i] = BagWidgets.Text(itemDetail, "Extra" + i, 616f, 403f + i * 21f, "", 13, "#f2deae", TextAnchor.UpperLeft, 290f);
            extraTexts[0].lineSpacing = 1.45f; // the set blurb wraps over several 21px lines
            equipButton = new BagWidgets.BagButton(itemDetail, "Equip", 612f, 528f, 140f, 32f, "Equip", BagSprites.Tone.Blue, Activate);
            bagEnhanceButton = new BagWidgets.BagButton(itemDetail, "Enhance", 766f, 528f, 146f, 32f, "Enhance", BagSprites.Tone.Gold, EnhanceSelected);
            discardButton = new BagWidgets.BagButton(itemDetail, "Discard", 836f, 236f, 76f, 26f, "Discard", BagSprites.Tone.Steel, DiscardSelected);
            discardButton.Label.fontSize = 12;

            supplyDetail = UiKit.Rect(bagRoot, "SupplyDetail", 0f, 0f, UiKit.RefWidth, UiKit.RefHeight);
            supplyName = BagWidgets.Text(supplyDetail, "Name", 704f, 175f, "", 19, "#f2deae", TextAnchor.UpperLeft, 210f, true);
            supplyQuantity = BagWidgets.Text(supplyDetail, "Quantity", 616f, 285f, "", 15, "#ffffff", TextAnchor.UpperLeft, 290f, true);
            supplyBlurb = BagWidgets.Text(supplyDetail, "Blurb", 616f, 340f, "", 14, "#b4d5e7", TextAnchor.UpperLeft, 288f);
            supplyBlurb.lineSpacing = 1.3f;
            useButton = new BagWidgets.BagButton(supplyDetail, "Use", 612f, 528f, 140f, 32f, "Use", BagSprites.Tone.Gold, Activate);
        }

        // ---- actions ----

        void NextPage()
        {
            page = (page + 1) % PageCount;
            cellSel = page * PageSize;
            confirmDiscard = false;
            Refresh();
        }

        /// <summary>Web Sort: gear by slot order then item level (highest first); supplies by count.</summary>
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
            cellSel = 0;
            lastClickIndex = -1;
            Refresh();
        }

        /// <summary>Bag items only; the first click arms it, the second discards.</summary>
        void DiscardSelected()
        {
            PlayerState p = RpgSession.Player;
            Item item = p != null ? SelectedItem(p) : null;
            if (item == null || IsEquipped(p, item)) return;
            if (!confirmDiscard) { confirmDiscard = true; Refresh(); return; }
            confirmDiscard = false;
            if (NetMode.IsClient) { NetClient.SendInvAction(InvOp.Discard, item.uid, 0); return; }
            p.RemoveFromInventory(item.uid);
            Flash("Discarded " + item.name);
            Refresh();
        }

        // ---- refresh ----

        void RefreshBag(PlayerState p)
        {
            for (int i = 0; i < PageSize; i++)
            {
                int index = page * PageSize + i, bag = index - supplyOrder.Length;
                BagWidgets.Slot v = gridSlots[i];
                Item item = bag >= 0 && bag < p.Inventory.Count ? p.Inventory[bag] : null;
                v.SetFrame(index == cellSel, item != null ? LootSprites.RarityColor(item) : BagWidgets.Hex("#607b91"), index >= CellCount);
                if (index < supplyOrder.Length)
                {
                    int count = SupplyCount(p, supplyOrder[index]);
                    v.SetIcon(SupplySprite(supplyOrder[index]), 48f, new Color(1f, 1f, 1f, count > 0 ? 1f : 0.4f));
                    v.Count.text = count.ToString();
                }
                else
                {
                    v.SetIcon(item != null ? LootSprites.Icon(item) : null, 60f, item == null || p.CanEquip(item) ? Color.white : new Color(1f, 0.6f, 0.6f, 0.55f));
                    int plus = EnhanceStats.Level(item);
                    v.Count.text = plus > 0 ? "+" + plus : "";
                }
            }
            pageButton.Set(true, (page + 1) + " / " + PageCount + " >");
            bagCountText.text = p.Inventory.Count + "/" + PlayerState.InventorySize;

            bool supply = cellSel < supplyOrder.Length;
            Item selected = SelectedItem(p);
            supplyDetail.gameObject.SetActive(supply);
            itemDetail.gameObject.SetActive(!supply && selected != null);
            detailSlot.Root.gameObject.SetActive(supply || selected != null);
            selectHint.enabled = !supply && selected == null;
            if (supply) ShowSupply(p, supplyOrder[cellSel]);
            else if (selected != null) ShowItem(p, selected);
        }

        void ShowSupply(PlayerState p, Supply kind)
        {
            int count = SupplyCount(p, kind);
            detailSlot.SetFrame(false, BagWidgets.Hex("#70899d"));
            detailSlot.SetIcon(SupplySprite(kind), 48f, new Color(1f, 1f, 1f, count > 0 ? 1f : 0.4f));
            detailSlot.Count.text = count.ToString();
            supplyName.text = SupplyNames[(int)kind];
            supplyQuantity.text = "Quantity: " + count;
            supplyBlurb.text = SupplyBlurbs[(int)kind];
            useButton.Set(kind == Supply.HpPotion || kind == Supply.MpPotion);
        }

        void ShowItem(PlayerState p, Item item)
        {
            bool usable = p.CanEquip(item);
            detailSlot.SetFrame(false, LootSprites.RarityColor(item));
            detailSlot.SetIcon(LootSprites.Icon(item), 62f, usable ? Color.white : new Color(1f, 0.6f, 0.6f, 0.55f));
            detailSlot.Count.text = "";
            itemName.text = EnhanceStats.DisplayName(item);
            string owner = string.IsNullOrEmpty(item.classId) ? "" : "   " + (usable ? "" : "<color=#ff8070>") + RpgClasses.Get(item.classId).Name + " only" + (usable ? "" : "</color>");
            itemSlotText.text = ItemInfo.SlotLabel(item.slot) + owner;
            itemLevelText.text = "Lv. " + item.itemLevel;

            Item worn = p.GetEquipped(item.slot);
            Item compare = worn != item ? worn : null;
            var lines = ItemInfo.Lines(item, compare);
            for (int i = 0; i < MaxStatLines; i++)
            {
                bool has = i < lines.Count;
                statRules[i].enabled = has;
                statTexts[i].text = has ? StatText(item, lines[i]) : "";
                compareTexts[i].text = "";
                if (!has) continue;
                double d = compare != null ? lines[i].Delta : lines[i].Value; // nothing worn: the whole stat is a gain (web compareToEquipped)
                double shown = lines[i].Percent ? Math.Round(d * 100.0) : Math.Round(d);
                compareTexts[i].text = (shown > 0 ? "+" : "") + shown + (lines[i].Percent ? "%" : "");
                compareTexts[i].color = BagWidgets.Hex(shown > 0 ? "#8fff8f" : shown < 0 ? "#ff7070" : "#cfcfe0");
            }

            EnhanceView e = Preview(p, item);
            bool enhanceable = EnhanceStats.IsEnhanceable(item);
            foreach (Text t in extraTexts) t.text = "";
            extraTexts[0].horizontalOverflow = enhanceable ? HorizontalWrapMode.Overflow : HorizontalWrapMode.Wrap;
            if (!enhanceable)
            {
                RarityDef r = ItemDefs.Rarity(item.rarity);
                string prefix = r != null && !string.IsNullOrEmpty(r.Prefix) ? r.Prefix : "Normal";
                SetExtra(0, prefix + " " + ItemInfo.SlotLabel(item.slot).ToLowerInvariant() + " from the level " + item.itemLevel + " equipment set.", "#b4d5e7");
            }
            else if (e.Max) SetExtra(0, "Enhance " + e.Reason, "#f2deae");
            else
            {
                SetExtra(0, "Enhance  +" + e.From + " -> +" + e.To + "   Success " + e.Chance + "%", "#f2deae");
                SetExtra(1, e.StatLabel + " " + e.StatFrom + " -> " + e.StatTo, "#8fff8f");
                SetExtra(2, "Cost: 1 Upgrade Stone (have " + e.Owned + ")", e.Can ? "#c7e2f2" : "#ff8f8f");
            }

            equipButton.Set(true, usable ? "Equip" : RpgClasses.Get(item.classId).Name + " only", !usable);
            bagEnhanceButton.Set(true, null, !e.Can);
            discardButton.Set(true, confirmDiscard ? "Sure?" : "Discard");
        }

        void SetExtra(int i, string text, string color) { extraTexts[i].text = text; extraTexts[i].color = BagWidgets.Hex(color); }

        /// <summary>"ATK 21 (+4)": the effective value, plus the part that comes from enhancing (web describeItemStats).</summary>
        static string StatText(Item item, StatLine l)
        {
            string text = ItemInfo.Format(l);
            if (l.Percent || EnhanceStats.Level(item) == 0) return text;
            string key = l.Label == "ATK" ? "atk" : l.Label == "DEF" ? "def" : l.Label == "HP" ? "maxHp" : l.Label == "MP" ? "maxMp" : null;
            int bonus = key != null ? RpgMath.Round(l.Value) - item.stats.GetInt(key) : 0;
            return bonus > 0 ? text + " (+" + bonus + ")" : text;
        }
    }
}
