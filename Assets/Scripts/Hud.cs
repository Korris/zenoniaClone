using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// IMGUI HUD (top-left): HP bar + FPS readout identical in spirit to the web one (avg FPS and worst frame, refreshed twice a second).
    /// Strings are rebuilt only when their value changes, so the HUD itself does not generate per-frame garbage.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        const float RefreshSeconds = 0.5f;
        float windowTime, worstMs;
        int frames, shownFps;
        int shownWorstMs;
        string fpsText = "FPS --";
        string hpText = "";
        string toggleText = "";
        int toggleState = -1;
        int lastHp = -1;
        GUIStyle label, small;
        WarriorController hero;

        public void Init(WarriorController warrior) { hero = warrior; }

        void Update()
        {
            float dt = Time.unscaledDeltaTime; // real frame time (hit-stop does not hide hitches)
            frames++;
            windowTime += dt;
            worstMs = Mathf.Max(worstMs, dt * 1000f);
            if (windowTime >= RefreshSeconds)
            {
                shownFps = Mathf.RoundToInt(frames / windowTime);
                shownWorstMs = Mathf.RoundToInt(worstMs);
                fpsText = "FPS " + shownFps + "  ·  max " + shownWorstMs + "ms";
                frames = 0; windowTime = 0f; worstMs = 0f;
            }
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
                small = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
            }
            float s = Screen.height / 640f;
            label.fontSize = Mathf.RoundToInt(15f * s);
            small.fontSize = Mathf.RoundToInt(12f * s);

            // HP bar
            float x = 12f * s, y = 12f * s, w = 230f * s, h = 20f * s;
            int hp = hero != null ? Mathf.CeilToInt(hero.Hp) : 0;
            if (hp != lastHp) { lastHp = hp; hpText = "HP " + hp + " / " + (int)WarriorController.MaxHp; }
            float frac = hero != null ? Mathf.Clamp01(hero.Hp / WarriorController.MaxHp) : 0f;
            Fill(new Rect(x - 2f, y - 2f, w + 4f, h + 4f), new Color(0.03f, 0.06f, 0.1f, 0.85f));
            Fill(new Rect(x, y, w * frac, h), frac > 0.3f ? new Color(0.2f, 0.78f, 0.35f) : new Color(0.9f, 0.25f, 0.25f));
            label.normal.textColor = Color.white;
            GUI.Label(new Rect(x + 6f * s, y, w, h), hpText, label);

            // FPS (green near 60+, amber when dipping, red when laggy)
            Color fpsColor = shownFps >= 55 ? new Color(0.49f, 1f, 0.6f) : shownFps >= 40 ? new Color(1f, 0.84f, 0.42f) : new Color(1f, 0.42f, 0.42f);
            Fill(new Rect(x - 2f, y + h + 8f * s, 190f * s, 20f * s), new Color(0.03f, 0.07f, 0.12f, 0.8f));
            label.normal.textColor = fpsColor;
            GUI.Label(new Rect(x + 6f * s, y + h + 8f * s, 190f * s, 20f * s), fpsText, label);

            small.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
            string line = hero != null ? hero.GearLabel : "";
            GUI.Label(new Rect(x, y + h + 34f * s, 520f * s, 18f * s), line, small);
            GUI.Label(new Rect(x, y + h + 52f * s, 520f * s, 18f * s), "Move WASD/Arrows   Attack J/Space (4-hit combo)   Dash K/Shift", small);
            int state = (FeelConfig.BloomOn ? 1 : 0) | (FeelConfig.FxOn ? 2 : 0);
            if (state != toggleState)
            {
                toggleState = state;
                toggleText = "[B] Bloom: " + (FeelConfig.BloomOn ? "ON" : "OFF") + "    [F] Effects: " + (FeelConfig.FxOn ? "ON" : "OFF") + "    (toggle to A/B compare)";
            }
            small.normal.textColor = new Color(1f, 0.9f, 0.55f, 0.95f);
            GUI.Label(new Rect(x, y + h + 70f * s, 520f * s, 18f * s), toggleText, small);
            GUI.color = Color.white;
        }

        static void Fill(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
