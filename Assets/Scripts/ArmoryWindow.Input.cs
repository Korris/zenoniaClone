using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// ArmoryWindow: taps and keys. A tap selects a card / slot / pedestal, a second tap within 0.35 s activates it (wear, take off, drink).
    /// Arrows: Left / Right walk the shelf (the page follows) or the ring of pedestals; Up goes from the list to the carousel, Down comes
    /// back; in the grid view the arrows move over the board. J / Space activates. The keys are read with InputAdapter.WindowPressed:
    /// this window is a modal, so the normal key gate keeps them from the hero.
    /// </summary>
    public sealed partial class ArmoryWindow
    {
        const float DoubleTapSeconds = 0.35f;
        const int PedestalTapBase = 1000; // keeps pedestals and cells apart in the double-tap check
        float lastTapTime;
        int lastTap = -1;

        bool IsDoubleTap(int id)
        {
            bool same = id == lastTap && Time.unscaledTime - lastTapTime < DoubleTapSeconds;
            lastTap = id;
            lastTapTime = Time.unscaledTime;
            confirmDiscard = false;
            return same;
        }

        void OnCellTap(int cell)
        {
            bool twice = IsDoubleTap(cell);
            model.Worn = -1;
            model.Cell = cell;
            if (twice) Activate(); else Refresh();
        }

        /// <summary>A pedestal: the paper shows what is worn there. An empty one also lists what can go on it; Pet opens the Pet window.</summary>
        void OnPedestalTap(int index)
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            string slot = Slots.All[index];
            if (slot == Slots.Pet) { OpenPets(); return; }
            bool twice = IsDoubleTap(PedestalTapBase + index);
            model.Worn = index;
            if (p.GetEquipped(slot) == null) model.SetFilter(BagModel.FilterOf(slot));
            if (twice) Activate(); else Refresh();
        }

        void ReadKeys()
        {
            if (dragItem != null) return;
            int dx = (InputAdapter.WindowPressed(GameKey.Right) ? 1 : 0) - (InputAdapter.WindowPressed(GameKey.Left) ? 1 : 0);
            int dy = (InputAdapter.WindowPressed(GameKey.Down) ? 1 : 0) - (InputAdapter.WindowPressed(GameKey.Up) ? 1 : 0);
            if (dx != 0 || dy != 0) { Navigate(dx, dy); Refresh(); }
            if (InputAdapter.WindowPressed(GameKey.Attack)) Activate();
        }

        void Navigate(int dx, int dy)
        {
            confirmDiscard = false;
            if (model.Worn >= 0) // on the carousel
            {
                if (dy > 0) { model.Worn = -1; model.Cell = Mathf.Min(model.Page * model.PageSize, Mathf.Max(0, model.CellCount - 1)); return; }
                int at = System.Array.IndexOf(Ring, Slots.All[model.Worn]);
                model.Worn = Slots.Index(Ring[(at + dx + Ring.Length) % Ring.Length]);
                return;
            }
            bool topRow = !gridView || model.Cell < GridCols;
            if (dy < 0 && topRow) { model.Worn = Slots.Index(Slots.Armor); return; } // the pedestal nearest to the list
            int step = dx + (gridView ? dy * GridCols : 0);
            model.Cell = Mathf.Clamp(model.Cell + step, 0, Mathf.Max(0, model.CellCount - 1));
            model.Page = model.Cell / model.PageSize;
        }
    }
}
