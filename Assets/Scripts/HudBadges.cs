using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Badges of the menu icon row (hud-compact-parts-board-states-v2.png, "With Badge"), on the upper right of an icon's plate:
    ///   count  on Skill: free skill points (it sat on the Bag button of the old HUD, whose Skill tab opened the tree)
    ///   dot    on Bag: an item the player has not looked at yet (NewItemFlags)
    ///   "!"    on Quests: a quest is ready to hand in (the hero's own QuestLog)
    /// The count is compared every frame; the two others are looked up four times a second. Nothing is allocated.
    /// </summary>
    public sealed class HudBadges
    {
        const float Period = 0.25f;
        readonly PaperBadge skillPoints;
        readonly GameObject newItem, questReady;
        float wait;
        bool shownItem, shownQuest;

        public HudBadges(RectTransform bag, RectTransform skill, RectTransform quests)
        {
            float w = bag.sizeDelta.x;
            newItem = PaperKit.Mark(bag, PaperMark.Dot, w - 5f, 5f, 12f);
            skillPoints = new PaperBadge(skill, w - 4f, 5f, 17f);
            questReady = PaperKit.Mark(quests, PaperMark.Alert, w - 4f, 5f, 16f);
            PaperHudCompact.Reskin(newItem, PaperPiece.CBadgeDot);
            PaperHudCompact.Reskin(skillPoints.Root.gameObject, PaperPiece.CBadgeCount);
            PaperHudCompact.Reskin(questReady, PaperPiece.CBadgeAlert);
            newItem.SetActive(false);
            questReady.SetActive(false);
        }

        public void Tick(PlayerState p, float dt)
        {
            skillPoints.Set(p != null ? p.SkillPointsFree : 0);
            wait -= dt;
            if (wait > 0f) return;
            wait = Period;
            NewItemFlags.Tick(p, UiScreens.IsOpen(UiScreen.Bag));
            bool item = p != null && NewItemFlags.Any, quest = p != null && QuestReady(p.Quests);
            if (item != shownItem) { shownItem = item; newItem.SetActive(item); }
            if (quest != shownQuest) { shownQuest = quest; questReady.SetActive(quest); }
        }

        /// <summary>True when any quest of this character waits to be handed in.</summary>
        public static bool QuestReady(QuestLog log)
        {
            if (log == null) return false;
            QuestDef[] all = QuestCatalog.All;
            for (int i = 0; i < all.Length; i++) if (log.StateOf(all[i]) == QuestState.ReadyToTurnIn) return true;
            return false;
        }
    }
}
