using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --skill-ui "t=step;t=step": runs skill screen / magazine steps at t seconds after start (screenshots via --shots at the right moments).
    /// The screen is opened through UiScreens, so the steps drive whichever look is switched on: the old tree window (SkillTreeWindow) or,
    /// with --ui-new skill, the Training Hall (TrainingHallWindow). Learn / assign go through the real window handlers (Tap*), so the UI path is exercised.
    /// Steps: open | close | level:N | learn:I[:times] | select:I | assign (= assign-mode) | slot:K (flat magazine slot 0-8, 0 = Dash)
    ///        | respec (first = arm / ask, second = confirm) | confirm-respec (the Respec question only) | gold:N | mp (refill HP / MP)
    ///        | cast:I (hero.TryCastSkill) | face:D | page:D (touch magazine, window closed).
    /// Training Hall only: tier:T (plank 0-2) | drag:I:K (statue I dropped on slot K) | move:K:J (slot K dropped on slot J) | unslot:K (slot K dropped off the belt)
    ///        | drag-demo:I:K (statue I held on its way to slot K).
    /// </summary>
    public partial class DebugCapture
    {
        string skillUi;

        IEnumerator RunSkillUi(float start)
        {
            foreach (string item in skillUi.Split(';'))
            {
                string[] ts = item.Split('=');
                float at = float.Parse(ts[0], CultureInfo.InvariantCulture);
                while (Time.realtimeSinceStartup - start < at) yield return null;
                try { SkillUiStep(ts[1].Trim()); }
                catch (Exception e) { Debug.LogError("[ZenoniaProto] --skill-ui step '" + item + "' failed: " + e.Message); }
            }
        }

        static void SkillUiStep(string step)
        {
            string[] p = step.Split(':');
            HeroController hero = GameSession.Hero;
            PlayerState rpg = hero != null ? hero.Rpg : null;
            int a = p.Length > 1 ? int.Parse(p[1], CultureInfo.InvariantCulture) : 0, b = p.Length > 2 ? int.Parse(p[2], CultureInfo.InvariantCulture) : 1;
            bool hall = TrainingHallWindow.IsOpen; // the rebuilt look is showing: same steps, its handlers
            switch (p[0])
            {
                case "open": UiScreens.Open(UiScreen.Skill); break;
                case "close": UiScreens.Close(UiScreen.Skill); break;
                case "level": rpg.Level = a; rpg.RecomputeStats(); break;
                case "gold": rpg.Gold = a; break;
                case "mp": rpg.Mp = rpg.MaxMp; rpg.Hp = rpg.MaxHp; break;
                case "learn":
                    if (hall) { TrainingHallWindow.TapNode(a); for (int i = 0; i < b; i++) TrainingHallWindow.TapLearn(); }
                    else { SkillTreeWindow.TapNode(a); for (int i = 0; i < b; i++) SkillTreeWindow.TapLearn(); }
                    break;
                case "select": if (hall) TrainingHallWindow.TapNode(a); else SkillTreeWindow.TapNode(a); break;
                case "assign": case "assign-mode": if (hall) TrainingHallWindow.TapAssign(); else SkillTreeWindow.TapAssign(); break;
                case "slot": if (hall) TrainingHallWindow.TapSlot(a); else SkillTreeWindow.TapSlot(a); break;
                case "respec": case "confirm-respec": if (hall) TrainingHallWindow.TapRespec(); else SkillTreeWindow.TapRespec(); break;
                case "tier": TrainingHallWindow.TapTier(a); break;
                case "drag": TrainingHallWindow.TestDragStatue(a, b); break;
                case "move": TrainingHallWindow.TestDragSlot(a, b); break;
                case "unslot": TrainingHallWindow.TestDragSlot(a, -1); break;
                case "drag-demo": TrainingHallWindow.TestDragDemo(a, b); break;
                case "cast": Debug.Log("[ZenoniaProto] cast " + a + " => " + hero.TryCastSkill(a)); break;
                case "face": hero.Teleport(hero.Position, a); break;
                case "page": TouchControls.CycleMagazine(a); break;
                default: Debug.LogError("[ZenoniaProto] unknown --skill-ui step " + step); break;
            }
            Debug.Log("[ZenoniaProto] skill-ui " + step + " -> sp " + (rpg != null ? rpg.SkillPointsFree : -1) + " ranks " + (rpg != null ? string.Join(",", rpg.Skills.ToList()) : ""));
        }
    }
}
