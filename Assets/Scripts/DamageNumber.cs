using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Floating damage / status numbers. Fixed-size pool (no per-hit allocations) drawn with IMGUI over the world
    /// (world position -> screen point). Text strings are created once at spawn.
    /// </summary>
    public class DamageNumber : MonoBehaviour
    {
        const int Capacity = 32;
        const float LifeSeconds = 0.8f;
        const float RiseUnitsPerSecond = 22f;

        struct Entry
        {
            public bool Active;
            public string Text;
            public Color Color;
            public float X, Y, Age;
            public bool Big, Crit, Faint;
            public float DriftX;
        }

        static DamageNumber instance;
        static readonly Entry[] Pool = new Entry[Capacity];
        static int cursor;
        GUIStyle style;
        Camera cam;

        public static void Spawn(float x, float y, string text, Color color, bool big, bool crit = false, bool faint = false)
        {
            if (!NetMode.RendersFx) return; // headless server: no cosmetics
            if (instance == null) return;
            cursor = (cursor + 1) % Capacity; // overwrite the oldest slot when saturated
            Pool[cursor] = new Entry { Active = true, Text = text, Color = color, X = x, Y = y, Age = 0f, Big = big, Crit = crit && !faint, Faint = faint, DriftX = Random.Range(-7f, 7f) };
        }

        public static void ResetState()
        {
            for (int i = 0; i < Capacity; i++) Pool[i].Active = false;
        }

        void Awake() { instance = this; }
        void OnDestroy() { if (instance == this) instance = null; }

        void Update()
        {
            float dt = Time.deltaTime; // numbers keep floating during hit-stop
            for (int i = 0; i < Capacity; i++)
            {
                if (!Pool[i].Active) continue;
                Pool[i].Age += dt;
                Pool[i].Y += RiseUnitsPerSecond * (Pool[i].Crit && FeelConfig.FxOn ? 0.75f : 1f) * dt;
                if (FeelConfig.FxOn) Pool[i].X += Pool[i].DriftX * dt;
                if (Pool[i].Age >= LifeSeconds) Pool[i].Active = false;
            }
        }

        /// <summary>Pop animation: scale up past the target, then settle (overshoot); flat 1.0 when effects are off.</summary>
        static float PopScale(float age, bool crit)
        {
            if (!FeelConfig.FxOn) return 1f;
            float peak = crit ? FeelConfig.PopPeakCrit : FeelConfig.PopPeak;
            float rise = FeelConfig.PopRiseMs / 1000f, settle = FeelConfig.PopSettleMs / 1000f;
            if (age < rise) return Mathf.Lerp(0.5f, peak, age / rise);
            if (age < settle) return Mathf.Lerp(peak, 1f, (age - rise) / (settle - rise));
            return 1f;
        }

        void DrawOutline(Rect rect, string text, float d)
        {
            for (int k = 0; k < 4; k++)
            {
                float ox = (k & 1) == 0 ? -d : d, oy = k < 2 ? -d : d;
                GUI.Label(new Rect(rect.x + ox, rect.y + oy, rect.width, rect.height), text, style);
            }
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            }
            float scale = Screen.height / 640f;
            for (int i = 0; i < Capacity; i++)
            {
                if (!Pool[i].Active) continue;
                Vector3 sp = cam.WorldToScreenPoint(new Vector3(Pool[i].X, Pool[i].Y, 0f));
                if (sp.z < 0f) continue;
                float fade = (1f - Mathf.Clamp01((Pool[i].Age - 0.45f) / (LifeSeconds - 0.45f))) * (Pool[i].Faint ? 0.5f : 1f); // other players' numbers: half alpha
                bool crit = Pool[i].Crit && FeelConfig.FxOn;
                style.fontSize = Mathf.RoundToInt((Pool[i].Big ? 26f : 19f) * (Pool[i].Faint ? 0.7f : 1f) * (crit ? 1.15f : 1f) * PopScale(Pool[i].Age, crit) * scale);
                var rect = new Rect(sp.x - 80f, Screen.height - sp.y - 20f, 160f, 40f);
                GUI.color = new Color(0f, 0f, 0f, 0.85f * fade);
                style.normal.textColor = crit ? new Color(0.75f, 0.22f, 0.05f) : Color.black;
                if (crit) DrawOutline(rect, Pool[i].Text, 2.2f * scale);
                GUI.Label(new Rect(rect.x + 1.5f * scale, rect.y + 1.5f * scale, rect.width, rect.height), Pool[i].Text, style);
                style.normal.textColor = Pool[i].Color;
                GUI.color = new Color(1f, 1f, 1f, fade);
                GUI.Label(rect, Pool[i].Text, style);
            }
            GUI.color = Color.white;
        }
    }
}
