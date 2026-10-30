using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Offline preview casting for the new tree skills until the hero / netcode integration (skill plan phase 04) lands: a tapped magazine
    /// slot runs the skill executor on the local hero with the skill's cooldown, without spending MP or locking the hero. Online it does nothing
    /// (the server must own skills there). Phase 04 replaces this with HeroController.TryCastSkill.
    /// </summary>
    public static class SkillOfflineCast
    {
        static readonly Dictionary<string, float> readyAt = new Dictionary<string, float>();

        public static void Try(SkillDef def, int index)
        {
            HeroController hero = LocalPlayer.Hero;
            PlayerState p = RpgSession.Player;
            if (def == null || hero == null || p == null || hero.IsDead) return;
            if (p.Skills[index] <= 0) { RpgHud.Toast(def.Name + ": learn it first [T]", new Color(1f, 0.7f, 0.5f)); return; }
            float now = Time.time, ready;
            if (readyAt.TryGetValue(def.Id, out ready) && now < ready)
            {
                RpgHud.Toast(def.Name + ": " + (ready - now).ToString("0.0") + " s", new Color(0.8f, 0.85f, 1f));
                return;
            }
            if (SkillExecutor.Cast(hero, index)) readyAt[def.Id] = now + def.CooldownMs / 1000f;
            else RpgHud.Toast(def.Name + ": no target in range", new Color(1f, 0.85f, 0.5f));
        }

        /// <summary>Seconds of cooldown left for the slot overlay (0 when ready).</summary>
        public static float LeftSeconds(SkillDef def)
        {
            float ready;
            return def != null && readyAt.TryGetValue(def.Id, out ready) ? Mathf.Max(0f, ready - Time.time) : 0f;
        }

        public static void ResetStatics() { readyAt.Clear(); }
    }
}
