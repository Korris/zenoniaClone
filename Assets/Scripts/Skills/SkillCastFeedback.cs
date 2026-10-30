using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Why the local player's skill press did nothing: a short toast (throttled), so a dead key never feels broken. Silent for everyone else.</summary>
    public static class SkillCastFeedback
    {
        public enum Reason { NotLearned, NoMp, NoTarget }

        static float lastAt = -10f;

        public static void Refused(HeroController hero, SkillDef def, Reason why)
        {
            if (hero != LocalPlayer.Hero || !NetMode.RendersFx || Time.unscaledTime - lastAt < 0.6f) return;
            lastAt = Time.unscaledTime;
            string text;
            switch (why)
            {
                case Reason.NotLearned: text = def.Name + ": learn it first [T]"; break;
                case Reason.NoMp: text = "Not enough MP"; break;
                default: text = def.Name + ": no target in range"; break;
            }
            RpgHud.Toast(text, new Color(1f, 0.82f, 0.55f));
        }
    }
}
