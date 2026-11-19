using UnityEngine;
using ZenoniaProto.Rpg;
using W = ZenoniaProto.InventoryRoWidgets;
using B = ZenoniaProto.InventoryRoBoxes;

namespace ZenoniaProto
{
    /// <summary>
    /// TEMPORARY test tool (2026-11-03, to check the enhance effect band by band): a "-1" button beside Enhance that lowers the selected
    /// weapon's + by one, free and always succeeding. Test builds only (never in a release build) and offline only: online the server owns the item.
    /// Remove this file and the two lines that call it in InventoryPanel.Detail.cs (BuildDowngrade, RefreshDowngrade) when the enhance look is settled.
    /// </summary>
    public partial class InventoryPanel
    {
        B.IconButton downgradeButton;

        static bool DowngradeAvailable { get { return BagActions.DowngradeAvailable; } }

        /// <summary>A small "-1" pill right of the Effect strip.</summary>
        void BuildDowngrade(RectTransform parent)
        {
            if (!DowngradeAvailable) return;
            downgradeButton = new B.IconButton(parent, "DevDowngrade", RightW - 70f, 359f, 50f, 26f, "btn-grey", null, "-1", 13, DowngradeSelected);
        }

        void RefreshDowngrade(Item item)
        {
            if (downgradeButton == null) return;
            bool can = DowngradeAvailable && EnhanceStats.IsEnhanceable(item);
            downgradeButton.Set(can, null, EnhanceStats.Level(item) <= 0);
        }

        void DowngradeSelected()
        {
            PlayerState p = RpgSession.Player;
            Show(BagActions.DevDowngrade(p, p != null ? model.Selected(p) : null));
        }
    }
}
