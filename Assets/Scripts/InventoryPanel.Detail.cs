using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Right panel: the selected item or supply, its stats compared with what is worn, the enhance preview (BagActions.Preview), and Equip / Unequip, Enhance, Discard. Coordinates are local to the panel.</summary>
    public partial class InventoryPanel
    {
        const int MaxStatLines = 6;

        readonly Text[] statTexts = new Text[MaxStatLines], compareTexts = new Text[MaxStatLines], extraTexts = new Text[3];
        readonly Text[] itemValues = new Text[MaxStatLines], equippedValues = new Text[MaxStatLines];
        readonly Image[] statRules = new Image[MaxStatLines];
        RectTransform itemDetail, supplyDetail, rarityPlaque;
        BagWidgets.Slot detailSlot;
        Text selectHint, itemName, itemSlotText, itemLevelText, supplyName, supplyQuantity, supplyBlurb, rarityText, enhanceTitle;
        BagWidgets.BagButton equipButton, useButton, enhanceButton, discardButton;

        void BuildDetail(RectTransform right)
        {
            const float Width = RightW, Inner = RightW - 28f, Left = 14f;
            selectHint = BagWidgets.Text(right, "Hint", Width * 0.5f, 170f, "", 15, "#d9d3bd", TextAnchor.UpperCenter, Inner);
            selectHint.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailSlot = new BagWidgets.Slot(right, "DetailSlot", Left + 4f, 18f, 96f, null);

            itemDetail = UiKit.Rect(right, "ItemDetail", 0f, 0f, Width, PanelH);
            itemName = BagWidgets.Text(itemDetail, "Name", 122f, 12f, "", 22, "#ffedb3", TextAnchor.UpperLeft, 152f, true);
            itemName.horizontalOverflow = HorizontalWrapMode.Wrap;
            itemName.rectTransform.sizeDelta = new Vector2(152f, 50f);
            itemSlotText = BagWidgets.Text(itemDetail, "Slot", 122f, 59f, "", 16, "#fff5dc", TextAnchor.UpperLeft, 152f);
            rarityPlaque = BagWidgets.Img(itemDetail, "RarityPlaque", 120f, 80f, 100f, 22f, BagSprites.RarityTag(), Color.white).rectTransform;
            rarityText = UiKit.Label(UiKit.Fill(rarityPlaque, "Rarity"), "", 14, BagWidgets.Hex("#ffedb3"), TextAnchor.MiddleCenter, true);
            BagWidgets.Style(rarityText, true);
            rarityText.fontStyle = FontStyle.Bold;
            rarityText.alignByGeometry = true;
            rarityText.horizontalOverflow = HorizontalWrapMode.Overflow;
            itemLevelText = BagWidgets.Text(itemDetail, "Level", 122f, 105f, "", 14, "#ffffff", TextAnchor.UpperLeft, 152f);
            BagWidgets.Divider(itemDetail, "ItemRule", Left, 128f, Inner);
            BagWidgets.Img(itemDetail, "TableHeader", Left, 140f, Inner, 23f, BagSprites.Panel(), Color.white);
            BagWidgets.Text(itemDetail, "StatHeader", Left + 5f, 143f, "STAT", 11, "#ffedb3", TextAnchor.UpperLeft, 54f, true);
            BagWidgets.Text(itemDetail, "ItemHeader", 110f, 143f, "ITEM", 11, "#ffedb3", TextAnchor.UpperRight, 48f, true);
            BagWidgets.Text(itemDetail, "EquippedHeader", 207f, 143f, "EQUIPPED", 11, "#ffedb3", TextAnchor.UpperRight, 88f, true);
            BagWidgets.Text(itemDetail, "DiffHeader", 274f, 143f, "DIFF", 11, "#ffedb3", TextAnchor.UpperRight, 48f, true);
            for (int i = 0; i < MaxStatLines; i++)
            {
                float y = 169f + i * 20f;
                statRules[i] = BagWidgets.Img(itemDetail, "Rule" + i, Left, y - 5f, Inner, 1f, null, BagWidgets.Hex("#c9a46a55"));
                statTexts[i] = BagWidgets.Text(itemDetail, "Stat" + i, Left + 5f, y, "", 15, "#ffffff", TextAnchor.UpperLeft, 64f, true);
                itemValues[i] = BagWidgets.Text(itemDetail, "ItemValue" + i, 110f, y, "", 16, "#ffffff", TextAnchor.UpperRight, 48f);
                equippedValues[i] = BagWidgets.Text(itemDetail, "EquippedValue" + i, 197f, y, "", 16, "#ffffff", TextAnchor.UpperRight, 64f);
                compareTexts[i] = BagWidgets.Text(itemDetail, "Compare" + i, Left + Inner, y, "", 16, "#cfcfe0", TextAnchor.UpperRight, 60f, true);
            }
            for (int i = 0; i < extraTexts.Length; i++)
            {
                extraTexts[i] = BagWidgets.Text(itemDetail, "Extra" + i, Left + 4f, 330f + i * 22f, "", 16, "#ffedb3", TextAnchor.UpperLeft, Inner - 8f);
                extraTexts[i].horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            BagWidgets.Divider(itemDetail, "EnhanceRule", Left, 298f, Inner);
            enhanceTitle = BagWidgets.Text(itemDetail, "EnhanceTitle", Left + 4f, 305f, "Enhance Preview", 19, "#ffedb3", TextAnchor.UpperLeft, Inner, true);
            BagWidgets.Divider(itemDetail, "ActionRule", Left, 396f, Inner);
            discardButton = new BagWidgets.BagButton(itemDetail, "Discard", Left, 412f, 76f, 38f, "Discard", BagSprites.Tone.Steel, DiscardSelected);
            discardButton.Label.fontSize = 16;
            equipButton = new BagWidgets.BagButton(itemDetail, "Equip", 94f, 412f, 82f, 38f, "Equip", BagSprites.Tone.Blue, Activate);
            equipButton.Label.fontSize = 17;
            enhanceButton = new BagWidgets.BagButton(itemDetail, "Enhance", 180f, 412f, 96f, 38f, "Enhance", BagSprites.Tone.Gold, EnhanceSelected);
            enhanceButton.Label.fontSize = 17;
            BuildDowngrade(Left, 370f, Inner);
            if (downgradeButton != null) { downgradeButton.Root.anchoredPosition = new Vector2(238f, -370f); downgradeButton.Root.sizeDelta = new Vector2(38f, 22f); downgradeButton.Label.fontSize = 12; }

            supplyDetail = UiKit.Rect(right, "SupplyDetail", 0f, 0f, Width, PanelH);
            supplyName = BagWidgets.Text(supplyDetail, "Name", 122f, 24f, "", 18, "#ffedb3", TextAnchor.UpperLeft, 152f, true);
            supplyName.horizontalOverflow = HorizontalWrapMode.Wrap;
            supplyQuantity = BagWidgets.Text(supplyDetail, "Quantity", Left, 144f, "", 14, "#ffffff", TextAnchor.UpperLeft, Inner, true);
            supplyQuantity.horizontalOverflow = HorizontalWrapMode.Wrap;
            supplyBlurb = BagWidgets.Text(supplyDetail, "Blurb", Left, 204f, "", 14, "#c2e3f4", TextAnchor.UpperLeft, Inner);
            supplyBlurb.horizontalOverflow = HorizontalWrapMode.Wrap;
            useButton = new BagWidgets.BagButton(supplyDetail, "Use", Left, 412f, Inner, 38f, "Use", BagSprites.Tone.Gold, Activate);
        }

        /// <summary>Bag items only; the first tap arms it, the second discards.</summary>
        void DiscardSelected()
        {
            PlayerState p = RpgSession.Player;
            Item item = p != null ? model.Selected(p) : null;
            if (item == null || wornSel >= 0) return;
            if (!confirmDiscard) { confirmDiscard = true; Refresh(); return; }
            confirmDiscard = false;
            Show(BagActions.Discard(p, item));
        }

        void RefreshDetail(PlayerState p)
        {
            BagSupply kind = BagSupply.Coin;
            bool supply = wornSel < 0 && model.SupplyAt(model.Cell, out kind);
            Item selected = model.Selected(p);
            supplyDetail.gameObject.SetActive(supply);
            itemDetail.gameObject.SetActive(!supply && selected != null);
            detailSlot.Root.gameObject.SetActive(supply || selected != null);
            selectHint.enabled = !supply && selected == null;
            if (selectHint.enabled) selectHint.text = wornSel >= 0 ? "Empty slot. Pick an item from the list." : "Select an item.";
            if (supply) ShowSupply(p, kind);
            else if (selected != null) ShowItem(p, selected);
            if (!supply && selected == null) detailSlot.SetEnhance(null);
        }

        void ShowSupply(PlayerState p, BagSupply kind)
        {
            int count = BagModel.SupplyCount(p, kind);
            detailSlot.SetFrame(false, BagWidgets.Hex("#70899d"));
            detailSlot.SetIcon(BagText.SupplySprite(kind), 72f, new Color(1f, 1f, 1f, count > 0 ? 1f : 0.4f));
            detailSlot.SetEnhance(null);
            detailSlot.Count.text = "";
            supplyName.text = BagText.SupplyName(kind);
            supplyQuantity.text = "Quantity: " + count + (BagText.IsQuickPotion(p, kind) ? "\n(quick button)" : "");
            supplyBlurb.text = BagText.SupplyBlurb(kind);
            useButton.Set(BagModel.IsPotion(kind));
        }

        void ShowItem(PlayerState p, Item item)
        {
            bool worn = wornSel >= 0, usable = p.CanEquip(item);
            detailSlot.SetFrame(true, LootSprites.RarityColor(item));
            detailSlot.SetIcon(LootSprites.Icon(item), 80f, !usable ? new Color(1f, 0.6f, 0.6f, 0.55f) : worn || p.MeetsLevel(item) ? Color.white : new Color(0.55f, 0.55f, 0.6f, 0.6f));
            detailSlot.SetEnhance(item);
            detailSlot.Count.text = "";
            itemName.text = BagText.ItemName(item);
            itemName.color = BagWidgets.Hex("#ffe18a");
            rarityText.text = BagText.RarityName(item).ToUpperInvariant();
            rarityText.color = BagWidgets.Hex("#ffe8a0");
            rarityPlaque.sizeDelta = new Vector2(Mathf.Ceil(rarityText.preferredWidth) + 26f, 22f);
            itemSlotText.text = ItemInfo.SlotLabel(item.slot) + (string.IsNullOrEmpty(item.classId) || usable ? "" : "  <color=#ff8070>" + RpgClasses.Get(item.classId).Name + " only</color>");
            bool isPet = item.slot == Slots.Pet;
            itemLevelText.text = isPet ? "" : !EquipLevel.Applies(item) ? "Lv. " + item.itemLevel : worn || p.MeetsLevel(item) ? EquipLevel.Label(item) : "<color=#ff7070>" + EquipLevel.Label(item) + "</color>";

            Item wornItem = p.GetEquipped(item.slot);
            Item compare = wornItem != item ? wornItem : null;
            var lines = ItemInfo.Lines(item, compare);
            for (int i = 0; i < MaxStatLines; i++)
            {
                bool has = i < lines.Count;
                statRules[i].enabled = has;
                statTexts[i].text = has ? lines[i].Label.ToUpperInvariant() : "";
                itemValues[i].text = has ? BagText.TableValue(lines[i].Value, lines[i].Percent) : "";
                equippedValues[i].text = has ? worn ? BagText.TableValue(lines[i].Value, lines[i].Percent) : compare != null ? BagText.TableValue(lines[i].Value - lines[i].Delta, lines[i].Percent) : "--" : "";
                compareTexts[i].text = "";
                if (!has || worn) continue; // no comparison for what is already on
                double shown;
                compareTexts[i].text = BagText.Change(lines[i], compare != null, out shown);
                compareTexts[i].color = BagWidgets.Hex(shown > 0 ? "#8fff8f" : shown < 0 ? "#ff7070" : "#cfcfe0");
            }

            BagEnhanceView e = BagActions.Preview(p, item);
            bool enhanceable = EnhanceStats.IsEnhanceable(item);
            enhanceTitle.text = isPet ? "Old Pet" : enhanceable ? "Enhance Preview" : "Item Details";
            foreach (Text t in extraTexts) t.text = "";
            if (isPet)
            {
                SetExtra(0, "Becomes an account pet when you next log in.", "#ffedb3"); // only an old save shows one, before AccountPets.Migrate
                SetExtra(1, "Pets: Pet window [P].", "#c2e3f4");
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
            else if (!ItemInfo.ChangesAppearance(item.slot)) SetExtra(0, ItemInfo.NoAppearanceLine, "#c2e3f4"); // necklace, gloves, boots: stats only

            equipButton.Set(true, worn ? "Unequip" : usable ? "Equip" : "Class only", !worn && !usable);
            enhanceButton.Set(enhanceable, null, !e.Can);
            RefreshDowngrade(item); // TEMPORARY test button
            discardButton.Set(!worn, confirmDiscard ? "Sure?" : "Discard");
        }

        void SetExtra(int i, string text, string color) { extraTexts[i].text = text; extraTexts[i].color = BagWidgets.Hex(color); }
    }
}
