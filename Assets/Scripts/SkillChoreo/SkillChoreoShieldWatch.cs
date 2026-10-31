using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Ends a sustained choreography with what it draws. Every sustained skill ends when the caster falls. A shield also ends with its buff, on the authority only because
    /// BuffState is not replicated: End(true) when soaked away or the caster falls, End(false) when its time runs out. A replaying client plays the shield for its full time.
    /// The shield is the caster's own; allies under the same skill only get the sparkle at cast time.
    /// </summary>
    public sealed class SkillChoreoShieldWatch : MonoBehaviour
    {
        const float ExpireSlackMs = 100f; // the buff vanished with less time than this left: it ran out, it was not broken
        SkillChoreoPlayer player;
        HeroController hero;
        bool watchShield, seen;
        float lastLeftMs;

        public static void Attach(SkillChoreoPlayer player, HeroController hero, bool watchShield)
        {
            SkillChoreoShieldWatch w = player.gameObject.AddComponent<SkillChoreoShieldWatch>();
            w.player = player; w.hero = hero; w.watchShield = watchShield;
        }

        void Update()
        {
            if (hero == null) return; // the player removes itself when the caster is gone
            if (hero.IsDead) { player.End(watchShield); enabled = false; return; }
            if (!watchShield) return;
            BuffState s = HeroBuffs.For(hero);
            if (s.ShieldLeft > 0f) { seen = true; lastLeftMs = s.LeftMs(BuffKind.Shield); return; }
            if (!seen) return;
            player.End(lastLeftMs > ExpireSlackMs);
            enabled = false;
        }
    }
}
