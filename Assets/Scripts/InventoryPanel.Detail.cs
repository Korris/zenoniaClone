using System;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Right panel: the selected item or supply, its stats compared with what is worn, the enhance preview, and Equip / Unequip, Enhance, Discard. Coordinates are local to the panel.</summary>
    public partial class InventoryPanel
    {
        const int MaxStatLines = 6;
        const string GoldBlurb = "Gold used for trading.", StoneBlurb = "Used to enhance weapons. Success drops at higher +.";

        readonly Text[] statTexts = new Text[MaxStatLines], compareTexts = new Text[MaxStatLines], extraTexts = new Text[3];
        readonly Image[] statRules = new Image[MaxStatLines];
        RectTransform itemDetail, supplyDetail;
        BagWidgets.Slot detailSlot;
        Text selectHint, itemName, itemSlotText, itemLevelText, supplyName, supplyQuantity, supplyBlurb;
        BagWidgets.BagButton equipButton, useButton, enhanceButton, discardButton;

        void BuildDetail(RectTransform right)
        {
            const float Width = 210f, Inner = 186f, Left = 12f;
            selectHint = BagWidgets.Text(right, "Hint", Width * 0.5f, 170f, "", 15, "#d9d3bd", TextAnchor.UpperCenter, Inner);
            selectHint.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailSlot = new BagWidgets.Slot(right, "DetailSlot", Left, 12f, 64f, null);

            itemDetail = UiKit.Rect(right, "ItemDetail", 0f, 0f, Width, PanelH);
            itemName = BagWidgets.Text(itemDetail, "Name", 84f, 12f, "", 15, "#ffedb3", TextAnchor.UpperLeft, 118f, true);
            itemName.horizontalOverflow = HorizontalWrapMode.Wrap;
            itemSlotText = BagWidgets.Text(itemDetail, "Slot", 84f, 52f, "", 12, "#c7e2f2", TextAnchor.UpperLeft, 118f);
            itemLevelText = BagWidgets.Text(itemDetail, "Level", 84f, 66f, "", 12, "#ffffff", TextAnchor.UpperLeft, 118f, true);
            for (int i = 0; i < MaxStatLines; i++)
            {
                float y = 96f + i * 25f;
                statRules[i] = BagWidgets.Img(itemDetail, "Rule" + i, Left, y - 5f, Inner, 1f, null, BagWidgets.Hex("#c9a46a55"));
                statTexts[i] = BagWidgets.Text(itemDetail, "Stat" + i, Left, y, "", 13, "#c2e3f4", TextAnchor.UpperLeft, 140f);
                compareTexts[i] = BagWidgets.Text(itemDetail, "Compare" + i, Left + Inner, y, "", 13, "#cfcfe0", TextAnchor.UpperRight, 70f, true);
            }
            for (int i = 0; i < extraTexts.Length; i++)
            {
                extraTexts[i] = BagWidgets.Text(itemDetail, "Extra" + i, Left, 248f + i * 19f, "", 12, "#ffedb3", TextAnchor.UpperLeft, Inner);
                extraTexts[i].horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            discardButton = new BagWidgets.BagButton(itemDetail, "Discard", Left, 312f, Inner, 36f, "Discard", BagSprites.Tone.Steel, DiscardSelected);
            discardButton.Label.fontSize = 13;
            equipButton = new BagWidgets.BagButton(itemDetail, "Equip", Left, 354f, Inner, 46f, "Equip", BagSprites.Tone.Blue, Activate);
            enhanceButton = new BagWidgets.BagButton(itemDetail, "Enhance", Left, 404f, EnhanceWidth(Inner), 46f, "Enhance", BagSprites.Tone.Gold, EnhanceSelected);
            BuildDowngrade(Left, 404f, Inner); // TEMPORARY test button, InventoryPanel.DevDowngrade.cs

            supplyDetail = UiKit.Rect(right, "SupplyDetail", 0f, 0f, Width, PanelH);
            supplyName = BagWidgets.Text(supplyDetail, "Name", 84f, 18f, "", 15, "#ffedb3", TextAnchor.UpperLeft, 118f, true);
            supplyName.horizontalOverflow = HorizontalWrapMode.Wrap;
            supplyQuantity = BagWidgets.Text(supplyDetail, "Quantity", Left, 96f, "", 14, "#ffffff", TextAnchor.UpperLeft, Inner, true);
            supplyQuantity.horizontalOverflow = HorizontalWrapMode.Wrap;
            supplyBlurb = BagWidgets.Text(supplyDetail, "Blurb", Left, 144f, "", 13, "#c2e3f4", TextAnchor.UpperLeft, Inner);
            supplyBlurb.horizontalOverflow = HorizontalWrapMode.Wrap;
            useButton = new BagWidgets.BagButton(supplyDetail, "Use", Left, 354f, Inner, 46f, "Use", BagSprites.Tone.Gold, Activate);
        }

        /// <summary>The next +level, its success chance and the stat it raises, or why it cannot be enhanced.</summary>
        struct EnhanceView { public bool Can, Max; public string Reason, StatLabel; public int From, To, StatFrom, StatTo, Owned, Chance; }

        static EnhanceView Preview(PlayerState p, Item item)
        {
            var v = new EnhanceView { Owned = p != null ? p.UpgradeStone : 0, StatLabel = "ATK" };
            if (p == null || !EnhanceStats.IsEnhanceable(item)) { v.Reason = "Weapons only"; return v; }
            string key = EnhanceStats.StatKey(item);
            int baseValue = item.stats.GetInt(key);
            v.StatLabel = key == "atk" ? "ATK" : key == "def" ? "DEF" : key == "maxHp" ? "Max HP" : key == "maxMp" ? "Max MP" : key;
            v.From = EnhanceStats.Level(item);
            v.StatFrom = baseValue + EnhanceStats.Bonus(baseValue, v.From, EnhanceStats.Scale(item));
            if (v.From >= EnhanceConfig.MaxEnhance) { v.Max = true; v.To = v.From; v.StatTo = v.StatFrom; v.Reason = "Max +" + EnhanceConfig.MaxEnhance; return v; }
            IUpgradeMethod method = StoneUpgradeMethod.Instance;
            v.To = v.From + 1;
            v.StatTo = baseValue + EnhanceStats.Bonus(baseValue, v.To, EnhanceStats.Scale(item));
            v.Chance = RpgMath.Round(method.SuccessChance(v.From) * 100.0);
            v.Can = method.CanPay(p, v.From);
            if (!v.Can) v.Reason = "Need Upgrade Stone";
            return v;
        }

        /// <summary>Bag items only; the first tap arms it, the second discards.</summary>
        void DiscardSelected()
        {
            PlayerState p = RpgSession.Player;
            Item item = p != null ? SelectedItem(p) : null;
            if (item == null || wornSel >= 0) return;
            if (!confirmDiscard) { confirmDiscard = true; Refresh(); return; }
            confirmDiscard = false;
            if (NetMode.IsClient) { NetClient.SendInvAction(InvOp.Discard, item.uid, 0); return; }
            p.RemoveFromInventory(item.uid);
            Flash("Discarded " + item.name);
            Refresh();
        }

        void RefreshDetail(PlayerState p)
        {
            Supply kind;
            bool supply = wornSel < 0 && SupplyAtCell(cellSel, out kind);
            Item selected = SelectedItem(p);
            supplyDetail.gameObject.SetActive(supply);
            itemDetail.gameObject.SetActive(!supply && selected != null);
            detailSlot.Root.gameObject.SetActive(supply || selected != null);
            selectHint.enabled = !supply && selected == null;
            if (selectHint.enabled) selectHint.text = wornSel >= 0 ? "Empty slot. Pick an item from the list." : "Select an item.";
            if (supply) { SupplyAtCell(cellSel, out kind); ShowSupply(p, kind); }
            else if (selected != null) ShowItem(p, selected);
            if (!supply && selected == null) detailSlot.SetEnhance(null);
        }

        void ShowSupply(PlayerState p, Supply kind)
        {
            int count = SupplyCount(p, kind);
            detailSlot.SetFrame(false, BagWidgets.Hex("#70899d"));
            detailSlot.SetIcon(SupplySprite(kind), 42f, new Color(1f, 1f, 1f, count > 0 ? 1f : 0.4f));
            detailSlot.SetEnhance(null);
            detailSlot.Count.text = "";
            bool potion = IsPotion(kind);
            bool quick = potion && p.PotionPick(PotionIsHp(kind)) == PotionSize(kind);
            supplyName.text = potion ? PotionCatalog.Name(PotionIsHp(kind), PotionSize(kind)) : kind == Supply.Coin ? "Gold" : "Upgrade Stone";
            supplyQuantity.text = "Quantity: " + count + (quick ? "\n(quick button)" : "");
            supplyBlurb.text = potion ? PotionCatalog.Effect(PotionIsHp(kind), PotionSize(kind)) + " Use: drink it and set it on the quick button." : kind == Supply.Coin ? GoldBlurb : StoneBlurb;
            useButton.Set(potion);
        }

        void ShowItem(PlayerState p, Item item)
        {
            bool worn = wornSel >= 0, usable = p.CanEquip(item);
            detailSlot.SetFrame(false, LootSprites.RarityColor(item));
            detailSlot.SetIcon(LootSprites.Icon(item), 52f, usable ? Color.white : new Color(1f, 0.6f, 0.6f, 0.55f));
            detailSlot.SetEnhance(item);
            detailSlot.Count.text = "";
            itemName.text = EnhanceStats.DisplayName(item);
            itemName.color = LootSprites.RarityColor(item);
            itemSlotText.text = ItemInfo.SlotLabel(item.slot) + (string.IsNullOrEmpty(item.classId) || usable ? "" : "  <color=#ff8070>" + RpgClasses.Get(item.classId).Name + " only</color>");
            bool isPet = item.slot == Slots.Pet;
            itemLevelText.text = isPet ? "" : "Lv. " + item.itemLevel;

            Item wornItem = p.GetEquipped(item.slot);
            Item compare = wornItem != item ? wornItem : null;
            var lines = ItemInfo.Lines(item, compare);
            for (int i = 0; i < MaxStatLines; i++)
            {
                bool has = i < lines.Count;
                statRules[i].enabled = has;
                statTexts[i].text = has ? StatText(item, lines[i]) : "";
                compareTexts[i].text = "";
                if (!has || worn) continue; // no comparison for what is already on
                double d = compare != null ? lines[i].Delta : lines[i].Value; // nothing worn: the whole stat is a gain
                double shown = lines[i].Percent ? Math.Round(d * 100.0) : Math.Round(d);
                compareTexts[i].text = (shown > 0 ? "+" : "") + shown + (lines[i].Percent ? "%" : "");
                compareTexts[i].color = BagWidgets.Hex(shown > 0 ? "#8fff8f" : shown < 0 ? "#ff7070" : "#cfcfe0");
            }

            EnhanceView e = Preview(p, item);
            bool enhanceable = EnhanceStats.IsEnhanceable(item);
            foreach (Text t in extraTexts) t.text = "";
            if (isPet)
            {
                SetExtra(0, PetDurabilityLine(item), "#ffedb3");
                SetExtra(1, "Cannot be enhanced.", "#c2e3f4");
            }
            else if (enhanceable)
            {
                if (e.Max) SetExtra(0, "Enhance " + e.Reason, "#ffedb3");
                else
                {
                    SetExtra(0, "+" + e.From + " -> +" + e.To + "   Success " + e.Chance + "%", "#ffedb3");
                    SetExtra(1, e.StatLabel + " " + e.StatFrom + " -> " + e.StatTo, "#8fff8f");
                    SetExtra(2, "Stone: 1 (have " + e.Owned + ")", e.Can ? "#c7e2f2" : "#ff8f8f");
                }
            }

            equipButton.Set(true, worn ? "Unequip" : usable ? "Equip" : RpgClasses.Get(item.classId).Name + " only", !worn && !usable);
            enhanceButton.Set(enhanceable, null, !e.Can);
            RefreshDowngrade(item); // TEMPORARY test button
            discardButton.Set(!worn, confirmDiscard ? "Sure?" : "Discard");
        }

        void SetExtra(int i, string text, string color) { extraTexts[i].text = text; extraTexts[i].color = BagWidgets.Hex(color); }

        /// <summary>"ATK 21 (+4)": the effective value, plus the part that comes from enhancing.</summary>
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
