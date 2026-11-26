using System.Collections;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// --hero-select-test --shot-path P: the hero select (reached by CREATE NEW on the character list) through its real input path (FakeInput). Taps every offered class (HeroSelectScreen.PickerClasses: the Archer, no Fighter) with a shot of each, picks the Cleric again, pages the skills, starts
    /// the Cleric, opens the in-game change screen and backs out of it. Writes P_open.png, P_warrior.png ... P_ingame.png and PASS / FAIL lines with the E2e tag.
    /// </summary>
    public partial class DebugCapture
    {
        IEnumerator RunHeroSelectTest()
        {
            while (!CharacterListScreen.IsShown) yield return null; // offline start: the character list first, its CREATE NEW leads to the hero select
            yield return WaitSeconds(1f);
            yield return Press(CentreOf("CreateNew"), false, "create new");
            while (!HeroSelectScreen.IsShown) yield return null;
            yield return WaitSeconds(1f);
            yield return new WaitForEndOfFrame();
            Save(shotPath + "_open.png");
            foreach (HeroClass id in HeroSelectScreen.PickerClasses)
            {
                HeroClassDef c = HeroClassDef.Get(id);
                yield return Press(CentreOf("Pick" + (int)c.Id), false, "hero " + c.Name);
                for (int i = 0; i < 12; i++)
                {
                    yield return WaitSeconds(0.45f); // a full preview loop: does the gear stay in the frame while it swings?
                    yield return new WaitForEndOfFrame();
                    Save(shotPath + (i == 0 ? "_" + c.Name.ToLowerInvariant() : "_preview_" + c.Name.ToLowerInvariant() + "_" + i.ToString("D2")) + ".png");
                }
            }

            // The loop above ends on the Archer; the Cleric is picked again so the start below has to spawn her.
            string startArg = Arg(System.Environment.GetCommandLineArgs(), "--hero-select-start"); // --hero-select-start Fighter: start with that class instead
            HeroClass startClass = startArg != null ? ParseClass(startArg) : HeroClass.Cleric;
            yield return Press(CentreOf("Pick" + (int)startClass), false, "hero " + startClass + " again");
            yield return Press(CentreOf("SkillNext"), true, "next skills");
            yield return Press(CentreOf("Skill0"), true, "first skill of the next page");
            yield return WaitSeconds(0.3f);
            yield return new WaitForEndOfFrame();
            Save(shotPath + "_skill.png");
            yield return Press(CentreOf("StartButton"), false, "start");
            yield return WaitSeconds(1f);
            if (CharacterCustomizeScreen.OfflineOpen) { CharacterCustomizeScreen.TestSetName("Tester"); yield return Press(CentreOf("Create"), false, "name and create the new hero"); yield return WaitSeconds(1f); } // captures have no saves: every class is new
            HeroController hero = GameSession.Hero;
            E2e((hero != null && hero.Class == HeroClassDef.Get(startClass) && !HeroSelectScreen.IsShown ? "PASS" : "FAIL") + " start spawned the picked hero: " + (hero != null ? hero.Class.Name : "none"));
            yield return new WaitForEndOfFrame();
            Save(shotPath + "_started.png");

            HeroChangeScreen.Open();
            yield return WaitSeconds(0.7f);
            E2e((CharacterListScreen.IsShown ? "PASS" : "FAIL") + " change hero opens the character list over the game");
            yield return new WaitForEndOfFrame();
            Save(shotPath + "_ingame.png");
            yield return Press(CentreOf("EnterGame"), false, "disabled enter on the current hero");
            yield return Press(CentreOf("BackToGame"), false, "back");
            yield return WaitSeconds(0.3f);
            E2e((!CharacterListScreen.IsShown && !HeroChangeScreen.IsOpen && GameSession.Hero == hero ? "PASS" : "FAIL") + " back returns to the same hero");
            Application.Quit();
        }

        static IEnumerator WaitSeconds(float s)
        {
            float until = Time.realtimeSinceStartup + s;
            while (Time.realtimeSinceStartup < until) yield return null;
        }
    }
}
