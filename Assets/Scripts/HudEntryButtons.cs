using System;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Menu icon row of the compact HUD (hud-compact-mobile-v1.png), under the minimap and right-aligned with it: five flat dark round
    /// plates, Bag, Skill, Pet, Quests and "..." which opens a small strip with Gacha and Settings (HudEntryButtons.MoreStrip.cs).
    /// On PC a key label sits under each icon, read from the live bindings. Badges (HudBadges): red dot on Bag = a new item, the number
    /// on Skill = free skill points, "!" on Quests = a quest to hand in.
    /// Bag, Skill, Pet and Gacha press their GameKey, so they open exactly what the key opens (RpgSession / WindowsHost through
    /// UiScreens: old window or rebuild); Quests and Settings open their windows as the old round buttons did.
    /// Layout numbers are in HudAnchors.
    /// </summary>
    public sealed partial class HudEntryButtons
    {
        static readonly Vector2 TopRight = new Vector2(1f, 1f), Mid = new Vector2(0.5f, 0.5f);
        const float Plate = HudAnchors.IconSize, Pitch = HudAnchors.IconPitch, Radius = Plate * 0.5f, GlyphSize = Plate * 0.64f;

        readonly RectTransform root;
        readonly HudBadges badges;

        public HudEntryButtons(RectTransform safe)
        {
            root = UiKit.Fill(safe, "EntryButtons");
            float y = -HudAnchors.IconRowCenterY;
            RectTransform bag = Icon(root, "Bag", TopRight, CellX(4), y, InputAdapter.KeyLabel(GameKey.Inventory), () => TouchControls.Press(GameKey.Inventory));
            RectTransform skill = Icon(root, "Skill", TopRight, CellX(3), y, InputAdapter.KeyLabel(GameKey.SkillTree), () => TouchControls.Press(GameKey.SkillTree));
            Icon(root, "Pet", TopRight, CellX(2), y, InputAdapter.KeyLabel(GameKey.PetWindow), () => TouchControls.Press(GameKey.PetWindow));
            RectTransform quests = Icon(root, "Quests", TopRight, CellX(1), y, InputAdapter.KeyLabel(GameKey.Quests), QuestLogWindow.Open);
            Icon(root, "More", TopRight, CellX(0), y, null, ToggleMore);
            badges = new HudBadges(bag, skill, quests);
            BuildStrip(); // last: its veil lies over the row, so a tap outside the strip only closes it
        }

        /// <summary>Centre of icon number `fromRight` (0 = the rightmost): the plate of the last one ends at the minimap's right edge.</summary>
        static float CellX(int fromRight) { return -(HudAnchors.MinimapMargin + Radius + fromRight * Pitch); }

        /// <summary>
        /// One icon: touch cell, dark plate, glyph, on PC the key label under it. Returns the plate (badges sit on its corner).
        /// `key` = the name of its key (PC only; null = none).
        /// </summary>
        static RectTransform Icon(Transform parent, string name, Vector2 corner, float x, float y, string key, Action onTap)
        {
            RectTransform art;
            RectTransform cell = PaperHudCompact.Cell(parent, name, corner, x, y, Pitch, Plate, out art);
            Image plate = PaperHudCompact.Plate(art, PaperPiece.CMenuPlate, Radius);
            string id = name.ToLowerInvariant();
            Sprite glyph = PaperArt.Get(PaperPiece.CMenuIconPrefix + id) ?? PaperArt.Get(PaperPiece.EntryIconPrefix + id); // the older cut icon until the compact one comes
            if (glyph != null) TouchControls.Child(art, "Icon", glyph, GlyphSize, Color.white).preserveAspect = true;
            else PaperTheme.Caption(art, name == "More" ? "..." : name.Substring(0, 1), 18, PaperTheme.TextLight); // "..." sits low, as dots do
            if (HudLayout.IsWeb) PaperHudCompact.KeyLabel(cell, Mid, 0f, -(Pitch + HudAnchors.IconKeyHeight) * 0.5f + 2f, key);
            PaperHudPress.Tap(cell.gameObject, onTap, down => PaperHudCompact.Press(plate, PaperPiece.CMenuPlate, Radius, down));
            return art;
        }

        /// <summary>Per frame while a hero plays; `visible` = the HUD layer is shown. The badges are kept up to date behind a window too (the bag must clear its dot).</summary>
        public void Tick(PlayerState p, bool visible, float dt)
        {
            badges.Tick(p, dt);
            if (!visible) CloseMore();
        }
    }
}
