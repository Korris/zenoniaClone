using System.Collections;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --net-bot skills: the fight bot of an online client that first spends its skill points through the real InvOp.SkillLearn request path
    /// (the server must be started with --dev-level so there are points to spend), then keeps pressing its learned skills one after the other
    /// while it fights, so every casting rule of the server and every replay path of the other clients is exercised.
    /// </summary>
    public partial class DebugCapture
    {
        int botSkillCursor = -1;

        /// <summary>Learns skills (lowest rank first, so prerequisites open up) until the points are gone or nothing more is learnable.</summary>
        IEnumerator LearnSkillsOnline()
        {
            HeroController hero = GameSession.Hero;
            int sent = 0;
            for (int round = 0; round < 80 && hero != null; round++)
            {
                PlayerState p = hero.Rpg;
                int pick = -1, lowest = int.MaxValue;
                for (int i = 0; i < SkillDef.PerClass; i++)
                {
                    SkillDef d = SkillTable.Get(p.ClassId, i);
                    if (d == null || p.Skills.CanLearn(d, p.Level) != LearnResult.Ok || p.Skills[i] >= 5 || p.Skills[i] >= lowest) continue;
                    lowest = p.Skills[i]; pick = i;
                }
                if (pick < 0) break;
                NetClient.SendInvAction(InvOp.SkillLearn, "", (byte)pick);
                sent++;
                yield return new WaitForSeconds(0.2f);
            }
            Debug.Log("[ZenoniaProto] net bot: sent " + sent + " skill learn requests, ranks " + string.Join(",", hero.Rpg.Skills.ToList()) + ", sp left " + hero.Rpg.SkillPointsFree);
        }

        /// <summary>Next learned skill after the previous one (round robin); -1 when nothing is learned.</summary>
        int NextBotSkill(PlayerState p)
        {
            for (int n = 1; n <= SkillDef.PerClass; n++)
            {
                int i = (botSkillCursor + n) % SkillDef.PerClass;
                if (p.Skills[i] > 0) return i;
            }
            return -1;
        }
    }
}
