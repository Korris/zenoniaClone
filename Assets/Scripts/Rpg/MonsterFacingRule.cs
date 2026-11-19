using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Which of the 4 directions a monster faces (0 down, 1 up, 2 left, 3 right), engine-free so the hysteresis is unit-tested.
    /// The old rule (plain axis dominance |dx| &gt; |dy|) flipped every tick for a target near the 45 degree diagonal or standing on the monster's
    /// feet (tiny vector), which made the 2x-drawn Mire Hydra Queen spin in a 40-player crowd (user report 2026-11-19). Sticky keeps the current
    /// facing unless the new direction leaves the current quadrant by more than SwitchMarginDeg, and ignores vectors shorter than MinVectorUnits.
    /// </summary>
    public static class MonsterFacingRule
    {
        /// <summary>Vectors shorter than this (world units) keep the current facing: a hero inside the boss's body has no meaningful direction.</summary>
        public const float MinVectorUnits = 4f;
        /// <summary>Extra degrees past the 45 degree quadrant edge a direction must reach before the facing turns.</summary>
        public const float SwitchMarginDeg = 18f;
        /// <summary>Wanted bosses: after a turn the facing holds this long (a forced turn, e.g. a cone skill aiming, ignores it).</summary>
        public const float WantedHoldMs = 400f;

        /// <summary>The legacy axis-dominance pick (still used to choose the new facing once the current one is left).</summary>
        public static int Plain(float dx, float dy)
        {
            if (Math.Abs(dx) > Math.Abs(dy)) return dx > 0f ? 3 : 2;
            return dy > 0f ? 1 : 0;
        }

        /// <summary>Centre angle (degrees, atan2 convention: 0 = right, 90 = up) of a facing.</summary>
        public static float CenterDeg(int facing)
        {
            switch (facing & 3) { case 0: return -90f; case 1: return 90f; case 2: return 180f; default: return 0f; }
        }

        /// <summary>The facing for direction (dx, dy) given the current one, with the dead zone and the switch margin.</summary>
        public static int Sticky(int current, float dx, float dy, float minLen = MinVectorUnits, float marginDeg = SwitchMarginDeg)
        {
            if (dx * dx + dy * dy < minLen * minLen) return current;
            float ang = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
            float d = (ang - CenterDeg(current)) % 360f;
            if (d < -180f) d += 360f; else if (d > 180f) d -= 360f;
            if (Math.Abs(d) <= 45f + marginDeg) return current;
            return Plain(dx, dy);
        }
    }
}
