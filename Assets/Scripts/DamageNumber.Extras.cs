using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Crit "event" and finisher accents of the floating numbers (ChatGPT V3 "Comic Impact", mockup
    /// ArtLibrary/ui/damage-font/damage-numbers-v2-mockup-chatgpt-1.png): hit flash + comic burst behind the crit number, CRITICAL
    /// banner + sparks in front; small slash + ring behind finisher numbers; 2 sparks on heals. Art: Resources/Sprites/ui/damage-font/
    /// fx-*.png (Tools/build-damage-number-fx.py); a missing file only skips that layer. Also the plain-text fallback.
    /// </summary>
    public partial class DamageNumber
    {
        static Texture2D burstTex, labelTex, slashTex, ringTex, flashTex;
        static readonly Texture2D[] SparkTex = new Texture2D[4];
        static bool fxLoaded;
        GUIStyle plainStyle;

        static void LoadFx()
        {
            fxLoaded = true;
            burstTex = Fx("crit-burst"); labelTex = Fx("critical-label"); slashTex = Fx("slash"); ringTex = Fx("finisher-ring"); flashTex = Fx("hit-flash");
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

        /// <summary>Behind the crit: impact flash (0-120 ms) and the comic burst framing the number. Large burst (170 px at 720p,
        /// at least 1.9x the number width) for the first crit in 300 ms, small (120 px, 1.4x) after; explodes with the number
        /// (0-70 ms), holds while it is read, fades 300-450 ms.</summary>
        static void DrawCritBehind(float cx, float cy, float ms, float digitPx, float textW, float seed, float alpha, bool big)
        {
            if (!FeelConfig.FxOn) return;
            if (!fxLoaded) LoadFx();
            float px = Screen.height / 720f;
            if (ms < 120f) DrawCentered(flashTex, cx, cy, Mathf.Lerp(0.6f, 1.5f, EaseOut(ms / 120f)) * digitPx * 2f, seed * 90f, 1f - ms / 120f);
            if (ms < 450f)
            {
                float w = big ? Mathf.Max(170f * px, textW * 1.9f) : Mathf.Max(120f * px, textW * 1.4f); // first in-game pass: the spec 125 / 90 px hid behind the digits
                float s = ms < 70f ? Mathf.Lerp(0.4f, 1.12f, EaseOut(ms / 70f)) : Mathf.Lerp(1.12f, 1f, Mathf.Clamp01((ms - 70f) / 100f));
                float a = ms < 300f ? 1f : 1f - (ms - 300f) / 150f;
                DrawCentered(burstTex, cx, cy, w * s, (seed - 0.5f) * 10f, a * alpha);
            }
        }

        /// <summary>In front of the crit: CRITICAL banner above the number (in at 70 ms with a pop, stays with it) and 5 sparks.</summary>
        static void DrawCritFront(float cx, float cy, float ms, float digitPx, float textW, float seed, float alpha)
        {
            if (!FeelConfig.FxOn) return;
            if (!fxLoaded) LoadFx();
            if (ms >= 70f)
            {
                float s = ms < 140f ? Mathf.Lerp(0.5f, 1.15f, EaseOut((ms - 70f) / 70f)) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((ms - 140f) / 80f));
                float w = Mathf.Max(digitPx * 2.6f, textW * 0.9f) * s;
                if (labelTex != null) DrawCentered(labelTex, cx - digitPx * 0.1f, cy - digitPx * 0.95f, w, -3f, alpha);
                else DamageNumberFont.Draw("CRITICAL", null, cx, cy - digitPx * 0.8f, digitPx * 0.42f * s, alpha, Color.white);
            }
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
