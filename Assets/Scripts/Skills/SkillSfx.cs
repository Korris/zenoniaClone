using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Skill sounds by convention: Resources/Audio/sfx/skills/&lt;class&gt;/&lt;skill&gt;-{cast,impact,loop}. A skill without a clip is silent (no warning),
    /// the same key never retriggers within 70 ms (multi-hit strikes and rain would otherwise stack into noise).
    /// </summary>
    public static class SkillSfx
    {
        public const string Cast = "cast", Impact = "impact", Loop = "loop";
        const float MinGapSeconds = 0.07f;
        static readonly Dictionary<string, bool> Exists = new Dictionary<string, bool>();
        static readonly Dictionary<string, float> LastAt = new Dictionary<string, float>();

        public static void Play(SkillContext ctx, string kind, float volume = 0.9f)
        {
            if (ctx == null || !ctx.View || !ctx.Body.IsLocal && !NearListener(ctx)) return;
            string path = "skills/" + ctx.Def.Class + "/" + ctx.Def.Id.Substring(ctx.Def.Class.Length + 1) + "-" + kind;
            bool ok;
            if (!Exists.TryGetValue(path, out ok)) Exists[path] = ok = Resources.Load<AudioClip>("Audio/sfx/" + path) != null;
            if (!ok) return;
            float last;
            if (LastAt.TryGetValue(path, out last) && Time.unscaledTime - last < MinGapSeconds) return;
            LastAt[path] = Time.unscaledTime;
            GameSfx.Play(path, volume);
        }

        /// <summary>Other players' skills are heard only when they happen on screen-ish distance of the local hero.</summary>
        static bool NearListener(SkillContext ctx)
        {
            HeroController me = LocalPlayer.Hero;
            return me == null || (me.Position - ctx.Feet).sqrMagnitude < 200f * 200f;
        }
    }
}
