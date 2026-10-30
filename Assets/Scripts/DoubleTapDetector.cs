namespace ZenoniaProto
{
    /// <summary>
    /// Pure double-tap logic for the four directions (no Unity calls, so it is unit-testable): a direction going from released to held twice
    /// within <see cref="WindowSeconds"/> is a double-tap. Each direction is tracked on its own, so diagonals count per axis.
    /// </summary>
    public sealed class DoubleTapDetector
    {
        public const float WindowSeconds = 0.25f;
        const int Dirs = 4;
        readonly bool[] last = new bool[Dirs];
        readonly float[] lastTap = new float[Dirs];

        public DoubleTapDetector() { for (int i = 0; i < Dirs; i++) lastTap[i] = float.NegativeInfinity; }

        /// <summary>Feed this frame's held flags (Up, Down, Left, Right) and the time; true when any direction was just double-tapped.</summary>
        public bool Update(bool up, bool down, bool left, bool right, float now)
        {
            bool fired = false;
            fired |= Step(0, up, now);
            fired |= Step(1, down, now);
            fired |= Step(2, left, now);
            fired |= Step(3, right, now);
            return fired;
        }

        bool Step(int i, bool held, float now)
        {
            bool edge = held && !last[i];
            last[i] = held;
            if (!edge) return false;
            if (now - lastTap[i] <= WindowSeconds)
            {
                lastTap[i] = float.NegativeInfinity; // a third tap starts a new pair
                return true;
            }
            lastTap[i] = now;
            return false;
        }
    }
}
