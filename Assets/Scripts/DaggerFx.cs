using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Assassin basic-attack FX, deliberately unlike the warrior crescent and the wizard orbs: quick thin twin cross-slashes (an X), short sharp
    /// afterimage lines, dark violet shadow wisps and a fast star-burst; the double-stab finisher adds two piercing thrust streaks and a bigger X flash.
    /// Every line is a tapered diamond sprite (violet body + white-hot additive core) drawn by one pooled manager.
    /// </summary>
    public class DaggerFx : MonoBehaviour
    {
        sealed class Line
        {
            public GameObject Go; public SpriteRenderer Body, Core;
            public Vector2 Pos; public float Angle, Length, Width, Life, Age, StartFrac, Slide;
            public Color Edge, Hot;
        }

        const int PoolMax = 40;
        static readonly List<Line> All = new List<Line>();
        static Transform root;
        static Sprite diamond;

        public static void Init(Transform parent)
        {
            root = parent;
            All.Clear();
            var go = new GameObject("DaggerFx");
            go.transform.SetParent(parent, false);
            go.AddComponent<DaggerFx>();
            Texture2D t = FxSprites.ShardTexture;
            diamond = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), t.width, 0, SpriteMeshType.FullRect);
        }

        /// <summary>One melee dagger hit at pos. angle = facing (deg, 0 = right, 90 = up); echo = second stab of the finisher.</summary>
        public static void Strike(Vector2 pos, float angle, float reach, bool finisher, bool echo, int step)
        {
            if (root == null || !FeelConfig.FxOn) return;
            ClassPalette p = ClassPalette.Violet;
            float len = reach * (finisher ? 1.9f : 1.5f);
            float spread = finisher ? 38f : 30f;
            float flip = (step % 2 == 0) != echo ? -1f : 1f; // alternate which diagonal leads
            // twin cross-slashes: two thin lines crossing at the hit point
            Add(pos, angle + spread * flip, len, finisher ? 4.4f : 3.2f, 0.16f, 0.35f, 0f, p.Edge, Color.white);
            Add(pos, angle - spread * flip, len, finisher ? 4.4f : 3.2f, 0.16f, 0.35f, 0.02f, p.Mid, Color.white);
            // short afterimage speed lines trailing behind the strike
            Vector2 fwd = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)), side = new Vector2(-fwd.y, fwd.x);
            for (int i = -1; i <= 1; i++)
                Add(pos - fwd * (len * 0.45f) + side * (i * 4f), angle, len * 0.55f, 1.1f, 0.2f, 0.4f, 0f, p.Mid, p.Core);
            if (finisher)
            {
                // two piercing thrust streaks shooting forward past the target, plus a bigger X flash
                for (int i = -1; i <= 1; i += 2)
                    Add(pos + side * (i * 3f) - fwd * 6f, angle, len * 1.2f, 3f, 0.18f, 0.2f, 0f, p.Edge, Color.white, 70f);
                Add(pos, angle + 45f, len * 1.15f, 4f, 0.2f, 0.5f, 0.03f, p.Mid, Color.white);
                Add(pos, angle - 45f, len * 1.15f, 4f, 0.2f, 0.5f, 0.03f, p.Mid, Color.white);
            }
            UltimateFx.Dust(pos, finisher ? 5 : 3, 5f, p.Wisp);
            UltimateFx.Embers(pos, finisher ? 22 : 12, 3f, 18f, new Color(p.Glow.r, p.Glow.g, p.Glow.b, 1f));
        }

        /// <summary>Public one-off light ray (wizard finisher): tapered line from pos along angle with a white-hot core.</summary>
        public static void Ray(Vector2 pos, float angle, float length, float width, float life, Color edge, Color hot) { Add(pos, angle, length, width, life, 0.3f, 0f, edge, hot); }

        static void Add(Vector2 pos, float angle, float length, float width, float life, float startFrac, float delay, Color edge, Color hot, float slide = 0f)
        {
            Line l = null;
            for (int i = 0; i < All.Count; i++) if (All[i].Age >= All[i].Life) { l = All[i]; break; }
            if (l == null)
            {
                if (All.Count >= PoolMax) return;
                l = new Line { Go = new GameObject("Line"), Life = 0f, Age = 1f };
                l.Go.transform.SetParent(root, false);
                l.Body = l.Go.AddComponent<SpriteRenderer>();
                l.Body.sprite = diamond;
                l.Core = ProceduralSprites.Child(l.Go.transform, "Core", diamond);
                l.Core.sharedMaterial = FeelMaterials.Additive;
                All.Add(l);
            }
            l.Pos = pos; l.Angle = angle; l.Length = length; l.Width = width; l.Life = life; l.Age = -delay; l.StartFrac = startFrac; l.Slide = slide;
            l.Edge = edge; l.Hot = hot;
            l.Body.sortingOrder = FeelConfig.FxOrder + 1;
            l.Core.sortingOrder = FeelConfig.FxOrder + 2;
            Draw(l);
        }

        void Update()
        {
            for (int i = 0; i < All.Count; i++)
            {
                Line l = All[i];
                if (l.Age >= l.Life) continue;
                l.Age += Time.deltaTime; // real time so the lines keep flashing through hit-stop
                if (l.Age >= l.Life) { l.Go.SetActive(false); continue; }
                Draw(l);
            }
        }

        static void Draw(Line l)
        {
            if (l.Age < 0f) { l.Go.SetActive(false); return; }
            float t = Mathf.Clamp01(l.Age / l.Life);
            float grow = Mathf.Lerp(l.StartFrac, 1f, 1f - (1f - t) * (1f - t) * (1f - t));
            float fade = t < 0.4f ? 1f : 1f - (t - 0.4f) / 0.6f;
            float rad = l.Angle * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            l.Go.SetActive(true);
            l.Go.transform.position = (Vector3)(l.Pos + dir * (l.Slide * t));
            l.Go.transform.rotation = Quaternion.Euler(0f, 0f, l.Angle);
            // the diamond is ~0.31 of its height tall at the widest point: scale y by width / 0.31 for the wanted world thickness
            l.Go.transform.localScale = new Vector3(l.Length * grow, l.Width / 0.31f * (1f - 0.5f * t), 1f);
            l.Body.color = new Color(l.Edge.r, l.Edge.g, l.Edge.b, fade);
            l.Core.transform.localScale = new Vector3(1f, 0.45f, 1f);
            l.Core.color = new Color(l.Hot.r, l.Hot.g, l.Hot.b, fade);
        }
    }
}
