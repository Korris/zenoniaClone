using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>ArmoryWindow: fills the pinned paper for what is selected (see ArmoryWindow.ComparePanel.cs for its pieces). All texts come from the item / player data.</summary>
    public sealed partial class ArmoryWindow
    {
        static readonly Color Gain = PaperTheme.Hex("#1E7A2E"), Loss = PaperTheme.Hex("#C0261E");

        void RefreshCompare(PlayerState p)
        {
            BagSupply kind = BagSupply.Coin;
            bool worn = model.Worn >= 0, supply = !worn && model.SupplyAt(model.Cell, out kind);
            Item item = supply ? null : model.Selected(p);
            head.SetActive(supply || item != null);
            bagFace.SetActive(item != null && !worn);
            wornFace.SetActive(item != null && worn);
            supplyFace.SetActive(supply);
            hintText.text = "";
            if (downgradeButton != null) downgradeButton.Root.gameObject.SetActive(item != null && EnhanceStats.IsEnhanceable(item) && EnhanceStats.Level(item) > 0);
            if (supply) { ShowSupply(p, kind); return; }
            if (item == null) { ShowHint(p, worn); return; }

            bool usable = p.CanEquip(item), meets = worn || p.MeetsLevel(item);
            int tier = PaperTheme.RarityTier(item.rarity);
            detailSlot.Set(tier, LootSprites.Icon(item), true);
            ItemEnhanceSlotFx.Show(detailSlot.Icon, item);
            nameText.text = BagText.ItemName(item);
            typeText.text = ItemInfo.SlotLabel(item.slot) + (usable ? "" : "   <color=#C0261E>" + RpgClasses.Get(item.classId).Name + " only</color>");
            rarityPill.gameObject.SetActive(true);
            rarityText.text = BagText.RarityName(item);
            rarityPill.GetComponent<UnityEngine.UI.Image>().color = PaperTheme.Rarity(tier);
            rarityPill.sizeDelta = new Vector2(Mathf.Max(70f, rarityText.preferredWidth + 28f), 21f);
            levelNeedText.text = item.slot == Slots.Pet ? "" : EquipLevel.Label(item);
            levelNeedText.color = meets ? PaperTheme.Ink : Loss;
            equippedLabel.SetActive(worn);
            ownedText.text = "";
            if (worn) ShowWorn(p, item); else ShowBagItem(p, item, usable);
        }

        void ShowHint(PlayerState p, bool worn)
        {
            LayoutActions(null, false, null);
            if (!worn) { hintText.text = model.Cells.Count == 0 ? "Nothing to show here." : "Select an item."; return; }
            if (Slots.All[model.Worn] != Slots.Pet) { hintText.text = "Empty slot.\nPick an item from the bag."; return; }
            PetDef pet = p.Pet; // the Pet pedestal reached with the arrow keys (a tap opens the Pet window at once)
            hintText.text = pet != null ? "Combat pet: " + pet.Name + "  " + PetArt.Stars(p.PetStars) : "No combat pet.";
            LayoutActions("Pet Window", false, null);
            mainButton.SetEnabled(true);
        }

        void ShowSupply(PlayerState p, BagSupply kind)
        {
            detailSlot.Set(0, BagText.SupplySprite(kind), true);
            ItemEnhanceSlotFx.Show(detailSlot.Icon, null);
            nameText.text = BagText.SupplyName(kind);
            typeText.text = BagModel.IsPotion(kind) ? "Consumable" : "Material";
            rarityPill.gameObject.SetActive(false);
            levelNeedText.text = "";
            equippedLabel.SetActive(false);
            ownedText.text = "Owned: " + BagModel.SupplyCount(p, kind);
            blurbText.text = BagText.SupplyBlurb(kind) + (BagText.IsQuickPotion(p, kind) ? "\n\nThis size is on the quick button." : "");
            LayoutActions(BagModel.IsPotion(kind) ? "Use" : null, false, null);
            mainButton.SetEnabled(true);
        }

        /// <summary>A bag item: every stat against what is worn in its slot, then one note line (enhance preview of a weapon, or what the item does not do).</summary>
        void ShowBagItem(PlayerState p, Item item, bool usable)
        {
            Item wornItem = p.GetEquipped(item.slot);
            var lines = ItemInfo.Lines(item, wornItem);
            for (int i = 0; i < StatRows; i++)
            {
                bool has = i < lines.Count;
                rowLabel[i].text = has ? lines[i].Label.ToUpperInvariant() : "";
                rowItem[i].text = has ? BagText.TableValue(lines[i].Value, lines[i].Percent) : "";
                rowWorn[i].text = !has ? "" : wornItem != null ? BagText.TableValue(lines[i].Value - lines[i].Delta, lines[i].Percent) : "--";
                rowChange[i].text = "";
                if (!has) continue;
                double shown;
                rowChange[i].text = "(" + BagText.Change(lines[i], wornItem != null, out shown) + ")";
                rowChange[i].color = shown > 0 ? Gain : shown < 0 ? Loss : PaperTheme.InkSoft;
            }
            noteText.rectTransform.anchoredPosition = new Vector2(4f, -(46f + Mathf.Min(lines.Count, StatRows) * 17f));
            bool enhanceable = EnhanceStats.IsEnhanceable(item);
            if (confirmDiscard) { noteText.text = "Discard " + BagText.ItemName(item) + "? It cannot be recovered."; noteText.color = Loss; }
            else
            {
                noteText.color = PaperTheme.InkSoft;
                BagEnhanceView e = BagActions.Preview(p, item);
                noteText.text = item.slot == Slots.Pet ? "Becomes an account pet when you next log in. Pets: Pet window [P]."
                    : !enhanceable ? ItemInfo.ChangesAppearance(item.slot) ? "" : ItemInfo.NoAppearanceLine + "."
                    : e.Max ? "Enhance " + e.Reason + "."
                    : "Enhance +" + e.From + " to +" + e.To + ": success " + e.Chance + "%, " + e.StatLabel + " " + e.StatFrom + " to " + e.StatTo + ". Costs 1 Upgrade Stone (have " + e.Owned + ").";
            }
            if (confirmDiscard) LayoutActions("Cancel", false, "Discard");
            else LayoutActions(usable ? "Equip" : "Class only", enhanceable, "Discard");
            mainButton.SetEnabled(confirmDiscard || usable);
        }

        /// <summary>A worn item: its stats on one line and, for a weapon / offhand, the next enhance step (level, stat, chance, stones).</summary>
        void ShowWorn(PlayerState p, Item item)
        {
            var lines = ItemInfo.Lines(item);
            string stats = "";
            foreach (StatLine l in lines) stats += (stats == "" ? "" : "      ") + l.Label.ToUpperInvariant() + "  " + BagText.TableValue(l.Value, l.Percent);
            wornStatsText.text = stats;
            bool enhanceable = EnhanceStats.IsEnhanceable(item);
            enhanceBlock.SetActive(enhanceable);
            wornNoteText.text = enhanceable || ItemInfo.ChangesAppearance(item.slot) ? "" : ItemInfo.NoAppearanceLine + ".";
            if (enhanceable)
            {
                BagEnhanceView e = BagActions.Preview(p, item);
                plusText.text = e.Max ? "+" + e.From + "  (max)" : "+" + e.From + "   >>   <color=#1E7A2E>+" + e.To + "</color>";
                gainText.text = e.Max ? e.StatLabel + "  " + e.StatFrom : e.StatLabel + "   " + e.StatFrom + "   >>   <color=#1E7A2E>" + e.StatTo + "</color>";
                chanceBar.Root.gameObject.SetActive(!e.Max);
                chanceBar.Set(e.Chance / 100f);
                chanceBar.SetText("Success  " + e.Chance + "%");
                stonesText.text = e.Max ? "Upgrade Stones  " + e.Owned : "Upgrade Stones  1 / " + e.Owned;
                stonesText.color = e.Max || e.Can ? PaperTheme.Ink : Loss;
            }
            LayoutActions("Unequip", enhanceable, null, true);
            mainButton.SetEnabled(true);
        }
    }
}
