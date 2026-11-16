using UnityEngine;
using UnityEngine.EventSystems;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// TrainingHallWindow: programmatic taps and drags for capture runs (DebugCapture.Skills.cs --skill-ui steps, DebugCapture.UiSkill.cs)
    /// and UI smoke tests. Each one goes through the same handler the pointer calls, like SkillTreeWindow.Tap*. No-ops while closed.
    /// </summary>
    public sealed partial class TrainingHallWindow
    {
        public static void TapNode(int i) { if (IsOpen && i >= 0 && i < SkillDef.PerClass) instance.OnStatue(i); }
        public static void TapLearn() { if (IsOpen) instance.OnLearn(); }
        public static void TapAssign() { if (IsOpen) instance.OnAssign(); }
        public static void TapSlot(int flat) { if (IsOpen) instance.OnSlot(flat); }
        /// <summary>First call opens the question, a second one answers Respec (the old window's two taps).</summary>
        public static void TapRespec() { if (IsOpen) instance.OnRespec(); }
        public static void TapTier(int tier) { if (IsOpen) instance.OnTier(tier); }

        /// <summary>A real drag through the handlers the pointer calls: statue `skill` onto belt slot `toFlat` (0-8).</summary>
        public static void TestDragStatue(int skill, int toFlat)
        {
            if (!IsOpen || skill < 0 || skill >= SkillDef.PerClass || toFlat < 0 || toFlat >= BeltSlots) return;
            instance.RunDrag(true, skill, instance.statues[skill].Root, ScreenCentre(instance.beltSlots[toFlat].Root));
        }

        /// <summary>A real drag of belt slot `fromFlat` (1-8) onto slot `toFlat`, or off the belt (toFlat &lt; 0: dropped on the shelf).</summary>
        public static void TestDragSlot(int fromFlat, int toFlat)
        {
            if (!IsOpen || fromFlat < 1 || fromFlat >= BeltSlots || toFlat >= BeltSlots) return;
            instance.RunDrag(false, fromFlat, instance.beltSlots[fromFlat].Root, ScreenCentre(toFlat < 0 ? instance.statues[1].Root : instance.beltSlots[toFlat].Root));
        }

        void RunDrag(bool statue, int index, RectTransform from, Vector2 dropAt)
        {
            if (EventSystem.current == null) return;
            var e = new PointerEventData(EventSystem.current);
            e.pressPosition = ScreenCentre(from);
            e.position = e.pressPosition + new Vector2(0f, -30f);
            BeginDrag(statue, index, e);
            e.position = dropAt;
            MoveDrag(e);
            EndDrag(e);
        }

        static Vector2 ScreenCentre(RectTransform rt) { return RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center)); }

        /// <summary>Statue `skill` held two thirds of the way to belt slot `toFlat`, the slots glowing: the look of a drag in progress.</summary>
        public static void TestDragDemo(int skill, int toFlat)
        {
            PlayerState p = RpgSession.Player;
            if (!IsOpen || p == null || skill < 0 || skill >= SkillDef.PerClass || p.Skills[skill] <= 0) return;
            toFlat = Mathf.Clamp(toFlat, 1, BeltSlots - 1);
            instance.selected = skill;
            instance.assignMode = false;
            instance.dragSkill = skill;
            instance.ghost.sprite = SkillIcons.Get(SkillTable.Get(p.ClassId, skill));
            instance.ghost.gameObject.SetActive(true);
            instance.ghost.transform.SetAsLastSibling();
            var from = new Vector2(StatueCx(skill), StatueBottom(skill) - PaperSkillStatue.H + PaperSkillStatue.RingCentre.y);
            var to = new Vector2(SlotX(toFlat) + PaperSkillBeltSlot.Size * 0.5f, SlotY + PaperSkillBeltSlot.Size * 0.5f);
            Vector2 at = Vector2.Lerp(from, to, 0.68f);
            instance.PlaceGhost(at.x, at.y);
            instance.Refresh();
        }
    }
}
