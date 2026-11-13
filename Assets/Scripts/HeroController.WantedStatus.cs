using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Slow / poison / knockback a Wanted Boss skill can inflict on a hero (docs/wanted-boss-skills-design.md). Reuses the engine-free
    /// MonsterStatus a monster already carries for the exact same rules (refresh to the stronger value, DoT ticking per source) -- only the
    /// carrier changes, so there is one status system, not two. Authority only; HeroController.WantedOverlay.cs shows the result.
    /// </summary>
    public partial class HeroController
    {
        readonly MonsterStatus wantedStatus = new MonsterStatus();
        Action<object, float> wantedDotSink;

        public MonsterStatus WantedStatus { get { return wantedStatus; } }
        /// <summary>Move speed multiplier from an active slow (1 = none); frozen already stops movement in Simulate before this is read.</summary>
        float WantedSpeedFactor { get { return wantedStatus.SpeedFactor; } }
        /// <summary>True while this hero is frozen by a Wanted Boss skill (local / authority: the timer; a remote proxy: the replicated flag).</summary>
        public bool WantedFrozen { get { return proxy ? proxyFrozen : frozenMs > 0f; } }

        void TickWantedStatus(float dtMs) { wantedStatus.Tick(dtMs, wantedDotSink ?? (wantedDotSink = OnWantedDot)); }

        void OnWantedDot(object src, float dmg) { TakeDamage(Mathf.Max(1, Mathf.RoundToInt(dmg))); }

        /// <summary>Called by Monster.WantedSkills.cs when this hero stood inside a boss skill's locked shape at the strike instant. Dash /
        /// invuln dodge it exactly like a plain hit (TakeDamage's own guard); a kill skips the status and the shove. Freeze is its own timer,
        /// not a MonsterStatus entry (OwnerSim.FreezeLeft needs to restore / replay it exactly like dash or invuln); no knockback while frozen.</summary>
        public void TakeWantedSkillHit(Vector2 bossFeet, float bossAtk, WantedSkillDef skill)
        {
            if (dead || God || invuln > 0f || dashLeft > 0f) return;
            TakeDamage(CombatMath.Damage(bossAtk, skill.DamageMult, DefStat));
            if (dead) return;
            if (skill.StatusKind == StatusKind.Freeze) frozenMs = Mathf.Max(frozenMs, skill.StatusMs); // non-stacking: the stronger of remaining / new
            else if (skill.StatusKind != StatusKind.None && skill.StatusMs > 0)
                wantedStatus.Apply(new StatusSpec(skill.StatusKind, skill.StatusValue, skill.StatusMs), null, false, bossAtk);
            if (skill.KnockbackTiles > 0f && frozenMs <= 0f) ApplyWantedKnockback(bossFeet, skill.KnockbackTiles);
        }

        /// <summary>
        /// Online local hero only: self-predicts a Freeze-type skill's hit the instant its telegraph resolves (same server-clock tick
        /// WantedSkillTelegraphFx fires the impact picture), so the hero stops right there instead of sliding until the next correction arrives
        /// a round trip later. Only skips dead / God / an active dash -- those are locally driven and known for certain; invuln is left out on
        /// purpose, because the predicted copy only learns it from the last reconciliation and is often still showing a stale >0 from an unrelated
        /// recent hit (normal melee during the same fight), which would silently swallow a real freeze. OwnerSim.FreezeLeft / RestoreSim correct
        /// a wrong guess either way (a voided hit just un-freezes again next snapshot).
        /// </summary>
        public void PredictFreeze(int ms)
        {
            if (!predicted || dead || God || dashLeft > 0f) return;
            frozenMs = Mathf.Max(frozenMs, ms);
        }

        void ApplyWantedKnockback(Vector2 bossFeet, float tiles)
        {
            Vector2 away = new Vector2(x, y) - bossFeet;
            away = away.sqrMagnitude > 0.01f ? away.normalized : DirVec[facing];
            float dist = tiles * GameConfig.TileUnits;
            bool bx, by;
            Vector2 p = FrostHollowMap.Move(new Vector2(x, y), away.x * dist, away.y * dist, BoxW, BoxH, out bx, out by);
            x = p.x; y = p.y;
        }
    }
}
