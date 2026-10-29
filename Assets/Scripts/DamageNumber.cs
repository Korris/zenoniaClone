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
            public bool Big;
        }

        static DamageNumber instance;
        static readonly Entry[] Pool = new Entry[Capacity];
        static int cursor;
        GUIStyle style;
        Camera cam;

        public static void Spawn(float x, float y, string text, Color color, bool big)
        {
            if (instance == null) return;
            cursor = (cursor + 1) % Capacity; // overwrite the oldest slot when saturated
            Pool[cursor] = new Entry { Active = true, Text = text, Color = color, X = x, Y = y, Age = 0f, Big = big };
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
                Pool[i].Y += RiseUnitsPerSecond * dt;
                if (Pool[i].Age >= LifeSeconds) Pool[i].Active = false;
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
                float fade = 1f - Mathf.Clamp01((Pool[i].Age - 0.45f) / (LifeSeconds - 0.45f));
                style.fontSize = Mathf.RoundToInt((Pool[i].Big ? 26f : 19f) * scale);
                var rect = new Rect(sp.x - 80f, Screen.height - sp.y - 20f, 160f, 40f);
                GUI.color = new Color(0f, 0f, 0f, 0.85f * fade);
                style.normal.textColor = Color.black;
                GUI.Label(new Rect(rect.x + 1.5f * scale, rect.y + 1.5f * scale, rect.width, rect.height), Pool[i].Text, style);
                style.normal.textColor = Pool[i].Color;
                GUI.color = new Color(1f, 1f, 1f, fade);
                GUI.Label(rect, Pool[i].Text, style);
            }
            GUI.color = Color.white;
        }
    }
}
