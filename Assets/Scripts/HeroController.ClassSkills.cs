using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Class combo extras that are not tree skills (the tree skills live in Skills/ and HeroController.Skill.cs).</summary>
    public partial class HeroController
    {
        /// <summary>Tiny cosmetic heal pulse (cleric finisher): a few percent of max HP and a green sparkle around the hero.</summary>
        public void HealPulse(float fraction)
        {
            if (dead || hp >= MaxHp) return;
            hp = Mathf.Min(MaxHp, hp + MaxHp * fraction);
            HeroFx.HealSparkle(new Vector2(x, y));
        }
    }
}
