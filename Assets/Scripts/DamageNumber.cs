using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Floating damage / status numbers. Fixed-size pool (no per-hit allocations) drawn with IMGUI over the world
    /// (world position -> screen point). Glyphs come from the sprite font (DamageNumberFont), design by ChatGPT
    /// (docs/reference/damage-numbers-chatgpt-spec.md, V3 "Comic Impact"): per-kind motion (DamageNumberMotion), screen-box anti-overlap
    /// layout (DamageNumber.Layout.cs) and the crit "event" (burst, CRITICAL banner, sparks: DamageNumber.Extras.cs).
    /// The kind is read from the Spawn arguments so the offline and online callers stay unchanged. Text the font cannot draw
    /// ("Shadow Step!") uses plain IMGUI text.
    /// </summary>
    public partial class DamageNumber : MonoBehaviour
    {
        const float TopMarginPx = 96f; // 720p px kept clear at the top (status bar, minimap, boss bar)
        const int Capacity = 256; // Multi-Hit Barrage: 10 monsters in an 18-hit skill peak near 90 live numbers (ChatGPT V3)

        struct Entry
        {
            public bool Active;
            public string Text;
            public Color Color;
            public float X, Y, AgeMs;  // world anchor; AgeMs < 0 = delayed spawn (EXP next to a crit)
            public float OffX, OffY;   // layout offset in 720p px (OffY up)
            public bool BigBurst;      // crit: the first crit within 300 ms gets the large burst (V3)
            public float Seed;         // per-number random (slash angle, spark directions)
            public DamageNumberKind Kind;
        }

        static DamageNumber instance;
        static readonly Entry[] Pool = new Entry[Capacity];
        static int cursor;
        Camera cam;

        /// <summary>`stream`: the monster hit (your own hits only); its plain hits rise in the Damage Stream zigzag instead of looking for free space.</summary>
        public static void Spawn(float x, float y, string text, Color color, bool big, bool crit = false, bool faint = false, object stream = null)
        {
            if (!NetMode.RendersFx) return; // headless server: no cosmetics
            if (instance == null) return;
            DamageNumberKind kind = DamageNumberMotion.Classify(text, color, big, crit && FeelConfig.FxOn, faint);
            if (kind == DamageNumberKind.Exp && MergeExp(text)) return;
            if ((kind == DamageNumberKind.Other || kind == DamageNumberKind.OtherHurt) && CountActive(DamageNumberKind.Other, DamageNumberKind.OtherHurt) >= 4) return; // spec: at most 4 numbers from other players
            var e = new Entry { Active = true, Text = text, Color = color, X = x, Y = y, Kind = kind, Seed = Random.value };
            if (stream != null && kind == DamageNumberKind.Normal) StreamPlace(ref e, stream);
            else if (!Place(ref e)) return;
            if (kind == DamageNumberKind.Crit) e.BigBurst = !RecentCrit(500f); // one big burst per 500 ms: a barrage crits often (ChatGPT V3)
            cursor = (cursor + 1) % Capacity; // overwrite the oldest slot when saturated
            Pool[cursor] = e;
        }

        public static void ResetState()
        {
            for (int i = 0; i < Capacity; i++) Pool[i].Active = false;
            Streams.Clear();
        }

        void Awake() { instance = this; }
        void OnDestroy() { if (instance == this) instance = null; }

        void Update()
        {
            float dtMs = Time.deltaTime * 1000f; // numbers keep floating during hit-stop
            for (int i = 0; i < Capacity; i++)
            {
                if (!Pool[i].Active) continue;
                Pool[i].AgeMs += dtMs;
                if (Pool[i].AgeMs >= DamageNumberMotion.Of(Pool[i].Kind).LifeMs) Pool[i].Active = false;
            }
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || HeroChangeScreen.IsOpen) return; // IMGUI would draw on top of the hero select
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            float px = Screen.height / 720f; // spec sizes are for a 720 px tall screen
            for (int pass = 0; pass < 2; pass++) // crits last so no other number covers them
                for (int n = 1; n <= Capacity; n++) // oldest first so the newest number is on top
                {
                    int i = (cursor + n) % Capacity;
                    if (!Pool[i].Active || Pool[i].AgeMs < 0f || (Pool[i].Kind == DamageNumberKind.Crit) != (pass == 1)) continue;
                    Vector3 sp = cam.WorldToScreenPoint(View25D.Label(Pool[i].X, Pool[i].Y));
                    if (sp.z < 0f) continue;
                    DrawEntry(ref Pool[i], sp.x, Screen.height - sp.y, px);
                }
            GUI.color = Color.white;
        }

        void DrawEntry(ref Entry e, float sx, float sy, float px)
        {
            DamageNumberMotion m = DamageNumberMotion.Of(e.Kind);
            float t = e.AgeMs, alpha = m.Fade(t);
            if (alpha <= 0.01f) return;
            float height = m.HeightPx;
            float cap = height * m.Scale(t) * px;
            Vector2 shake = m.Shake(t);
            float cx = sx + (e.OffX + shake.x) * px;
            float cy = sy - (e.OffY + m.Rise(t) + shake.y) * px;
            cy = Mathf.Max(cy, TopMarginPx * px); // never under the top HUD or off the screen
            float baseY = cy + cap * 0.5f; // the number is centred on its anchor, baseline half a cap below
            float textW = height * px * 0.62f * e.Text.Length; // rough run width for the crit frame sizes
            if (e.Kind == DamageNumberKind.Crit) DrawCritBehind(cx, cy, t, height * px, textW, e.Seed, alpha, e.BigBurst);
            else if (e.Kind == DamageNumberKind.Finisher) DrawFinisherBehind(cx, cy, t, height * px, e.Seed);
            if (!DamageNumberFont.Draw(e.Text, m.Style, cx, baseY, cap, alpha, e.Kind == DamageNumberKind.Pet ? e.Color : Color.white)) // only a pet's number is tinted
                DrawPlainText(e.Text, e.Color, cx, cy, height * m.Scale(t) * px, alpha);
            if (e.Kind == DamageNumberKind.Crit) DrawCritFront(cx, cy, t, height * px, textW, e.Seed, alpha);
            else if (e.Kind == DamageNumberKind.Heal) DrawSparks(cx, cy, t, height * px, e.Seed, alpha, 2, 0.7f);
        }
    }
}
