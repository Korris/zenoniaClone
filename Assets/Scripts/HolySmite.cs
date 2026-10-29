using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Cleric impact (ranged-attack-fx-reference.png): a tall vertical GOLD light pillar, 4-point star flares, green crystal shards bursting and a
    /// green ground halo ring. Hits use one pillar; the finisher adds two side pillars and a wider halo. Purely visual (the shot already dealt damage).
    /// </summary>
    public class HolySmite : MonoBehaviour
    {
        const float LifeSec = 0.4f, PillarHeight = 70f;
        static readonly List<HolySmite> Pool = new List<HolySmite>();
        static Transform root;
        static readonly Color Gold = new Color(1f, 0.88f, 0.32f);

        SpriteRenderer pillar, core, cross;
        Vector2 pos;
        float age, width, crossSize, delay;

        public static void Init(Transform parent) { root = parent; Pool.Clear(); }

        /// <summary>Plays the gold pillar impact at pos. big = finisher (bigger, three pillars).</summary>
        public static void Impact(Vector2 pos, bool big)
        {
            if (root == null || !FeelConfig.FxOn) return;
            ClassPalette p = ClassPalette.Green;
            Add(pos, 0f, big ? 6f : 4.5f, big ? 26f : 18f);
            if (big)
            {
                Add(pos + new Vector2(-12f, -3f), 0.05f, 3.2f, 14f);
                Add(pos + new Vector2(12f, -3f), 0.09f, 3.2f, 14f);
                for (int i = -2; i <= 2; i++) DaggerFx.Ray(pos + new Vector2(0f, 4f), 90f + i * 20f, 40f, 2.2f, 0.3f, p.Mid, Gold);
            }
            ShockRing.Spawn(pos, big ? 34f : 18f, big ? 0.55f : 0.4f, p.Mid, UltimateFx.GroundFxOrder + 9, 0.72f, 0.2f); // green ground halo
            ShockRing.Spawn(pos, big ? 20f : 11f, 0.32f, Gold, UltimateFx.GroundFxOrder + 10, 0.72f, 0.3f);
            UltimateFx.Shards(pos + new Vector2(0f, 2f), big ? 20 : 12, 95f, new Color(0.45f, 1f, 0.55f, 1f)); // green crystal shards
            UltimateFx.Embers(pos, big ? 12 : 7, 8f, 28f, new Color(0.75f, 1f, 0.45f, 1f));                      // leaf-like motes drifting up
            UltimateFx.Sparkles(pos + new Vector2(0f, 6f), 6, Gold);
        }

        static void Add(Vector2 pos, float delay, float width, float cross)
        {
            HolySmite s = null;
            for (int i = 0; i < Pool.Count; i++) if (!Pool[i].gameObject.activeSelf) { s = Pool[i]; break; }
            if (s == null)
            {
                var go = new GameObject("HolyPillar");
                go.transform.SetParent(root, false);
                s = go.AddComponent<HolySmite>();
                s.pillar = ProceduralSprites.Child(go.transform, "Pillar", FxSprites.Pillar());
                s.core = ProceduralSprites.Child(go.transform, "Core", FxSprites.Pillar());
                s.core.sharedMaterial = FeelMaterials.Additive;
                s.cross = ProceduralSprites.Child(go.transform, "Cross", FxSprites.Star());
                s.cross.sharedMaterial = FeelMaterials.Additive;
                Pool.Add(s);
            }
            s.pos = pos; s.age = -delay; s.delay = delay; s.width = width; s.crossSize = cross;
            s.pillar.sortingOrder = FeelConfig.FxOrder + 1; s.core.sortingOrder = FeelConfig.FxOrder + 2; s.cross.sortingOrder = FeelConfig.FxOrder + 3;
            s.transform.position = new Vector3(pos.x, pos.y, 0f);
            s.gameObject.SetActive(true);
            s.Apply();
        }

        void Update()
        {
            age += Time.deltaTime; // real time so the pillar plays through the hit-stop freeze
            if (age >= LifeSec) { gameObject.SetActive(false); return; }
            Apply();
        }

        void Apply()
        {
            bool on = age >= 0f;
            pillar.enabled = core.enabled = cross.enabled = on;
            if (!on) return;
            float t = age / LifeSec;
            float rise = Mathf.Clamp01(age / 0.06f);                   // shoots up fast
            float fade = t < 0.25f ? 1f : 1f - (t - 0.25f) / 0.75f;
            float w = width * (1f - 0.55f * t);                        // and thins out
            // pillar sprite is 4 units tall at scale 1 (FxSprites.Pillar)
            pillar.transform.localScale = new Vector3(w * 2.4f, PillarHeight * rise / 4f, 1f);
            core.transform.localScale = new Vector3(w * 0.9f, PillarHeight * rise / 4f, 1f);
            pillar.color = new Color(Gold.r, Gold.g, Gold.b, 0.8f * fade);
            core.color = new Color(1f, 1f, 0.85f, fade);
            float cs = crossSize * Mathf.Lerp(1.4f, 0.5f, t);
            cross.transform.position = new Vector3(pos.x, pos.y + 5f, 0f);
            cross.transform.localScale = new Vector3(cs, cs, 1f);
            cross.color = new Color(1f, 0.95f, 0.6f, fade);
        }
    }
}
