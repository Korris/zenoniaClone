using System.Collections;
using System.IO;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --character-select-test --save-root DIR --shot-path SHOTS: the offline character select screen through the real input path (FakeInput taps and holds).
    /// Empty list, three seeded saves, CREATE NEW -> class step (classes with a save refused) -> look step -> Back -> again -> Confirm into the world,
    /// Change hero over the game (the played hero cannot be entered or deleted), turning the big hero, ENTER GAME on another save, delete with a short tap
    /// (nothing happens), KEEP, then a two-second hold (the save file is gone). DIR must be a throw-away folder: the run writes and deletes saves there.
    /// </summary>
    public partial class DebugCapture
    {
        static void Cs(string line) { Debug.Log("[ZenoniaProto] charselect-e2e: " + line); }

        IEnumerator RunCharacterSelectTest()
        {
            Directory.CreateDirectory(shotPath);
            if (!SaveStore.Enabled) Cs("FAIL needs --save-root DIR (saves are off)");
            foreach (HeroClassDef d in HeroClassDef.All) SaveStore.Delete(RpgSession.ClassId(d.Id)); // a fresh device
            while (!CharacterListScreen.IsShown) yield return null;
            yield return WaitSeconds(1f);
            Cs((CharacterListScreen.Shown == CharacterListScreen.Mode.Offline && !HeroSelectScreen.IsShown && CharacterListScreen.TestHeroes.Count == 0 ? "PASS" : "FAIL") + " offline start opens the character list, not the class select");
            yield return Shot("01-offline-empty");

            Seed("mechanic", 23, 1, 2, 0);
            Seed("wizard", 41, 2, 5, 0);
            Seed("assassin", 12, 0, 7, 0);
            CharacterListScreen.TestReload();
            yield return WaitSeconds(1f);
            Cs((CharacterListScreen.TestHeroes.Count == 3 ? "PASS" : "FAIL") + " three saves listed: " + CharacterListScreen.TestHeroes.Count);
            yield return Shot("02-offline-three-saves");
            yield return Press(CentreOf("HeroRow0"), false, "wizard card"); // class order: wizard, mechanic, assassin
            yield return WaitSeconds(0.6f);
            Cs((CharacterListScreen.TestSelectedName == "Wizard" ? "PASS" : "FAIL") + " a tap on a card picks it: " + CharacterListScreen.TestSelectedName);
            yield return Press(CentreOf("ListTurnRight"), false, "turn");
            yield return Press(CentreOf("ListTurnRight"), false, "turn");
            yield return Press(CentreOf("HeroRow1"), false, "mechanic card");
            yield return WaitSeconds(0.5f);
            yield return Shot("03-mechanic-picked-turned");

            // ---- create: class step -> look step -> Back -> again -> Confirm ----
            yield return Press(CentreOf("CreateNew"), false, "create new");
            yield return WaitSeconds(1f);
            Cs((HeroSelectScreen.IsShown && HeroSelectScreen.Creating && !CharacterListScreen.IsShown ? "PASS" : "FAIL") + " CREATE NEW opens the class step");
            yield return Press(CentreOf("Pick" + (int)HeroClass.Wizard), false, "wizard (has a save)");
            yield return WaitSeconds(0.4f);
            yield return Shot("04-class-step-wizard-taken");
            yield return Press(CentreOf("StartButton"), false, "next on a taken class");
            yield return WaitSeconds(0.5f);
            Cs((HeroSelectScreen.IsShown && !CharacterCustomizeScreen.IsShown ? "PASS" : "FAIL") + " a class that has a save cannot go on");
            yield return Press(CentreOf("BackButton"), false, "back");
            yield return WaitSeconds(0.6f);
            Cs((CharacterListScreen.IsShown && !HeroSelectScreen.IsShown ? "PASS" : "FAIL") + " BACK on the class step returns to the list");
            yield return Press(CentreOf("HeroRow5"), false, "the last empty slot");
            yield return WaitSeconds(0.8f);
            Cs((HeroSelectScreen.Creating ? "PASS" : "FAIL") + " an empty slot opens the class step too");
            yield return Press(CentreOf("Pick" + (int)HeroClass.Warrior), false, "warrior");
            yield return Press(CentreOf("StartButton"), false, "next");
            yield return WaitSeconds(0.8f);
            Cs((CharacterCustomizeScreen.OfflineOpen && !HeroSelectScreen.IsShown ? "PASS" : "FAIL") + " NEXT opens the look step");
            yield return Press(CentreOf("LookBack"), false, "back from the look step");
            yield return WaitSeconds(0.8f);
            Cs((HeroSelectScreen.IsShown && HeroSelectScreen.Creating && !CharacterCustomizeScreen.IsShown ? "PASS" : "FAIL") + " BACK on the look step returns to the class step");
            yield return Press(CentreOf("StartButton"), false, "next again (warrior kept)");
            yield return WaitSeconds(0.8f);
            yield return Press(CentreOf("Hair4"), false, "coral hair");
            yield return Shot("05-look-step-warrior");
            yield return Press(CentreOf("Create"), false, "confirm");
            yield return WaitSeconds(1.5f);
            HeroController h = GameSession.Hero;
            Cs((h != null && h.Class.Id == HeroClass.Warrior && !CharacterListScreen.IsShown ? "PASS" : "FAIL") + " confirm puts the new warrior in the world");
            Cs((File.Exists(SaveStore.PathFor("warrior")) ? "PASS" : "FAIL") + " the new hero is saved on the device");
            yield return WaitSeconds(0.5f);
            yield return Shot("06-warrior-in-world");

            // ---- Change hero over the game ----
            HeroChangeScreen.Open(); // what Settings > Change hero calls
            yield return WaitSeconds(1f);
            Cs((CharacterListScreen.IsShown && CharacterListScreen.TestHeroes.Count == 4 && CharacterListScreen.TestSelectedName == "Warrior" ? "PASS" : "FAIL") + " Change hero shows the list over the game, on the hero being played");
            Cs((CharacterListScreen.TestSlotOf("Warrior") == 5 && CharacterListScreen.TestSlotOf("Wizard") == 0 ? "PASS" : "FAIL") + " the new hero sits in the slot that was tapped: " + CharacterListScreen.TestSlotOf("Warrior"));
            yield return Shot("07-over-game-warrior-playing");
            yield return Press(CentreOf("EnterGame"), false, "enter on the played hero");
            yield return Press(CentreOf("DeleteHero"), false, "delete on the played hero");
            yield return WaitSeconds(0.4f);
            Cs((GameSession.Hero == h && CharacterListScreen.IsShown && !CharacterListScreen.TestDeleteConfirmOpen ? "PASS" : "FAIL") + " the played hero can neither be entered again nor deleted");
            CharacterListScreen.TestSelect("Mechanic");
            yield return WaitSeconds(0.5f);
            yield return Press(CentreOf("EnterGame"), false, "enter the mechanic");
            yield return WaitSeconds(1.5f);
            Cs((GameSession.Hero != null && GameSession.Hero.Class.Id == HeroClass.Mechanic && RpgSession.Player.Level == 23 && !HeroChangeScreen.IsOpen ? "PASS" : "FAIL") + " ENTER GAME switches to the mechanic save (Lv " + (RpgSession.Player != null ? RpgSession.Player.Level : 0) + ")");
            yield return Shot("08-mechanic-in-world");

            // ---- delete: tap, then hold ----
            HeroChangeScreen.Open();
            yield return WaitSeconds(0.8f);
            CharacterListScreen.TestSelect("Assassin");
            yield return WaitSeconds(0.4f);
            yield return Press(CentreOf("DeleteHero"), false, "delete assassin");
            yield return WaitSeconds(0.4f);
            Cs((CharacterListScreen.TestDeleteConfirmOpen ? "PASS" : "FAIL") + " DELETE asks first");
            yield return Shot("09-delete-confirm");
            yield return Press(CentreOf("HoldDelete"), false, "short tap on hold-to-delete");
            yield return WaitSeconds(0.4f);
            Cs((File.Exists(SaveStore.PathFor("assassin")) && CharacterListScreen.TestDeleteConfirmOpen ? "PASS" : "FAIL") + " a short tap deletes nothing");
            yield return Press(CentreOf("KeepHero"), false, "keep");
            yield return WaitSeconds(0.3f);
            Cs((!CharacterListScreen.TestDeleteConfirmOpen && File.Exists(SaveStore.PathFor("assassin")) ? "PASS" : "FAIL") + " KEEP closes and keeps the hero");
            yield return Press(CentreOf("DeleteHero"), false, "delete assassin again");
            yield return WaitSeconds(0.3f);
            yield return PressHold(CentreOf("HoldDelete"), 1f, "hold 1 s", false);
            yield return Shot("10-delete-holding");
            yield return PressHold(CentreOf("HoldDelete"), CharacterListScreen.DeleteHoldSeconds + 0.4f, "hold to delete", true);
            yield return WaitSeconds(0.6f);
            Cs((!File.Exists(SaveStore.PathFor("assassin")) && CharacterListScreen.TestHeroes.Count == 3 ? "PASS" : "FAIL") + " a two-second hold deletes the save");
            yield return Shot("11-after-delete");
            yield return Press(CentreOf("BackToGame"), false, "back to the game");
            yield return WaitSeconds(0.5f);
            Cs((!CharacterListScreen.IsShown && !HeroChangeScreen.IsOpen && GameSession.Hero != null && GameSession.Hero.Class.Id == HeroClass.Mechanic ? "PASS" : "FAIL") + " BACK returns to the same hero");
            Cs("done");
            Application.Quit();
        }

        /// <summary>A save of a class with a level and a chosen look, as if played before.</summary>
        static void Seed(string classId, int level, int style, int hair, int outfit)
        {
            PlayerState p = PlayerState.NewGame(classId);
            p.Level = level;
            p.Appearance = CharacterAppearanceData.ForNewCharacter(classId, style, hair, outfit);
            SaveStore.Write(SaveSerializer.Capture(p, false, 0f, 0f, false, 0, "frostHollow"));
        }

        /// <summary>Holds the pointer down at a screen point for `seconds` (the hold-to-delete button). release = let go afterwards; false keeps it down for the next call.</summary>
        IEnumerator PressHold(Vector2 screen, float seconds, string what, bool release)
        {
            if (fakeInput == null)
            {
                fakeInput = UnityEngine.EventSystems.EventSystem.current.gameObject.AddComponent<FakeInput>();
                UnityEngine.EventSystems.EventSystem.current.currentInputModule.inputOverride = fakeInput;
            }
            UnityEngine.EventSystems.EventSystem.current.SendMessage("OnApplicationFocus", true);
            fakeInput.UseTouch = false;
            fakeInput.Pos = screen;
            if (fakeInput.UpFrame != int.MaxValue)
            {
                yield return null;
                fakeInput.DownFrame = Time.frameCount + 1;
                fakeInput.UpFrame = int.MaxValue;
            }
            yield return WaitSeconds(seconds);
            if (release)
            {
                fakeInput.UpFrame = Time.frameCount + 1;
                while (Time.frameCount <= fakeInput.UpFrame + 1) yield return null;
            }
            Cs("hold " + what + " at " + screen);
        }
    }
}
