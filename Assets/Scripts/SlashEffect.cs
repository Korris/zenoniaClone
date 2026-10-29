using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Anime slash crescent (warrior-slashes-chibi.png recoloured to the warrior red palette: row 0 = normal slash, row 1 = finisher arc). Pooled.</summary>
    public class SlashEffect : MonoBehaviour
    {
        static readonly List<SlashEffect> Pool = new List<SlashEffect>();
        static Transform root;
        static Sprite[][] frames;
        SpriteRenderer sr, core; // core = additive copy of the same frame (bright centre, feeds the bloom)
        bool finisherLook;
        Sprite[] row;
        float age, life;

        public static void Init(Transform parent)
        {
            root = parent;
            Pool.Clear();
            // 4 columns x 2 rows; each cell becomes a 1-unit sprite (scaled per use: slash 32u, arc 48u like the web FX strips).
            Texture2D tex = SpriteSheet.Load("Sprites/fx/warrior-slashes-chibi");
            frames = tex != null ? SpriteTint.Slice(SpriteTint.Colorize(tex, ClassPalette.Red.Tint), 4, 2, tex.width / 4, new Vector2(0.5f, 0.5f)) : null;
        }

        /// <summary>Spawns a slash. angleDeg: 0 = right, 90 = up (counter-clockwise).</summary>
        public static void Spawn(float x, float y, float angleDeg, float scale, bool finisher, bool flipY, int order)
        {
            if (frames == null || root == null) return;
            SlashEffect fx = null;
            for (int i = 0; i < Pool.Count; i++) if (!Pool[i].gameObject.activeSelf) { fx = Pool[i]; break; }
            if (fx == null)
            {
                var go = new GameObject("Slash");
                go.transform.SetParent(root, false);
                fx = go.AddComponent<SlashEffect>();
                fx.sr = go.AddComponent<SpriteRenderer>();
                fx.core = ProceduralSprites.Child(go.transform, "Core", null);
                fx.core.sharedMaterial = FeelMaterials.Additive;
                Pool.Add(fx);
            }
            fx.row = SpriteSheet.Row(frames, finisher ? 1 : 0);
            fx.age = 0f;
            fx.life = finisher ? 0.24f : 0.17f;
            float size = (finisher ? 48f : 32f) * scale;
            fx.transform.position = new Vector3(x, y, 0f);
            fx.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            fx.transform.localScale = new Vector3(size, flipY ? -size : size, 1f);
            fx.sr.sortingOrder = order;
            fx.core.sortingOrder = order + 1;
            fx.finisherLook = finisher;
            fx.gameObject.SetActive(true);
            fx.Apply();
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= life) { gameObject.SetActive(false); return; }
            Apply();
        }

        void Apply()
        {
            if (row == null || row.Length == 0) { sr.sprite = null; return; }
            int i = Mathf.Min(row.Length - 1, Mathf.FloorToInt(age / life * row.Length));
            sr.sprite = row[i];
            float fade = Mathf.Clamp01((life - age) / (life * 0.35f));
            sr.color = new Color(1f, 1f, 1f, fade);
            core.enabled = FeelConfig.FxOn;
            core.sprite = sr.sprite;
            Color g = ClassPalette.Red.Glow;
            core.color = new Color(g.r, g.g, g.b, fade * (finisherLook ? 1f : 0.85f));
        }
    }
}
