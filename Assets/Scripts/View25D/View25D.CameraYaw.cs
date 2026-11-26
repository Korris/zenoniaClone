using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Player camera yaw / tilt on the 2.5D maps (View25DCameraControl, 22/11/2026): the ground directions the camera looks along, the screen row of an
    /// 8-way facing, and the standing props, which are stood up once at build and turned again whenever the camera rotation changes.
    /// </summary>
    public static partial class View25D
    {
        static readonly List<Transform> Standing = new List<Transform>(1024);
        static Quaternion standingApplied = Quaternion.identity;

        /// <summary>Camera yaw round the world vertical, degrees (0 on 2D maps, on the Hydra prototype and with rotation off).</summary>
        public static float YawDeg { get { return View25DCameraControl.Yaw; } }

        /// <summary>Ground direction that points up the screen (north at yaw 0).</summary>
        public static Vector2 GroundForward { get { float a = YawDeg * Mathf.Deg2Rad; return new Vector2(-Mathf.Sin(a), Mathf.Cos(a)); } }

        /// <summary>Ground direction that points right on the screen (east at yaw 0).</summary>
        public static Vector2 GroundRight { get { float a = YawDeg * Mathf.Deg2Rad; return new Vector2(Mathf.Cos(a), Mathf.Sin(a)); } }

        /// <summary>LookAtGroundOffset along the screen's up direction on the ground (pure +y at yaw 0).</summary>
        public static Vector2 LookAtGroundOffsetVec { get { return GroundForward * LookAtGroundOffset; } }

        /// <summary>Sheet row (Dir8Util order) that shows the world facing `dir8` on screen under the camera yaw: a world direction at angle t is drawn
        /// at t - yaw, and the rows are 45 deg apart counter-clockwise from "down".</summary>
        public static int ScreenDir8(int dir8, float yawDeg)
        {
            if (dir8 < 0 || yawDeg == 0f) return dir8;
            int r = Mathf.RoundToInt(dir8 - yawDeg / 45f);
            return ((r % 8) + 8) % 8;
        }

        /// <summary>Takes a prop off the camera-facing list (View25DDirectionalProps turns its own cards).</summary>
        public static void UnregisterStanding(Transform t) { Standing.Remove(t); }

        static void RegisterStanding(Transform t)
        {
            Standing.Add(t);
            standingApplied = t.rotation;
        }

        /// <summary>Per frame from View25DWorld: turns every standing prop (map props, border forest, dungeon points) to the camera rotation when it changed.</summary>
        public static void TickStanding()
        {
            Quaternion q = Tilt;
            if (q == standingApplied) return;
            standingApplied = q;
            for (int i = Standing.Count - 1; i >= 0; i--)
            {
                Transform t = Standing[i];
                if (t == null) { Standing.RemoveAt(i); continue; } // map rebuilt
                t.rotation = q;
            }
            View25DDirectionalProps.Tick(); // multi-view props: the view of the camera's side on its fixed card
        }

        /// <summary>2.5D off: forget the props (they are rebuilt flat with the next map).</summary>
        public static void ClearStanding() { Standing.Clear(); standingApplied = Quaternion.identity; View25DDirectionalProps.Clear(); }
    }
}
