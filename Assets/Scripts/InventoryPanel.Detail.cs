using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using W = ZenoniaProto.InventoryRoWidgets;
using B = ZenoniaProto.InventoryRoBoxes;

namespace ZenoniaProto
{
    /// <summary>
    /// Right panel: the selected item (icon, name +N, rarity pill, type, Req. Lv), "Equipped vs Selected" for ATK, DEF, HP, Crit, Speed
    /// (and MP when either item has it) with green up / red down changes, Effect (enhance preview, hat source ...), then Equip / Enhance /
    /// Discard (Discard asks "Sure?" first). A potion or stone shows its count and blurb, with Use for potions. Coordinates are local to the panel.
    /// </summary>
    public partial class InventoryPanel
    {
        static readonly string[] CompareStats = { "ATK", "DEF", "HP", "Crit", "Speed", "MP" };
        static readonly string[] CompareIcons = { "bag-stat-atk", "bag-stat-def", "bag-stat-hp", "bag-stat-crit", "bag-stat-speed", "bag-stat-hp" };
        const float RowY = 184f, RowStep = 27f, RowX = 14f, RowW = RightW - 28f; // table rows: zebra stripes, 1 px rules

        readonly Text[] rowLabels = new Text[6], rowWorn = new Text[6], rowNew = new Text[6], rowDiff = new Text[6];
        readonly Image[] rowIcons = new Image[6], rowArrows = new Image[6], rowBacks = new Image[6];
        RectTransform itemDetail, rarityPlaque, compareGroup;
        W.Cell detailSlot;
        Text selectHint, itemName, itemTypeText, itemLevelText, rarityText, wornName, newName, effectTitle, effectText;
        B.IconButton equipButton, enhanceButton, discardButton;

        void BuildDetail(RectTransform right)
        {
            selectHint = W.Txt(right, "Hint", 20f, 200f, RightW - 40f, 60f, "", 17, W.InkSoft, TextAnchor.MiddleCenter, false);
            detailSlot = new W.Cell(right, "DetailSlot", 14f, 14f, 84f, null);
            detailSlot.Back.sprite = W.Art("bag-detail-frame");

            itemDetail = UiKit.Rect(right, "ItemDetail", 0f, 0f, RightW, PanelH);
            itemName = W.Txt(itemDetail, "Name", 108f, 10f, RightW - 118f, 36f, "", 24, W.Good);
            itemName.horizontalOverflow = HorizontalWrapMode.Wrap;
            itemName.resizeTextForBestFit = true; itemName.resizeTextMinSize = 14; itemName.resizeTextMaxSize = 24;
            B.PillBox rarity = B.Pill(itemDetail, "RarityPlaque", 108f, 47f, 26f, "bag-pill-white", "", 15, Color.white, 70f);
            rarityPlaque = rarity.Root;
            rarityText = rarity.Label;
            itemTypeText = W.Txt(itemDetail, "Type", 110f, 76f, 150f, 24f, "", 15, W.InkSoft, TextAnchor.MiddleLeft, false);
            itemLevelText = W.Txt(itemDetail, "Level", RightW - 120f, 76f, 106f, 24f, "", 15, W.Ink, TextAnchor.MiddleRight, false);

            compareGroup = UiKit.Rect(itemDetail, "Compare", 0f, 0f, RightW, PanelH); // hidden for potions and stones
            W.Img(compareGroup, "CompareBox", 8f, 104f, RightW - 16f, 244f, "bag-section").color = new Color(1f, 1f, 1f, 0.82f); // a softer blue rim
            B.Pill(compareGroup, "EquippedPill", 84f, 116f, 26f, "bag-pill-white", "Equipped", 15, W.InkSoft, 90f, true).Skin.color = new Color(0.86f, 0.89f, 0.93f);
            B.Pill(compareGroup, "SelectedPill", 255f, 116f, 26f, "bag-pill-mint", "Selected", 15, Color.white, 90f, true);
            wornName = W.Txt(compareGroup, "EquippedName", 14f, 149f, 140f, 26f, "", 14, W.Ink, TextAnchor.MiddleCenter);
            W.Img(compareGroup, "Arrow", 158f, 153f, 22f, 18f, "bag-ico-compare");
            newName = W.Txt(compareGroup, "SelectedName", 184f, 149f, 142f, 26f, "", 14, W.Good, TextAnchor.MiddleCenter);
            for (int i = 0; i < CompareStats.Length; i++)
            {
                float y = RowY + i * RowStep;
                rowBacks[i] = W.Img(compareGroup, "RowBack" + i, RowX, y, RowW, RowStep, null);
                rowBacks[i].color = i % 2 == 0 ? RoSoftTheme.Hex("#F7F9FC") : RoSoftTheme.Hex("#EEF3F8");
                W.Img(rowBacks[i].transform, "RowRule", 0f, RowStep - 1f, RowW, 1f, null).color = RoSoftTheme.Hex("#C9D7E5");
                W.Img(rowBacks[i].transform, "ColRule", 162f - RowX, 0f, 1f, RowStep, null).color = RoSoftTheme.Hex("#C9D7E5");
                rowIcons[i] = W.Img(compareGroup, "StatIcon" + i, 20f, y + 2.5f, 22f, 22f, CompareIcons[i]);
                rowLabels[i] = W.Txt(compareGroup, "Stat" + i, 50f, y, 70f, RowStep, CompareStats[i], 16, W.Ink);
                rowWorn[i] = W.Txt(compareGroup, "Worn" + i, 98f, y, 56f, RowStep, "", 16, W.Ink, TextAnchor.MiddleRight);
                rowNew[i] = W.Txt(compareGroup, "New" + i, 176f, y, 62f, RowStep, "", 16, W.Good, TextAnchor.MiddleRight);
                rowArrows[i] = W.Img(compareGroup, "Arrow" + i, 250f, y + 7.5f, 14f, 12f, "bag-arrow-up");
                rowDiff[i] = W.Txt(compareGroup, "Diff" + i, 266f, y, 56f, RowStep, "", 16, W.Good, TextAnchor.MiddleRight);
            }

            W.Img(itemDetail, "EffectBox", 8f, 352f, RightW - 16f, 104f, "bag-section").color = new Color(1f, 1f, 1f, 0.82f);
            W.Img(itemDetail, "EffectStrip", 16f, 359f, 150f, 26f, "bag-label");
            effectTitle = W.Txt(itemDetail, "EffectTitle", 48f, 359f, 116f, 25f, "Effect", 16, W.Ink);
            effectText = W.Txt(itemDetail, "EffectText", 26f, 389f, RightW - 52f, 56f, "", 14, W.Ink, TextAnchor.UpperLeft, false);
            effectText.horizontalOverflow = HorizontalWrapMode.Wrap;
            effectText.lineSpacing = 1.1f;

            equipButton = new B.IconButton(right, "Equip", 10f, 462f, 104f, 54f, "btn-mint", "bag-ico-equip", "Equip", 20, Activate);
            enhanceButton = new B.IconButton(itemDetail, "Enhance", 118f, 462f, 112f, 54f, "btn-sky", "bag-ico-enhance", "Enhance", 18, EnhanceSelected);
            discardButton = new B.IconButton(itemDetail, "Discard", 234f, 462f, 96f, 54f, "btn-coral", "bag-ico-discard", "Discard", 16, DiscardSelected);
            BuildDowngrade(itemDetail);
        }

        /// <summary>Bag items only; the first tap arms it ("Sure?"), the second discards (the server decides online).</summary>
        void DiscardSelected()
        {
            PlayerState p = RpgSession.Player;
            Item item = p != null ? model.Selected(p) : null;
            if (item == null || wornSel >= 0) return;
            if (!confirmDiscard) { confirmDiscard = true; Flash("Tap Sure? to discard " + BagText.ItemName(item) + "."); Refresh(); return; }
            confirmDiscard = false;
            Show(BagActions.Discard(p, item));
        }

        void RefreshDetail(PlayerState p)
        {
            BagSupply kind = BagSupply.Coin;
            bool supply = wornSel < 0 && model.SupplyAt(model.Cell, out kind);
            Item selected = supply ? null : model.Selected(p);
            itemDetail.gameObject.SetActive(supply || selected != null);
            detailSlot.Root.gameObject.SetActive(supply || selected != null);
            equipButton.Set(supply || selected != null);
            selectHint.text = supply || selected != null ? "" : wornSel >= 0 ? "Empty slot.\nPick an item from the bag." : "Select an item.";
            detailSlot.SetState(false, false, Color.white);
            detailSlot.Back.sprite = W.Art("bag-detail-frame");
            if (supply) ShowSupply(p, kind);
            else if (selected != null) ShowItem(p, selected);
        }

        void ShowSupply(PlayerState p, BagSupply kind)
        {
            int count = BagModel.SupplyCount(p, kind);
            detailSlot.SetIcon(BagText.SupplySprite(kind), Color.white, 0.74f);
            ItemEnhanceSlotFx.Show(detailSlot.Icon, null);
            detailSlot.Count.text = "";
            itemName.text = BagText.SupplyName(kind);
            itemName.color = W.Ink;
            rarityPlaque.gameObject.SetActive(false);
            itemTypeText.text = BagModel.IsPotion(kind) ? "Consumable" : "Material";
            itemLevelText.text = "x" + count;
            compareGroup.gameObject.SetActive(false);
            effectTitle.text = "Effect";
            effectText.text = BagText.SupplyBlurb(kind) + (BagText.IsQuickPotion(p, kind) ? "\nOn the quick potion button." : "");
            equipButton.Set(BagModel.IsPotion(kind), "Use", count <= 0);
            enhanceButton.Set(false);
            discardButton.Set(false);
            RefreshDowngrade(null);
        }

        void ShowItem(PlayerState p, Item item)
        {
            bool worn = wornSel >= 0, usable = p.CanEquip(item);
            Color rarity = LootSprites.RarityColor(item);
            if (rarity.maxColorComponent - Mathf.Min(rarity.r, Mathf.Min(rarity.g, rarity.b)) < 0.15f) rarity = RoSoftTheme.Hex("#8C99A6"); // common (white / grey) items: a grey pill, ink name
            detailSlot.SetIcon(LootSprites.Icon(item), !usable ? new Color(1f, 0.6f, 0.6f, 0.55f) : worn || p.MeetsLevel(item) ? Color.white : new Color(0.55f, 0.55f, 0.6f, 0.6f), 0.8f);
            ItemEnhanceSlotFx.Show(detailSlot.Icon, item);
            detailSlot.Count.text = "";
            itemName.text = BagText.ItemName(item);
            itemName.color = Color.Lerp(rarity, W.Ink, 0.35f);
            rarityPlaque.gameObject.SetActive(true);
            rarityText.text = BagText.RarityName(item);
            rarityPlaque.GetComponent<Image>().color = Color.Lerp(rarity, Color.white, 0.1f);
            itemTypeText.text = ItemInfo.SlotLabel(item.slot) + (string.IsNullOrEmpty(item.classId) || usable ? "" : "  <color=#d2483f>" + RpgClasses.Get(item.classId).Name + " only</color>");
            bool isPet = item.slot == Slots.Pet, isHat = item.slot == Slots.Headgear;
            itemLevelText.text = isPet || isHat ? "" : !EquipLevel.Applies(item) ? "Lv. " + item.itemLevel : worn || p.MeetsLevel(item) ? EquipLevel.Label(item) : "<color=#d2483f>" + EquipLevel.Label(item) + "</color>";

            compareGroup.gameObject.SetActive(true);
            Item wornItem = p.GetEquipped(item.slot);
            Item compare = worn ? item : wornItem;
            wornName.text = compare != null ? BagText.ItemName(compare) : "(none)";
            newName.text = BagText.ItemName(item);
            newName.color = Color.Lerp(rarity, W.Ink, 0.35f);
            ShowCompare(item, compare, worn);

            BagEnhanceView e = BagActions.Preview(p, item);
            bool enhanceable = EnhanceStats.IsEnhanceable(item);
            effectTitle.text = isPet ? "Old Pet" : enhanceable ? "Enhance" : "Effect";
            effectText.text = EffectLines(item, e, enhanceable, isPet, isHat);

            equipButton.Set(true, worn ? "Unequip" : usable ? "Equip" : "Class only", !worn && !usable);
            enhanceButton.Set(true, null, !enhanceable || !e.Can);
            discardButton.Set(true, confirmDiscard ? "Sure?" : "Discard", worn);
            RefreshDowngrade(item);
        }

        /// <summary>Fixed rows ATK, DEF, HP, Crit, Speed (+ MP when either item has it): what is worn, what is selected, the change.</summary>
        void ShowCompare(Item item, Item worn, bool same)
        {
            var byLabel = new Dictionary<string, StatLine>();
            if (item != null) foreach (StatLine l in ItemInfo.Lines(item, same ? null : worn)) byLabel[l.Label] = l;
            for (int i = 0; i < CompareStats.Length; i++)
            {
                StatLine l;
                bool has = byLabel.TryGetValue(CompareStats[i], out l);
                bool shown = item != null && (i < 5 || has);
                rowIcons[i].enabled = rowArrows[i].enabled = shown;
                rowBacks[i].gameObject.SetActive(shown); // with its rules
                rowLabels[i].text = shown ? CompareStats[i] : "";
                if (!shown) { rowWorn[i].text = rowNew[i].text = rowDiff[i].text = ""; continue; }
                bool percent = i == 3 || i == 4;
                double now = has ? l.Value : 0.0, before = same ? now : worn == null ? 0.0 : has ? l.Value - l.Delta : WornValue(worn, CompareStats[i]);
                rowWorn[i].text = worn == null && !same ? "-" : BagText.TableValue(before, percent);
                rowNew[i].text = BagText.TableValue(now, percent);
                double d = System.Math.Round((now - before) * (percent ? 100.0 : 1.0));
                rowArrows[i].enabled = d != 0.0;
                W.SetArt(rowArrows[i], d >= 0 ? "bag-arrow-up" : "bag-arrow-down");
                Color c = d > 0 ? W.Good : d < 0 ? W.Bad : W.InkSoft;
                rowNew[i].color = rowDiff[i].color = c;
                rowDiff[i].text = d == 0.0 ? "-" : (d > 0 ? "+" : "") + d + (percent ? "%" : "");
            }
        }

        /// <summary>A stat of the worn item alone (the selected item lacks it, so ItemInfo.Lines gave no line).</summary>
        static double WornValue(Item worn, string label)
        {
            foreach (StatLine l in ItemInfo.Lines(worn)) if (l.Label == label) return l.Value;
            return 0.0;
        }

        static string EffectLines(Item item, BagEnhanceView e, bool enhanceable, bool isPet, bool isHat)
        {
            if (isPet) return "Becomes an account pet when you next log in.\nPets: Pet window [P].";
            if (isHat) return "Cosmetic hat: changes your look only. No stats.\n" + BagText.HatSource(item);
            if (enhanceable)
            {
                if (e.Max) return "Enhance " + e.Reason;
                return "+" + e.From + " -> +" + e.To + "   Success " + e.Chance + "%\n" + e.StatLabel + " " + e.StatFrom + " -> " + e.StatTo
                    + "\n<color=" + (e.Can ? "#5F7488" : "#D2483F") + ">Upgrade Stone: 1 (have " + e.Owned + ")</color>";
            }
            return ItemInfo.ChangesAppearance(item.slot) ? "" : ItemInfo.NoAppearanceLine;
        }
    }
}
