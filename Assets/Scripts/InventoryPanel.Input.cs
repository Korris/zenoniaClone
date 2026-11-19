using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Keys and taps of the bag window: arrows move the selection (BagModel), J / Space or a second tap within 0.35 s activates.</summary>
    public partial class InventoryPanel
    {
        void Update()
        {
            if (!IsOpen) return;
            FitWindow();
            if (messageText.text.Length > 0 && Time.unscaledTime > messageUntil) messageText.text = "";
            heroView.Tick(Time.unscaledDeltaTime);
            int dx = (InputAdapter.Pressed(GameKey.Right) ? 1 : 0) - (InputAdapter.Pressed(GameKey.Left) ? 1 : 0);
            int dy = (InputAdapter.Pressed(GameKey.Down) ? 1 : 0) - (InputAdapter.Pressed(GameKey.Up) ? 1 : 0);
            if (dx != 0 || dy != 0) { Navigate(dx, dy); Refresh(); }
            if (InputAdapter.Pressed(GameKey.Attack)) Activate();
        }

        /// <summary>Arrows: worn slots cycle, Right leaves them for the grid; in the grid, Left from the first column goes back to them.</summary>
        void Navigate(int dx, int dy)
        {
            confirmDiscard = false;
            if (model.Worn >= 0)
            {
                if (dx > 0) { model.Worn = -1; model.Cell = 0; return; }
                model.Worn = (model.Worn + dy + dx + Slots.All.Length) % Slots.All.Length;
                return;
            }
            if (dx < 0 && model.Cell % GridCols == 0) { model.Worn = 0; return; }
            model.Cell = Mathf.Clamp(model.Cell + dx + dy * GridCols, 0, Mathf.Min(PageSize, model.CellCount) - 1);
        }

        bool IsDoubleTap(int index)
        {
            bool same = index == lastClickIndex && Time.unscaledTime - lastClickTime < DoubleClickSeconds;
            lastClickIndex = index;
            lastClickTime = Time.unscaledTime;
            confirmDiscard = false;
            return same;
        }

        void OnCellClick(int cell)
        {
            CloseSortList();
            bool twice = IsDoubleTap(cell);
            model.Worn = -1;
            model.Cell = cell;
            if (twice) Activate(); else Refresh();
        }

        /// <summary>A worn slot: select it and list what can go in it.</summary>
        void OnWornClick(int index)
        {
            if (Slots.All[index] == Slots.Pet) { UiScreens.Open(UiScreen.Pet); return; } // Pet Companion: its own window
            bool twice = IsDoubleTap(WornClickBase + index);
            model.Worn = index;
            SetFilter(BagModel.FilterOf(Slots.All[index]));
            if (twice) Activate(); else Refresh();
        }

        /// <summary>Capture runs (--bag-ui): "stats" opens the Stats popup, "sort" the Sort list, "discard" arms Discard, "tab:N" picks category tab N.</summary>
        public static void TestUi(string what)
        {
            if (instance == null) return;
            if (what == "stats") instance.statsPopup.gameObject.SetActive(true);
            else if (what == "sort") instance.ToggleSortList();
            else if (what == "discard") instance.DiscardSelected();
            else if (what.StartsWith("tab:")) { instance.SetFilter(TabFilters[int.Parse(what.Substring(4)) % TabFilters.Length]); instance.model.Worn = -1; instance.Refresh(); }
        }
    }
}
