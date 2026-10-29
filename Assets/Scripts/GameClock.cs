using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Single per-frame simulation delta. Runs first; returns 0 during hit-stop so gameplay freezes briefly.</summary>
    [DefaultExecutionOrder(-1000)]
    public class GameClock : MonoBehaviour
    {
        /// <summary>Seconds of simulation to advance this frame (clamped, 0 while hit-stopped).</summary>
        public static float Dt { get; private set; }
        static float hitStopLeft;

        public static void HitStop(float ms)
        {
            hitStopLeft = Mathf.Max(hitStopLeft, ms / 1000f);
        }

        public static void ResetState()
        {
            hitStopLeft = 0f;
            Dt = 0f;
        }

        void Update()
        {
            float raw = Mathf.Min(Time.deltaTime, 0.05f); // never simulate a giant step after a hitch
            if (hitStopLeft > 0f)
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
