using System.Collections;
using System.IO;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --appearance-test --shot-path DIR: the offline appearance step of a new hero through the real input path (FakeInput taps on the kit buttons).
    /// Warrior (male body) then Cleric (female body, from the in-game change hero screen, with a Back on the way): colour combos, 8-way turn, walk and
    /// attack previews, Random, Confirm, then the created hero in the world in the picked colours. Captures never touch saves (SaveStore off), so every
    /// class is a new hero. Writes DIR/*.png and "appearance-e2e: PASS/FAIL" lines.
    /// </summary>
    public partial class DebugCapture
    {
        static void A(string line) { Debug.Log("[ZenoniaProto] appearance-e2e: " + line); }

        IEnumerator RunAppearanceTest()
        {
            Directory.CreateDirectory(shotPath);
            while (!HeroSelectScreen.IsShown) yield return null;
            yield return WaitSeconds(1f);

            // ---- Warrior: male body ----
            yield return Press(CentreOf("Pick" + (int)HeroClass.Warrior), false, "warrior");
            yield return Press(CentreOf("StartButton"), false, "start a new warrior");
            yield return WaitSeconds(1f);
            A((CharacterCustomizeScreen.IsShown && CharacterCustomizeScreen.OfflineOpen && !HeroSelectScreen.IsShown && GameSession.Hero == null ? "PASS" : "FAIL") + " start on a class without a save opens the appearance step");
            yield return Shot("01-warrior-step-random-look");
            yield return PickColours(4, 1, "02-warrior-coral-red-sky-blue");
            for (int i = 0; i < 4; i++) yield return Press(CentreOf("TurnRight"), false, "turn right");
            yield return WaitSeconds(0.4f);
            yield return Shot("03-warrior-coral-sky-turned-up-left");
            yield return Press(CentreOf("Motion1"), false, "walk tab");
            yield return Press(CentreOf("TurnLeft"), false, "turn left");
            yield return Press(CentreOf("TurnLeft"), false, "turn left");
            yield return WaitSeconds(0.35f);
            yield return Shot("04-warrior-walk-up");
            yield return PickColours(2, 4, null);
            yield return Press(CentreOf("Motion2"), false, "attack tab");
            yield return Press(CentreOf("TurnLeft"), false, "turn left");
            yield return Press(CentreOf("TurnLeft"), false, "turn left");
            yield return Press(CentreOf("TurnLeft"), false, "turn left");
            yield return WaitSeconds(0.3f);
            yield return Shot("05-warrior-honey-mint-attack-a");
            yield return WaitSeconds(0.25f);
            yield return Shot("06-warrior-honey-mint-attack-b");
            CharacterAppearanceData before = CharacterAppearanceData.ForNewCharacter("warrior", 0, 2, 4);
            bool changed = false;
            for (int i = 0; i < 5 && !changed; i++)
            {
                yield return Press(CentreOf("Random"), false, "random");
                yield return WaitSeconds(0.2f);
                changed = !before.SameLook(CharacterAppearanceData.Sanitize(CharacterCustomizeScreen.TestLook, "warrior"));
            }
            A((changed ? "PASS" : "FAIL") + " Random rolls a new look");
            yield return Shot("07-warrior-random");
            yield return Press(CentreOf("Motion0"), false, "idle tab");
            yield return PickColours(6, 5, "08-warrior-lavender-lilac-plum-final");
            yield return Press(CentreOf("Create"), false, "confirm");
            yield return WaitSeconds(1.5f);
            yield return CheckSpawned(HeroClass.Warrior, 6, 5);
            yield return InWorldShots("09-warrior-in-world");

            // ---- Cleric: female body, from the change hero screen, Back once ----
            HeroChangeScreen.Open();
            yield return WaitSeconds(0.8f);
            yield return Press(CentreOf("Pick" + (int)HeroClass.Cleric), false, "cleric");
            yield return Press(CentreOf("StartButton"), false, "start a new cleric");
            yield return WaitSeconds(1f);
            A((CharacterCustomizeScreen.OfflineOpen && GameSession.Hero != null && GameSession.Hero.Class.Id == HeroClass.Warrior ? "PASS" : "FAIL") + " over the game: the appearance step opens, the warrior still plays");
            yield return Press(CentreOf("LookBack"), false, "back");
            yield return WaitSeconds(0.8f);
            A((!CharacterCustomizeScreen.IsShown && HeroSelectScreen.IsShown ? "PASS" : "FAIL") + " Back returns to the class choice");
            yield return Press(CentreOf("StartButton"), false, "start the cleric again");
            yield return WaitSeconds(1f);
            A((CharacterCustomizeScreen.OfflineOpen ? "PASS" : "FAIL") + " Back kept the cleric picked");
            yield return PickColours(7, 0, "10-cleric-sakura-pink-coral-red");
            yield return PickColours(3, 2, "11-cleric-silver-sage-green");
            yield return Press(CentreOf("Motion1"), false, "walk tab");
            yield return Press(CentreOf("TurnRight"), false, "turn right");
            yield return WaitSeconds(0.3f);
            yield return Shot("12-cleric-silver-sage-walk-right");
            yield return Press(CentreOf("Motion0"), false, "idle tab");
            yield return PickColours(5, 3, "13-cleric-sky-blue-warm-gold-final");
            yield return Press(CentreOf("Create"), false, "confirm");
            yield return WaitSeconds(1.5f);
            A((!HeroChangeScreen.IsOpen && !CharacterCustomizeScreen.IsShown ? "PASS" : "FAIL") + " confirm closes the change hero screen");
            yield return CheckSpawned(HeroClass.Cleric, 5, 3);
            yield return InWorldShots("14-cleric-in-world");
            Application.Quit();
        }

        IEnumerator PickColours(int hair, int outfit, string shot)
        {
            yield return Press(CentreOf("Hair" + hair), false, "hair " + hair);
            yield return Press(CentreOf("Outfit" + outfit), false, "outfit " + outfit);
            yield return WaitSeconds(0.4f);
            CharacterAppearanceData l = CharacterCustomizeScreen.TestLook;
            A((l != null && l.stored && l.hairColorIndex == hair && l.outfitColorIndex == outfit && l.equippedHeadgearId == 0 ? "PASS" : "FAIL") + " taps picked hair " + hair + " (" + HeroPalettes.HairName(hair) + "), outfit " + outfit + " (" + HeroPalettes.OutfitName(outfit) + ")");
            if (shot != null) yield return Shot(shot);
        }

        /// <summary>The spawned hero wears the picked look, its state stores it bare-headed, and a save round trip keeps it.</summary>
        IEnumerator CheckSpawned(HeroClass id, int hair, int outfit)
        {
            yield return null;
            HeroController h = GameSession.Hero;
            PlayerState p = RpgSession.Player;
            CharacterAppearanceData want = CharacterAppearanceData.ForNewCharacter(RpgSession.ClassId(id), 0, hair, outfit);
            A((h != null && h.Class.Id == id ? "PASS" : "FAIL") + " confirm spawned the " + id);
            A((h != null && h.View.IsLayered && h.View.Appearance.hairColorIndex == hair && h.View.Appearance.outfitColorIndex == outfit ? "PASS" : "FAIL") + " the hero is drawn in the picked colours");
            A((p != null && want.SameLook(p.Appearance) ? "PASS" : "FAIL") + " the character state stores the look (stored, no hat)");
            PlayerState back = p != null ? SaveSerializer.Restore(SaveSerializer.Parse(SaveSerializer.ToJson(SaveSerializer.Capture(p, false, 0f, 0f, false, 1)))) : null;
            A((back != null && want.SameLook(back.Appearance) ? "PASS" : "FAIL") + " a save round trip keeps the look");
        }

        IEnumerator InWorldShots(string name)
        {
            HeroController h = GameSession.Hero;
            if (h == null) yield break;
            Camera.main.orthographicSize = 32f;
            h.God = true;
            yield return WaitSeconds(0.8f);
            yield return Shot(name + "-idle");
            InputAdapter.BotHeld = k => k == GameKey.Right;
            yield return WaitSeconds(0.45f);
            yield return Shot(name + "-walk-right");
            InputAdapter.BotHeld = k => k == GameKey.Down;
            yield return WaitSeconds(0.45f);
            yield return Shot(name + "-walk-down");
            InputAdapter.BotHeld = null;
            InputAdapter.BotPressed = k => k == GameKey.Attack;
            yield return WaitSeconds(0.35f);
            yield return Shot(name + "-attack");
            InputAdapter.BotPressed = null;
            yield return WaitSeconds(0.5f);
        }
    }
}
