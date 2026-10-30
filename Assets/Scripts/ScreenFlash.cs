using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Full-screen colour flash (ultimate impacts): drawn over the world by the HUD's OnGUI, decays on real time.</summary>
    public static class ScreenFlash
    {
        static Color color;
        static float life, left;

        public static void Trigger(Color c, float seconds)
        {
            if (!NetMode.RendersFx) return; // headless server: no cosmetics
            if (!FeelConfig.FxOn) return;
            color = c; life = left = Mathf.Max(0.01f, seconds);
        }

        public static void ResetState() { left = 0f; }

        /// <summary>Call from a Repaint OnGUI.</summary>
        public static void Draw()
        {
            if (left <= 0f) return;
            left -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(left / life);
            GUI.color = new Color(color.r, color.g, color.b, color.a * t * t);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
