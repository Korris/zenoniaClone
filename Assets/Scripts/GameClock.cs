using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Single per-frame simulation delta. Runs first; returns 0 during hit-stop so gameplay freezes briefly.</summary>
    [DefaultExecutionOrder(-1000)]
    public class GameClock : MonoBehaviour
    {
        /// <summary>Seconds of simulation to advance this frame (clamped, 0 while hit-stopped).</summary>
        public static float Dt { get; private set; }
        /// <summary>True while a menu (inventory) is open: the whole simulation is frozen (Dt = 0).</summary>
        public static bool Paused;
        static float hitStopLeft;

        public static void HitStop(float ms)
        {
            hitStopLeft = Mathf.Max(hitStopLeft, ms / 1000f);
        }

        public static void ResetState()
        {
            hitStopLeft = 0f;
            Dt = 0f;
            Paused = false;
        }

        void Update()
        {
            float raw = Mathf.Min(Time.deltaTime, 0.05f); // never simulate a giant step after a hitch
            if (Paused) Dt = 0f;
            else if (hitStopLeft > 0f)
            {
                hitStopLeft -= raw;
                Dt = 0f;
            }
            else
            {
                Dt = raw;
            }
        }
    }
}
