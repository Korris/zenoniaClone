using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Pooled hit impact: stretched additive sparks (ParticleSystem built in code), a short white flash quad, and for crits / finishers an
    /// expanding shock ring. Everything is additive so it blooms; nothing is allocated per hit.
    /// </summary>
    public class ImpactFx : MonoBehaviour
    {
        const float ArcDeg = 150f, LifeSec = 0.4f;
        static readonly List<ImpactFx> Pool = new List<ImpactFx>();
        static Transform root;
        static Material sparkMaterial, ringMaterial;
        static int cursor;

        ParticleSystem ps;
        SpriteRenderer ring, flashQuad;
        float age, ringLife, ringRadius, flashSize;
        Color ringColor, flashColor;
        bool ringOn;

        public static void Init(Transform parent)
        {
            root = parent;
            Pool.Clear();
            cursor = 0;
        }

        /// <summary>Spawns an impact at (x, y). dir = direction the target is knocked (sparks fan out that way).</summary>
        public static void Spawn(float x, float y, Vector2 dir, bool crit, bool finisher, ClassPalette palette = null, bool quiet = false)
        {
            if (!NetMode.RendersFx) return; // headless server: no cosmetics
            if (!FeelConfig.FxOn || root == null) return;
            ImpactFx fx = null;
            for (int i = 0; i < Pool.Count; i++) if (!Pool[i].gameObject.activeSelf) { fx = Pool[i]; break; }
            if (fx == null)
            {
                if (Pool.Count < FeelConfig.ImpactPool) fx = Create();
                else { fx = Pool[cursor]; cursor = (cursor + 1) % Pool.Count; fx.ps.Clear(); } // recycle the oldest
            }
            bool strong = crit || finisher;
            ClassPalette pal = palette ?? ClassPalette.Current;
            var sparkColor = fx.ps.colorOverLifetime; sparkColor.color = pal.Sparks;
            fx.ringColor = pal.Edge; fx.flashColor = Color.Lerp(Color.white, pal.Glow, 0.65f); // saturated so the flash does not wash out to white on snow
            fx.age = 0f;
            fx.transform.position = new Vector3(x, y, 0f);
            float angle = dir.sqrMagnitude > 0.001f ? Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg : 90f;
            // The circle shape's arc starts at local +X and runs counter-clockwise: rotate so the arc is centred on `angle`.
            fx.transform.rotation = Quaternion.Euler(0f, 0f, angle - ArcDeg * 0.5f);
            fx.gameObject.SetActive(true);

            var main = fx.ps.main;
            main.startSize = new ParticleSystem.MinMaxCurve(strong ? 0.9f : 0.7f, strong ? 1.8f : 1.4f);
            fx.ps.Play();
            fx.ps.Emit(strong ? FeelConfig.SparksStrong : FeelConfig.SparksNormal + Random.Range(0, 3));

            fx.flashSize = finisher ? 20f : strong ? 15f : 10f;
            fx.flashQuad.enabled = !quiet; // quiet = a painted impact sprite plays at the hit: sparks only, no white flash quad / ring
            fx.ringOn = strong && !quiet;
            fx.ringRadius = finisher ? 30f : 21f;
            fx.ringLife = finisher ? 0.26f : 0.2f;
            fx.ring.enabled = strong && !quiet;
            fx.Animate();
        }

        static ImpactFx Create()
        {
            var go = new GameObject("Impact");
            go.transform.SetParent(root, false);
            var fx = go.AddComponent<ImpactFx>();
            fx.BuildParticles();
            fx.ring = AddSprite(go.transform, "Ring", FxSprites.Ring(), FeelConfig.FxOrder + 3, new Color(1f, 0.82f, 0.55f, 1f));
            if (ringMaterial == null) ringMaterial = new Material(Shader.Find("Sprites/Default"));
            fx.ring.sharedMaterial = ringMaterial; // normal blend: a saturated ring stays coloured on snow instead of washing out
            fx.flashQuad = AddSprite(go.transform, "Flash", FxSprites.Dot(), FeelConfig.FxOrder + 4, Color.white);
            Pool.Add(fx);
            return fx;
        }

        static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, int order, Color color)
        {
            SpriteRenderer sr = ProceduralSprites.Child(parent, name, sprite);
            sr.sharedMaterial = FeelMaterials.Additive;
            sr.sortingOrder = order;
            sr.color = color;
            return sr;
        }

        void BuildParticles()
        {
            ps = gameObject.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(FeelConfig.SparkSpeedMin, FeelConfig.SparkSpeedMax);
            main.startColor = Color.white;
            main.maxParticles = 48;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.enabled = false; // bursts are emitted explicitly with ps.Emit(n)

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;
            shape.arc = ArcDeg;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

            // white-hot -> orange -> red, fading out
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.7f, 0.25f), 0.45f), new GradientColorKey(new Color(1f, 0.25f, 0.1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = grad;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.15f)));

            var r = GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 1.5f;
            r.velocityScale = 0.05f;
            r.sortingOrder = FeelConfig.FxOrder + 2;
            if (sparkMaterial == null) sparkMaterial = FeelMaterials.NewAdditive(FxSprites.DotTexture);
            r.sharedMaterial = sparkMaterial;
            gameObject.SetActive(false);
        }

        void Update()
        {
            age += Time.deltaTime; // real time so the burst plays out during hit-stop
            if (age >= LifeSec) { gameObject.SetActive(false); return; }
            Animate();
        }

        void Animate()
        {
            float f = Mathf.Clamp01(age / 0.07f);
            flashQuad.transform.localScale = Vector3.one * flashSize * (0.6f + 0.6f * f);
            flashQuad.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0.7f * (1f - f));
            if (flashQuad.enabled && f >= 1f) flashQuad.enabled = false;
            if (!ringOn) return;
            float t = Mathf.Clamp01(age / ringLife);
            float eased = 1f - (1f - t) * (1f - t);
            ring.transform.localScale = Vector3.one * (ringRadius * 2f * Mathf.Lerp(0.25f, 1f, eased));
            ring.color = new Color(ringColor.r, ringColor.g, ringColor.b, 1f - t);
            if (t >= 1f) ring.enabled = false;
        }
    }
}
