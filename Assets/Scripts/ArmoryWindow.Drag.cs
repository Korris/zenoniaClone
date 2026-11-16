using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>On every shelf card: hands the drag gestures of that card to the window.</summary>
    public sealed class ArmoryCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        ArmoryWindow window;
        int index;

        public void Init(ArmoryWindow owner, int cardIndex) { window = owner; index = cardIndex; }
        public void OnBeginDrag(PointerEventData e) { window.BeginCardDrag(index, e); }
        public void OnDrag(PointerEventData e) { window.MoveCardDrag(e); }
        public void OnEndDrag(PointerEventData e) { window.EndCardDrag(e); }
    }

    /// <summary>
    /// ArmoryWindow: "Drag to Equip". A card dragged upwards is carried as a ghost of its icon; over a pedestal (or over the hero) the
    /// pedestal it would go on lights up. Dropped on the hero or on the pedestal of its own slot it is worn (the same Equip as the button);
    /// on another pedestal it goes back and a message says why; anywhere else it just goes back. A drag that starts sideways is a swipe
    /// instead: it turns the shelf's page, so the two gestures do not fight on a phone. Potions are not dragged: tap Use.
    /// </summary>
    public sealed partial class ArmoryWindow
    {
        const float SwipePixels = 60f, GhostSize = 64f;
        Image ghost;
        Item dragItem;
        int dragOver = -1;
        bool swiping;

        void BuildDragGhost()
        {
            ghost = UiKit.Icon(content, "DragGhost", 0f, 0f, GhostSize);
            ghost.gameObject.SetActive(false);
        }

        internal void BeginCardDrag(int index, PointerEventData e)
        {
            Vector2 moved = e.position - e.pressPosition;
            swiping = Mathf.Abs(moved.x) > Mathf.Abs(moved.y);
            int cell = model.Page * ShelfCards + index;
            Item item = model.ItemAt(cell);
            if (swiping || item == null || RpgSession.Player == null) return;
            confirmDiscard = false;
            model.Worn = -1;
            model.Cell = cell;
            dragItem = item;
            ghost.sprite = LootSprites.Icon(item);
            ghost.gameObject.SetActive(true);
            ghost.transform.SetAsLastSibling();
            Refresh();
            MoveCardDrag(e);
        }

        internal void MoveCardDrag(PointerEventData e)
        {
            if (dragItem == null) return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, e.position, null, out local); // from the middle of the design frame
            PlaceGhost(local.x + PaperLayout.DesignWidth * 0.5f, PaperLayout.DesignHeight * 0.5f - local.y);
            bool onHero;
            int over = PedestalUnder(e.position, out onHero);
            HoverPedestal(over < 0 && onHero ? Slots.Index(dragItem.slot) : over);
        }

        internal void EndCardDrag(PointerEventData e)
        {
            if (swiping)
            {
                swiping = false;
                float dx = e.position.x - e.pressPosition.x;
                if (Mathf.Abs(dx) >= SwipePixels) TurnPage(dx < 0f ? 1 : -1);
                return;
            }
            if (dragItem == null) return;
            Item item = dragItem;
            bool onHero;
            int over = PedestalUnder(e.position, out onHero);
            CancelDrag();
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            if (onHero || (over >= 0 && Slots.All[over] == item.slot)) { Equip(p, item); return; }
            if (over >= 0) Flash("This goes on the " + ItemInfo.SlotLabel(item.slot) + " pedestal");
            Refresh(); // the card goes back to the shelf
        }

        /// <summary>Ghost centred on a point of the design frame (top-left based units).</summary>
        void PlaceGhost(float x, float y) { ghost.rectTransform.anchoredPosition = new Vector2(x - GhostSize * 0.5f, -(y - GhostSize * 0.5f)); }

        void HoverPedestal(int index)
        {
            if (index == dragOver) return;
            dragOver = index;
            for (int i = 0; i < pedestals.Length; i++) pedestals[i].Light(i == model.Worn || i == dragOver);
        }

        void CancelDrag()
        {
            dragItem = null;
            swiping = false;
            ghost.gameObject.SetActive(false);
            HoverPedestal(-1);
        }
    }
}
