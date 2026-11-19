using System.Collections;
using System.IO;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --character-select-test --save-root DIR --shot-path SHOTS: the offline character select screen through the real input path (FakeInput taps and holds).
    /// Empty list; two saves from before slots (one per class) + one slot save: the old ones move into slots (.migrated backups kept); CREATE -> class step
    /// -> Back; an empty slot -> a SECOND Wizard (classes with a save are no longer locked) -> look step -> Back -> again -> naming (a name another hero has is
    /// refused, case-insensitive) -> Create into the world; Change hero over the game (two Wizards listed, the played one can be neither entered nor deleted),
    /// ENTER the older Wizard and the migrated Mechanic (their own saves), delete with a short tap (nothing), KEEP, then a two-second hold (the slot file is gone).
    /// DIR must be a throw-away folder: the run writes and deletes saves there.
    /// </summary>
    public partial class DebugCapture
    {
        static void Cs(string line) { Debug.Log("[ZenoniaProto] charselect-e2e: " + line); }
        static string Pf(bool ok) { return ok ? "PASS" : "FAIL"; }

        IEnumerator RunCharacterSelectTest()
        {
            Directory.CreateDirectory(shotPath);
            if (!SaveStore.Enabled) Cs("FAIL needs --save-root DIR (saves are off)");
            ClearSaveRoot(); // a fresh device
            while (!CharacterListScreen.IsShown) yield return null;
            yield return WaitSeconds(1f);
            CharacterListScreen.TestReload();
            yield return WaitSeconds(0.5f);
            Cs(Pf(CharacterListScreen.Shown == CharacterListScreen.Mode.Offline && !HeroSelectScreen.IsShown && CharacterListScreen.TestHeroes.Count == 0) + " offline start opens the character list, not the class select");
            yield return Shot("01-offline-empty");

            // ---- saves from before slots move over ----
            Seed(2, "assassin", 12, 0, 7, 0, "Shade");
            SeedLegacy("mechanic", 23, 1, 2, 0, 4); // remembered slot 4
            SeedLegacy("wizard", 41, 2, 5, 0, -1);  // no slot file: first free = 0
            CharacterListScreen.TestReload();
            yield return WaitSeconds(1f);
            CheckMigrated();
            Cs(Pf(CharacterListScreen.TestHeroes.Count == 3) + " three saves listed: " + CharacterListScreen.TestHeroes.Count);
            yield return Shot("02-offline-three-saves-migrated");
            yield return Press(CentreOf("HeroRow0"), false, "wizard card");
            yield return WaitSeconds(0.6f);
            Cs(Pf(CharacterListScreen.TestSelectedName == "Wizard") + " a tap on a card picks it: " + CharacterListScreen.TestSelectedName);
            yield return Press(CentreOf("ListTurnRight"), false, "turn");
            yield return Press(CentreOf("ListTurnRight"), false, "turn");
            yield return Press(CentreOf("HeroRow4"), false, "mechanic card");
            yield return WaitSeconds(0.5f);
            yield return Shot("03-mechanic-picked-turned");

            // ---- create: class step -> Back; empty slot -> a second Wizard -> look -> Back -> again -> name -> Create ----
            yield return Press(CentreOf("CreateNew"), false, "create new");
            yield return WaitSeconds(1f);
            Cs(Pf(HeroSelectScreen.IsShown && HeroSelectScreen.Creating && !CharacterListScreen.IsShown) + " CREATE opens the class step");
            yield return Press(CentreOf("BackButton"), false, "back");
            yield return WaitSeconds(0.6f);
            Cs(Pf(CharacterListScreen.IsShown && !HeroSelectScreen.IsShown) + " BACK on the class step returns to the list");
            yield return Press(CentreOf("HeroRow5"), false, "the last empty slot");
            yield return WaitSeconds(0.8f);
            Cs(Pf(HeroSelectScreen.Creating) + " an empty slot opens the class step too");
            yield return Press(CentreOf("Pick" + (int)HeroClass.Wizard), false, "wizard (one is saved already)");
            yield return WaitSeconds(0.4f);
            yield return Shot("04-class-step-wizard-open");
            yield return Press(CentreOf("StartButton"), false, "next on a class that has a hero");
            yield return WaitSeconds(0.8f);
            Cs(Pf(CharacterCustomizeScreen.OfflineOpen && !HeroSelectScreen.IsShown) + " a class that already has a hero can be made again");
            yield return Press(CentreOf("LookBack"), false, "back from the look step");
            yield return WaitSeconds(0.8f);
            Cs(Pf(HeroSelectScreen.IsShown && HeroSelectScreen.Creating && !CharacterCustomizeScreen.IsShown) + " BACK on the look step returns to the class step");
            yield return Press(CentreOf("StartButton"), false, "next again (wizard kept)");
            yield return WaitSeconds(0.8f);
            yield return Press(CentreOf("Hair4"), false, "coral hair");
            yield return Press(CentreOf("Create"), false, "create without a name");
            yield return WaitSeconds(0.4f);
            Cs(Pf(CharacterCustomizeScreen.OfflineOpen && GameSession.Hero == null && CharacterCustomizeScreen.TestStatus == HeroNameRules.RuleText) + " an empty name is refused: " + CharacterCustomizeScreen.TestStatus);
            CharacterCustomizeScreen.TestSetName("wIZARD");
            yield return Press(CentreOf("Create"), false, "create with a name already on the device");
            yield return WaitSeconds(0.4f);
            // the refusal shows in the hint under the name (the status line stays empty when it would repeat the hint)
            bool taken = CharacterCustomizeScreen.TestStatus == HeroNameRules.TakenText || CharacterCustomizeScreen.TestHint == HeroNameRules.TakenText;
            Cs(Pf(CharacterCustomizeScreen.OfflineOpen && GameSession.Hero == null && taken) + " a name another hero has is refused (any case): " + CharacterCustomizeScreen.TestHint);
            yield return Shot("05a-naming-step-name-taken");
            CharacterCustomizeScreen.TestSetName("Merlin");
            yield return WaitSeconds(0.4f);
            Cs(Pf(CharacterCustomizeScreen.TestHint == "3-16 letters, digits or _" && string.IsNullOrEmpty(CharacterCustomizeScreen.TestStatus)) + " a free name clears the warning");
            yield return Shot("05b-naming-step-merlin");
            yield return Press(CentreOf("Create"), false, "create");
            yield return WaitSeconds(1.5f);
            HeroController h = GameSession.Hero;
            Cs(Pf(h != null && h.Class.Id == HeroClass.Wizard && RpgSession.CurrentName == "Merlin" && RpgSession.CurrentSlot == 5 && !CharacterListScreen.IsShown) + " CREATE puts the new wizard Merlin in the world, slot " + RpgSession.CurrentSlot);
            SaveData merlin = SaveStore.Load(5);
            Cs(Pf(merlin != null && merlin.name == "Merlin" && merlin.classId == "wizard" && merlin.level == 1) + " the new hero is saved in the slot that was tapped, with its name");
            yield return WaitSeconds(0.5f);
            yield return Shot("06-merlin-in-world");

            // ---- Change hero over the game: two wizards ----
            HeroChangeScreen.Open(); // what Settings > Change hero calls
            yield return WaitSeconds(1f);
            int wizards = CharacterListScreen.TestHeroes.FindAll(c => c.classId == "wizard").Count;
            Cs(Pf(CharacterListScreen.IsShown && CharacterListScreen.TestHeroes.Count == 4 && wizards == 2 && CharacterListScreen.TestSelectedName == "Merlin") + " Change hero lists both wizards over the game, on the hero being played");
            Cs(Pf(CharacterListScreen.TestSlotOf("Merlin") == 5 && CharacterListScreen.TestSlotOf("Wizard") == 0 && CharacterListScreen.TestSlotOf("Mechanic") == 4) + " every hero sits in its slot: Merlin " + CharacterListScreen.TestSlotOf("Merlin"));
            yield return Shot("07-two-wizards-merlin-playing");
            yield return Press(CentreOf("EnterGame"), false, "enter on the played hero");
            yield return Press(CentreOf("DeleteHero"), false, "delete on the played hero");
            yield return WaitSeconds(0.4f);
            Cs(Pf(GameSession.Hero == h && CharacterListScreen.IsShown && !CharacterListScreen.TestDeleteConfirmOpen) + " the played hero can neither be entered again nor deleted");
            CharacterListScreen.TestSelect("Wizard");
            yield return WaitSeconds(0.5f);
            yield return Shot("07b-two-wizards-old-one-picked");
            yield return Press(CentreOf("EnterGame"), false, "enter the older wizard");
            yield return WaitSeconds(1.5f);
            Cs(Pf(GameSession.Hero != null && GameSession.Hero.Class.Id == HeroClass.Wizard && RpgSession.CurrentSlot == 0 && RpgSession.Player.Level == 41) + " ENTER on the other wizard loads its own save (Lv " + (RpgSession.Player != null ? RpgSession.Player.Level : 0) + ")");
            merlin = SaveStore.Load(5);
            Cs(Pf(merlin != null && merlin.name == "Merlin" && merlin.hasPosition) + " switching saved Merlin in its own slot");
            HeroChangeScreen.Open();
            yield return WaitSeconds(0.8f);
            CharacterListScreen.TestSelect("Mechanic");
            yield return WaitSeconds(0.5f);
            yield return Press(CentreOf("EnterGame"), false, "enter the mechanic");
            yield return WaitSeconds(1.5f);
            Cs(Pf(GameSession.Hero != null && GameSession.Hero.Class.Id == HeroClass.Mechanic && RpgSession.Player.Level == 23 && !HeroChangeScreen.IsOpen) + " ENTER on the migrated mechanic keeps its old save (Lv " + (RpgSession.Player != null ? RpgSession.Player.Level : 0) + ")");
            yield return Shot("08-mechanic-in-world");

            // ---- delete: tap, then hold ----
            HeroChangeScreen.Open();
            yield return WaitSeconds(0.8f);
            CharacterListScreen.TestSelect("Shade");
            yield return WaitSeconds(0.4f);
            yield return Press(CentreOf("DeleteHero"), false, "delete the assassin");
            yield return WaitSeconds(0.4f);
            Cs(Pf(CharacterListScreen.TestDeleteConfirmOpen) + " DELETE asks first");
            yield return Shot("09-delete-confirm");
            string shade = SaveStore.PathForSlot(2);
            yield return Press(CentreOf("HoldDelete"), false, "short tap on hold-to-delete");
            yield return WaitSeconds(0.4f);
            Cs(Pf(File.Exists(shade) && CharacterListScreen.TestDeleteConfirmOpen) + " a short tap deletes nothing");
            yield return Press(CentreOf("KeepHero"), false, "keep");
            yield return WaitSeconds(0.3f);
            Cs(Pf(!CharacterListScreen.TestDeleteConfirmOpen && File.Exists(shade)) + " KEEP closes and keeps the hero");
            yield return Press(CentreOf("DeleteHero"), false, "delete the assassin again");
            yield return WaitSeconds(0.3f);
            yield return PressHold(CentreOf("HoldDelete"), 1f, "hold 1 s", false);
            yield return Shot("10-delete-holding");
            yield return PressHold(CentreOf("HoldDelete"), CharacterListScreen.DeleteHoldSeconds + 0.4f, "hold to delete", true);
            yield return WaitSeconds(0.6f);
            Cs(Pf(!File.Exists(shade) && CharacterListScreen.TestHeroes.Count == 3) + " a two-second hold deletes the save");
            yield return Shot("11-after-delete");
            yield return Press(CentreOf("BackToGame"), false, "back to the game");
            yield return WaitSeconds(0.5f);
            Cs(Pf(!CharacterListScreen.IsShown && !HeroChangeScreen.IsOpen && GameSession.Hero != null && GameSession.Hero.Class.Id == HeroClass.Mechanic) + " BACK returns to the same hero");
            Cs("done");
            Application.Quit();
        }
    }
}
