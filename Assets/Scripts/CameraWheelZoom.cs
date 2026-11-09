using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZenoniaProto
{
    /// <summary>
    /// Mouse-wheel zoom of the gameplay camera on the PC build (Windows standalone and the editor): wheel up zooms in, wheel down out,
    /// 8 % per notch, eased over 0.15 s, 0.6x..1x of the default half-height (zoom in only), remembered in PlayerPrefs (zenonia.zoom).
    /// Never on WebGL / mobile, and never in a capture / bot run (--zoom) or when dev-start.json sets cameraViewHeight: those own the size.
    /// Ignored while a window owns the pointer / keyboard (chat typing, modal windows, inventory, hero change, account menu, pointer over UI).
    /// Writes the size in Update; CameraFollow adopts it as its base in LateUpdate (punch-in scales it, the map clamp reads it).
    /// </summary>
    public class CameraWheelZoom : MonoBehaviour
    {
        const string PrefKey = "zenonia.zoom";
        const float MinFactor = 0.6f, MaxFactor = 1f, // never wider than the default view (user, 2026-11-05)
             NotchStep = 0.08f, EaseSeconds = 0.15f;

        /// <summary>Zoom multiplier of the default half-height (1 = default). CameraFollow.Configure applies it, so a map change keeps the zoom.</summary>
        public static float Current { get; private set; } = 1f;

        Camera cam;
        float target = 1f, velocity;
        float minimum = MinFactor;
        bool savePreference = true;

        static bool InteractiveTest(string[] args)
        {
            if (Array.IndexOf(args, "--wheel-zoom") < 0) return false;
            foreach (string arg in args)
                if (arg == "--shots" || arg == "--bot" || arg == "--offscreen-capture" || arg == "--net-bot"
                    || arg == "--run-seconds" || arg.StartsWith("--record-") || arg.EndsWith("-test") || arg == "--login-shots")
                    return false;
            return true;
        }

        /// <summary>PC build only, and only when nothing else (capture --zoom, dev-start.json cameraViewHeight) owns the camera size.</summary>
        public static bool Allowed()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            if (HudLayout.IsWeb || Application.isMobilePlatform) return false;
            string[] args = Environment.GetCommandLineArgs();
            if (InteractiveTest(args)) return true;
            if (DebugCapture.Requested(args) || Array.IndexOf(args, "--zoom") >= 0) return false;
            return !DevStartConfig.HasCameraViewHeight();
#else
            return false;
#endif
        }

        /// <summary>Adds the zoom to the gameplay camera when allowed (call before CameraFollow.Configure so the saved zoom frames the first view).</summary>
        public static void Attach(Camera camera)
        {
            if (camera == null || !Allowed() || camera.GetComponent<CameraWheelZoom>() != null) return;
            camera.gameObject.AddComponent<CameraWheelZoom>();
        }

        void Awake()
        {
            cam = GetComponent<Camera>();
            try { target = Mathf.Clamp(PlayerPrefs.GetFloat(PrefKey, 1f), MinFactor, MaxFactor); } catch (Exception) { target = 1f; }
            Current = target;
            string[] args = Environment.GetCommandLineArgs();
            int zoomIndex = Array.IndexOf(args, "--zoom");
            float initial;
            if (InteractiveTest(args) && zoomIndex >= 0 && zoomIndex + 1 < args.Length
                && float.TryParse(args[zoomIndex + 1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out initial) && initial > 0f)
            {
                target = Mathf.Min(MaxFactor, initial / (DevStartConfig.CameraViewHeight() * 0.5f));
                minimum = Mathf.Min(MinFactor, target * MinFactor);
                Current = target;
                savePreference = false; // test close-ups must not alter normal play's saved camera
            }
        }

        void OnDestroy() { Current = 1f; }

        /// <summary>A panel scrolls its own list or owns the keyboard: the wheel is theirs (same gates as InputAdapter / TouchControls).</summary>
        static bool WindowOwnsPointer()
        {
            return GameSession.Hero == null || UiWindows.BlocksGameplay || InventoryPanel.IsOpen || HeroChangeScreen.IsOpen || AccountScreenGui.MenuOpen
                || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
        }

        void Update()
        {
            if (cam == null) return;
            float notches = WindowOwnsPointer() ? 0f : InputAdapter.MouseScrollDelta();
            if (notches != 0f)
            {
                target = Mathf.Clamp(target * Mathf.Pow(1f - NotchStep, notches), minimum, MaxFactor); // wheel up = smaller view = closer
                if (savePreference)
                    try { PlayerPrefs.SetFloat(PrefKey, target); PlayerPrefs.Save(); } catch (Exception) { /* prefs are a convenience */ }
            }
            float defaultHalf = DevStartConfig.CameraViewHeight() * 0.5f;
            float want = Mathf.Min(target, MaxFactorForMap(defaultHalf));
            Current = Mathf.SmoothDamp(Current, want, ref velocity, EaseSeconds, Mathf.Infinity, Time.unscaledDeltaTime); // unscaled: hit-stop / pause
            if (Mathf.Abs(Current - want) < 0.0005f) { Current = want; velocity = 0f; }
            cam.orthographicSize = defaultHalf * Current;
        }

        /// <summary>Zooming out stops where the view would exceed the map (CameraFollow centres a smaller map); the default is always allowed.</summary>
        float MaxFactorForMap(float defaultHalf)
        {
            float fit = Mathf.Min(GameConfig.MapH * 0.5f, GameConfig.MapW * 0.5f / Mathf.Max(0.01f, cam.aspect)) / defaultHalf;
            return Mathf.Max(1f, Mathf.Min(MaxFactor, fit));
        }
    }
}
