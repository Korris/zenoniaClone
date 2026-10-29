using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Pooled arcane rune flash: a rotating magic circle (ring + counter-rotating inner ring) with an 8-point star, drawn flat (squash) or
    /// upright. Used for the wizard staff-tip cast flash, the hit starburst and the finisher's magic circle decal. Also hosts the
    /// wizard-specific effect recipes (CastFlash / Impact / FinisherBurst).
    /// </summary>
    public class RuneFx : MonoBehaviour
    {
        static readonly List<RuneFx> Pool = new List<RuneFx>();
        static Transform root;
        static Material normal;
        SpriteRenderer ring, inner, star;
        float age, life, size, squash, spin;
        bool rings;
        Color edge, hot;

        public static void Init(Transform parent) { root = parent; Pool.Clear(); }

        public static void Spawn(Vector2 pos, float diameter, float squash, float lifeSec, float spin, Color edge, Color hot, int order, bool rings = true)
        {
            if (root == null || !FeelConfig.FxOn) return;
            RuneFx r = null;
            for (int i = 0; i < Pool.Count; i++) if (!Pool[i].gameObject.activeSelf) { r = Pool[i]; break; }
            if (r == null)
            {
                var go = new GameObject("RuneFx");
                go.transform.SetParent(root, false);
                r = go.AddComponent<RuneFx>();
                if (normal == null) normal = new Material(Shader.Find("Sprites/Default"));
                r.ring = ProceduralSprites.Child(go.transform, "Ring", FxSprites.Ring()); r.ring.sharedMaterial = normal;
                r.inner = ProceduralSprites.Child(go.transform, "Inner", FxSprites.Ring()); r.inner.sharedMaterial = normal;
                r.star = ProceduralSprites.Child(go.transform, "Star", FxSprites.Star()); r.star.sharedMaterial = FeelMaterials.Additive;
                Pool.Add(r);
            }
            r.rings = rings;
            r.age = 0f; r.life = lifeSec; r.size = diameter; r.squash = squash; r.spin = spin; r.edge = edge; r.hot = hot;
            r.ring.sortingOrder = order; r.inner.sortingOrder = order + 1; r.star.sortingOrder = order + 2;
            r.transform.position = new Vector3(pos.x, pos.y, 0f);
            r.gameObject.SetActive(true);
            r.Apply();
        }

        void Update()
        {
            age += Time.deltaTime; // real time: runes keep spinning through the hit-stop
            if (age >= life) { gameObject.SetActive(false); return; }
            Apply();
        }

        void Apply()
        {
            float t = age / life;
            float grow = 1f - (1f - t) * (1f - t);
            float fade = t < 0.5f ? 1f : 1f - (t - 0.5f) / 0.5f;
            float d = size * Mathf.Lerp(0.55f, 1f, grow);
            ring.transform.localScale = new Vector3(d, d * squash, 1f);
            inner.transform.localScale = new Vector3(d * 0.66f, d * 0.66f * squash, 1f);
            star.transform.localScale = new Vector3(d * 0.9f, d * 0.9f * squash, 1f);
            ring.transform.localRotation = Quaternion.Euler(0f, 0f, age * spin);
            inner.transform.localRotation = Quaternion.Euler(0f, 0f, -age * spin * 1.4f);
            star.transform.localRotation = Quaternion.Euler(0f, 0f, age * spin * 0.6f);
            ring.enabled = inner.enabled = rings;
            ring.color = new Color(edge.r, edge.g, edge.b, fade);
            inner.color = new Color(edge.r, edge.g, edge.b, fade * 0.85f);
            star.color = new Color(hot.r, hot.g, hot.b, fade * 0.8f);
        }

        // ---- wizard recipes ----

        /// <summary>Small magic circle + rune burst at the staff tip when a bolt is cast (blue wizard, green cleric halo).</summary>
        public static void CastFlash(Vector2 tip, bool big, ClassPalette p)
        {
            Spawn(tip, big ? 18f : 12f, 1f, 0.22f, 360f, p.Mid, p.Core, FeelConfig.FxOrder - 8);
            Spawn(tip, big ? 11f : 7f, 1f, 0.3f, -520f, p.Edge, p.Core, FeelConfig.FxOrder - 7); // second ring swirling the other way
            UltimateFx.Sparkles(tip, big ? 9 : 5, p.Mid);
        }

        /// <summary>Bolt hit: blue starburst, expanding rune ring and shard sparks.</summary>
        public static void Impact(Vector2 pos, ClassPalette p)
        {
            Spawn(pos, 22f, 1f, 0.24f, 300f, p.Edge, p.Core, FeelConfig.FxOrder + 4);
            ShockRing.Spawn(pos, 13f, 0.28f, p.Mid, FeelConfig.FxOrder + 3, 1f, 0.3f);
            for (int i = 0; i < 6; i++) DaggerFx.Ray(pos, i * 60f + Random.Range(0f, 30f), Random.Range(12f, 20f), 1.8f, 0.22f, p.Mid, p.Core); // crystal spikes
            UltimateFx.Shards(pos, 14, 90f, Color.Lerp(p.Mid, Color.white, 0.4f));
            UltimateFx.Sparkles(pos, 6, p.Core);
        }

        /// <summary>Finisher: big arcane explosion with a rotating ground magic circle and 8 radial light rays.</summary>
        public static void FinisherBurst(Vector2 pos, float radius, ClassPalette p)
        {
            Spawn(pos + new Vector2(0f, -3f), radius * 2.1f, 0.72f, 0.7f, 140f, p.Edge, new Color(0.8f, 0.95f, 1f), UltimateFx.GroundFxOrder + 8); // circle decal on the ground
            Spawn(pos, radius * 1.1f, 1f, 0.3f, 420f, p.Mid, p.Core, FeelConfig.FxOrder + 4);                                                    // upright starburst
            ShockRing.Spawn(pos, radius * 1.5f, 0.45f, p.Edge, FeelConfig.FxOrder + 3, 1f, 0.25f);
            for (int i = 0; i < 8; i++) DaggerFx.Ray(pos, i * 45f + 22.5f, radius * 1.35f, 2.6f, 0.3f, p.Edge, new Color(0.9f, 1f, 1f));
            UltimateFx.Shards(pos, 26, 120f, Color.Lerp(p.Mid, Color.white, 0.4f));
            UltimateFx.Sparkles(pos, 14, p.Core);
        }
    }
}
