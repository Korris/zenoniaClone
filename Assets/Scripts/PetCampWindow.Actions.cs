using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PetCampWindow: taps, keys and the card drag of the Team tab.
    ///   - A shelf card: that pet is selected and carried ("Tap a nest to put ..."); a nest then takes it. Dragging the card up onto a nest does the same.
    ///   - A nest with a pet (nothing carried): the pet is selected and Remove / Details show under the nest.
    ///   - An empty nest (nothing carried): the selected pet goes there, like the old window's slot tap.
    /// A pet that is in another nest moves; the pet it replaces takes the nest it came from (a swap). Every step is the old InvOp.PetEquip through
    /// PetActions.Send; the checks are PetTeam.Refusal, the ones the server runs. P closes (read here: the key gate holds P back from WindowsHost
    /// while a modal is open), Esc closes through WindowsHost, Left / Right walk the shelf.
    /// </summary>
    public sealed partial class PetCampWindow
    {
        const float SwipePixels = 60f, GhostSize = 72f;
        Image ghost;
        int dragPet = -1, dragOver = -1;
        bool swiping;

        void ReadKeys()
        {
            if (Time.frameCount != openedFrame && InputAdapter.WindowPressed(GameKey.PetWindow))
            {
                closedFrame = Time.frameCount;
                Close();
                return;
            }
            if (tab != Tab.Team && tab != Tab.Growth) return;
            int dx = (InputAdapter.WindowPressed(GameKey.Right) ? 1 : 0) - (InputAdapter.WindowPressed(GameKey.Left) ? 1 : 0);
            if (dx != 0) StepOwned(dx);
        }

        /// <summary>The next / previous pet the account owns becomes the selection (arrow keys, the Growth tab's arrows).</summary>
        void StepOwned(int by)
        {
            AccountState a = PetActions.Account;
            if (a == null || a.pets.Count == 0) return;
            int n = PetConfig.All.Length, at = selected < 0 ? (by > 0 ? -1 : n) : selected;
            for (int i = 0; i < n; i++)
            {
                at = (at + by + n) % n;
                if (a.OwnsPet(PetConfig.All[at].Id)) break;
            }
            selected = at;
            menuSlot = -1;
            int onShelf = 0;
            for (int i = 0; i < at; i++) if (a.OwnsPet(PetConfig.All[i].Id)) onShelf++;
            shelfPage = onShelf / ShelfCards;
            Refresh();
        }

        void OnShelfTap(int card)
        {
            int pet = PetOnCard(card);
            if (pet < 0) return;
            placing = !(placing && pet == selected); // a second tap on the carried pet puts it down
            selected = pet;
            menuSlot = -1;
            Refresh();
        }

        void OnNestTap(int slot)
        {
            PlayerState p = PetActions.Me;
            if (p == null) return;
            rulesPopup.gameObject.SetActive(false);
            if (p.Level < PetRules.SlotUnlockLevel[slot]) { Toast(PetViewModel.SlotNames[slot] + " opens at level " + PetRules.SlotUnlockLevel[slot]); return; }
            PetDef inNest = PetConfig.Get(p.Team[slot]);
            if (placing && selected >= 0)
            {
                if (inNest == PetConfig.All[selected]) { placing = false; Refresh(); return; }
                Place(p, slot, selected);
                return;
            }
            if (inNest != null)
            {
                selected = PetConfig.IndexOf(inNest);
                menuSlot = menuSlot == slot ? -1 : slot;
                Refresh();
                return;
            }
            if (selected >= 0) Place(p, slot, selected);
            else Toast("Pick a pet from Owned Pets first");
        }

        /// <summary>Species `pet` into nest `slot`. In another nest already: it moves, and a pet it replaces takes the nest it left.</summary>
        void Place(PlayerState p, int slot, int pet)
        {
            PetDef d = PetConfig.All[pet];
            int from = System.Array.IndexOf(p.Team, d.Id);
            PetDef replaced = PetConfig.Get(p.Team[slot]);
            if (from == slot) return;
            if (p.Account != null)
            {
                var after = (string[])p.Team.Clone(); // the team once the pet left its old nest: what the server will check
                if (from >= 0) after[from] = "";
                string why = PetTeam.Refusal(after, slot, d.Id, p.Level, p.Account);
                if (why != null) { Toast(why); return; } // the server would say the same; save the round trip
            }
            if (from >= 0) PetActions.Send(InvOp.PetEquip, PetRules.EncodeSlot(from, -1));
            PetActions.Send(InvOp.PetEquip, PetRules.EncodeSlot(slot, pet));
            if (from >= 0 && replaced != null) PetActions.Send(InvOp.PetEquip, PetRules.EncodeSlot(from, PetConfig.IndexOf(replaced)));
            placing = false;
            menuSlot = -1;
            Refresh();
        }

        void RemoveFromNest()
        {
            PlayerState p = PetActions.Me;
            if (p == null || menuSlot < 0 || PetConfig.Get(p.Team[menuSlot]) == null) return;
            PetActions.Send(InvOp.PetEquip, PetRules.EncodeSlot(menuSlot, -1));
            menuSlot = -1;
            Refresh();
        }

        // ---- drag a shelf card onto a nest (the gestures of the bag's "Drag to Equip": up = carry, sideways = turn the shelf's page) ----

        void BuildDragGhost()
        {
            ghost = UiKit.Icon(content, "DragGhost", 0f, 0f, GhostSize);
            ghost.gameObject.SetActive(false);
        }

        /// <summary>PetCampCard's drag callback: step 0 = begin, 1 = move, 2 = end.</summary>
        void OnCardDrag(int card, int step, PointerEventData e)
        {
            if (step == 0)
            {
                Vector2 moved = e.position - e.pressPosition;
                swiping = Mathf.Abs(moved.x) > Mathf.Abs(moved.y);
                int pet = PetOnCard(card);
                if (swiping || pet < 0) return;
                selected = dragPet = pet;
                placing = true;
                menuSlot = -1;
                ghost.sprite = PetArt.Icon(PetConfig.All[pet]);
                ghost.color = PetArt.TintOf(PetConfig.All[pet]);
                ghost.gameObject.SetActive(true);
                ghost.transform.SetAsLastSibling();
                Refresh();
            }
            if (step == 2 && swiping)
            {
                swiping = false;
                float dx = e.position.x - e.pressPosition.x;
                if (Mathf.Abs(dx) >= SwipePixels) TurnShelf(dx < 0f ? 1 : -1);
                return;
            }
            if (dragPet < 0) return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, e.position, null, out local); // from the middle of the design frame
            ghost.rectTransform.anchoredPosition = new Vector2(local.x + PaperLayout.DesignWidth * 0.5f - GhostSize * 0.5f, local.y - PaperLayout.DesignHeight * 0.5f + GhostSize * 0.5f);
            int over = NestUnder(e.position);
            if (step < 2) { HoverNest(over); return; }
            CancelDrag();
            PlayerState p = PetActions.Me;
            if (p != null && over >= 0) OnNestTap(over); // the same path as a tap while carrying
        }

        int NestUnder(Vector2 screen)
        {
            for (int i = 0; i < nests.Length; i++) if (RectTransformUtility.RectangleContainsScreenPoint(nests[i].Root, screen, null)) return i;
            return -1;
        }

        void HoverNest(int slot)
        {
            if (slot == dragOver) return;
            if (dragOver >= 0) nests[dragOver].Root.localScale = Vector3.one;
            dragOver = slot;
            if (slot >= 0) nests[slot].Root.localScale = new Vector3(1.06f, 1.06f, 1f); // the nest under the carried pet swells a little
        }

        void CancelDrag()
        {
            dragPet = -1;
            swiping = false;
            ghost.gameObject.SetActive(false);
            HoverNest(-1);
        }
    }
}
