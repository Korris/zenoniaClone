using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Elongated horizontal shot built from tapered diamond sprites (ranged-attack-fx-reference.png):
    ///   HolyArrow (cleric)  = green light arrow with leaf-shaped tips, a white-gold core and a long thin gold sparkle line trailing behind;
    ///   Plasma (mechanic)   = elongated plasma bolt with white-hot core and two thin motion streaks.
    /// Bodies use normal blending (stay saturated on snow), cores + trails are additive (bloom in the class colour).
    /// </summary>
    public sealed class StreakBoltRig
    {
        readonly Transform root;
        readonly SpriteRenderer body, tipA, tipB, core, trail, streakA, streakB, halo;
        static Sprite diamond;
        BoltStyle style;
        ClassPalette pal;
        float angle, scale, age;

        public StreakBoltRig(Transform parent)
        {
            root = new GameObject("StreakRig").transform;
            root.SetParent(parent, false);
            if (diamond == null) { Texture2D t = FxSprites.ShardTexture; diamond = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), t.width, 0, SpriteMeshType.FullRect); }
            halo = Layer("Halo", FxSprites.Dot(), true, -3);
            trail = Layer("Trail", diamond, true, -2);
            streakA = Layer("StreakA", diamond, true, -2);
            streakB = Layer("StreakB", diamond, true, -2);
            body = Layer("Body", diamond, false, 0);
            tipA = Layer("TipA", diamond, false, 1);
            tipB = Layer("TipB", diamond, false, 1);
            core = Layer("Core", diamond, true, 2);
        }

        SpriteRenderer Layer(string name, Sprite sprite, bool additive, int order)
        {
            SpriteRenderer sr = ProceduralSprites.Child(root, name, sprite);
            if (additive) sr.sharedMaterial = FeelMaterials.Additive;
            sr.sortingOrder = FeelConfig.FxOrder - 12 + order;
            return sr;
        }

        public void Begin(Vector2 pos, float angleDeg, float boltScale, BoltStyle boltStyle, ClassPalette palette)
        {
            style = boltStyle; pal = palette; angle = angleDeg; scale = boltScale; age = 0f;
            root.gameObject.SetActive(true);
            Tick(pos, 0f);
        }

        public void End() { root.gameObject.SetActive(false); }

        /// <summary>Places a diamond line of the given length / thickness whose head end is `back` units behind the bolt head.</summary>
        void Line(SpriteRenderer sr, float back, float length, float width, float side, Color c, float rotOffset = 0f)
        {
            float rad = angle * Mathf.Deg2Rad;
            var fwd = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            var perp = new Vector2(-fwd.y, fwd.x);
            Vector2 center = -fwd * (back + length * 0.5f) + perp * side;
            sr.transform.localPosition = new Vector3(center.x, center.y, 0f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle + rotOffset);
            sr.transform.localScale = new Vector3(length, width / 0.31f, 1f);
            sr.color = c;
            sr.enabled = c.a > 0f;
        }

        public void Tick(Vector2 pos, float dt)
        {
            age += dt;
            root.position = new Vector3(pos.x, pos.y, 0f);
            float s = scale;
            Color mid = pal.Mid, edge = pal.Edge, hot = pal.Core;
            halo.transform.localScale = Vector3.one * (13f * s);
            halo.color = new Color(edge.r, edge.g, edge.b, 0.35f);
            bool holy = style == BoltStyle.HolyArrow;
            if (holy)
            {
                Color gold = new Color(1f, 0.9f, 0.45f);
                Line(body, 0f, 24f * s, 5.5f * s, 0f, new Color(mid.r, mid.g, mid.b, 1f));
                Line(core, 1f, 16f * s, 1.8f * s, 0f, new Color(1f, 1f, 0.8f, 1f));
                // leaf-shaped tips flaring from the head
                Line(tipA, 4f, 11f * s, 3.4f * s, 0f, new Color(edge.r, edge.g, edge.b, 1f), 24f);
                Line(tipB, 4f, 11f * s, 3.4f * s, 0f, new Color(edge.r, edge.g, edge.b, 1f), -24f);
                // long thin gold sparkle line trailing behind (+ a faint second one)
                Line(trail, 20f * s, 52f * s, 1.0f * s, 0f, new Color(gold.r, gold.g, gold.b, 0.95f));
                Line(streakA, 24f * s, 30f * s, 0.8f * s, 2.2f * s * Mathf.Sin(age * 30f), new Color(mid.r, mid.g, mid.b, 0.8f));
                streakB.enabled = false;
                if (dt > 0f) UltimateFx.Sparkles(pos - new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * (10f * s), 1, gold);
            }
            else
            {
                Line(body, 0f, 19f * s, 5f * s, 0f, new Color(mid.r, mid.g, mid.b, 1f));
                Line(core, 1f, 13f * s, 1.7f * s, 0f, new Color(hot.r, hot.g, hot.b, 1f));
                tipA.enabled = tipB.enabled = false;
                Line(trail, 16f * s, 26f * s, 1.3f * s, 0f, new Color(edge.r, edge.g, edge.b, 0.9f));
                Line(streakA, 14f * s, 24f * s, 0.9f * s, 3.2f * s, new Color(mid.r, mid.g, mid.b, 0.85f));
                Line(streakB, 14f * s, 24f * s, 0.9f * s, -3.2f * s, new Color(mid.r, mid.g, mid.b, 0.85f));
                if (dt > 0f) UltimateFx.Sparkles(pos - new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * (8f * s), 1, mid);
            }
        }
    }
}
