using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Pooled additive expanding ring (shockwave / frost ring). squash &lt; 1 flattens it onto the ground plane.</summary>
    public class ShockRing : MonoBehaviour
    {
        static readonly List<ShockRing> Pool = new List<ShockRing>();
        static Transform root;
        static Material normal;
        SpriteRenderer sr;
        float age, life, radius, squash, startFrac;
        Color color;

        public static void Init(Transform parent) { root = parent; Pool.Clear(); }

        /// <summary>additive = false uses normal alpha blending: saturated colour that stays visible on bright snow (additive washes out to white). Ring growing from startFrac*radius to radius over lifeSec; order = sorting order (ground FX use the ground range).</summary>
        public static void Spawn(Vector2 pos, float radius, float lifeSec, Color color, int order, float squash = 0.72f, float startFrac = 0.2f, bool additive = false)
        {
            if (root == null || !FeelConfig.FxOn) return;
            ShockRing r = null;
            for (int i = 0; i < Pool.Count; i++) if (!Pool[i].gameObject.activeSelf) { r = Pool[i]; break; }
            if (r == null)
            {
                var go = new GameObject("ShockRing");
                go.transform.SetParent(root, false);
                r = go.AddComponent<ShockRing>();
                r.sr = go.AddComponent<SpriteRenderer>();
                r.sr.sprite = FxSprites.Ring();
                Pool.Add(r);
            }
            r.age = 0f; r.life = lifeSec; r.radius = radius; r.squash = squash; r.startFrac = startFrac; r.color = color;
            if (normal == null) normal = new Material(Shader.Find("Sprites/Default"));
            r.sr.sharedMaterial = additive ? FeelMaterials.Additive : normal;
            r.sr.sortingOrder = order;
            r.transform.position = new Vector3(pos.x, pos.y, 0f);
            r.gameObject.SetActive(true);
            r.Apply();
        }

        void Update()
        {
            age += Time.deltaTime; // real time: rings keep expanding through the hit-stop freeze
            if (age >= life) { gameObject.SetActive(false); return; }
            Apply();
        }

        void Apply()
        {
            float t = age / life;
            float e = 1f - (1f - t) * (1f - t) * (1f - t);
            float d = radius * 2f * Mathf.Lerp(startFrac, 1f, e);
            transform.localScale = new Vector3(d, d * squash, 1f);
            sr.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
        }
    }
}
