using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Crit "event" and finisher accents of the floating numbers (ChatGPT V3 "Comic Impact", mockup
    /// ArtLibrary/ui/damage-font/damage-numbers-v2-mockup-chatgpt-1.png): sparks around the yellow crit number (burst and CRITICAL
    /// banner dropped 20/11); small slash + ring behind finisher numbers; 2 sparks on heals. Art: Resources/Sprites/ui/damage-font/
    /// fx-*.png (Tools/build-damage-number-fx.py); a missing file only skips that layer. Also the plain-text fallback.
    /// </summary>
    public partial class DamageNumber
    {
        static Texture2D slashTex, ringTex;
        static readonly Texture2D[] SparkTex = new Texture2D[4];
        static bool fxLoaded;
        GUIStyle plainStyle;

        static void LoadFx()
        {
            fxLoaded = true;
            slashTex = Fx("slash"); ringTex = Fx("finisher-ring");
            for (int i = 0; i < SparkTex.Length; i++) SparkTex[i] = Fx("spark-" + i);
        }

        static Texture2D Fx(string name) { return Resources.Load<Texture2D>("Sprites/ui/damage-font/fx-" + name); }

        /// <summary>Draws a texture centred on (cx, cy), w px wide (height from the aspect), rotated deg, tinted alpha.</summary>
        static void DrawCentered(Texture2D tex, float cx, float cy, float w, float deg, float alpha)
        {
            if (tex == null || alpha <= 0.01f) return;
            float h = w * tex.height / tex.width;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            if (deg == 0f) { GUI.DrawTexture(new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h), tex); return; }
            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(deg, new Vector2(cx, cy));
            GUI.DrawTexture(new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h), tex);
            GUI.matrix = saved;
        }

        static float EaseOut(float u) { u = Mathf.Clamp01(u); return 1f - (1f - u) * (1f - u); }
        static float Frac(float v) { return v - Mathf.Floor(v); }

        /// <summary>In front of the crit: 5 small sparks only. The CRITICAL banner and the comic burst were dropped (user 20/11:
        /// "bỏ chữ critical ... chỉ hiện màu vàng là đủ"); the yellow crit digits carry it.</summary>
        static void DrawCritFront(float cx, float cy, float ms, float digitPx, float textW, float seed, float alpha)
        {
            DrawSparks(cx, cy, ms, digitPx, seed, alpha, 5, 1f);
        }

        /// <summary>n sparks flying out from the number edge (18-32 px at 720p) over 220-320 ms.</summary>
        static void DrawSparks(float cx, float cy, float ms, float digitPx, float seed, float alpha, int n, float size)
        {
            if (!FeelConfig.FxOn) return;
            if (!fxLoaded) LoadFx();
            float px = Screen.height / 720f;
            for (int i = 0; i < n; i++)
            {
                float life = 220f + 100f * Frac(seed * 7.3f + i * 0.37f);
                if (ms >= life) continue;
                float u = ms / life, ang = (seed * 360f + i * 360f / n + 20f * Frac(seed * 13f + i)) * Mathf.Deg2Rad;
                float dist = digitPx * 0.8f + (18f + 14f * Frac(seed * 3.1f + i * 0.61f)) * EaseOut(u) * px;
                DrawCentered(SparkTex[i % SparkTex.Length], cx + Mathf.Cos(ang) * dist * 1.5f, cy + Mathf.Sin(ang) * dist * 0.8f, 14f * px * size * (1f - 0.4f * u), 0f, (1f - u * u) * alpha);
            }
        }

        /// <summary>Behind finisher numbers: a small slash arc sweeping across (0-260 ms) over a ring that pops out (0-320 ms).</summary>
        static void DrawFinisherBehind(float cx, float cy, float ms, float digitPx, float seed)
        {
            if (!FeelConfig.FxOn || ms >= 320f) return;
            if (!fxLoaded) LoadFx();
            float u = ms / 320f;
            DrawCentered(ringTex, cx, cy + digitPx * 0.15f, digitPx * Mathf.Lerp(1.8f, 3.2f, EaseOut(u)), 0f, 0.85f * (1f - u * u));
            if (ms < 260f)
            {
                float v = ms / 260f;
                DrawCentered(slashTex, cx + Mathf.Lerp(-8f, 6f, v) * digitPx / 32f, cy, digitPx * 2.6f * Mathf.Lerp(0.6f, 1f, EaseOut(v * 1.5f)), -10f + 20f * seed, v < 0.5f ? 1f : 2f - 2f * v);
            }
        }

        /// <summary>Fallback for text without glyphs (skill callouts) or a missing font: the old bold label with a dark shadow.</summary>
        void DrawPlainText(string text, Color color, float cx, float cy, float heightPx, float alpha)
        {
            if (plainStyle == null) plainStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            plainStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(heightPx * 0.8f));
            float s = Screen.height / 720f;
            var rect = new Rect(cx - 160f * s, cy - 20f * s, 320f * s, 40f * s);
            GUI.color = new Color(0f, 0f, 0f, 0.85f * alpha);
            plainStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(rect.x + 1.5f * s, rect.y + 1.5f * s, rect.width, rect.height), text, plainStyle);
            plainStyle.normal.textColor = color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(rect, text, plainStyle);
        }
    }
}
