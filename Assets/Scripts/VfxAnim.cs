using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Pooled one-shot player for a painted VFX row (cast swirl, impact). Normal alpha-blend sprite for the base (keeps the colour on snow) plus a faint
    /// additive copy so the white cores feed the bloom. Runs on real time so it keeps playing through the hit-stop freeze.
    /// </summary>
    public class VfxAnim : MonoBehaviour
    {
        const float CastFps = 26f, ImpactFps = 16f;
        public const float FinisherScale = 1.35f;
        static readonly List<VfxAnim> Pool = new List<VfxAnim>();
        static Transform root;

        SpriteRenderer sr, glow;
        Sprite[] frames;
        float age, life, fps, fadeTail;

        public static void Init(Transform parent) { root = parent; Pool.Clear(); }

        /// <summary>Cast swirl / muzzle blast at the staff tip or barrel. angle only matters for oriented sprites (mechanic muzzle, 0 = right).</summary>
        public static void Cast(VfxSet set, Vector2 at, float angle, bool finisher)
        {
            Cast(set, at, angle, finisher, FeelConfig.FxOrder - 7);
        }

        /// <summary>Same with an explicit sorting order (e.g. behind the caster: GameConfig.OrderBase(feet.y) - 1).</summary>
        public static void Cast(VfxSet set, Vector2 at, float angle, bool finisher, int order)
        {
            Play(set.Rows[VfxSet.Cast], at, angle, (finisher ? FinisherScale : 1f), CastFps, order);
        }

        /// <summary>Impact at the hit point, never rotated (the cleric pillar stays upright).</summary>
        public static void Impact(VfxSet set, Vector2 at, bool finisher)
        {
            Play(set.Rows[VfxSet.Impact], at, 0f, (finisher ? FinisherScale : 1f), ImpactFps, FeelConfig.FxOrder + 2);
        }

        /// <summary>
        /// The set's blast row (Mechanic v2 cannon blast) at the burst point, sized so its widest frame spans the blast diameter x BlastFill.
        /// False when the set has no blast row (the caller plays its impact instead).
        /// </summary>
        public static bool Blast(VfxSet set, Vector2 at, float radius)
        {
            Sprite[] row = set.Rows.Length > VfxSet.Blast ? set.Rows[VfxSet.Blast] : null;
            if (row == null || radius <= 0f) return false;
            float widest = 0f;
            foreach (Sprite s in row) widest = Mathf.Max(widest, s.rect.width / s.pixelsPerUnit);
            Play(row, at, 0f, 2f * radius * BlastFill / Mathf.Max(1f, widest), BlastFps, FeelConfig.FxOrder + 2);
            return true;
        }

        const float BlastFps = 12f, BlastFill = 0.85f; // widest frame (fire + rocks) x0.85 of the blast diameter (r24: ~41 units); a full-size fireball covered the hero next to the target

        /// <summary>Additive glow copy alpha: faint so the white cores feed the bloom without washing out on snow.</summary>
        public static float GlowAlpha { get { return FeelConfig.PaintedGlowAlpha; } }

        static void Play(Sprite[] frames, Vector2 at, float angle, float scale, float fps, int order)
        {
            if (root == null || frames == null) return;
            VfxAnim a = null;
            for (int i = 0; i < Pool.Count; i++) if (!Pool[i].gameObject.activeSelf) { a = Pool[i]; break; }
            if (a == null)
            {
                var go = new GameObject("VfxAnim");
                go.transform.SetParent(root, false);
                a = go.AddComponent<VfxAnim>();
                a.sr = go.AddComponent<SpriteRenderer>();
                a.glow = ProceduralSprites.Child(go.transform, "Glow", null);
                a.glow.sharedMaterial = FeelMaterials.Additive;
                Pool.Add(a);
            }
            a.frames = frames; a.fps = fps; a.age = 0f; a.life = frames.Length / fps; a.fadeTail = 0.5f / fps;
            a.sr.sortingOrder = order; a.glow.sortingOrder = order + 1;
            a.transform.position = new Vector3(at.x, at.y, 0f);
            a.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            a.transform.localScale = new Vector3(scale, scale, 1f);
            a.gameObject.SetActive(true);
            a.Apply();
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= life) { gameObject.SetActive(false); return; }
            Apply();
        }

        void Apply()
        {
            Sprite s = frames[Mathf.Min(frames.Length - 1, (int)(age * fps))];
            float fade = Mathf.Clamp01((life - age) / fadeTail); // last half frame fades out
            sr.sprite = glow.sprite = s;
            sr.color = new Color(1f, 1f, 1f, fade);
            glow.enabled = FeelConfig.FxOn;
            glow.color = new Color(1f, 1f, 1f, GlowAlpha * fade);
        }
    }
}
