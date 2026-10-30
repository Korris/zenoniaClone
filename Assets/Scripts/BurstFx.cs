using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Pooled one-shot FX sprite from the web class FX art (Sprites/fx/&lt;class&gt;/&lt;name&gt;.png, 4 source px per world unit):
    /// muzzle flash, arcane burst, explosion. A normal sprite plus an additive copy (pushed above 1.0 so it feeds the bloom).
    /// </summary>
    public class BurstFx : MonoBehaviour
    {
        public const float SourcePpu = 4f;
        static readonly List<BurstFx> Pool = new List<BurstFx>();
        static Transform root;
        static readonly Dictionary<string, Sprite> Tinted = new Dictionary<string, Sprite>();
        SpriteRenderer sr, core;
        Color coreTint;
        float age, life, baseScale;
        bool grows;

        public static void Init(Transform parent) { root = parent; Pool.Clear(); Tinted.Clear(); }

        /// <summary>FX sprite (pivot centre, 4 px per unit); null when the PNG is missing.</summary>
        public static Sprite Art(string folder, string name, Color? tint = null)
        {
            Sprite plain = SpriteSheet.Whole("Sprites/fx/" + folder + "/" + name, SourcePpu, new Vector2(0.5f, 0.5f));
            if (plain == null || !tint.HasValue) return plain;
            string key = folder + "/" + name + "/" + ColorUtility.ToHtmlStringRGB(tint.Value);
            Sprite s;
            if (!Tinted.TryGetValue(key, out s))
            {
                Texture2D tex = SpriteTint.Colorize(plain.texture, tint.Value); // recoloured copy (web tinted-sprite-cache), built once per art + colour
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), SourcePpu, 0, SpriteMeshType.FullRect);
                Tinted[key] = s;
            }
            return s;
        }

        /// <summary>Plays the FX once. grow = scales up from 55% (blast), otherwise static (flash).</summary>
        public static void Spawn(string folder, string name, Vector2 pos, float angleDeg, float scale, float lifeSec, bool grow = false, Color? tint = null)
        {
            if (!NetMode.RendersFx) return; // headless server: no cosmetics
            Sprite art = folder != null ? Art(folder, name, tint) : null;
            if (art == null || root == null) return;
            BurstFx fx = null;
            for (int i = 0; i < Pool.Count; i++) if (!Pool[i].gameObject.activeSelf) { fx = Pool[i]; break; }
            if (fx == null)
            {
                var go = new GameObject("BurstFx");
                go.transform.SetParent(root, false);
                fx = go.AddComponent<BurstFx>();
                fx.sr = go.AddComponent<SpriteRenderer>();
                fx.core = ProceduralSprites.Child(go.transform, "Core", null);
                fx.core.sharedMaterial = FeelMaterials.Additive;
                Pool.Add(fx);
            }
            fx.sr.sprite = art;
            fx.core.sprite = art;
            fx.sr.sortingOrder = FeelConfig.FxOrder - 5;
            fx.core.sortingOrder = FeelConfig.FxOrder - 4;
            fx.coreTint = tint.HasValue ? new Color(Mathf.Lerp(tint.Value.r, 1f, 0.15f), tint.Value.g, tint.Value.b) : new Color(1f, 0.85f, 0.7f);
            fx.age = 0f; fx.life = lifeSec; fx.baseScale = scale; fx.grows = grow;
            fx.transform.position = new Vector3(pos.x, pos.y, 0f);
            fx.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            fx.gameObject.SetActive(true);
            fx.Apply();
        }

        void Update()
        {
            age += Time.deltaTime; // real time: the blast keeps animating through the hit-stop freeze
            if (age >= life) { gameObject.SetActive(false); return; }
            Apply();
        }

        void Apply()
        {
            float t = age / life;
            float s = baseScale * (grows ? Mathf.Lerp(0.55f, 1f, 1f - (1f - t) * (1f - t)) : 1f);
            transform.localScale = new Vector3(s, s, 1f);
            float fade = Mathf.Clamp01((1f - t) / 0.45f);
            sr.color = new Color(1f, 1f, 1f, fade);
            core.enabled = FeelConfig.FxOn;
            core.color = new Color(coreTint.r, coreTint.g, coreTint.b, fade * (grows ? 0.22f : 0.6f)); // blasts are big and already bright: keep the additive copy gentle
        }
    }
}
