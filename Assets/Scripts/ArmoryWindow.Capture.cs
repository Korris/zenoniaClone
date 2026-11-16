using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// ArmoryWindow: states for capture runs (DebugCapture.UiBag.cs) that a screenshot cannot reach with a mouse: a selection, a view,
    /// a filter, a page, the discard question, a drag stopped half way, a button pressed. Each one goes through the same methods the taps call.
    /// No-ops while the window is closed.
    /// </summary>
    public sealed partial class ArmoryWindow
    {
        /// <summary>The card of bag item `bagIndex` (the bag's own order) selected, whatever the sort.</summary>
        public static void TestSelectBagItem(int bagIndex)
        {
            PlayerState p = RpgSession.Player;
            if (!IsOpen || p == null || bagIndex < 0 || bagIndex >= p.Inventory.Count) return;
            int cell = instance.model.CellOf(p.Inventory[bagIndex]);
            if (cell >= 0) instance.OnCellTap(cell);
            instance.lastTap = -1;
        }

        /// <summary>The first card of a supply selected: "hp" / "mp" = the first potion stack of that kind, "stone" = the upgrade stones.</summary>
        public static void TestSelectSupply(string which)
        {
            if (!IsOpen) return;
            for (int i = 0; i < instance.model.Cells.Count; i++)
            {
                BagCell c = instance.model.Cells[i];
                if (!c.IsSupply) continue;
                bool potion = BagModel.IsPotion(c.Supply);
                if (which == "stone" ? c.Supply == BagSupply.Stone : potion && BagModel.PotionIsHp(c.Supply) == (which == "hp")) { instance.OnCellTap(i); break; }
            }
            instance.lastTap = -1;
        }

        /// <summary>A pedestal selected as the arrow keys do (the Pet pedestal too: a tap would open the Pet window instead).</summary>
        public static void TestSelectWorn(string slot)
        {
            if (!IsOpen || Slots.Index(slot) < 0) return;
            if (slot == Slots.Pet) { instance.model.Worn = Slots.Index(slot); instance.Refresh(); }
            else instance.OnPedestalTap(Slots.Index(slot));
            instance.lastTap = -1;
        }

        public static void TestView(bool grid) { if (IsOpen) instance.SetGridView(grid); }
        public static void TestFilter(BagFilter f) { if (IsOpen) instance.PickFilter(f); }
        /// <summary>0-3 = BagSort, 4 = Tidy Bag (the Sort box's options).</summary>
        public static void TestSort(int option) { if (IsOpen) instance.PickSort(option); }
        public static void TestPage(int page) { if (IsOpen) instance.TurnPage(page - instance.model.Page); }
        public static void TestStats() { if (IsOpen) instance.ToggleStats(); }
        public static void TestSortList() { if (IsOpen) instance.sortBox.SetOpen(true); }

        /// <summary>Presses a button of the paper / the carousel: main (Equip, Unequip, Use, Cancel), enhance, discard, enhance-equipped.</summary>
        public static void TestPress(string button)
        {
            if (!IsOpen) return;
            if (button == "main") instance.OnMainButton();
            else if (button == "enhance") instance.EnhanceSelected();
            else if (button == "discard") instance.DiscardSelected();
            else if (button == "enhance-equipped") instance.EnhanceEquipped();
            else Debug.LogWarning("[ZenoniaProto] --bag-press: unknown button '" + button + "' (main, enhance, discard, enhance-equipped)");
        }

        /// <summary>
        /// A real drag of the selected card, through the same handlers the pointer calls: picked up, carried to the pedestal of `target`
        /// (a slot id) or to the hero ("hero"), and dropped there.
        /// </summary>
        public static void TestDragDrop(string target)
        {
            if (!IsOpen || instance.gridView || instance.model.Worn >= 0 || UnityEngine.EventSystems.EventSystem.current == null) return;
            int index = instance.model.Cell % ShelfCards;
            RectTransform to = target == "hero" || Slots.Index(target) < 0 ? instance.heroRect : instance.pedestals[Slots.Index(target)].Root;
            var e = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            e.pressPosition = ScreenCentre(instance.cards[index].Root);
            e.position = e.pressPosition + new Vector2(0f, 30f);
            instance.BeginCardDrag(index, e);
            e.position = ScreenCentre(to);
            instance.MoveCardDrag(e);
            instance.EndCardDrag(e);
        }

        static Vector2 ScreenCentre(RectTransform rt) { return RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center)); }

        /// <summary>The selected card held two thirds of the way to its pedestal, which is lit: the look of a drag in progress.</summary>
        public static void TestDragDemo()
        {
            PlayerState p = RpgSession.Player;
            if (!IsOpen || p == null || instance.gridView || instance.model.Worn >= 0) return;
            Item item = instance.model.ItemAt(instance.model.Cell);
            if (item == null) return;
            instance.dragItem = item;
            instance.ghost.sprite = LootSprites.Icon(item);
            instance.ghost.gameObject.SetActive(true);
            instance.ghost.transform.SetAsLastSibling();
            int slot = Mathf.Max(0, Slots.Index(item.slot));
            var from = new Vector2(CardX + (instance.model.Cell % ShelfCards) * CardStep + PaperCard.W * 0.5f, CardY + PaperCard.H * 0.4f);
            Vector2 at = Vector2.Lerp(from, PedestalAt[slot], 0.68f);
            instance.PlaceGhost(at.x, at.y);
            instance.HoverPedestal(slot);
            instance.Refresh();
        }
    }
}
