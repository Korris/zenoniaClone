using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// ArmoryWindow: the grid view (bag-state-grid-50-slots-consumable-selected.png). The shelf shows ten cards; the Grid button swaps
    /// it for a wooden board with every cell at once: the stacks the hero owns, then all 50 bag slots (empty ones too), and chained
    /// cells for what the bag does not have. The carousel shrinks to a small scene above the board, the filter row and the paper on the
    /// right move with it. Same model, same taps: only the page size and the padding change.
    /// </summary>
    public sealed partial class ArmoryWindow
    {
        const int GridCols = 10, GridRows = 6, GridCells = GridCols * GridRows;
        const float GridX = 7f, GridY = 196f, GridW = 719f, GridH = 404f, SlotSize = 56f, SlotStep = 60f;

        sealed class GridSlot { public PaperSlot Slot; public Text Corner; public GameObject Lock; }

        readonly GridSlot[] gridSlots = new GridSlot[GridCells];
        GameObject grid;
        bool gridView;

        void BuildGrid()
        {
            RectTransform root = UiKit.Rect(content, "Grid", 0f, 0f, 10f, 10f);
            grid = root.gameObject;
            PaperKit.Frame(root, "Board", GridX, GridY, GridW, GridH, PaperFrame.Wood);
            float x0 = GridX + (GridW - (GridCols * SlotStep - (SlotStep - SlotSize))) * 0.5f, y0 = GridY + (GridH - (GridRows * SlotStep - (SlotStep - SlotSize))) * 0.5f;
            for (int i = 0; i < GridCells; i++)
            {
                int cell = i;
                var g = new GridSlot { Slot = new PaperSlot(root, "Slot" + i, x0 + (i % GridCols) * SlotStep, y0 + (i / GridCols) * SlotStep, SlotSize, () => OnCellTap(cell)) };
                g.Corner = PaperTheme.Body(g.Slot.Root, "Corner", 2f, SlotSize - 20f, SlotSize - 7f, 16f, "", PaperTheme.BodySize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleRight, true);
                g.Lock = PaperKit.Mark(g.Slot.Root, PaperMark.Lock, 12f, 12f, 18f);
                g.Lock.SetActive(false);
                gridSlots[i] = g;
            }
        }

        /// <summary>Shelf of cards (false) or the board with every slot (true). The selection stays on the same cell.</summary>
        void SetGridView(bool on)
        {
            if (gridView == on) return;
            gridView = on;
            confirmDiscard = false;
            CancelDrag();
            model.PageSize = on ? GridCells : ShelfCards;
            model.PadToBag = on;
            ApplyView();
            Refresh();
        }

        /// <summary>Puts every part where the current view wants it.</summary>
        void ApplyView()
        {
            shelf.SetActive(!gridView);
            grid.SetActive(gridView);
            carousel.localScale = Vector3.one * (gridView ? MiniScale : 1f);
            carousel.anchoredPosition = gridView ? new Vector2(700f - HeroX * MiniScale, -34f) : Vector2.zero;
            foreach (ArmoryPedestal v in pedestals) v.ShowTag(!gridView);
            carouselButtons.SetActive(!gridView);
            if (gridView) statsPopup.gameObject.SetActive(false);
            filters.anchoredPosition = new Vector2(0f, -(gridView ? 154f : 392f));
            compareFrame.anchoredPosition = new Vector2(CompareX, -(gridView ? GridY : 54f));
            compareFrame.sizeDelta = new Vector2(CompareW, gridView ? GridH : 327f);
            viewButton.Label.text = gridView ? "Shelf" : "Grid";
        }

        void RefreshGrid(PlayerState p)
        {
            for (int i = 0; i < GridCells; i++)
            {
                GridSlot g = gridSlots[i];
                bool selected = model.Worn < 0 && i == model.Cell;
                BagCell c = i < model.Cells.Count ? model.Cells[i] : default(BagCell);
                string corner = "";
                bool unusable = false;
                if (i >= model.CellCount) g.Slot.Set(-1, null, false, true); // the bag has no such slot
                else if (c.IsSupply)
                {
                    g.Slot.Set(0, BagText.SupplySprite(c.Supply), selected);
                    corner = "x" + BagModel.SupplyCount(p, c.Supply);
                }
                else if (c.Item != null)
                {
                    g.Slot.Set(PaperTheme.RarityTier(c.Item.rarity), LootSprites.Icon(c.Item), selected);
                    int plus = EnhanceStats.Level(c.Item);
                    corner = plus > 0 ? "+" + plus : "";
                    unusable = WhyLocked(p, c.Item) != null;
                }
                else g.Slot.Set(-1, null, selected);
                g.Slot.Icon.color = unusable ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
                ItemEnhanceSlotFx.Show(g.Slot.Icon, c.IsSupply ? null : c.Item);
                g.Corner.text = corner;
                if (g.Lock.activeSelf != unusable) g.Lock.SetActive(unusable);
            }
        }
    }
}
