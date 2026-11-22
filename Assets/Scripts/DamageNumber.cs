using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Floating damage / status numbers. Fixed-size pool of structs (Capacity slots allocated once = prewarmed; a ring that overwrites the
    /// oldest slot when full = the cap on live numbers; no GameObject, no Instantiate / Destroy per hit), drawn in the IMGUI Repaint pass
    /// over the world (world position -> screen point) through DamageNumberBatch (one draw call per atlas). Glyphs come from the sprite font
    /// (DamageNumberFont), design by ChatGPT (docs/reference/damage-numbers-chatgpt-spec.md, V3 "Comic Impact"): per-kind motion
    /// (DamageNumberMotion), screen-box anti-overlap layout (DamageNumber.Layout.cs) and the crit "event" (red-orange digits on a spiky burst
    /// + sparks: DamageNumber.Extras.cs). 22/11 RO spec: one white number per hit, a yellow TOTAL above a multi-hit cluster (DamageNumber.Total.cs).
    /// The kind is read from the Spawn arguments so the offline and online callers stay unchanged. Text the font cannot draw
    /// ("Shadow Step!") uses plain IMGUI text.
    /// </summary>
    public partial class DamageNumber : MonoBehaviour
    {
        const float TopMarginPx = 96f; // 720p px kept clear at the top (status bar, minimap, boss bar)
        const float OlderScale = 0.8f; // a number pushed up its monster's ladder (DamageNumber.Stream.cs) shrinks to this
        const int Capacity = 256; // Multi-Hit Barrage: 10 monsters in an 18-hit skill peak near 90 live numbers (ChatGPT V3)

        struct Entry
        {
            public bool Active;
            public string Text;
            public Color Color;
            public float X, Y, AgeMs;  // world anchor; AgeMs < 0 = delayed spawn (EXP next to a crit, split parts, the TOTAL)
            public float OffX, OffY;   // layout offset in 720p px (OffY up)
            public bool BigBurst;      // crit: draws the spiky burst (the first crit within 500 ms, and a crit TOTAL)
            public float Seed;         // per-number random (slash angle, spark directions)
            public object Key;         // the monster your own hit landed on (same-spot stack, DamageNumber.Stream.cs), else null
            public DamageNumberKind Kind;
            public float SizeMul, PopMul; // multi-hit parts (DamageNumber.MultiHit.cs): size factor, a harder pop; 0 = 1
            public float LifeMs, RisePx;  // per-number motion override (stream numbers, DamageNumber.Stream.cs); 0 = the kind's value
            public float Lift;            // push-up of an older number in its monster's ladder (720p px, eased; DamageNumber.Stream.cs)
            public bool Chip;             // small running total of tiny / damage-over-time hits beside the ladder (DamageNumber.MultiHit.cs)
            public int ChipValue;
            public int Value;             // damage of your own hit / part (the TOTAL sums these as they show, DamageNumber.Total.cs)
            public bool Last;             // last part of its real hit (a single number is its own last part): the TOTAL may follow it
            public bool Counted;          // already added into its ladder's chain total
            public bool Total;            // this entry is a ladder's TOTAL (Kind Total, or Crit when the chain had a crit)
        }

        static DamageNumber instance;
        static readonly Entry[] Pool = new Entry[Capacity];
        static int cursor;
        Camera cam;

        /// <summary>`stream`: the monster hit (your own hits only); its plain hits go on that monster's ladder instead of looking for free space.
        /// Returns true when the number was added (it is then Pool[cursor]).</summary>
        public static bool Spawn(float x, float y, string text, Color color, bool big, bool crit = false, bool faint = false, object stream = null)
        {
            if (!NetMode.RendersFx) return false; // headless server: no cosmetics
            if (instance == null) return false;
            DamageNumberKind kind = DamageNumberMotion.Classify(text, color, big, crit && FeelConfig.FxOn, faint);
            if (kind == DamageNumberKind.Exp && MergeExp(text)) return false;
            // Other players' hits and the boss hitting other players are hidden (ChatGPT RO-style spec 20/11, 40-player raids stay readable).
            if (kind == DamageNumberKind.Other || kind == DamageNumberKind.OtherHurt) return false;
            var e = new Entry { Active = true, Text = text, Color = color, X = x, Y = y, Kind = kind, Seed = Random.value, Counted = stream == null };
            if (stream != null && (kind == DamageNumberKind.Normal || kind == DamageNumberKind.Finisher || kind == DamageNumberKind.Crit)) StreamPlace(ref e, stream);
            else if (!Place(ref e)) return false;
            if (kind == DamageNumberKind.Crit) e.BigBurst = !RecentCrit(500f); // one burst per 500 ms: a barrage crits often (ChatGPT V3)
            cursor = (cursor + 1) % Capacity; // overwrite the oldest slot when saturated
            Pool[cursor] = e;
            return true;
        }

        public static void ResetState()
        {
            for (int i = 0; i < Capacity; i++) { Pool[i].Active = false; Pool[i].Key = null; }
            Stacks.Clear();
            Chains.Clear();
            SkillHitContext.ResetState();
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
                if (Pool[i].AgeMs >= MotionOf(ref Pool[i]).LifeMs) { Pool[i].Active = false; continue; }
                if (!Pool[i].Counted && Pool[i].AgeMs >= 0f) { Pool[i].Counted = true; OnShown(i); } // your hit just popped: chain TOTAL
            }
            UpdateLadders(dtMs);
        }

        /// <summary>The kind's motion with the number's own life / rise override (stream numbers fade faster, rise less).</summary>
        static DamageNumberMotion MotionOf(ref Entry e)
        {
            DamageNumberMotion m = DamageNumberMotion.Of(e.Kind);
            if (e.LifeMs > 0f && e.LifeMs != m.LifeMs)
            {
                float k = e.LifeMs / m.LifeMs;
                m.FadeFromMs *= k; m.RiseDelayMs *= k; m.LifeMs = e.LifeMs;
            }
            if (e.RisePx > 0f) m.RisePx = e.RisePx;
            return m;
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || HeroChangeScreen.IsOpen) return; // IMGUI would draw on top of the hero select
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            float px = Screen.height / 720f; // spec sizes are for a 720 px tall screen
            DamageNumberBatch.Begin();
            for (int pass = 0; pass < 2; pass++) // crits and totals last so no other number covers them
                for (int n = 1; n <= Capacity; n++) // oldest first so the newest number is on top
                {
                    int i = (cursor + n) % Capacity;
                    if (!Pool[i].Active || Pool[i].AgeMs < 0f || (Pool[i].Kind == DamageNumberKind.Crit || Pool[i].Total) != (pass == 1)) continue;
                    Vector3 sp = cam.WorldToScreenPoint(View25D.Label(Pool[i].X, Pool[i].Y));
                    if (sp.z < 0f) continue;
                    DrawEntry(ref Pool[i], sp.x, Screen.height - sp.y, px, pass * 3);
                }
            DamageNumberBatch.Flush(); // one draw call per (layer, atlas)
            GUI.color = Color.white;
        }

        /// <summary>One number; `layer` = pass x 3 (DamageNumberBatch paint layers: +0 behind FX, +1 digits, +2 front sparks).</summary>
        void DrawEntry(ref Entry e, float sx, float sy, float px, int layer)
        {
            DamageNumberMotion m = MotionOf(ref e);
            float t = e.AgeMs, alpha = m.Fade(t);
            if (alpha <= 0.01f) return;
            float height = m.HeightPx * (e.SizeMul > 0f ? e.SizeMul : 1f);
            float scale = m.Scale(t);
            if (e.PopMul > 0f) scale = 1f + (scale - 1f) * e.PopMul; // multi-hit part: harder punch
            if (e.Lift > 0f && !e.Total) scale *= Mathf.Lerp(1f, OlderScale, Mathf.Clamp01(e.Lift / (height * StepMul))); // older ladder numbers step back
            float cap = height * scale * px;
            Vector2 shake = m.Shake(t);
            float cx = sx + (e.OffX + shake.x) * px;
            float cy = sy - (e.OffY + e.Lift + m.Rise(t) + shake.y) * px;
            cy = Mathf.Max(cy, TopMarginPx * px); // never under the top HUD or off the screen
            float baseY = cy + cap * 0.5f; // the number is centred on its anchor, baseline half a cap below
            float textW = height * px * 0.62f * e.Text.Length; // rough run width for the crit frame sizes
            if (e.Kind == DamageNumberKind.Finisher) DrawFinisherBehind(cx, cy, t, height * px, e.Seed, layer);
            else if (e.Kind == DamageNumberKind.Crit && e.BigBurst) DrawCritBurst(cx, cy, t, height * scale * px, textW * scale, e.Seed, alpha, layer); // 22/11 RO spec
            if (!DamageNumberFont.Draw(e.Text, m.Style, cx, baseY, cap, alpha, e.Kind == DamageNumberKind.Pet || e.Chip ? e.Color : Color.white, layer + 1)) // only a pet's number and a burn chip are tinted
                DrawPlainText(e.Text, e.Color, cx, cy, height * scale * px, alpha);
            if (e.Kind == DamageNumberKind.Crit) DrawCritFront(cx, cy, t, height * px, textW, e.Seed, alpha, layer + 2);
            else if (e.Kind == DamageNumberKind.Heal) DrawSparks(cx, cy, t, height * px, e.Seed, alpha, 2, 0.7f, layer + 2);
        }
    }
}
