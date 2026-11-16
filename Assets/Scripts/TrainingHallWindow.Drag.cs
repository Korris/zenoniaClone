using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>On every statue and belt slot of the Training Hall: hands the drag gestures of that piece to the window (the pattern of the bag's ArmoryCardDrag).</summary>
    public sealed class TrainingHallDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        TrainingHallWindow window;
        bool statue;
        int index;

        /// <summary>statue true = statue `pieceIndex` (tree index 0-7); false = belt slot `pieceIndex` (flat 1-8).</summary>
        public void Init(TrainingHallWindow owner, bool isStatue, int pieceIndex) { window = owner; statue = isStatue; index = pieceIndex; }
        public void OnBeginDrag(PointerEventData e) { window.BeginDrag(statue, index, e); }
        public void OnDrag(PointerEventData e) { window.MoveDrag(e); }
        public void OnEndDrag(PointerEventData e) { window.EndDrag(e); }
    }

    /// <summary>
    /// TrainingHallWindow: drag and drop over the belt, each drop asking the same ISkillTreeActions.Assign as a tap:
    ///   - a learned statue dragged onto a belt slot is placed there (the slots glow while it is carried);
    ///   - a filled belt slot dragged onto another slot moves there (the two swap, SkillLoadout.Place);
    ///   - a filled belt slot dropped off the belt is cleared (Assign(slot, -1): the action the old window never offered).
    /// Dash cannot be moved or replaced. An unlearned statue is not picked up: a message says why.
    /// </summary>
    public sealed partial class TrainingHallWindow
    {
        const float GhostSize = 56f;
        Image ghost;
        int dragSkill = -1, dragFrom = -1; // tree index being carried; the flat belt slot it came from (-1 = from a statue)

        void BuildDragGhost()
        {
            ghost = UiKit.Icon(content, "DragGhost", 0f, 0f, GhostSize);
            ghost.gameObject.SetActive(false);
        }

        internal void BeginDrag(bool statue, int index, PointerEventData e)
        {
            PlayerState p = RpgSession.Player;
            if (p == null || respecPopup.activeSelf) return;
            int skill = statue ? index : p.Loadout[index - 1];
            if (skill < 0) return; // an empty slot
            if (p.Skills[skill] <= 0) { Say(ActionResult.Fail("Learn the skill first")); return; }
            selected = skill;
            assignMode = false;
            dragSkill = skill;
            dragFrom = statue ? -1 : index;
            ghost.sprite = SkillIcons.Get(SkillTable.Get(p.ClassId, skill));
            ghost.gameObject.SetActive(true);
            ghost.transform.SetAsLastSibling();
            Refresh();
            MoveDrag(e);
        }

        internal void MoveDrag(PointerEventData e)
        {
            if (dragSkill < 0) return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, e.position, null, out local); // from the middle of the design frame
            PlaceGhost(local.x + PaperLayout.DesignWidth * 0.5f, PaperLayout.DesignHeight * 0.5f - local.y);
        }

        internal void EndDrag(PointerEventData e)
        {
            if (dragSkill < 0) return;
            int skill = dragSkill, from = dragFrom, over = SlotUnder(e.position);
            bool offBelt = !RectTransformUtility.RectangleContainsScreenPoint(belt, e.position, null);
            CancelDrag();
            if (over == 0) Say(ActionResult.Fail("Dash is fixed in the first slot"));
            else if (over > 0 && over != from) Say(SkillTreeActions.Current.Assign(over - 1, skill));
            else if (from > 0 && offBelt) Say(SkillTreeActions.Current.Assign(from - 1, -1)); // taken off the belt
            Refresh();
        }

        /// <summary>Ghost centred on a point of the design frame (top-left based units).</summary>
        void PlaceGhost(float x, float y) { ghost.rectTransform.anchoredPosition = new Vector2(x - GhostSize * 0.5f, -(y - GhostSize * 0.5f)); }

        void CancelDrag()
        {
            dragSkill = dragFrom = -1;
            ghost.gameObject.SetActive(false);
        }
    }
}
