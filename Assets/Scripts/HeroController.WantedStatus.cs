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

        void OnWantedDot(object src, float dmg) { TakeDamage(Mathf.Max(1, Mathf.RoundToInt(dmg)), true); } // a tick, not a hit: no invuln window

        // Client copy of the server's poison (NetEventType.HeroStatus): the server-clock instant it runs out, and when Venom Drain last took it.
        float netPoisonEndMs, netDrainedAtMs = -1e9f;

        /// <summary>Poison time left (ms) for the HUD icon, the party bar and the on-body effect: the authority's own MonsterStatus, or on a
        /// client the replicated end time (the local predicted hero never runs wantedStatus). 0 when dead or clean.</summary>
        public float PoisonMsLeft
        {
            get
            {
                if (dead) return 0f;
                float own = wantedStatus.DotMsLeft(StatusKind.Poison);
                float net = netPoisonEndMs > 0f ? netPoisonEndMs - NetClient.ServerNowMs : 0f;
                return Mathf.Max(own, net, 0f);
            }
        }

        /// <summary>ms since Venom Drain last drained this hero (client: from the event; authority: its own clock), large when never.</summary>
        public float MsSinceDrained { get { return StatusNowMs - netDrainedAtMs; } }

        /// <summary>The clock netDrainedAtMs is kept in: the zone clock on the authority, the server clock on a client, game time offline.</summary>
        static float StatusNowMs
        {
            get
            {
                if (NetMode.IsOffline) return Time.time * 1000f;
                if (NetMode.IsServerSide) return ZoneHost.Active != null ? ZoneHost.Active.ClockMs : 0f;
                return NetClient.ServerNowMs;
            }
        }

        /// <summary>Client: a HeroStatus event (poison applied / refreshed / cured) played at server time `serverMs`.</summary>
        public void ProxyStatus(int kind, uint msLeft, bool drained, double serverMs)
        {
            if (kind != (int)StatusKind.Poison) return;
            netPoisonEndMs = msLeft > 0 ? (float)serverMs + msLeft : 0f;
            if (drained) netDrainedAtMs = (float)serverMs;
        }

        /// <summary>Venom Drain started with this poisoned hero inside its circle: the poison lasts at least `ms` more (authority), and every
        /// client hears the new time so the HUD icon keeps counting until the strike.</summary>
        public void HoldWantedPoison(float ms)
        {
            if (dead || !wantedStatus.Has(StatusKind.Poison)) return;
            wantedStatus.HoldDot(StatusKind.Poison, ms);
            NetEventBus.HeroStatus(this, StatusKind.Poison, wantedStatus.DotMsLeft(StatusKind.Poison), false);
        }

        /// <summary>Called by Monster.WantedSkills.cs when this hero stood inside a boss skill's locked shape at the strike instant. Dash /
        /// invuln dodge it exactly like a plain hit (TakeDamage's own guard); a kill skips the status and the shove. Freeze is its own timer,
        /// not a MonsterStatus entry (OwnerSim.FreezeLeft needs to restore / replay it exactly like dash or invuln); no knockback while frozen.
        /// Returns the HP really lost (Venom Drain heals the boss from it). A draining skill also removes this hero's poison.</summary>
        public float TakeWantedSkillHit(Vector2 bossFeet, float bossAtk, WantedSkillDef skill)
        {
            if (dead || God || invuln > 0f || dashLeft > 0f) return 0f;
            float before = hp;
            TakeDamage(CombatMath.Damage(bossAtk, skill.DamageMult, DefStat));
            float taken = Mathf.Max(0f, before - hp);
            if (skill.DrainPoisoned)
            {
                wantedStatus.ClearDot(StatusKind.Poison);
                netDrainedAtMs = StatusNowMs;
                NetEventBus.HeroStatus(this, StatusKind.Poison, 0f, true);
            }
            if (dead) return taken;
            if (StatusRules.CancelsPendingStab(skill.StatusKind, skill.KnockbackTiles)) stabLeft = 0f; // a real disable drops the committed second stab
            if (skill.StatusKind == StatusKind.Freeze) frozenMs = Mathf.Max(frozenMs, skill.StatusMs); // non-stacking: the stronger of remaining / new
            else if (skill.StatusKind != StatusKind.None && skill.StatusMs > 0)
            {
                wantedStatus.Apply(new StatusSpec(skill.StatusKind, skill.StatusValue, skill.StatusMs), null, false, bossAtk);
                if (skill.StatusKind == StatusKind.Poison) NetEventBus.HeroStatus(this, StatusKind.Poison, wantedStatus.DotMsLeft(StatusKind.Poison), false);
            }
            if (skill.KnockbackTiles > 0f && frozenMs <= 0f) ApplyWantedKnockback(bossFeet, skill.KnockbackTiles);
            return taken;
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
