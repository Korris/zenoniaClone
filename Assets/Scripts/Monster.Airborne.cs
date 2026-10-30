using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Earth Break launch (airborne, shadow stays on the ground) and the per-step status tick (statuses themselves: Monster.Status.cs).</summary>
    public partial class Monster
    {
        const float LaunchMs = 1500f, LaunchPeakUnits = 34f;
        float airborneLeft;

        public bool IsAirborne { get { return airborneLeft > 0f; } }
        public bool IsBoss { get { return def.IsBoss; } }

        /// <summary>Earth Break: throws the monster into the air for 1.5 s (AI paused). The boss is immune. Returns true when launched.</summary>
        public bool Launch()
        {
            if (IsDying || def.IsBoss) return false;
            airborneLeft = LaunchMs;
            knockLeft = 0f;
            return true;
        }

        /// <summary>Vertical lift of the sprite (parabola over the launch).</summary>
        float AirLift()
        {
            if (airborneLeft <= 0f) return 0f;
            float u = 1f - airborneLeft / LaunchMs;
            return LaunchPeakUnits * 4f * u * (1f - u);
        }

        /// <summary>
        /// Advances statuses (damage over time ticks here) and the launch timer; returns true while the AI must not run: dying, airborne, frozen or
        /// stunned. A windup interrupted by freeze / stun is cancelled into the cooldown, so the strike never lands while frozen.
        /// </summary>
        bool TickStatus(float dtMs)
        {
            status.Tick(dtMs, dotSink ?? (dotSink = OnDot));
            if (state == State.Dying) return true;
            if (airborneLeft > 0f)
            {
                airborneLeft -= dtMs;
                if (airborneLeft <= 0f)
                {
                    airborneLeft = 0f;
                    Enter(State.Cooldown, 350f); // lands, shakes it off, resumes the chase
                    UltimateFx.Dust(new Vector2(x, y), 5, 6f);
                }
                return true;
            }
            if (!status.Disabled) return false;
            if (state == State.Windup) Enter(State.Cooldown, def.CooldownMs);
            return true;
        }
    }
}
