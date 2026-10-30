using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Pooled painted-frame player for skill art (cast / loop / impact rows). One-shot by default; `looping` keeps cycling until Stop().
    /// Plain normal-blend sprite plus a faint additive copy so bright cores feed the bloom (same look as VfxAnim).
    /// </summary>
    public sealed class SkillFxAnim : MonoBehaviour
    {
        static readonly List<SkillFxAnim> Pool = new List<SkillFxAnim>();
        static Transform root;
        SpriteRenderer sr, glow;
        Sprite[] frames;
        float age, fps, life;
        bool looping, stopped;

        public static void Init(Transform parent) { root = parent; Pool.Clear(); }

        /// <summary>Starts an animation at `at`; `life` &lt;= 0 plays the frames once, `looping` cycles for `life` seconds (or until Stop).</summary>
        public static SkillFxAnim Play(Sprite[] frames, float fps, Vector2 at, float angleDeg, float scale, int order, bool looping = false, float life = 0f)
        {
            if (frames == null || frames.Length == 0 || !NetMode.RendersFx || !FeelConfig.FxOn) return null;
            if (root == null) root = Zone.Main != null ? Zone.Main.Root : null;
            if (root == null) return null;
            SkillFxAnim a = null;
            for (int i = 0; i < Pool.Count; i++) if (Pool[i] == null) { Pool.RemoveAt(i--); } else if (!Pool[i].gameObject.activeSelf) { a = Pool[i]; break; }
            if (a == null)
            {
                var go = new GameObject("SkillFxAnim");
                go.transform.SetParent(root, false);
                a = go.AddComponent<SkillFxAnim>();
                a.sr = go.AddComponent<SpriteRenderer>();
                a.glow = ProceduralSprites.Child(go.transform, "Glow", null);
                a.glow.sharedMaterial = FeelMaterials.Additive;
                Pool.Add(a);
            }
            a.frames = frames; a.fps = Mathf.Max(1f, fps); a.age = 0f; a.looping = looping; a.stopped = false;
            a.life = looping ? (life > 0f ? life : 60f) : frames.Length / a.fps;
            a.sr.sortingOrder = order; a.glow.sortingOrder = order + 1;
            a.transform.position = new Vector3(at.x, at.y, 0f);
            a.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            a.transform.localScale = new Vector3(scale, scale, 1f);
            a.gameObject.SetActive(true);
            a.Apply();
            return a;
        }

        /// <summary>Moves a looping animation (projectile body, attached zone).</summary>
        public void MoveTo(Vector2 at, float angleDeg)
        {
            transform.position = new Vector3(at.x, at.y, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
        }

        /// <summary>Ends the animation immediately (a projectile that hit, a zone that expired).</summary>
        public void Stop() { stopped = true; }

        void Update()
        {
            age += GameClock.Dt;
            if (age >= life || stopped) { gameObject.SetActive(false); return; }
            Apply();
        }

        void Apply()
        {
            int n = frames.Length, f = looping ? (int)(age * fps) % n : Mathf.Min(n - 1, (int)(age * fps));
            sr.sprite = frames[f];
            glow.sprite = frames[f];
            float tail = looping ? Mathf.Clamp01((life - age) * 4f) : Mathf.Clamp01((life - age) * fps * 2f);
            sr.color = new Color(1f, 1f, 1f, tail);
            glow.color = new Color(1f, 1f, 1f, tail * FeelConfig.PaintedGlowAlpha);
        }
    }
}
