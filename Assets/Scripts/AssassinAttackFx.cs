using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Assassin basic-attack FX painted per the approved board docs/reference/assassin-basic-attack-board.png (2026-11-08):
    /// hit 1 Crescent Cut = crescent slash, hit 2 Rising Fang = rising arc, hit 3 Cross Rend = X cross, hit 4 Shadow Lunge = stab streak per stab
    /// with a hit spark at its tip. Art: Resources/Sprites/classes/assassin/fx/attack-v2/ROW_N.png (4 frames per row, per-frame pivot in
    /// Data/classes/assassin/vfx-attack-v2.json; cut by Tools/build-assassin-attack-fx-v2.py). Arcs are drawn facing right (pivot = arc centre),
    /// the streak points right from its tail (pivot = tail). Each row plays over the hit's active time plus half its recovery
    /// (ComboConfigs.AssassinHits), so the art lasts as long as the swing it belongs to. Real time, so it keeps playing through hit-stop.
    /// DaggerFx.Swing falls back to the old violet crescents when this art is missing.
    /// </summary>
    public class AssassinAttackFx : MonoBehaviour
    {
        const string Folder = "Sprites/classes/assassin/fx/attack-v2/", Json = "Data/classes/assassin/vfx-attack-v2";
        const float UnitsPerPx = 0.058f;                 // same scale as VfxSprites
        const float ArcSpan = 1.5f, StabSpan = 1.6f;     // widest arc frame = 1.5 x the hit's reach; streak length = 1.6 x reach
        const float SparkSize = 20f;                     // hit spark ~20 units wide (hero ~20 tall)
        const float StabLifeMs = 120f;                   // one stab: the 90 ms gap to the second stab + a little
        static readonly string[] RowNames = { "slash", "rise", "cross", "stab", "spark" };
        public const int Slash = 0, Rise = 1, Cross = 2, Stab = 3, Spark = 4;

        [Serializable] class Frame { public string name; public int w, h; public float px, py; }
        [Serializable] class Meta { public Frame[] frames; }

        static Sprite[][] rows;
        static bool loaded;
        static readonly List<AssassinAttackFx> Pool = new List<AssassinAttackFx>();
        static Transform root;

        SpriteRenderer sr, glow;
        Sprite[] frames;
        float age, life;

        public static void Init(Transform parent) { root = parent; Pool.Clear(); Load(); } // load the art up front so the first swing does not hitch

        /// <summary>True when every row loaded (callers fall back to the procedural crescents otherwise).</summary>
        public static bool Ready { get { Load(); return rows != null; } }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                TextAsset json = Resources.Load<TextAsset>(Json);
                if (json == null) { Debug.LogWarning("[ZenoniaProto] Missing Resources/" + Json + ".json, Assassin uses the old slash FX"); return; }
                Meta meta = JsonUtility.FromJson<Meta>(json.text);
                var r = new Sprite[RowNames.Length][];
                for (int i = 0; i < r.Length; i++) r[i] = new Sprite[4];
                foreach (Frame f in meta.frames)
                {
                    string[] parts = f.name.Split('_');
                    int row = Array.IndexOf(RowNames, parts[0]), n = int.Parse(parts[1]);
                    Texture2D tex = SpriteSheet.Load(Folder + f.name);
                    if (row < 0 || n < 0 || n > 3 || tex == null) return;
                    r[row][n] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(f.px / tex.width, f.py / tex.height), 1f / UnitsPerPx, 0, SpriteMeshType.FullRect);
                }
                foreach (Sprite[] row in r) if (!Array.TrueForAll(row, s => s != null)) return;
                rows = r;
            }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] Assassin attack FX failed: " + e.Message); }
        }

        /// <summary>Loads the art before the first swing so it does not hitch.</summary>
        public static void Preload() { Load(); }

        /// <summary>
        /// One combo swing at the hit box centre. angle = facing (deg, 0 = right, counter-clockwise); echo = the second stab of hit 4.
        /// Facing left the arcs are mirrored top-to-bottom so the rising arc still rises.
        /// </summary>
        public static void Swing(Vector2 center, float angle, ComboHit h, int step, bool finisher, bool echo)
        {
            if (root == null || !Ready) return;
            bool left = Mathf.Abs(Mathf.DeltaAngle(angle, 180f)) < 45f;
            float lifeMs = h.ActiveMs + 0.5f * h.RecoverMs;
            if (finisher)
            {
                Vector2 fwd = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)), side = new Vector2(-fwd.y, fwd.x);
                Vector2 tail = center - fwd * (h.Reach * 0.55f) + side * (echo ? -3f : 3f);
                Play(rows[Stab], tail, angle, Fit(rows[Stab], h.Reach * StabSpan), StabLifeMs, left, FeelConfig.FxOrder + 1);
                Play(rows[Spark], center + fwd * (h.Reach * 0.3f) + side * (echo ? -3f : 3f), 0f, Fit(rows[Spark], SparkSize), StabLifeMs * 1.3f, false, FeelConfig.FxOrder + 2);
                return;
            }
            int row = step == 2 ? Rise : step == 3 ? Cross : Slash;
            Play(rows[row], center, angle, Fit(rows[row], h.Reach * ArcSpan * h.FxScale / 1.2f), lifeMs, left, FeelConfig.FxOrder);
        }

        /// <summary>Scale that makes the widest frame of a row `units` wide.</summary>
        static float Fit(Sprite[] row, float units)
        {
            float widest = 1f;
            foreach (Sprite s in row) widest = Mathf.Max(widest, s.rect.width / s.pixelsPerUnit);
            return units / widest;
        }

        static void Play(Sprite[] frames, Vector2 at, float angle, float scale, float lifeMs, bool flipY, int order)
        {
            AssassinAttackFx a = null;
            for (int i = 0; i < Pool.Count; i++) if (!Pool[i].gameObject.activeSelf) { a = Pool[i]; break; }
            if (a == null)
            {
                var go = new GameObject("AssassinAttackFx");
                go.transform.SetParent(root, false);
                a = go.AddComponent<AssassinAttackFx>();
                a.sr = go.AddComponent<SpriteRenderer>();
                a.glow = ProceduralSprites.Child(go.transform, "Glow", null);
                a.glow.sharedMaterial = FeelMaterials.Additive;
                Pool.Add(a);
            }
            a.frames = frames; a.age = 0f; a.life = Mathf.Max(0.05f, lifeMs / 1000f);
            a.sr.sortingOrder = order; a.glow.sortingOrder = order + 1;
            a.transform.position = new Vector3(at.x, at.y, 0f);
            a.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            a.transform.localScale = new Vector3(scale, flipY ? -scale : scale, 1f);
            a.gameObject.SetActive(true);
            a.Apply();
        }

        void Update()
        {
            age += Time.unscaledDeltaTime;
            if (age >= life) { gameObject.SetActive(false); return; }
            Apply();
        }

        void Apply()
        {
            int n = frames.Length;
            Sprite s = frames[Mathf.Min(n - 1, (int)(age / life * n))];
            float fade = Mathf.Clamp01((life - age) / (0.5f * life / n)); // the last half frame fades out
            sr.sprite = glow.sprite = s;
            sr.color = new Color(1f, 1f, 1f, fade);
            glow.enabled = FeelConfig.FxOn;
            glow.color = new Color(1f, 1f, 1f, FeelConfig.PaintedGlowAlpha * fade);
        }
    }
}
