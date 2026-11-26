using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZenoniaProto
{
    /// <summary>
    /// Mouse-wheel zoom of the gameplay camera on the PC build (Windows standalone and the editor): wheel up zooms in, wheel down out,
    /// 8 % per notch, eased over 0.15 s, 0.6x..1x of the default half-height (zoom in only; RO3 2.5D maps 0.86x..1.15x), remembered in PlayerPrefs (zenonia.zoom.v2).
    /// Never on WebGL / mobile, and never in a capture / bot run (--zoom) or when dev-start.json sets cameraViewHeight: those own the size.
    /// Ignored while a window owns the pointer / keyboard (chat typing, modal windows, inventory, hero change, account menu, pointer over UI).
    /// Writes the size in Update; CameraFollow adopts it as its base in LateUpdate (punch-in scales it, the map clamp reads it).
    /// </summary>
    public class CameraWheelZoom : MonoBehaviour
    {
        const string PrefKey = "zenonia.zoom.v2"; // v2 with the 1.35x wider default (GameConfig.WorldZoomOut): an old saved zoom-in must not hide the new view
        const float MinFactor = 0.6f, MaxFactor = 1f, // never wider than the default view (user, 2026-11-05)
             NotchStep = 0.08f, EaseSeconds = 0.15f;
        /// <summary>RO3 2.5D maps (camera loop, ChatGPT round 2: 85..55 px narrowed to 80..60): hero 80 px closest .. 60 px widest around the default 69 px,
        /// by camera distance only (pitch / FOV fixed). The only place the view may be wider than the default: the forest border (View25DGroundApron) fills it.</summary>
        const float Ro3MinFactor = 69f / 80f, Ro3MaxFactor = 1f; // widest = the default view (user 25/11: "lock zoom out, đang cho zoom out nhiều quá"; was 69/60)
        /// <summary>--map3d (22/11 user: "scroll ko thay zoom"): the 60..80 px band read as no zoom on a real 3D map, so hero 115 px closest there; widest
        /// 1.22x the default (RO3 loop round 1, ChatGPT: "giới hạn để hero còn >= 50-52 px tại 720p": field 65 -> 53 px, village 62 -> 51 px; was 48 px).</summary>
        const float Map3DMinFactor = 69f / 115f, Map3DMaxFactor = 1f; // widest = the default view (user 25/11, was 1.22)
        static bool Ro3Map { get { return View25D.Active && View25D.Profile.Ro3Look; } }
        static float RangeMin(float flat) { return Map3D.Active ? Map3DMinFactor : Ro3Map ? Ro3MinFactor : flat; }
        static float RangeMax(float flat) { return Map3D.Active ? Map3DMaxFactor : Ro3Map ? Ro3MaxFactor : flat; }

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
#if UNITY_STANDALONE || UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
            // No HudLayout.IsWeb check: the desktop exe uses the "web" HUD, so that check switched the wheel off on PC (user 22/11: "scroll ko thay zoom").
            // WebGL is outside the #if; on phones there is no wheel, the 2.5D pinch (View25DCameraInput) drives ZoomByFactor through this component.
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
            try { target = Mathf.Clamp(PlayerPrefs.GetFloat(PrefKey, 1f), Map3DMinFactor, Map3DMaxFactor); } catch (Exception) { target = 1f; }
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

        static float pinch = 1f;
        /// <summary>Two-finger pinch on a 2.5D map (View25DCameraInput): multiplies the view size by `factor` (&lt; 1 = closer), same limits as the wheel.</summary>
        public static void ZoomByFactor(float factor) { if (factor > 0f) pinch *= factor; }

        /// <summary>A panel scrolls its own list or owns the keyboard: the wheel is theirs (same gates as InputAdapter / TouchControls).</summary>
        internal static bool WindowOwnsPointer()
        {
            return GameSession.Hero == null || UiWindows.BlocksGameplay || InventoryPanel.IsOpen || HeroChangeScreen.IsOpen || AccountScreenGui.MenuOpen
                || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
        }

        void Update()
        {
            if (cam == null) return;
            float notches = WindowOwnsPointer() ? 0f : InputAdapter.MouseScrollDelta();
            float pinched = pinch;
            pinch = 1f;
            if (notches != 0f || pinched != 1f)
            {
                target = Mathf.Clamp(target * Mathf.Pow(1f - NotchStep, notches) * pinched, RangeMin(minimum), RangeMax(MaxFactor)); // wheel up = smaller view = closer
                if (savePreference)
                    try { PlayerPrefs.SetFloat(PrefKey, target); PlayerPrefs.Save(); } catch (Exception) { /* prefs are a convenience */ }
            }
            float defaultHalf = DevStartConfig.CameraViewHeight() * 0.5f;
            float want = Map3D.Active || Ro3Map ? Mathf.Clamp(target, RangeMin(minimum), RangeMax(MaxFactor)) : Mathf.Min(target, MaxFactorForMap(defaultHalf));
            Current = Mathf.SmoothDamp(Current, want, ref velocity, EaseSeconds, Mathf.Infinity, Time.unscaledDeltaTime); // unscaled: hit-stop / pause
            if (Mathf.Abs(Current - want) < 0.0005f) { Current = want; velocity = 0f; }
            cam.orthographicSize = defaultHalf * Current;
        }

        /// <summary>Zooming out stops where the view would exceed the map (CameraFollow.FitToMap): on a small map even the default is narrowed.</summary>
        float MaxFactorForMap(float defaultHalf)
        {
            float fit = CameraFollow.FitToMap(defaultHalf, cam.aspect) / defaultHalf;
            return Mathf.Max(minimum, Mathf.Min(MaxFactor, fit));
        }
    }
}
