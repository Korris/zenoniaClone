using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Ultimate status effects: Earth Break launch (airborne, shadow stays on the ground) and Ice Storm chill (slow + blue tint).</summary>
    public partial class Monster
    {
        const float LaunchMs = 1500f, LaunchPeakUnits = 34f, ChillSpeedFactor = 0.5f;
        float airborneLeft, chillLeft;

        public bool IsAirborne { get { return airborneLeft > 0f; } }
        public bool IsBoss { get { return def.IsBoss; } }
        float SpeedScale { get { return chillLeft > 0f ? ChillSpeedFactor : 1f; } }
        Color ChillTint { get { return chillLeft > 0f ? new Color(0.68f, 0.86f, 1f) : Color.white; } }

        /// <summary>Earth Break: throws the monster into the air for 1.5 s (AI paused). The boss is immune. Returns true when launched.</summary>
        public bool Launch()
        {
            if (IsDying || def.IsBoss) return false;
            airborneLeft = LaunchMs;
            knockLeft = 0f;
            return true;
        }

        /// <summary>Ice Storm: slows movement and tints the sprite blue for the given time.</summary>
        public void Chill(float ms) { if (!IsDying) chillLeft = Mathf.Max(chillLeft, ms); }

        /// <summary>Vertical lift of the sprite (parabola over the launch).</summary>
        float AirLift()
        {
            if (airborneLeft <= 0f) return 0f;
            float u = 1f - airborneLeft / LaunchMs;
            return LaunchPeakUnits * 4f * u * (1f - u);
        }

        /// <summary>Advances the timers; returns true while the monster is airborne (the AI must not run).</summary>
        bool TickStatus(float dtMs)
        {
            chillLeft = Mathf.Max(0f, chillLeft - dtMs);
            if (airborneLeft <= 0f) return false;
            airborneLeft -= dtMs;
            if (airborneLeft <= 0f)
            {
                airborneLeft = 0f;
                Enter(State.Cooldown, 350f); // lands, shakes it off, resumes the chase
                UltimateFx.Dust(new Vector2(x, y), 5, 6f);
            }
            return true;
        }
    }
}
