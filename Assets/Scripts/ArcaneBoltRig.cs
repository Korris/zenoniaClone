using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Wizard magic missile, built from layers under the (pooled) projectile object:
    ///   halo (blue glow) - core orb (white-cyan) - spinning 8-point rune star + counter-spinning rune ring - two orbiting sparkle motes -
    ///   tapered additive comet trail (TrailRenderer) - falling sparkle particles.
    /// The rig ignores the parent scale (the projectile keeps scale 1 while it is active) and sizes itself from the bolt scale.
    /// </summary>
    public sealed class ArcaneBoltRig
    {
        readonly Transform root;
        readonly SpriteRenderer halo, disc, core, star, ring, moteA, moteB, beam;
        static Sprite diamond;
        ClassPalette pal = ClassPalette.Arcane;
        readonly TrailRenderer trail;
        float age, scale, angle;

        public ArcaneBoltRig(Transform parent)
        {
            root = new GameObject("ArcaneRig").transform;
            root.SetParent(parent, false);
            halo = Layer("Halo", FxSprites.Dot(), true, -4);
            disc = Layer("Disc", FxSprites.Dot(), false, -3);
            ring = Layer("RuneRing", FxSprites.Ring(), false, -2);
            star = Layer("RuneStar", FxSprites.Star(), true, -1);
            core = Layer("Core", FxSprites.Dot(), true, 0);
            moteA = Layer("MoteA", FxSprites.Dot(), true, 1);
            moteB = Layer("MoteB", FxSprites.Dot(), true, 1);
            if (diamond == null) { Texture2D t = FxSprites.ShardTexture; diamond = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), t.width, 0, SpriteMeshType.FullRect); }
            beam = Layer("Beam", diamond, true, -5); // straight bright beam tail
            var go = new GameObject("Trail");
            go.transform.SetParent(root, false);
            trail = go.AddComponent<TrailRenderer>();
            trail.material = FeelMaterials.NewAdditive(FxSprites.DotTexture);
            trail.time = 0.2f;
            trail.minVertexDistance = 0.6f;
            trail.alignment = LineAlignment.View;
            trail.sortingOrder = FeelConfig.FxOrder - 14;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.35f, 0.55f), new Keyframe(1f, 0f)); // tapered comet tail
            trail.emitting = false;
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
            pal = palette;
            angle = angleDeg;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.Lerp(Color.white, pal.Core, 0.5f), 0f), new GradientColorKey(pal.Mid, 0.3f), new GradientColorKey(pal.Edge, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.5f, 0.4f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = g;
            scale = boltScale;
            age = 0f;
            root.gameObject.SetActive(true);
            root.position = new Vector3(pos.x, pos.y, 0f);
            trail.widthMultiplier = 5.5f * boltScale;
            trail.Clear();
            trail.emitting = true;
            Tick(pos, 0f);
        }

        public void End()
        {
            trail.emitting = false;
            root.gameObject.SetActive(false);
        }

        /// <summary>Positions + animates all layers at the projectile position (world, already lifted); dt in seconds.</summary>
        public void Tick(Vector2 pos, float dt)
        {
            age += dt;
            root.position = new Vector3(pos.x, pos.y, 0f);
            float s = scale;
            Color mid = pal.Mid, edge = pal.Edge;
            float rad = angle * Mathf.Deg2Rad;
            beam.transform.localPosition = new Vector3(-Mathf.Cos(rad), -Mathf.Sin(rad), 0f) * (17f * s);
            beam.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            beam.transform.localScale = new Vector3(36f * s, 1.5f * s / 0.31f * 0.5f, 1f);
            beam.color = new Color(pal.Core.r, pal.Core.g, pal.Core.b, 0.9f);
            halo.transform.localScale = Vector3.one * (11f * s * (1f + 0.08f * Mathf.Sin(age * 24f)));
            halo.color = new Color(edge.r, edge.g, edge.b, 0.5f);
            disc.transform.localScale = Vector3.one * (6f * s);
            disc.color = new Color(mid.r, mid.g, mid.b, 0.95f); // normal-blend disc keeps the orb crisp on snow
            core.transform.localScale = Vector3.one * (3.4f * s);
            core.color = new Color(pal.Core.r, pal.Core.g, pal.Core.b, 1f);
            star.transform.localScale = Vector3.one * (8.5f * s);
            star.transform.localRotation = Quaternion.Euler(0f, 0f, age * 420f);
            star.color = new Color(pal.Core.r, pal.Core.g, pal.Core.b, 0.95f);
            ring.transform.localScale = Vector3.one * (7.5f * s);
            ring.transform.localRotation = Quaternion.Euler(0f, 0f, -age * 260f);
            ring.color = new Color(edge.r, edge.g, edge.b, 0.9f);
            // two sparkle motes spiralling around the orb, half a turn apart
            for (int i = 0; i < 2; i++)
            {
                float a = age * 15f + i * Mathf.PI;
                float r = (5.5f + 1.2f * Mathf.Sin(age * 9f + i)) * s;
                SpriteRenderer m = i == 0 ? moteA : moteB;
                m.transform.localPosition = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                m.transform.localScale = Vector3.one * (1.9f * s);
                m.color = new Color(pal.Core.r, pal.Core.g, pal.Core.b, 1f);
            }
            if (dt > 0f) UltimateFx.Sparkles(pos, 1, mid);
        }
    }
}
