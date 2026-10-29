using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// IMGUI HUD (top-left, below the UGUI RpgHud block with level / HP / MP / EXP / potions): class skill line, FPS readout identical in spirit
    /// to the web one (avg FPS and worst frame, refreshed twice a second), controls, and the Frostfang King boss bar (top centre) while the boss is alive.
    /// Hidden (except the boss bar) while the inventory window is open.
    /// Strings are rebuilt only when their value changes, so the HUD itself does not generate per-frame garbage.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        const float RefreshSeconds = 0.5f;
        float windowTime, worstMs;
        int frames, shownFps;
        int shownWorstMs;
        string fpsText = "FPS --";
        string toggleText = "";
        string bossText = "";
        int toggleState = -1;
        int lastBossHp = -1;
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
            ScreenFlash.Draw(); // under the HUD
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

            if (InventoryPanel.IsOpen) { DrawBossBar(s); GUI.color = Color.white; return; }

            // Signature skill (L) with cooldown, under the UGUI level / HP / MP / EXP block
            float x = 12f * s, w = 236f * s, h = 20f * s;
            float y = (RpgHud.BlockHeight + 12f) * s;
            if (hero.Class.SkillName != null)
            {
                float cd = hero.SkillCooldownLeftMs;
                string txt = "[L] " + hero.Class.SkillName + "  MP " + (int)hero.Class.SkillMp + (cd > 0f ? "   cooldown " + (cd / 1000f).ToString("F1") + "s" : hero.Mp >= hero.Class.SkillMp ? "   READY" : "   (not enough MP)");
                small.normal.textColor = cd > 0f || hero.Mp < hero.Class.SkillMp ? new Color(1f, 0.7f, 0.5f) : new Color(0.6f, 1f, 0.7f);
                Fill(new Rect(x - 2f, y, w + 4f, 18f * s), new Color(0.03f, 0.06f, 0.1f, 0.8f));
                GUI.Label(new Rect(x + 4f * s, y, w, 18f * s), txt, small);
            }
            y -= 6f * s;

            // FPS (green near 60+, amber when dipping, red when laggy)
            Color fpsColor = shownFps >= 55 ? new Color(0.49f, 1f, 0.6f) : shownFps >= 40 ? new Color(1f, 0.84f, 0.42f) : new Color(1f, 0.42f, 0.42f);
            Fill(new Rect(x - 2f, y + h + 8f * s, 190f * s, 20f * s), new Color(0.03f, 0.07f, 0.12f, 0.8f));
            label.normal.textColor = fpsColor;
            GUI.Label(new Rect(x + 6f * s, y + h + 8f * s, 190f * s, 20f * s), fpsText, label);

            small.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
            float ly = y + h + 34f * s;
            Fill(new Rect(x - 4f, ly - 2f, 560f * s, 112f * s), new Color(0.03f, 0.07f, 0.12f, 0.72f)); // keeps the text readable on bright snow
            GUI.Label(new Rect(x, ly, 600f * s, 18f * s), hero.GearLabel, small);
            GUI.Label(new Rect(x, ly + 18f * s, 600f * s, 18f * s), "Move WASD/Arrows   Attack J/Space (" + hero.Class.ComboLabel + ")   Dash K/Shift", small);
            GUI.Label(new Rect(x, ly + 36f * s, 600f * s, 18f * s), "[B] Inventory / enhance   [1] HP potion   [2] MP potion   Loot: walk over it", small);
            GUI.Label(new Rect(x, ly + 54f * s, 600f * s, 18f * s), "Switch class F1 Warrior F2 Wizard F3 Mechanic F4 Assassin F5 Cleric (own save)   [F7] touch buttons   [F9] collision", small);
            ly += 18f * s;
            int state = (FeelConfig.BloomOn ? 1 : 0) | (FeelConfig.FxOn ? 2 : 0);
            if (state != toggleState)
            {
                toggleState = state;
                toggleText = "[F6] Bloom: " + (FeelConfig.BloomOn ? "ON" : "OFF") + "    [F] Effects: " + (FeelConfig.FxOn ? "ON" : "OFF") + "    (toggle to A/B compare)";
            }
            small.normal.textColor = new Color(1f, 0.9f, 0.55f, 0.95f);
            GUI.Label(new Rect(x, ly + 54f * s, 600f * s, 18f * s), toggleText, small);
            if (hero.Class.Id == HeroClass.Mechanic)
            {
                small.normal.textColor = ClassPalette.MechanicBlue ? new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.75f, 0.35f);
                GUI.Label(new Rect(x, ly + 72f * s, 600f * s, 18f * s), "[C] Mechanic FX: " + (ClassPalette.MechanicBlue ? "BLUE" : "ORANGE"), small);
            }
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
