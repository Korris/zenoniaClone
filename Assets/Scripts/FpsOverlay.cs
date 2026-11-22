#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Small FPS readout (user 22/11: "show thêm FPS trong game"): frames per second averaged over 0.5 s + the mean frame time in ms, drawn with a dark
    /// outline under the top-left status block; green at 55+, yellow 30-55, red below 30. On by default in test / dev builds, never in a release build.
    /// Then the world render scale in % (RenderScale; "auto" while dynamic resolution holds it under the Settings ceiling).
    /// F12 shows / hides it (F3 is the offline class switch to Mechanic). Works the same on 2D, 2.5D and `--map3d` maps (IMGUI over everything).
    /// Hidden in clean capture runs (Hud.CaptureClean, `--no-hud`) so screenshots stay unchanged.
    /// </summary>
    public class FpsOverlay : MonoBehaviour
    {
        const float Window = 0.5f;
        static bool visible = true;
        int frames;
        float time;
        int fps;
        string text = "-- FPS";
        GUIStyle style;

        /// <summary>The readout is on screen (Hud skips its own F8 FPS line then).</summary>
        public static bool Visible { get { return visible && !ReleaseBuild.IsRelease; } }

        public static void Attach(GameObject root)
        {
            if (ReleaseBuild.IsRelease) return;
            root.AddComponent<FpsOverlay>();
        }

        static bool TogglePressed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.F12);
#elif ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb[Key.F12].wasPressedThisFrame;
#else
            return false;
#endif
        }

        void Update()
        {
            if (TogglePressed()) visible = !visible;
            frames++;
            time += Time.unscaledDeltaTime; // real frame time: hit-stop / slow motion do not change it
            if (time < Window) return;
            fps = Mathf.RoundToInt(frames / time);
            text = fps + " FPS  " + (time * 1000f / frames).ToString("F1") + " ms  " + Mathf.RoundToInt(RenderScale.Current * 100f) + "%" // render scale (dynamic resolution moves it)
                + (RenderScale.Dynamic && RenderScale.Current < RenderScale.Ceiling - 0.001f ? " auto" : "");
            frames = 0; time = 0f;
        }

        void OnGUI()
        {
            if (!visible || Hud.CaptureClean || Event.current.type != EventType.Repaint) return;
            if (GameSession.Hero == null || HeroChangeScreen.IsOpen) return; // login / hero select screens draw full screen
            float s = Screen.height / 640f;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            style.fontSize = Mathf.Max(11, Mathf.RoundToInt(13f * s));
            var r = new Rect(12f * s, (RpgHud.BlockHeight + 2f) * s, 220f * s, 18f * s);
            style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            float o = Mathf.Max(1f, s);
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0) GUI.Label(new Rect(r.x + dx * o, r.y + dy * o, r.width, r.height), text, style);
            style.normal.textColor = fps >= 55 ? new Color(0.45f, 1f, 0.5f) : fps >= 30 ? new Color(1f, 0.88f, 0.3f) : new Color(1f, 0.38f, 0.35f);
            GUI.Label(r, text, style);
        }
    }
}
