using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the rebuilt pet screen, Pet Camp (PetCampWindow; phase 08 of plans/261116-1206-adventure-paper-ui-rebuild).
    /// Use them with --ui-new pet --open-pets [team|collection|growth|capture] and the pet flags of DebugCapture.Pets.cs (--give-pets, --pet ID,
    /// --pet-team a,b,c,d,e, --pet-level N, --pet-stars N, --pet-food N, --pet-balls N, --rare-balls N, --shared-shards N, --pet-select id, --level N).
    ///   --pet-nest-demo empty        every nest emptied (the team of a new hero); works on the old window too
    ///   --pet-own none|id,id         the account owns exactly these species (dev-start.json may have given all ten); works on the old window too
    ///   --pet-shards N               every owned pet holds N shards of its kind (Star Up with enough / not enough); works on the old window too
    ///   --pet-camp-select id|none    that species selected (none = the "No pet selected" paper)
    ///   --pet-camp-place id          that species carried, as after a tap on its shelf card ("Tap a nest to put ...")
    ///   --pet-camp-nest N            tap nest N (0 Combat, 1-2 Support, 3-4 Stat): Remove / Details, or the carried pet goes there
    ///   --pet-camp-press a,b         press in order: remove, details, growth, place, levelup, starup, resetlevel, resetstars, convert10, convertall, rules, next, prev
    ///   --pet-camp-drag N            drag the selected pet's shelf card onto nest N and drop it;  --pet-camp-drag-hold N   the same, held over the nest
    /// Keys: --ui-keys "1=PetWindow;2=PetWindow;3=PetWindow;4=Close" (DebugCapture.UiRedesign.cs) presses the real keys.
    /// </summary>
    public partial class DebugCapture
    {
        partial void DebugUiPet()
        {
            PlayerState p = RpgSession.Player;
            PlayerRpg owner = RpgSession.Owner;
            if (p == null) return;
            string own = Val("--pet-own"), shards = Val("--pet-shards");
            if (own != null && owner != null && owner.Account != null)
            {
                var keep = new System.Collections.Generic.List<string>(own.Split(','));
                owner.Account.Run("capture", a =>
                {
                    a.pets.RemoveAll(o => !keep.Contains(o.id));
                    foreach (string id in keep) if (PetConfig.Get(id) != null) AccountPets.Add(a, id);
                    return ActionResult.Success("");
                }, r => { });
                p.AttachAccount(owner.Account.Live); // drops team pets the account no longer owns
                RpgSession.SyncGear();
                PetCampWindow.TestResync();
                if (Flag("--open-pets")) PetCampWindow.TestShow(Val("--open-pets"), Val("--pet-select"));
            }
            if (shards != null && owner != null && owner.Account != null)
            {
                owner.Account.Run("capture", a => { foreach (OwnedPet o in a.pets) o.shards = int.Parse(shards); return ActionResult.Success(""); }, r => { });
                p.AttachAccount(owner.Account.Live);
            }
            if (Val("--pet-nest-demo") == "empty")
            {
                for (int i = 0; i < PetRules.TeamSize; i++) if (p.Team[i] != "") p.SetTeamSlot(i, "");
                RpgSession.SyncGear();
            }
            if (!PetCampWindow.IsOpen) return; // the old look: only the data above applies

            string select = Val("--pet-camp-select"), place = Val("--pet-camp-place"), nest = Val("--pet-camp-nest"), press = Val("--pet-camp-press");
            string drag = Val("--pet-camp-drag"), hold = Val("--pet-camp-drag-hold");
            if (select != null) PetCampWindow.TestSelect(select, false);
            if (place != null) PetCampWindow.TestSelect(place, true);
            if (nest != null) PetCampWindow.TestNest(int.Parse(nest));
            if (press != null) foreach (string b in press.Split(',')) PetCampWindow.TestPress(b.Trim());
            if (drag != null) PetCampWindow.TestDrag(int.Parse(drag), true);
            if (hold != null) PetCampWindow.TestDrag(int.Parse(hold), false);
        }
    }
}
