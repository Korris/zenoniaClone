using System;
using System.Globalization;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Test-only camera override for 2.5D A/B shots (camera vs RO3, 21/11/2026): `--cam25d pitch,fov,heroPx[,feetY]` replaces the profile's camera on every
    /// 2.5D map. pitch = degrees from the HORIZONTAL (ChatGPT's convention; code tilt = 90 - pitch), fov = vertical FOV, heroPx = hero height on a 720-px-tall
    /// screen (top of hair to feet, no name / HP bar, staff not counted) reached by moving the camera, never by scaling the hero; feetY = hero feet in px from the
    /// top of a 720-px screen (default 389, ChatGPT: 53-55 % of the height). Ignored in release builds. Example: `--cam25d 58,27,69`.
    /// Hero pixel height does not depend on pitch or FOV: billboards face the camera, so on-screen height = units x 720 / (2 x orthoSize x ViewScale).
    /// </summary>
    public static class View25DCameraOverride
    {
        /// <summary>Hero from the top of the hair to the feet, world units at heroScale 1 (Cleric measured in game 21/11: 70 px at 720p with heroScale 1.15,
        /// half-height 108, ViewScale 1.11, feet at y 406 → 70 x 216 x 1.11 / 720 / 1.15 ≈ 20.3). Used only to turn a pixel height into a camera distance.</summary>
        public const float HeroUnits = 20.3f;
        public const float DefaultFeetY = 389f;

        static bool parsed, on;
        static float pitch, fov, heroPx, feetY = DefaultFeetY;

        static void Parse()
        {
            if (parsed) return;
            parsed = true;
            if (ReleaseBuild.IsRelease) return;
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "--cam25d");
            if (i < 0 || i + 1 >= a.Length) return;
            string[] p = a[i + 1].Split(',');
            float v;
            if (p.Length < 3) { Debug.LogWarning("[ZenoniaProto] --cam25d wants pitch,fov,heroPx[,feetY], got " + a[i + 1]); return; }
            if (!F(p[0], out pitch) || !F(p[1], out fov) || !F(p[2], out heroPx)) { Debug.LogWarning("[ZenoniaProto] --cam25d: bad number in " + a[i + 1]); return; }
            if (p.Length > 3 && F(p[3], out v)) feetY = v;
            pitch = Mathf.Clamp(pitch, 20f, 85f);
            fov = Mathf.Clamp(fov, 10f, 60f);
            heroPx = Mathf.Clamp(heroPx, 30f, 200f);
            on = true;
            Debug.Log("[ZenoniaProto] --cam25d pitch " + pitch + " fov " + fov + " heroPx " + heroPx + " feetY " + feetY);
        }

        static bool F(string s, out float v) { return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v); }

        public static bool On { get { Parse(); return on; } }
        /// <summary>Code tilt (from straight down) of the override.</summary>
        public static float TiltDeg { get { Parse(); return 90f - pitch; } }
        public static float FovDeg { get { Parse(); return fov; } }

        /// <summary>Camera distance multiplier that shows the hero heroPx tall on a 720-px screen at the default (unzoomed) camera.</summary>
        public static float ViewScale { get { Parse(); return ViewScaleFor(heroPx); } }

        /// <summary>Look-at height (units above the feet) that puts the feet feetY px from the top: (feetY - 360) px below the screen centre.</summary>
        public static float LookAtHeight { get { Parse(); return LookAtFor(feetY, ViewScale); } }

        /// <summary>Camera distance multiplier that shows the hero `px` tall on a 720-px screen at the default zoom, whatever dev-start heroScale is
        /// (so a test build at heroScale 1.15 and a release build at 1 show the same 69 px; camera loop round 1, ChatGPT item 1).</summary>
        public static float ViewScaleFor(float px) { return HeroUnits * DevStartConfig.HeroScale() * 720f / (GameConfig.ViewHeightUnits * px); }

        /// <summary>Look-at height that puts the feet `feetPx` from the top of a 720-px screen with this ViewScale.</summary>
        public static float LookAtFor(float feetPx, float viewScale) { return (feetPx - 360f) * GameConfig.ViewHeightUnits * viewScale / 720f; }
    }
}
