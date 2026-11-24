using System;
using System.Globalization;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Render scale of the world camera (22/11 perf pass 2; the game is fill-bound: 640x360 ran 3x the FPS of 1280x720 on the 3D map).
    /// The gameplay camera draws the world (2D, 2.5D and --map3d maps alike) into a smaller texture and RenderScaleCamera stretches it to the
    /// screen with a bilinear + light sharpen filter; the HUD (screen-space overlay canvas) and the IMGUI text (names, damage numbers, FPS) are
    /// drawn afterwards at the native resolution. World-space sprite labels (monster foot labels) are part of the world and follow the scale.
    /// Settings > "Resolution": Auto or a fixed 100 / 85 / 70 %; that value is the ceiling. Auto = 100 % native on every graphics level
    /// (ChatGPT "Visual Sharpness V1.0", 24/11/2026, docs/reference/ro3-compare/ro3-sharpness-analysis-chatgpt.txt section V / IX: the old Auto
    /// Medium 85 % + dynamic resolution down to 60 % was the soft "60% auto" look; PC renders native, phones aim at 100 % and only drop when needed).
    /// Settings > "Dynamic resolution" (default OFF on desktop, on on phones = the "Mobile Balanced" profile): when the frame rate falls under
    /// <see cref="TargetFps"/> the scale steps down 5 % at a time to <see cref="Floor"/> and climbs back when there is headroom (RenderScaleCamera,
    /// hysteresis + back-off so it does not flicker).
    /// Test builds: `--render-scale 0.85` fixes the ceiling for this run, `--dynres off|on` the dynamic switch (FPS runs), neither is saved.
    /// </summary>
    public static class RenderScale
    {
        public const float Floor = 0.6f, Step = 0.05f;
        /// <summary>Settings tabs: 0 = Auto (by quality), then fixed scales.</summary>
        public static readonly float[] Steps = { 0f, 1f, 0.85f, 0.7f };
        public static readonly string[] Labels = { "Auto", "100%", "85%", "70%" };

        const string StepKey = "zenonia.gfx.renderScale.v2", DynKey = "zenonia.gfx.dynamicRes.v2"; // v2 (24/11 Sharpness V1.0): back to Auto = native and dynamic off once (old saved 70 % / dynamic on were the blur)
        static bool loaded;
        static int step;
        static bool dynamicOn;
        static float forcedCeiling = -1f;
        static int forcedDyn = -1;

        /// <summary>Raised when the ceiling or the dynamic switch changed (Settings).</summary>
        public static event Action Changed;

        /// <summary>The scale the camera renders at this frame (set by RenderScaleCamera; dynamic resolution moves it between Floor and Ceiling).</summary>
        public static float Current = 1f;

        /// <summary>Frame rate dynamic resolution keeps: 55 on desktop (vsync 60), 30 on phones.</summary>
        public static float TargetFps { get { return Application.isMobilePlatform ? 30f : 55f; } }

        /// <summary>Scale of the graphics level when the Resolution tab is Auto: native on every level (Sharpness V1.0 preset; lower it by hand in Settings,
        /// or let dynamic resolution drop it when the frame rate needs it).</summary>
        public static float ForQuality(GraphicsQuality.Level l) { return 1f; }

        /// <summary>Dynamic resolution when the player never chose: off on desktop (PC High Quality = 100 % native), on on phones (Mobile Balanced).</summary>
        static bool DefaultDynamic { get { return Application.isMobilePlatform; } }

        public static int StepIndex { get { Load(); return step; } }

        public static bool Dynamic { get { Load(); return forcedDyn >= 0 ? forcedDyn == 1 : dynamicOn; } }

        /// <summary>Highest scale allowed now (the Settings choice; dynamic resolution never goes above it).</summary>
        public static float Ceiling
        {
            get
            {
                Load();
                if (forcedCeiling > 0f) return forcedCeiling;
                return step == 0 ? ForQuality(GraphicsQuality.Current) : Steps[step];
            }
        }

        public static void SetStep(int i)
        {
            Load();
            i = Mathf.Clamp(i, 0, Steps.Length - 1);
            if (i == step) return;
            step = i;
            Save(StepKey, i);
            if (Changed != null) Changed();
        }

        public static void SetDynamic(bool on)
        {
            Load();
            forcedDyn = -1; // the player's own choice wins over the test flag from now on
            if (on == dynamicOn) return;
            dynamicOn = on;
            Save(DynKey, on ? 1 : 0);
            if (Changed != null) Changed();
        }

        static void Save(string key, int v)
        {
            try { PlayerPrefs.SetInt(key, v); PlayerPrefs.Save(); }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] could not save the render scale setting: " + e.Message); } // still applies for this run
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                step = Mathf.Clamp(PlayerPrefs.GetInt(StepKey, 0), 0, Steps.Length - 1);
                dynamicOn = PlayerPrefs.GetInt(DynKey, DefaultDynamic ? 1 : 0) != 0;
            }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] could not read the render scale setting: " + e.Message); }
            if (ReleaseBuild.IsRelease) return;
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "--render-scale");
            float f;
            if (i >= 0 && i + 1 < a.Length && float.TryParse(a[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out f)) forcedCeiling = Mathf.Clamp(f, 0.3f, 1f);
            i = Array.IndexOf(a, "--dynres");
            if (i >= 0 && i + 1 < a.Length) forcedDyn = a[i + 1] == "off" ? 0 : 1;
        }
    }
}
