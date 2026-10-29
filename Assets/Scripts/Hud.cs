using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// IMGUI HUD (top-left): class + HP bar + FPS readout identical in spirit to the web one (avg FPS and worst frame, refreshed twice a second),
    /// controls, and the Frostfang King boss bar (top centre) while the boss is alive.
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
        string bossText = "";
        int toggleState = -1;
        int lastHp = -1, lastBossHp = -1;
        HeroClass lastClass = (HeroClass)(-1);
        GUIStyle label, small, center;

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
            HeroController hero = GameSession.Hero;
            if (hero == null) return; // class select screen is showing
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
                small = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
                center = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            }
            float s = Screen.height / 640f;
            label.fontSize = Mathf.RoundToInt(15f * s);
            small.fontSize = Mathf.RoundToInt(12f * s);
            center.fontSize = Mathf.RoundToInt(14f * s);

            // HP bar
            float x = 12f * s, y = 12f * s, w = 230f * s, h = 20f * s;
            int hp = Mathf.CeilToInt(hero.Hp);
            if (hp != lastHp || hero.Class.Id != lastClass)
            {
                lastHp = hp; lastClass = hero.Class.Id;
                hpText = hero.Class.Name + "  HP " + hp + " / " + (int)hero.MaxHp;
            }
            float frac = Mathf.Clamp01(hero.Hp / hero.MaxHp);
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
            float ly = y + h + 34f * s;
            Fill(new Rect(x - 4f, ly - 2f, 560f * s, 76f * s), new Color(0.03f, 0.07f, 0.12f, 0.72f)); // keeps the text readable on bright snow
            GUI.Label(new Rect(x, ly, 600f * s, 18f * s), hero.GearLabel, small);
            GUI.Label(new Rect(x, ly + 18f * s, 600f * s, 18f * s), "Move WASD/Arrows   Attack J/Space (" + hero.Class.ComboLabel + ")   Dash K/Shift", small);
            GUI.Label(new Rect(x, ly + 36f * s, 600f * s, 18f * s), "Switch class F1 Warrior / F2 Wizard / F3 Mechanic (respawn)   [F4] collision tint", small);
            int state = (FeelConfig.BloomOn ? 1 : 0) | (FeelConfig.FxOn ? 2 : 0);
            if (state != toggleState)
            {
                toggleState = state;
                toggleText = "[B] Bloom: " + (FeelConfig.BloomOn ? "ON" : "OFF") + "    [F] Effects: " + (FeelConfig.FxOn ? "ON" : "OFF") + "    (toggle to A/B compare)";
            }
            small.normal.textColor = new Color(1f, 0.9f, 0.55f, 0.95f);
            GUI.Label(new Rect(x, ly + 54f * s, 600f * s, 18f * s), toggleText, small);
            DrawBossBar(s);
            GUI.color = Color.white;
        }

        void DrawBossBar(float s)
        {
            Monster boss = Monster.Boss;
            if (boss == null || boss.IsDying) return;
            int hp = Mathf.CeilToInt(boss.Hp);
            if (hp != lastBossHp) { lastBossHp = hp; bossText = boss.DisplayName + "   " + hp + " / " + (int)boss.MaxHp; }
            float w = 360f * s, h = 24f * s, x = (Screen.width - w) * 0.5f, y = 14f * s;
            Fill(new Rect(x - 2f, y - 2f, w + 4f, h + 4f), new Color(0.03f, 0.04f, 0.08f, 0.9f));
            Fill(new Rect(x, y, w * Mathf.Clamp01(boss.Hp / boss.MaxHp), h), new Color(0.35f, 0.7f, 1f));
            center.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y, w, h), bossText, center);
        }

        static void Fill(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
