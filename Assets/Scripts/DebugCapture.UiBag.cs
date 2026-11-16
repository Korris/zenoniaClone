using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the rebuilt bag, the Armory Carousel (ArmoryWindow; phase 05 of plans/261116-1206-adventure-paper-ui-rebuild).
    /// Use them with --ui-new bag --open-inventory (and --fill-bag N, --level N, --plus N, --give-stones N, --pet ID, --worn-tier N ...).
    /// The old flags work on the new look too: --bag-item N (that bag item's card selected), --select-worn SLOT, --select-pet.
    ///   --bag-clear                 empty bag, no potions, no stones (the empty state)
    ///   --bag-add-class wizard      one weapon of another class in the bag (the "Wizard only" card)
    ///   --bag-filter weapon|offhand|armor|gloves|boots|necklace|pet|consumable|material|all
    ///   --bag-sort bagorder|rarity|level|type|tidy   (tidy = the old Sort button: reorders the bag itself)
    ///   --bag-view grid|shelf       the board with every slot / the card shelf
    ///   --bag-page N                shelf page N (1 = first)
    ///   --bag-supply hp|mp|stone    that supply's card selected
    ///   --bag-confirm discard       the discard question of the selected bag item
    ///   --bag-press main|enhance|discard|enhance-equipped[,...]   press those buttons in order (main = Equip / Unequip / Use / Cancel)
    ///   --bag-drag-demo             the selected card held on the way to its pedestal
    ///   --bag-drag-drop SLOT|hero   drag the selected card for real and drop it on that pedestal / on the hero (equip, or the "does not go" message)
    ///   --bag-stats / --bag-sort-open   the Stats popup / the open Sort list
    /// Keys: --ui-keys "1=Inventory;2=Right;3=Attack;4=Close" (DebugCapture.UiRedesign.cs) presses the real keys.
    /// </summary>
    public partial class DebugCapture
    {
        partial void DebugUiBag()
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            bool changed = false;
            if (Flag("--bag-clear"))
            {
                p.Inventory.Clear();
                p.UpgradeStone = p.HpPotions = p.MpPotions = p.HpPotionsMedium = p.HpPotionsLarge = p.MpPotionsMedium = p.MpPotionsLarge = 0;
                changed = true;
            }
            string foreign = Val("--bag-add-class");
            if (foreign != null && RpgClasses.IsValid(foreign))
            {
                if (p.Inventory.Count >= PlayerState.InventorySize) p.Inventory.RemoveAt(p.Inventory.Count - 1);
                p.AddToInventory(ItemFactory.Generate(RpgRandom.Default, Mathf.Max(1, p.Level - 5), Slots.Weapon, "rare", foreign));
                changed = true;
            }
            if (!ArmoryWindow.IsOpen) { if (changed) InventoryPanel.RefreshIfOpen(); return; } // the old look: only the bag contents above apply
            if (changed) ArmoryWindow.TestFilter(BagFilter.All); // redraws: the selections below read the new contents

            string filter = Val("--bag-filter"), sort = Val("--bag-sort"), view = Val("--bag-view"), page = Val("--bag-page");
            BagFilter f;
            if (filter != null && Enum.TryParse(filter, true, out f)) ArmoryWindow.TestFilter(f);
            BagSort s;
            if (sort == "tidy") ArmoryWindow.TestSort(4);
            else if (sort != null && Enum.TryParse(sort, true, out s)) ArmoryWindow.TestSort((int)s);
            if (view != null) ArmoryWindow.TestView(view == "grid");
            if (page != null) ArmoryWindow.TestPage(int.Parse(page) - 1);

            string bagItem = Val("--bag-item"), worn = Val("--select-worn"), supply = Val("--bag-supply");
            if (bagItem != null) ArmoryWindow.TestSelectBagItem(int.Parse(bagItem));
            if (supply != null) ArmoryWindow.TestSelectSupply(supply);
            if (worn != null) ArmoryWindow.TestSelectWorn(worn);
            if (Flag("--select-pet")) ArmoryWindow.TestSelectWorn(Slots.Pet);

            if (Val("--bag-confirm") == "discard") ArmoryWindow.TestPress("discard");
            string press = Val("--bag-press");
            if (press != null) foreach (string b in press.Split(',')) ArmoryWindow.TestPress(b.Trim());
            if (Flag("--bag-drag-demo")) ArmoryWindow.TestDragDemo();
            string drop = Val("--bag-drag-drop");
            if (drop != null) ArmoryWindow.TestDragDrop(drop);
            if (Flag("--bag-stats")) ArmoryWindow.TestStats();
            if (Flag("--bag-sort-open")) ArmoryWindow.TestSortList();
        }
    }
}
