using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --skill-ui "t=step;t=step": runs skill tree / magazine steps at t seconds after start (screenshots via --shots at the right moments).
    /// Steps: open | close | level:N | learn:I[:times] | select:I | assign | slot:K (flat magazine slot 0-8, 0 = Dash) | page:D (touch magazine, window closed)
    ///        | respec | gold:N | mp (refill HP / MP) | cast:I (hero.TryCastSkill) | face:D. Learn / assign go through the real window handlers (SkillTreeWindow.Tap*), so the UI path is exercised.
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
            Transform root = GameObject.Find("ZenoniaProto").transform;
            int a = p.Length > 1 ? int.Parse(p[1], CultureInfo.InvariantCulture) : 0, b = p.Length > 2 ? int.Parse(p[2], CultureInfo.InvariantCulture) : 1;
            switch (p[0])
            {
                case "open": SkillTreeWindow.Open(root); break;
                case "close": SkillTreeWindow.Close(); break;
                case "level": rpg.Level = a; rpg.RecomputeStats(); break;
                case "gold": rpg.Gold = a; break;
                case "mp": rpg.Mp = rpg.MaxMp; rpg.Hp = rpg.MaxHp; break;
                case "learn": SkillTreeWindow.TapNode(a); for (int i = 0; i < b; i++) SkillTreeWindow.TapLearn(); break;
                case "select": SkillTreeWindow.TapNode(a); break;
                case "assign": SkillTreeWindow.TapAssign(); break;
                case "slot": SkillTreeWindow.TapSlot(a); break;
                case "respec": SkillTreeWindow.TapRespec(); break;
                case "cast": Debug.Log("[ZenoniaProto] cast " + a + " => " + hero.TryCastSkill(a)); break;
                case "face": hero.Teleport(hero.Position, a); break;
                case "page": TouchControls.CycleMagazine(a); break;
                default: Debug.LogError("[ZenoniaProto] unknown --skill-ui step " + step); break;
            }
            Debug.Log("[ZenoniaProto] skill-ui " + step + " -> sp " + (rpg != null ? rpg.SkillPointsFree : -1) + " ranks " + (rpg != null ? string.Join(",", rpg.Skills.ToList()) : ""));
        }
    }
}
