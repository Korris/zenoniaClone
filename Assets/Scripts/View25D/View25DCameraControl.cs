using System;
using System.Globalization;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Player camera control on the RO3 2.5D maps (prototype 22/11/2026, user: "làm thử mức 1 và 2 cho tui xoay thử", after RO3's rotatable camera).
    /// Level 1: pitch (from the HORIZONTAL) between <see cref="MinPitch"/> and <see cref="MaxPitch"/>, default = the profile's 58 deg; zoom stays CameraWheelZoom.
    /// Level 2: yaw around the hero within ±<see cref="MaxYaw"/> (Settings "Rotate camera"), optional spring back to 0 (Settings "Camera springs back").
    /// View only: View25D.TiltDeg / YawDeg read this, View25D.Tilt turns every billboard round the world vertical, the sim, server and movement keys stay on
    /// the world axes (online input is 4 key bits; WASD stays map-axis, user 22/11: the touch joystick turns its push by the yaw first (TouchJoystick) and click-to-move steers to the clicked ground point (ClickMove)). Off on 2D maps and on the Hydra prototype
    /// (its profile has no RO3 look): there View25D keeps the profile camera. Input: View25DCameraInput. Test flags (not in release): `--cam-yaw deg`,
    /// `--cam-pitch deg` (from the horizontal).
    /// </summary>
    public static class View25DCameraControl
    {
        public const float MinPitch = 45f, MaxPitch = 70f, MaxYaw = 28f;
        const float SpringTime = 0.35f;
        const string RotateKey = "zenonia.cam.rotate", InvertKey = "zenonia.cam.invert", SpringKey = "zenonia.cam.spring";

        static float pitch = -1f; // degrees from the horizontal; < 0 = the profile's pitch (untouched / reset)
        static float yaw, yawVel;
        static bool loaded, rotateOn = true, invert, springBack;

        /// <summary>The map on screen takes the player camera (2.5D on, RO3 profile: Willowbrook, Frost Hollow, Frostfang Den, Rootburrow floors).</summary>
        public static bool Applies { get { return View25D.Active && View25D.Profile.Ro3Look; } }

        /// <summary>Code tilt (from straight down) for a profile tilt: the player's pitch when set, else the profile's.</summary>
        public static float TiltFor(float profileTilt)
        {
            Load();
            if (pitch < 0f || !Applies) return profileTilt;
            return 90f - pitch;
        }

        /// <summary>Yaw of the camera round the world vertical, degrees (+ = the view turns counter-clockwise on the ground); 0 when rotation is off.</summary>
        public static float Yaw { get { Load(); return rotateOn && Applies ? (Map3D.Active ? yaw : Mathf.Clamp(yaw, -MaxYaw, MaxYaw)) : 0f; } }

        /// <summary>Pitch now (from the horizontal), for the HUD / log.</summary>
        public static float PitchNow { get { return 90f - View25D.TiltDeg; } }

        public static void AddPitch(float deg)
        {
            Load();
            if (deg == 0f) return;
            float now = pitch < 0f ? 90f - View25D.TiltDeg : pitch;
            pitch = Mathf.Clamp(now + deg, Map3D.Active ? Min3DPitch : MinPitch, MaxPitch);
        }

        /// <summary>3D map (Map3D): the camera turns all the way round and may tilt lower (real trunks / rock sides to look at).</summary>
        public const float Min3DPitch = 38f;

        static float WrapOrClamp(float y) { return Map3D.Active ? Mathf.Repeat(y + 180f, 360f) - 180f : Mathf.Clamp(y, -MaxYaw, MaxYaw); }

        public static void AddYaw(float deg)
        {
            Load();
            if (!rotateOn || deg == 0f) return;
            yaw = WrapOrClamp(yaw + deg);
            yawVel = 0f;
        }

        /// <summary>Back to the profile camera (pitch 58, yaw 0). Zoom is CameraWheelZoom's own.</summary>
        public static void Reset() { pitch = -1f; yaw = 0f; yawVel = 0f; }

        /// <summary>Per frame from View25DCameraInput: spring the yaw back to 0 while the player is not turning (when that setting is on).</summary>
        public static void Tick(float dt, bool turning)
        {
            Load();
            if (!springBack || turning || yaw == 0f) return;
            yaw = Mathf.SmoothDamp(yaw, 0f, ref yawVel, SpringTime, Mathf.Infinity, dt);
            if (Mathf.Abs(yaw) < 0.05f) { yaw = 0f; yawVel = 0f; }
        }

        // ---- Settings window switches (PlayerPrefs, per device) ----
        public static bool RotateOn { get { Load(); return rotateOn; } set { Load(); rotateOn = value; if (!value) { yaw = 0f; yawVel = 0f; } Save(RotateKey, value); } }
        public static bool Invert { get { Load(); return invert; } set { Load(); invert = value; Save(InvertKey, value); } }
        public static bool SpringBack { get { Load(); return springBack; } set { Load(); springBack = value; Save(SpringKey, value); } }

        static void Save(string key, bool v)
        {
            try { PlayerPrefs.SetInt(key, v ? 1 : 0); PlayerPrefs.Save(); }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] could not save the camera setting: " + e.Message); } // still applies for this run
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                rotateOn = PlayerPrefs.GetInt(RotateKey, 1) != 0; // on for the test build (user wants to try it)
                invert = PlayerPrefs.GetInt(InvertKey, 0) != 0;
                springBack = PlayerPrefs.GetInt(SpringKey, 0) != 0;
            }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] could not read the camera settings: " + e.Message); }
            if (ReleaseBuild.IsRelease) return;
            string[] a = Environment.GetCommandLineArgs();
            float v;
            if (ArgFloat(a, "--cam-yaw", out v)) { rotateOn = true; springBack = false; yaw = Map3D.Requested ? Mathf.Repeat(v + 180f, 360f) - 180f : Mathf.Clamp(v, -MaxYaw, MaxYaw); } // capture runs: nothing saved
            if (ArgFloat(a, "--cam-pitch", out v)) pitch = Mathf.Clamp(v, Map3D.Requested ? Min3DPitch : MinPitch, MaxPitch);
        }

        static bool ArgFloat(string[] a, string key, out float v)
        {
            v = 0f;
            int i = Array.IndexOf(a, key);
            return i >= 0 && i + 1 < a.Length && float.TryParse(a[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }
    }
}
