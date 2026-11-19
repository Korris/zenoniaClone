using System;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Round menu buttons of the RO Soft HUD (02-hud.png), top centre-right left of the minimap: Bag, Skills, Pet, Quest, More, each an
    /// ivory plate with its painted icon and the English caption under it. Badges: a red dot-badge on Bag for an unseen item
    /// (NewItemFlags), the free skill points on Skills, the red "!" on Quest when a quest waits to be handed in. More opens a small column
    /// under it with Gacha and Settings. Bag / Skills / Pet press their GameKey, so they open exactly what the keys open (UiScreens: the
    /// current Bag window, whichever look it has); Quest and Settings open their windows.
    /// </summary>
    public sealed class RoHudMenu
    {
        const float Plate = RoHud.MenuPlate, Pitch = RoHud.MenuPitch, Icon = Plate * 0.72f, CaptionH = 16f;

        readonly RectTransform root, more;
        readonly GameObject itemBadge, questBadge, skillBadge;
        readonly Text skillCount;
        float wait;
        int shownSp = -1;
        bool shownItem, shownQuest;

        public RoHudMenu(RectTransform safe)
        {
            root = UiKit.Fill(safe, "RoHudMenu");
            root.pivot = RoHud.TopRight; // Fit() moves its right edge and scales it towards that corner
            RectTransform bag = Button("Bag", 4, "hud-icon-bag", () => TouchControls.Press(GameKey.Inventory));
            RectTransform skills = Button("Skills", 3, "hud-icon-skills", () => TouchControls.Press(GameKey.SkillTree));
            Button("Pet", 2, "hud-icon-pet", () => TouchControls.Press(GameKey.PetWindow));
            RectTransform quest = Button("Quest", 1, "hud-icon-quest", QuestLogWindow.Open);
            RectTransform moreBtn = Button("More", 0, "hud-icon-more", ToggleMore);
            itemBadge = Badge(bag, "hud-badge-alert", 16f);
            questBadge = Badge(quest, "hud-badge-alert", 18f);
            skillBadge = Badge(skills, "hud-count-badge", 20f);
            skillCount = RoHud.Label(UiKit.Fill(skillBadge.transform, "Count"), "", 11, Color.white, TextAnchor.MiddleCenter);
            itemBadge.SetActive(false); questBadge.SetActive(false); skillBadge.SetActive(false);

            // More: a short column under the More button (Gacha, Settings), closed by a second tap or after a choice.
            more = RoHud.Box(root, "MoreColumn", RoHud.TopRight, 0f, 0f, 104f, 66f);
            more.pivot = RoHud.TopRight;
            more.anchoredPosition = new Vector2(0f, -(RoHud.MenuTop + Plate + CaptionH + 6f)); // right-aligned under More
            MoreItem("Gacha", 0, "hud-icon-gacha", () => { CloseMore(); TouchControls.Press(GameKey.PetGacha); });
            MoreItem("Settings", 1, "hud-icon-settings", () => { CloseMore(); SettingsWindow.Open(); });
            more.gameObject.SetActive(false);
            moreBtn.SetAsLastSibling();
        }

        /// <summary>Centre of button `fromRight` (0 = More), units left of the right edge.</summary>
        static float CellX(int fromRight) { return Plate * 0.5f + fromRight * Pitch; }

        RectTransform Button(string caption, int fromRight, string icon, Action onTap)
        {
            RectTransform cell = RoHud.Box(root, caption, RoHud.TopRight, -CellX(fromRight), -(RoHud.MenuTop + (Plate + CaptionH) * 0.5f), Pitch, Plate + CaptionH);
            RectTransform plate = RoHud.Box(cell, "Plate", new Vector2(0.5f, 1f), 0f, -Plate * 0.5f, Plate, Plate);
            RoHud.Art(plate, "hud-menu-plate");
            RectTransform glyph = RoHud.Box(plate, "Icon", RoHud.Mid, 0f, 1f, Icon, Icon);
            RoHud.Art(glyph, icon);
            Text t = RoHud.Label(RoHud.Box(cell, "Caption", new Vector2(0.5f, 0f), 0f, CaptionH * 0.5f, Pitch + 8f, CaptionH), caption, 13, RoHud.Ink, TextAnchor.MiddleCenter, true, true, true); // ink on a white outline, as 02-hud.png
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            TouchControls.Img(cell, null, Color.clear, true); // the whole cell is the touch target
            PaperHudPress.Tap(cell.gameObject, onTap);
            return plate;
        }

        static GameObject Badge(RectTransform plate, string piece, float size)
        {
            RectTransform b = RoHud.Box(plate, "Badge", RoHud.TopRight, -4f, -4f, size, size);
            RoHud.Art(b, piece);
            return b.gameObject;
        }

        /// <summary>One entry of the More column: the navy pill with icon + caption (box model of RoHud.Pill), right-aligned.</summary>
        void MoreItem(string caption, int i, string icon, Action onTap)
        {
            RectTransform b;
            RoHud.Pill(more, caption, RoHud.TopRight, RoHud.TopRight, 0f, -i * 34f, 30f, "hud-party-title", icon, 18f, caption, 13, Color.white, out b, 104f, true);
            PaperHudPress.Tap(b.gameObject, onTap);
        }

        /// <summary>
        /// Puts the row's right end `rowRight` units from the right edge (left of the minimap and of the map pills, which grow with the
        /// map name) and, on a narrow screen (4:3), scales the row down so it stays clear of the status plate. `safeW` = safe-area width.
        /// </summary>
        public void Fit(float rowRight, float safeW)
        {
            float leftLimit = RoHud.Margin + RoHud.StatusPlateX + RoHud.StatusPlateW + 8f, rowW = CellX(4) + Pitch * 0.5f;
            float s = Mathf.Clamp((safeW - leftLimit - rowRight) / rowW, 0.7f, 1f);
            if (Mathf.Abs(root.offsetMax.x + rowRight) > 0.5f) root.offsetMax = new Vector2(-rowRight, root.offsetMax.y);
            if (Mathf.Abs(root.localScale.x - s) > 0.005f) root.localScale = new Vector3(s, s, 1f);
        }

        void ToggleMore() { more.gameObject.SetActive(!more.gameObject.activeSelf); }

        public void CloseMore() { if (more.gameObject.activeSelf) more.gameObject.SetActive(false); }

        /// <summary>Per frame while a hero plays (badges stay current behind windows; the bag must clear its badge).</summary>
        public void Tick(PlayerState p, bool visible, float dt)
        {
            if (!visible) CloseMore();
            int sp = p != null ? p.SkillPointsFree : 0;
            if (sp != shownSp) { shownSp = sp; skillBadge.SetActive(sp > 0); skillCount.text = sp > 99 ? "99" : sp.ToString(); }
            wait -= dt;
            if (wait > 0f) return;
            wait = 0.25f;
            NewItemFlags.Tick(p, UiScreens.IsOpen(UiScreen.Bag));
            bool item = p != null && NewItemFlags.Any, quest = p != null && HudBadges.QuestReady(p.Quests);
            if (item != shownItem) { shownItem = item; itemBadge.SetActive(item); }
            if (quest != shownQuest) { shownQuest = quest; questBadge.SetActive(quest); }
        }
    }
}
