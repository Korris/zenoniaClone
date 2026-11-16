using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// TEMPORARY test tool (2026-11-03, to check the enhance effect band by band): a "-1" button beside Enhance that lowers the selected
    /// weapon's + by one, free and always succeeding. Test builds only (never in a release build) and offline only: online the server owns the item.
    /// Remove this file and the two lines that call it in InventoryPanel.Detail.cs when the enhance look is settled.
    /// </summary>
    public partial class InventoryPanel
    {
        const float DowngradeW = 50f;
        BagWidgets.BagButton downgradeButton;

        static bool DowngradeAvailable { get { return BagActions.DowngradeAvailable; } }

        /// <summary>Width left for the Enhance button.</summary>
        static float EnhanceWidth(float full) { return DowngradeAvailable ? full - DowngradeW - 4f : full; }

        void BuildDowngrade(float left, float y, float full)
        {
            if (!DowngradeAvailable) return;
            downgradeButton = new BagWidgets.BagButton(itemDetail, "DevDowngrade", left + full - DowngradeW, y, DowngradeW, 46f, "-1", BagSprites.Tone.Steel, DowngradeSelected);
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
