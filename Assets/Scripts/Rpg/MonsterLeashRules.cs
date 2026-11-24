namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Leash rules of monsters, engine-free so they are unit-tested (Monster.Ai.cs feeds them).
    /// Bug 2026-11-24 ("đứng chém không trúng quái, sau vài hit mới trúng"): Idle / Wander had no leash, so a wandering monster drifted past its
    /// leash; the moment a hero woke it, Chase -> Return (home too far) -> Chase (hero inside 0.6 x aggro) flipped every tick. The monster froze,
    /// never struck, and every tick spent in Return dropped the hero's hit (Taurus rule: a monster walking home takes no damage). Two fixes:
    /// wanderers head home before they reach the leash, and a Taurus monster that decided to go home goes home (no re-aggro halfway).
    /// </summary>
    public static class MonsterLeashRules
    {
        /// <summary>A wandering monster further from home than this share of its leash turns back instead of wandering on.</summary>
        public const float WanderHomeFraction = 0.6f;

        /// <summary>Re-aggro share of the aggro range while walking home (the pre-Taurus rule).</summary>
        public const float ReturnAggroFraction = 0.6f;

        public static bool WanderTowardHome(float homeDist, float leash) { return homeDist > leash * WanderHomeFraction; }

        /// <summary>True when a monster walking home turns round to chase a hero at `heroDist`. Never for a reset-on-return (Taurus) monster:
        /// it is immune on the way and at full HP on arrival, so turning back would only freeze it at the leash edge, hits dropped.</summary>
        public static bool ReaggroOnReturn(bool resetOnReturn, float heroDist, float aggro)
        {
            return !resetOnReturn && heroDist < aggro * ReturnAggroFraction;
        }
    }
}
