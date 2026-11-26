using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Archer arrow in flight (BoltStyle.Arrow, ComboConfigs.ArcherHits, 2026-11-26). The arrow itself is a solid object, so it is the painted
    /// arrow sprite cut from the V3 weapons sheet (Sprites/hero-layered/weapons/archer-arrow.png, Tools/archer_v3_weapons.py), like the other
    /// summoned objects; the light is code-built (no FX frames, sprite-rules 20/11): a thin additive wind streak behind it in the class palette
    /// (ClassPalette.Leaf) and a few sparkles. Impact: a small flat ring + sparkles (Impact). Missing sprite: a plain shaft from the streak.
    /// </summary>
    public sealed class ArrowBoltRig
    {
        const string ArrowSheet = "Sprites/hero-layered/weapons/archer-arrow";
        const float ArrowUnits = 9f; // arrow length on screen (the hero is ~20.5 units tall; the cell art is 38 % of the figure)

        static Sprite arrowSprite, diamond;
        static bool loaded;
        readonly Transform root;
        readonly SpriteRenderer arrow, streak, core;
        ClassPalette pal;
        float angle, scale, age;

        public ArrowBoltRig(Transform parent)
        {
            if (!loaded)
            {
                loaded = true;
                Texture2D t = SpriteSheet.TryLoad(ArrowSheet);
                if (t != null) arrowSprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.75f, 0.5f), t.width / ArrowUnits, 0, SpriteMeshType.FullRect); // pivot near the head
                Texture2D s = FxSprites.ShardTexture;
                diamond = Sprite.Create(s, new Rect(0, 0, s.width, s.height), new Vector2(0.5f, 0.5f), s.width, 0, SpriteMeshType.FullRect);
            }
            root = new GameObject("ArrowRig").transform;
            root.SetParent(parent, false);
            streak = Layer("Streak", diamond, true, -1);
            core = Layer("Core", diamond, true, 0);
            arrow = Layer("Arrow", arrowSprite != null ? arrowSprite : diamond, false, 1);
        }

        SpriteRenderer Layer(string name, Sprite sprite, bool additive, int order)
        {
            SpriteRenderer sr = ProceduralSprites.Child(root, name, sprite);
            if (additive) sr.sharedMaterial = FeelMaterials.Additive;
            sr.sortingOrder = FeelConfig.FxOrder - 12 + order;
            return sr;
        }

        public void Begin(Vector2 pos, float angleDeg, float boltScale, ClassPalette palette)
        {
            pal = palette; angle = angleDeg; scale = boltScale; age = 0f;
            root.gameObject.SetActive(true);
            Tick(pos, 0f);
        }

        public void End() { root.gameObject.SetActive(false); }

        /// <summary>New flight angle (a skill arrow can curve).</summary>
        public void Aim(float angleDeg) { angle = angleDeg; }

        public void Tick(Vector2 pos, float dt)
        {
            age += dt;
            root.position = new Vector3(pos.x, pos.y, 0f);
            float rad = angle * Mathf.Deg2Rad;
            var fwd = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            arrow.transform.localRotation = Quaternion.Euler(0f, 0f, angle); // art points to screen-right
            if (arrowSprite != null) arrow.transform.localScale = new Vector3(scale, scale, 1f);
            else { arrow.transform.localScale = new Vector3(ArrowUnits * scale, 1.2f * scale / 0.31f, 1f); arrow.color = new Color(0.55f, 0.38f, 0.22f); }
            // the streak grows out of the shot over the first 60 ms, then trails the arrow (alpha 0.35-0.55: light, not a solid bar)
            float grow = Mathf.Clamp01(age / 0.06f);
            Line(streak, 4f * scale, 26f * scale * grow, 2.2f * scale, new Color(pal.Mid.r, pal.Mid.g, pal.Mid.b, 0.4f), fwd);
            Line(core, 4f * scale, 14f * scale * grow, 0.7f * scale, new Color(pal.Core.r, pal.Core.g, pal.Core.b, 0.55f), fwd);
            if (dt > 0f && Random.value < 0.5f) UltimateFx.Sparkles(pos - fwd * (10f * scale), 1, pal.Edge);
        }

        /// <summary>A line of `length` starting `back` units behind the arrow head, along the flight.</summary>
        void Line(SpriteRenderer sr, float back, float length, float width, Color c, Vector2 fwd)
        {
            sr.enabled = length > 0.1f && FeelConfig.FxOn;
            if (!sr.enabled) return;
            Vector2 centre = -fwd * (back + length * 0.5f);
            sr.transform.localPosition = new Vector3(centre.x, centre.y, 0f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            sr.transform.localScale = new Vector3(length, width / 0.31f, 1f);
            sr.color = c;
        }

        /// <summary>Arrow hit: a small flat ring and leaf sparkles; the charged shot (big) a wider ring.</summary>
        public static void Impact(Vector2 at, bool big, ClassPalette pal)
        {
            if (!NetMode.RendersFx) return;
            ShockRing.Spawn(at, big ? 13f : 8f, big ? 0.26f : 0.18f, new Color(pal.Mid.r, pal.Mid.g, pal.Mid.b, 0.75f), UltimateFx.GroundFxOrder + 6, 0.72f, 0.2f, true);
            UltimateFx.Sparkles(at, big ? 8 : 4, pal.Core);
        }
    }
}
