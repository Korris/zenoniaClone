using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Monster brain (web updateMonsterAi): returns the velocity for this step and whether the strike lands now.</summary>
    public partial class Monster
    {
        const float WanderSpeedFactor = 0.5f, ChaseSpeedFactor = 0.5f; // web: wander / chase at half speed, return at full speed
        /// <summary>Dormancy: a monster only thinks while a living hero is this close (a bit over the camera diagonal), so empty map parts cost nothing.</summary>
        public const float ActiveRadius = 320f;

        /// <summary>
        /// Picks who to fight. The current target is sticky while it lives, is still within ActiveRadius and the monster is mid-fight (chase /
        /// windup / cooldown); a monster that gave up (idle, wander, return) takes the nearest living hero in range. Null = dormant.
        /// </summary>
        HeroController AcquireTarget()
        {
            Vector2 here = new Vector2(x, y);
            // A wanted skill (Telegraph / Execute) keeps its target too: re-picking the nearest of 40 jittering heroes every tick flipped the boss.
            bool fighting = state == State.Chase || state == State.Windup || state == State.Cooldown
                || (!BossFacingLog.Legacy && (state == State.Telegraph || state == State.Execute));
            if (target != null && fighting && !target.IsDead && target.HomeZone == homeZone && (target.Position - here).sqrMagnitude <= ActiveRadius * ActiveRadius)
            {
                // A wanted boss chasing someone out of reach while another hero stands in reach fights the one in reach instead of walking off
                // (walking after a far ranged hero can pull the stationary boss past its leash: Return turns it home, Chase turns it back).
                if (def.IsWanted && !BossFacingLog.Legacy && state == State.Chase && (target.Position - here).sqrMagnitude > def.AttackRange * def.AttackRange)
                {
                    HeroController near = Zone.Main != null ? Zone.Main.Heroes.NearestAlive(here, def.AttackRange) : null;
                    if (near != null && near != target) { target = near; if (BossFacingLog.On) facingLog.Retargets++; }
                }
                return target; // a hero who took a portal is not on this map any more
            }
            Zone zone = Zone.Main;
            HeroController picked = zone != null ? zone.Heroes.NearestAlive(here, ActiveRadius) : null;
            if (BossFacingLog.On && def.IsWanted && picked != target) facingLog.Retargets++;
            target = picked;
            return target;
        }

        Vector2 Think(float dtMs, HeroController hero, out bool strike)
        {
            strike = false;
            if (def.IsWanted)
            {
                TickWantedSkills(dtMs);
                facingHoldMs = Mathf.Max(0f, facingHoldMs - dtMs);
                if (BossFacingLog.On) TickFacingLog(dtMs);
            }
            Vector2 here = new Vector2(x, y);
            Vector2 toHero = hero.Position - here;
            float dist = toHero.magnitude;
            Vector2 toHome = home - here;
            float homeDist = toHome.magnitude;
            timer -= dtMs;
            Vector2 dir = Vector2.zero;
            float factor = 1f;

            switch (state)
            {
                case State.Idle:
                    if (dist < def.AggroRange) { Enter(State.Chase, 0f); break; }
                    if (def.Stationary || timer > 0f) break;
                    float angle = Random.value * Mathf.PI * 2f;
                    wanderDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Enter(State.Wander, 500f + Random.value * 900f);
                    break;
                case State.Wander:
                    if (dist < def.AggroRange) { Enter(State.Chase, 0f); break; }
                    if (timer <= 0f) { Enter(State.Idle, 600f + Random.value * 1200f); break; }
                    dir = wanderDir; factor = WanderSpeedFactor;
                    break;
                case State.Chase:
                    if (homeDist > def.LeashRange) { Enter(State.Return, 0f); break; }
                    if (def.IsWanted && TryBeginWantedSkill()) break; // telegraph started: stands still until it lands
                    if (def.Ranged != null ? CanShoot(hero) : dist <= def.AttackRange) { Enter(State.Windup, def.WindupMs); break; } // ranged (Monster.Ranged.cs): stops at its range with a clear line
                    if (dist > 0.001f) dir = toHero / dist;
                    factor = ChaseSpeedFactor;
                    break;
                case State.Windup:
                    FaceTowards(toHero); // telegraph: stand still facing the hero, strike when the timer runs out
                    if (timer <= 0f) { strike = true; Enter(State.Cooldown, def.CooldownMs); }
                    break;
                case State.Cooldown:
                    if (timer <= 0f) Enter(State.Chase, 0f);
                    break;
                case State.Return:
                    if (homeDist < 6f) { Enter(State.Idle, 800f); break; }
                    if (dist < def.AggroRange * 0.6f) { Enter(State.Chase, 0f); break; }
                    dir = toHome / homeDist;
                    break;
                // Wanted Boss skill (NORMAL/Chase above -> Telegraph -> Execute -> back to Chase; Monster.WantedSkills.cs): the boss stands still
                // through both (dir stays zero) -- the shape and its strike instant were locked when the telegraph began.
                case State.Telegraph:
                    if (timer <= 0f) { FireWantedSkill(); Enter(State.Execute, ExecuteMs); }
                    break;
                case State.Execute:
                    if (timer <= 0f) Enter(State.Chase, 0f);
                    break;
            }
            if (dir == Vector2.zero) return Vector2.zero;
            FaceTowards(dir);
            return dir * (def.Speed * factor * SpeedScale);
        }

        void Enter(State next, float ms)
        {
            if (next == State.Return && state != State.Return && BossFacingLog.On && def.IsWanted) facingLog.Returns++;
            state = next; timer = ms;
        }

        float facingHoldMs; // wanted bosses: time left before the facing may turn again (MonsterFacingRule.WantedHoldMs)

        /// <summary>Turns toward `d` with hysteresis (Rpg/MonsterFacingRule.cs): tiny vectors and near-diagonal jitter keep the current facing, and
        /// a wanted boss holds a new facing for a moment. `force` (a cone skill locking its aim) turns at once by plain axis dominance.</summary>
        void FaceTowards(Vector2 d, bool force = false)
        {
            if (d == Vector2.zero) return;
            int next;
            if (BossFacingLog.Legacy || force) next = MonsterFacingRule.Plain(d.x, d.y);
            else if (def.IsWanted && facingHoldMs > 0f) return;
            else next = MonsterFacingRule.Sticky(facing, d.x, d.y);
            if (next == facing) return;
            facing = next;
            if (!def.IsWanted) return;
            facingHoldMs = MonsterFacingRule.WantedHoldMs;
            if (BossFacingLog.On) facingLog.Turns++;
        }

        /// <summary>Gentle push so monsters do not stack on one pixel.</summary>
        Vector2 Separation()
        {
            Vector2 push = Vector2.zero;
            IReadOnlyList<Monster> all = All;
            for (int i = 0; i < all.Count; i++)
            {
                Monster o = all[i];
                if (o == this || o.IsDying) continue;
                Vector2 d = new Vector2(x - o.x, y - o.y);
                float m = d.magnitude;
                if (m < 10f && m > 0.001f) push += d / m * 30f;
            }
            return push;
        }
    }
}
