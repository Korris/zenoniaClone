using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// ArmoryWindow: what the buttons, double taps, J / Space and a dropped card do. Every change to the player is a BagActions call (the same
    /// ones the old window makes: offline the PlayerState changes, online the request goes to the server); this file only decides
    /// what is selected afterwards and shows the outcome.
    /// </summary>
    public sealed partial class ArmoryWindow
    {
        /// <summary>The left button of the paper: Equip / Unequip / Use / Pet Window, or "Cancel" while the discard question is up.</summary>
        void OnMainButton()
        {
            if (confirmDiscard) { confirmDiscard = false; Refresh(); return; }
            Activate();
        }

        /// <summary>Drink the selected potion, wear the selected bag item, take the selected worn item off; the Pet pedestal opens the Pet window.</summary>
        void Activate()
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            confirmDiscard = false;
            lastTap = -1; // a third quick tap must not act on the card that slid into this place
            bool worn = model.Worn >= 0;
            if (worn && Slots.All[model.Worn] == Slots.Pet) { OpenPets(); return; }
            BagSupply kind;
            if (!worn && model.SupplyAt(model.Cell, out kind))
            {
                if (BagModel.IsPotion(kind)) Show(BagActions.UsePotion(p, kind));
                return;
            }
            Item item = model.Selected(p);
            if (item == null) return;
            if (worn) Show(BagActions.Unequip(p, item)); else Equip(p, item);
        }

        /// <summary>Wears a bag item (button, double tap, J, or its card dropped on a pedestal); the selection follows it to its pedestal.</summary>
        void Equip(PlayerState p, Item item)
        {
            BagResult r = BagActions.Equip(p, item);
            if (r.Ok) model.Worn = Mathf.Max(0, Slots.Index(item.slot)); // online: shown when the server's reply arrives
            Show(r);
        }

        void EnhanceSelected()
        {
            PlayerState p = RpgSession.Player;
            if (p != null) Show(BagActions.Enhance(p, model.Selected(p)));
        }

        /// <summary>Bag items only. The first press asks (the paper shows the question with Cancel / Discard), the second throws the item away.</summary>
        void DiscardSelected()
        {
            PlayerState p = RpgSession.Player;
            Item item = p != null && model.Worn < 0 ? model.Selected(p) : null;
            if (item == null) return;
            if (!confirmDiscard) { confirmDiscard = true; Refresh(); return; }
            confirmDiscard = false;
            Show(BagActions.Discard(p, item));
        }

        void DowngradeSelected()
        {
            PlayerState p = RpgSession.Player;
            if (p != null) Show(BagActions.DevDowngrade(p, model.Selected(p)));
        }

        /// <summary>The anvil sign: shows the worn weapon (or the worn piece already selected, when it can be enhanced) with its enhance preview. It spends nothing itself.</summary>
        void EnhanceEquipped()
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            confirmDiscard = false;
            bool keep = model.Worn >= 0 && EnhanceStats.IsEnhanceable(p.GetEquipped(Slots.All[model.Worn]));
            if (!keep) model.Worn = Slots.Index(p.GetEquipped(Slots.Weapon) != null || p.GetEquipped(Slots.Offhand) == null ? Slots.Weapon : Slots.Offhand);
            Refresh();
            if (p.GetEquipped(Slots.All[model.Worn]) == null) Flash("No weapon equipped");
        }

        /// <summary>The Pet window is its own screen: this one closes first (it would cover it).</summary>
        static void OpenPets()
        {
            Close();
            UiScreens.Open(UiScreen.Pet);
        }

        /// <summary>Says what happened and redraws. A request sent to the server changes nothing yet: its reply redraws through the state fingerprint.</summary>
        void Show(BagResult r)
        {
            if (!string.IsNullOrEmpty(r.Message)) Flash(r.Message);
            Refresh();
        }
    }
}
