using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Layout table of the compact HUD (hud-compact-mobile-v1.png, hud-compact-pc-v1.png; sizes from the size guide of
    /// hud-compact-parts-board-states-v2.png, 1 % of the screen height = 6.4 canvas units). One table so the pieces of different
    /// files never overlap:
    ///   right column, units from the top-right corner of the safe area: minimap, the menu icon row under it, the Wanted pill under that
    ///   bottom row (mobile), units from the bottom corners: joystick left, action cluster right, the chat lines between them
    /// The minimap numbers mirror Minimap.cs (its constants are private): 198 x 132 map + a 7-unit rim, 8 units from the corner.
    /// </summary>
    public static class HudAnchors
    {
        public const float MinimapWidth = 212f, MinimapHeight = 146f, MinimapMargin = 8f;
        public const float MinimapBottom = MinimapMargin + MinimapHeight;        // 154
        /// <summary>Centre line of the column (the minimap's centre), units left of the right edge.</summary>
        public const float ColumnX = MinimapMargin + MinimapWidth * 0.5f;        // 114

        // Menu icon row: round plates of 40 units (6.25 %) in 46-unit cells (the touch target), right-aligned with the minimap.
        public const float IconSize = 40f, IconPitch = 46f, IconRowTop = MinimapBottom + 4f, IconRowCenterY = IconRowTop + IconPitch * 0.5f;
        /// <summary>PC: the key label under each icon.</summary>
        public const float IconKeyHeight = 13f;
        /// <summary>Lowest point of the icon row (with its key labels on PC), units below the top.</summary>
        public static float EntryBottom { get { return IconRowTop + IconPitch + (HudLayout.IsWeb ? IconKeyHeight - 2f : 0f); } }

        public const float WantedCardWidth = 146f, WantedCardHeight = 72f; // the old hanging card (WantedHudCard until its compact pill replaces it)

        /// <summary>Top-right corner of the Wanted pill: just under the icon row, right-aligned with the minimap, mobile and PC alike.</summary>
        public static Vector2 WantedCardTopRight { get { return new Vector2(MinimapMargin, EntryBottom + 4f); } }

        // Mobile bottom row. Joystick: a ring of 106 units (16.5 %) centred here from the bottom-left corner.
        public const float JoystickSize = 106f, JoystickX = 104f, JoystickY = 100f;
        // Action cluster, from the bottom-right corner: attack 90 (14 %), skill slots 58 (9 %) on an arc of 88 around it, dash 45 (7 %)
        // right of it, the two potions 40 (6.25 %) left of it, the page dots at the outer end of the arc.
        public const float AttackSize = 90f, AttackX = 132f, AttackY = 98f, SkillSize = 58f, SkillArc = 88f, DashSize = 45f, PotionSize = 40f;
        /// <summary>Left end of the action cluster (the HP potion's touch cell), units from the right edge.</summary>
        public const float ActionClusterWidth = AttackX + 148f;
        /// <summary>Highest point of the action cluster (top skill slot), units above the bottom edge.</summary>
        public const float ActionClusterTop = AttackY + SkillArc + SkillSize * 0.5f;

        // Mobile chat: plain lines in the free span between the joystick and the action cluster, the typing bar under them.
        public const float ChatLeft = JoystickX + JoystickSize * 0.5f + 22f, ChatBarBottom = 6f, ChatBarHeight = 30f, ChatLineHeight = 18f;
    }
}
