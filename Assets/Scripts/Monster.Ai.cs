using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Monster brain (web updateMonsterAi): returns the velocity for this step and whether the strike lands now.</summary>
    public partial class Monster
    {
        const float WanderSpeedFactor = 0.5f, ChaseSpeedFactor = 0.5f; // web: wander / chase at half speed, return at full speed

        Vector2 Think(float dtMs, HeroController hero, out bool strike)
        {
            strike = false;
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
                    if (dist <= def.AttackRange) { Enter(State.Windup, def.WindupMs); break; }
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
            }
            if (dir == Vector2.zero) return Vector2.zero;
            FaceTowards(dir);
            return dir * (def.Speed * factor * SpeedScale);
        }

        void Enter(State next, float ms) { state = next; timer = ms; }

        void FaceTowards(Vector2 d)
        {
            if (d == Vector2.zero) return;
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) facing = d.x > 0f ? 3 : 2;
            else facing = d.y > 0f ? 1 : 0;
        }
    }
}
