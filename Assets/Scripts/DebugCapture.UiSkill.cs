using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the rebuilt skill screen, the Training Hall (TrainingHallWindow; phase 06 of plans/261116-1206-adventure-paper-ui-rebuild).
    /// Use with --ui-new skill --ui-open skill (and --class, --level N).
    ///   --hall-steps "learn:0:5,select:1,assign"   run these --skill-ui steps (DebugCapture.Skills.cs) at once, in order, right after the
    ///                                              screen opened: a state for one screenshot without timing. They work on the old
    ///                                              window too when the switch is off (the steps the old window has).
    ///   --hall-gold N                              the hero's gold (the Respec price in red when it is not enough)
    ///   --hall-fresh                               every rank and the magazine cleared first (dev-start.json starts a test hero with every skill at rank 10)
    ///   --hall-no-dev                              the same, and the real rules for this run: no bonus skill points, level and prerequisite locks on
    ///                                              (dev-start.json: skillPointBonus, allSkills). Then the first skill is learned, like a new hero.
    /// Timed scripts: --skill-ui "1=open;2=learn:0:3;3=assign;4=slot:2" drives whichever look is switched on.
    /// Keys: --ui-keys "1=SkillTree;2=Right;3=Down;4=Close" (DebugCapture.UiRedesign.cs) presses the real keys.
    /// </summary>
    public partial class DebugCapture
    {
        partial void DebugUiSkill()
        {
            string gold = Val("--hall-gold");
            if (gold != null && RpgSession.Player != null) RpgSession.Player.Gold = int.Parse(gold);
            ZenoniaProto.Rpg.PlayerState p = RpgSession.Player;
            bool noDev = Flag("--hall-no-dev");
            if (p != null && (noDev || Flag("--hall-fresh")))
            {
                if (noDev) { ZenoniaProto.Rpg.SkillRanks.DevBonus = 0; ZenoniaProto.Rpg.SkillRanks.DevUnlockAll = false; }
                p.Skills.Reset();
                p.Loadout.Clear();
                if (noDev) p.LearnSkill(0);
            }
            string steps = Val("--hall-steps");
            if (steps == null) return;
            foreach (string step in steps.Split(','))
            {
                try { SkillUiStep(step.Trim()); }
                catch (System.Exception e) { Debug.LogError("[ZenoniaProto] --hall-steps step '" + step + "' failed: " + e.Message); }
            }
        }
    }
}
