using System.Collections;
using System.IO;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --appearance-test --shot-path DIR: the offline appearance step of a new hero through the real input path (FakeInput taps on the kit buttons).
    /// Warrior (male body) then Cleric (female body, from the in-game change hero screen, with a Back on the way): colour combos, the idle-down
    /// preview (no turn / motion buttons, weapon breathing with the body, texture as sharp as the screen), Random, the name (refused when empty or too short), Create, then the created hero in the world in the picked colours under its name.
    /// Captures never touch saves (SaveStore off), so every class is a new hero. Writes DIR/*.png and "appearance-e2e: PASS/FAIL" lines.
    /// </summary>
    public partial class DebugCapture
    {
        static void A(string line) { Debug.Log("[ZenoniaProto] appearance-e2e: " + line); }

        IEnumerator RunAppearanceTest()
        {
            Directory.CreateDirectory(shotPath);
            while (!CharacterListScreen.IsShown) yield return null; // offline start: the character list first
            yield return WaitSeconds(1f);
            yield return Press(CentreOf("CreateNew"), false, "create new");
            while (!HeroSelectScreen.IsShown) yield return null;
            yield return WaitSeconds(1f);

            // ---- Warrior: male body ----
            yield return Press(CentreOf("Pick" + (int)HeroClass.Warrior), false, "warrior");
            yield return Press(CentreOf("StartButton"), false, "start a new warrior");
            yield return WaitSeconds(1f);
            A((CharacterCustomizeScreen.IsShown && CharacterCustomizeScreen.OfflineOpen && !HeroSelectScreen.IsShown && GameSession.Hero == null ? "PASS" : "FAIL") + " start on a class without a save opens the appearance step");
            yield return Shot("01-warrior-step-random-look");
            yield return HairStyleShots(HeroClass.Warrior);
            yield return PickColours(4, 1, "02-warrior-coral-red-sky-blue");
            yield return PreviewIdleDownChecks("warrior"); // DebugCapture.AppearanceBreath.cs: no turn / motion buttons, idle down, sharp texture
            yield return PickColours(2, 4, null);
            yield return BreathShots("warrior", "03-warrior-honey-breath");
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
            yield return PickColours(6, 5, "08-warrior-lavender-lilac-plum-final");
            CharacterAppearanceData warriorLook = CharacterCustomizeScreen.TestLook.Clone();
            yield return Press(CentreOf("Create"), false, "create without a name");
            yield return WaitSeconds(0.4f);
            A((CharacterCustomizeScreen.OfflineOpen && GameSession.Hero == null && CharacterCustomizeScreen.TestStatus == HeroNameRules.RuleText ? "PASS" : "FAIL") + " offline the hero must be named first: " + CharacterCustomizeScreen.TestStatus);
            CharacterCustomizeScreen.TestSetName("Ro");
            yield return WaitSeconds(0.3f);
            A((CharacterCustomizeScreen.TestHint == HeroNameRules.RuleText ? "PASS" : "FAIL") + " a two-letter name shows the name rule: " + CharacterCustomizeScreen.TestHint);
            CharacterCustomizeScreen.TestSetName("Rowan");
            yield return WaitSeconds(0.3f);
            yield return Shot("08b-warrior-named-rowan");
            yield return Press(CentreOf("Create"), false, "create");
            yield return WaitSeconds(1.5f);
            yield return CheckSpawned(HeroClass.Warrior, warriorLook);
            A((RpgSession.CurrentName == "Rowan" ? "PASS" : "FAIL") + " the hero plays under its name: " + RpgSession.CurrentName);
            yield return InWorldShots("09-warrior-in-world");

            // ---- Cleric: female body, from the change hero screen, Back once ----
            HeroChangeScreen.Open();
            yield return WaitSeconds(0.8f);
            yield return Press(CentreOf("CreateNew"), false, "create new over the game");
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
            yield return PreviewIdleDownChecks("cleric");
            yield return BreathShots("cleric", "12-cleric-silver-breath");
            yield return HairStyleShots(HeroClass.Cleric);
            yield return PickColours(5, 3, "13-cleric-sky-blue-warm-gold-final");
            CharacterAppearanceData clericLook = CharacterCustomizeScreen.TestLook.Clone();
            CharacterCustomizeScreen.TestSetName("Elin");
            yield return Press(CentreOf("Create"), false, "create");
            yield return WaitSeconds(1.5f);
            A((!HeroChangeScreen.IsOpen && !CharacterCustomizeScreen.IsShown ? "PASS" : "FAIL") + " confirm closes the change hero screen");
            yield return CheckSpawned(HeroClass.Cleric, clericLook);
            yield return InWorldShots("14-cleric-in-world");
            Application.Quit();
        }

        IEnumerator PickColours(int hair, int outfit, string shot)
        {
            bool fixedOutfit = CharacterCustomizeScreen.TestOutfitHidden; // RO Fantasy class bodies: no outfit colour to pick
            yield return Press(CentreOf("Hair" + hair), false, "hair " + hair);
            if (!fixedOutfit) yield return Press(CentreOf("Outfit" + outfit), false, "outfit " + outfit);
            yield return WaitSeconds(0.4f);
            CharacterAppearanceData l = CharacterCustomizeScreen.TestLook;
            A((l != null && l.stored && l.hairColorIndex == hair && (fixedOutfit || l.outfitColorIndex == outfit) && l.equippedHeadgearId == 0 ? "PASS" : "FAIL") + " taps picked hair " + hair + " (" + HeroPalettes.HairName(hair) + ")" + (fixedOutfit ? ", outfit fixed" : ", outfit " + outfit + " (" + HeroPalettes.OutfitName(outfit) + ")"));
            if (shot != null) yield return Shot(shot);
        }

        /// <summary>RO Fantasy bodies: the outfit colours are hidden, the hairstyle row offers 3 styles (warrior 4: 2 Starward + Knight V3 + Golden Paladin hair) and `>` steps through them (one shot each).</summary>
        IEnumerator HairStyleShots(HeroClass id)
        {
            string cls = HeroClassDef.Get(id).Name.ToLowerInvariant();
            bool roFantasy = HeroRenderer.LayeredSetFor(HeroClassDef.Get(id)) == HeroRenderer.RoFantasyRoot + cls + "/class";
            A((CharacterCustomizeScreen.TestOutfitHidden == roFantasy ? "PASS" : "FAIL") + " " + cls + ": outfit colours " + (roFantasy ? "hidden (fixed RO Fantasy outfit)" : "offered"));
            int n = CharacterCustomizeScreen.TestStyleCount;
            A((!roFantasy || n == (id == HeroClass.Warrior ? 4 : 3) ? "PASS" : "FAIL") + " " + cls + ": " + n + " hairstyles offered");
            for (int i = 0; i < n; i++)
            {
                int before = CharacterCustomizeScreen.TestLook.hairStyle;
                yield return Press(CentreOf("StyleNext"), false, "next hairstyle");
                yield return WaitSeconds(0.4f);
                int now = CharacterCustomizeScreen.TestLook.hairStyle;
                A((now == (before + 1) % n ? "PASS" : "FAIL") + " " + cls + ": > steps hairstyle " + before + " -> " + now);
                yield return Shot("style-" + cls + "-" + now);
            }
        }

        /// <summary>The spawned hero wears the picked look (style, colours), its state stores it bare-headed, and a save round trip keeps it.</summary>
        IEnumerator CheckSpawned(HeroClass id, CharacterAppearanceData want)
        {
            yield return null;
            HeroController h = GameSession.Hero;
            PlayerState p = RpgSession.Player;
            want = CharacterAppearanceData.ForNewCharacter(RpgSession.ClassId(id), want.hairStyle, want.hairColorIndex, want.outfitColorIndex);
            A((h != null && h.Class.Id == id ? "PASS" : "FAIL") + " confirm spawned the " + id);
            A((h != null && h.View.IsLayered && h.View.Appearance.hairColorIndex == want.hairColorIndex && h.View.Appearance.hairStyle == want.hairStyle ? "PASS" : "FAIL") + " the hero is drawn in the picked hairstyle " + want.hairStyle + " and colours");
            A((p != null && want.SameLook(p.Appearance) ? "PASS" : "FAIL") + " the character state stores the look (style, stored, no hat)");
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
