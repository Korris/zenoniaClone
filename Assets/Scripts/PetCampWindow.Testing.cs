using UnityEngine;
using UnityEngine.EventSystems;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PetCampWindow: states for capture runs (DebugCapture.Pets.cs --open-pets / --pet-select, DebugCapture.UiPet.cs) that a screenshot cannot
    /// reach with a mouse: a tab, a selection, a pet being carried, a tapped nest, a pressed button, a real drag. Each goes through the same
    /// methods the taps call. No-ops while the window is closed.
    /// </summary>
    public sealed partial class PetCampWindow
    {
        /// <summary>--open-pets [team|collection|growth|capture] and --pet-select id on the rebuilt screen.</summary>
        public static void TestShow(string tabName, string petId)
        {
            if (!IsOpen) return;
            PetDef d = PetConfig.Get(petId);
            if (d != null) instance.selected = PetConfig.IndexOf(d);
            instance.SetTab(tabName == "collection" ? Tab.Collection : tabName == "growth" ? Tab.Growth : tabName == "capture" ? Tab.Capture : Tab.Team);
        }

        /// <summary>Selects a species ("none" = nothing); `carry` = as a tap on its shelf card does: it is being placed.</summary>
        public static void TestSelect(string petId, bool carry)
        {
            if (!IsOpen) return;
            PetDef d = PetConfig.Get(petId);
            instance.selected = d != null ? PetConfig.IndexOf(d) : -1;
            instance.placing = carry && d != null;
            instance.menuSlot = -1;
            instance.Refresh();
        }

        /// <summary>The account's pets were changed under the open window (--pet-own): the selection starts over, as on Open.</summary>
        public static void TestResync()
        {
            if (!IsOpen) return;
            PlayerState p = PetActions.Me;
            instance.selected = p.PetId != "" ? PetConfig.IndexOf(p.Pet) : instance.FirstOwned(p.Account);
            instance.SetTab(instance.tab);
        }

        public static void TestNest(int slot) { if (IsOpen && slot >= 0 && slot < PetRules.TeamSize) instance.OnNestTap(slot); }

        /// <summary>Presses a button: remove, details, growth, place, levelup, starup, resetlevel, resetstars, convert10, convertall, rules, next, prev.</summary>
        public static void TestPress(string button)
        {
            if (!IsOpen) return;
            PetCampWindow w = instance;
            switch (button)
            {
                case "remove": w.RemoveFromNest(); break;
                case "details": w.SetTab(Tab.Collection); break;
                case "growth": w.SetTab(Tab.Growth); break;
                case "place": w.TogglePlacing(); break;
                case "levelup": w.SendGrowth(InvOp.PetLevelUp); break;
                case "starup": w.SendGrowth(InvOp.PetStarUp); break;
                case "resetlevel": w.SendGrowth(InvOp.PetResetLevel); break;
                case "resetstars": w.SendGrowth(InvOp.PetResetStars); break;
                case "convert10": PetViewModel.Convert(PetConfig.IndexOf(w.Shown), 10); break;
                case "convertall": PetViewModel.Convert(PetConfig.IndexOf(w.Shown), -1); break;
                case "rules": w.ToggleRules(); break;
                case "next": w.StepOwned(1); break;
                case "prev": w.StepOwned(-1); break;
                default: Debug.LogWarning("[ZenoniaProto] --pet-camp-press: unknown button '" + button + "'"); break;
            }
        }

        /// <summary>
        /// A real drag of the selected pet's shelf card through the handlers the pointer calls: picked up, carried to nest `slot` and dropped
        /// there; `drop` false = held over the nest (the look of a drag in progress).
        /// </summary>
        public static void TestDrag(int slot, bool drop)
        {
            if (!IsOpen || instance.tab != Tab.Team || EventSystem.current == null || slot < 0 || slot >= PetRules.TeamSize) return;
            int card = instance.owned.IndexOf(instance.selected) - instance.shelfPage * ShelfCards;
            if (card < 0 || card >= ShelfCards) return;
            var e = new PointerEventData(EventSystem.current);
            e.pressPosition = ScreenCentre(instance.shelfCards[card].Root);
            e.position = e.pressPosition + new Vector2(0f, 30f);
            instance.OnCardDrag(card, 0, e);
            e.position = ScreenCentre(instance.nests[slot].Root);
            instance.OnCardDrag(card, 1, e);
            if (drop) instance.OnCardDrag(card, 2, e);
        }

        static Vector2 ScreenCentre(RectTransform rt) { return RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center)); }
    }
}
