using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Assassin basic-attack FX: violet crescent and ribbon arcs shaped per combo hit (see Swing), and straight thrust streaks for the stabs.
    /// Each stab line is a tapered diamond sprite (violet body + white-hot additive core) drawn by one pooled manager.
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

        const string VioletHex = "#6a3fc4"; // deep violet, not pushed to full saturation (a hot #9a4dff bloomed pink, 2026-11-03), white core kept (SlashEffect classTint)

        /// <summary>
        /// Assassin combo swing: the Warrior's painted crescent in violet (user, 2026-11-03: "giống warrior"), one placement per hit to match the
        /// sprites (Tools/build-assassin-new-style.py): 1 forehand slash across the front; 2 rising backhand, the arc turned upward;
        /// 3 jumping X chop, two crescents crossing; 4 dash double stab, one narrow crescent per stab, the second mirrored.
        /// angle = facing (deg, 0 = right, 90 = up, counter-clockwise); echo = the second stab of hit 4.
        /// </summary>
        public static void Swing(Vector2 center, Vector2 feet, float angle, ComboHit h, int step, bool finisher, bool echo)
        {
            if (root == null) return;
            if (AssassinAttackFx.Ready) { AssassinAttackFx.Swing(center, angle, h, step, finisher, echo); return; } // painted board FX (2026-11-08); below = the old crescents
            if (finisher) { Slash(center, angle, h.FxScale * 1.15f, echo); return; }
            if (step == 2) { Slash(center, angle + 35f, h.FxScale, true); return; } // tilted up: the left hand rises from low to high
            if (step == 3)
            {
                Slash(center, angle - 30f, h.FxScale, false);
                Slash(center, angle + 30f, h.FxScale, true);
                return;
            }
            Slash(center, angle, h.FxScale, false);
        }

        const float SlashSize = 0.8f; // daggers reach less than the sword: a smaller crescent than the Warrior's

        static void Slash(Vector2 at, float angle, float scale, bool mirror)
        {
            SlashEffect.Spawn(at.x, at.y, angle, scale * SlashSize, false, mirror, FeelConfig.FxOrder, VioletHex, true);
        }

        /// <summary>One stab of the dash finisher: a broad straight streak shooting forward through the target, the second one beside the first.</summary>
        public static void Stab(Vector2 pos, float angle, float reach, bool second)
        {
            if (root == null || !FeelConfig.FxOn) return;
            Vector2 fwd = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)), side = new Vector2(-fwd.y, fwd.x);
            ClassPalette p = ClassPalette.Violet;
            Add(pos + side * (second ? -3f : 3f) - fwd * 8f, angle, reach * 2f, 5f, 0.16f, 0.25f, 0f, p.Edge, Color.white, 40f);
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
