using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Single per-frame simulation delta. Runs first; returns 0 during hit-stop so gameplay freezes briefly. Hit-stop and the inventory pause
    /// exist offline only (a shared zone cannot freeze for one player); a headless server steps a fixed 1/30 s per frame. Also advances the zone clock.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameClock : MonoBehaviour
    {
        /// <summary>Seconds of simulation to advance this frame (clamped, 0 while hit-stopped).</summary>
        public static float Dt { get; private set; }
        /// <summary>Server sim step: the process is capped at 30 frames per second, every frame is exactly one tick.</summary>
        public const float ServerFixedDt = 1f / 30f;
        /// <summary>True while a menu (inventory) is open: the whole simulation is frozen (Dt = 0).</summary>
        public static bool Paused;
        static float hitStopLeft;

        public static void HitStop(float ms)
        {
            if (!NetMode.UsesLocalTimeBending) return;
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
            if (NetMode.IsHeadless) Dt = ServerFixedDt;
            else if (Paused && NetMode.UsesLocalTimeBending) Dt = 0f;
            else if (hitStopLeft > 0f)
            {
                hitStopLeft -= raw;
                Dt = 0f;
            }
            else
            {
                Dt = raw;
            }
            if (Zone.Main != null) Zone.Main.Advance(Dt);
        }
    }
}
