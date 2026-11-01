using System.Collections;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// --hero-select-test --shot-path P: the hero select through its real input path (FakeInput). Taps every hero with a shot of each, checks Boxing cannot be picked, pages the skills, starts
    /// the Cleric, opens the in-game change screen and backs out of it. Writes P_open.png, P_warrior.png ... P_ingame.png and PASS / FAIL lines with the E2e tag.
    /// </summary>
    public partial class DebugCapture
    {
        IEnumerator RunHeroSelectTest()
        {
            while (!HeroSelectScreen.IsShown) yield return null;
            yield return WaitSeconds(1f);
            yield return new WaitForEndOfFrame();
            Save(shotPath + "_open.png");
            foreach (HeroClassDef c in HeroClassDef.All)
            {
                yield return Press(CentreOf("Pick" + (int)c.Id), false, "hero " + c.Name);
                for (int i = 0; i < 12; i++)
                {
                    yield return WaitSeconds(0.45f); // a full preview loop: does the gear stay in the frame while it swings?
                    yield return new WaitForEndOfFrame();
                    Save(shotPath + (i == 0 ? "_" + c.Name.ToLowerInvariant() : "_preview_" + c.Name.ToLowerInvariant() + "_" + i.ToString("D2")) + ".png");
                }
            }

            // The Cleric is still picked from the loop above, so a tap on Boxing must leave it picked: start below has to spawn the Cleric.
            yield return Press(CentreOf("PickBoxing"), false, "disabled hero Boxing");
            yield return Press(CentreOf("SkillNext"), true, "next skills");
            yield return Press(CentreOf("Skill4"), true, "last skill");
            yield return WaitSeconds(0.3f);
            yield return new WaitForEndOfFrame();
            Save(shotPath + "_skill.png");
            yield return Press(CentreOf("StartButton"), false, "start");
            yield return WaitSeconds(1f);
            HeroController hero = GameSession.Hero;
            E2e((hero != null && hero.Class == HeroClassDef.Cleric && !HeroSelectScreen.IsShown ? "PASS" : "FAIL") + " start spawned the picked hero: " + (hero != null ? hero.Class.Name : "none"));
            yield return new WaitForEndOfFrame();
            Save(shotPath + "_started.png");

            HeroChangeScreen.Open();
            yield return WaitSeconds(0.7f);
            E2e((HeroSelectScreen.IsShown ? "PASS" : "FAIL") + " change hero screen opens over the game");
            yield return new WaitForEndOfFrame();
            Save(shotPath + "_ingame.png");
            yield return Press(CentreOf("StartButton"), false, "disabled start on the current hero");
            yield return Press(CentreOf("BackButton"), false, "back");
            yield return WaitSeconds(0.3f);
            E2e((!HeroSelectScreen.IsShown && !HeroChangeScreen.IsOpen && GameSession.Hero == hero ? "PASS" : "FAIL") + " back returns to the same hero");
            Application.Quit();
        }

        static IEnumerator WaitSeconds(float s)
        {
            float until = Time.realtimeSinceStartup + s;
            while (Time.realtimeSinceStartup < until) yield return null;
        }
    }
}
