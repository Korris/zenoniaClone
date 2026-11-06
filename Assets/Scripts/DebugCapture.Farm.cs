using System.Collections;
using UnityEngine;

namespace ZenoniaProto
{
    public partial class DebugCapture
    {
        // --class warrior --farm-hold-check: exercises the actual touch-held -> InputAdapter -> hero path.
        // DebugCapture disables saves, so the test cannot overwrite the player's progress.
        IEnumerator RunFarmHoldCheck(HeroController hero)
        {
            hero.God = true;
            int startCasts = hero.SkillCasts;
            bool attackedBetweenCasts = false;
            TouchControls.SetHeld(GameKey.Skill1, true);
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 6f)
            {
                attackedBetweenCasts |= hero.IsBasicAttacking;
                yield return null;
            }
            TouchControls.SetHeld(GameKey.Skill1, false);
            int heldCasts = hero.SkillCasts - startCasts;
            start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 2f) yield return null;
            bool stopped = hero.SkillCasts == startCasts + heldCasts;
            bool pass = heldCasts >= 2 && heldCasts <= 3 && attackedBetweenCasts && stopped;
            Debug.Log("FARM HOLD CHECK: " + (pass ? "PASS" : "FAIL") + " casts=" + heldCasts + " attackBetween=" + attackedBetweenCasts + " stopped=" + stopped);
            Application.Quit(pass ? 0 : 1);
        }
    }
}
