using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Shared ultimate FX kit: four long-lived GPU particle systems (rock debris, dust, additive embers, additive ice shards; world space,
    /// emitted per particle with explicit velocities) plus a loader for the 6-frame painted strips with fixed per-stage anchors.
    /// </summary>
    public static class UltimateFx
    {
        public const int GroundFxOrder = -31000; // above the ground painting, below every y-sorted entity
        static Transform root;
        static ParticleSystem debris, dust, embers, shards;
        static readonly Dictionary<string, Sprite[]> Strips = new Dictionary<string, Sprite[]>();

        public static void Init(Transform parent)
        {
            root = parent;
            Strips.Clear();
            debris = Make("Debris", new Material(Shader.Find("Sprites/Default")) { mainTexture = FxSprites.ChunkTexture }, ParticleSystemRenderMode.Billboard, 16f, 400);
            dust = Make("Dust", new Material(Shader.Find("Sprites/Default")) { mainTexture = FxSprites.DotTexture }, ParticleSystemRenderMode.Billboard, 0f, 300);
            embers = Make("Embers", FeelMaterials.NewAdditive(FxSprites.DotTexture), ParticleSystemRenderMode.Stretch, -3f, 500);
            shards = Make("IceShards", FeelMaterials.NewAdditive(FxSprites.ShardTexture), ParticleSystemRenderMode.Stretch, 9f, 500);
            var sr = shards.GetComponent<ParticleSystemRenderer>();
            sr.lengthScale = 2.5f; sr.velocityScale = 0.05f;
        }

        /// <summary>Touches every particle system and the shared ring / line pools once so the first real effect does not pay for first-use setup.</summary>
        public static void Prewarm()
        {
            Vector2 far = new Vector2(-9999f, -9999f);
            Debris(far, 1, 1f, 1f); Dust(far, 1, 1f); Embers(far, 1, 1f, 1f, Color.white); Shards(far, 1, 1f, Color.white);
            foreach (ParticleSystem ps in new[] { debris, dust, embers, shards }) if (ps != null) ps.Clear();
            ShockRing.Spawn(far, 1f, 0.05f, Color.white, GroundFxOrder);
            ShockRing.Spawn(far, 1f, 0.05f, Color.white, GroundFxOrder, 0.7f, 0.2f, true);
        }

        static ParticleSystem Make(string name, Material mat, ParticleSystemRenderMode mode, float gravity, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false; main.loop = true; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = mode; r.sharedMaterial = mat; r.sortingOrder = FeelConfig.FxOrder + 6;
            ps.Play();
            return ps;
        }

        static void Emit(ParticleSystem ps, Vector2 pos, Vector2 vel, float life, float size, Color c, float rot = 0f, float spin = 0f)
        {
            if (ps == null || !FeelConfig.FxOn) return;
            var p = new ParticleSystem.EmitParams
            {
                position = new Vector3(pos.x, pos.y, 0f), velocity = new Vector3(vel.x, vel.y, 0f),
                startLifetime = life, startSize = size, startColor = c, rotation = rot, angularVelocity = spin,
            };
            ps.Emit(p, 1);
        }

        static Vector2 InDisc(float radius) { float a = Random.value * Mathf.PI * 2f; return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius * Mathf.Sqrt(Random.value); }

        /// <summary>Rock chunks flung up and out, tumbling, falling back under gravity.</summary>
        public static void Debris(Vector2 pos, int count, float spread, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 o = InDisc(spread);
                var v = new Vector2(o.x * 2.2f + Random.Range(-14f, 14f), Random.Range(0.55f, 1f) * speed);
                float shade = Random.Range(0.45f, 0.85f);
                Emit(debris, pos + o * 0.5f, v, 2f * v.y / (16f * 9.81f) + 0.1f, Random.Range(1.6f, 4.2f), new Color(shade * 1.1f, shade * 0.85f, shade * 0.7f, 1f), Random.value * 360f, Random.Range(-420f, 420f));
            }
        }

        /// <summary>Soft puffs that rise a little and fade (grey-blue snow dust).</summary>
        public static void Dust(Vector2 pos, int count, float spread, Color? tint = null)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 o = InDisc(spread);
                Emit(dust, pos + o, new Vector2(o.x * 1.6f, 6f + Random.value * 10f), Random.Range(0.5f, 0.9f), Random.Range(5f, 10f), tint.HasValue ? new Color(tint.Value.r, tint.Value.g, tint.Value.b, 0.6f) : new Color(0.78f, 0.84f, 0.92f, 0.32f), Random.value * 360f, Random.Range(-40f, 40f));
            }
        }

        /// <summary>Additive glowing motes. rise: initial upward speed; inward = pull towards the centre (charge-up).</summary>
        public static void Embers(Vector2 pos, int count, float spread, float rise, Color c, float inward = 0f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 o = InDisc(spread);
                Vector2 v = new Vector2(Random.Range(-10f, 10f), rise * Random.Range(0.5f, 1.2f)) - o.normalized * inward;
                Emit(embers, pos + o, v, Random.Range(0.4f, 0.9f), Random.Range(0.9f, 2.0f), c);
            }
        }

        /// <summary>Tiny additive sparkles that drift and fall (comet-tail dust, cast flashes).</summary>
        public static void Sparkles(Vector2 pos, int count, Color c)
        {
            for (int i = 0; i < count; i++)
                Emit(shards, pos + InDisc(2f), new Vector2(Random.Range(-14f, 14f), Random.Range(-6f, 12f)), Random.Range(0.3f, 0.55f), Random.Range(0.9f, 1.5f), new Color(c.r, c.g, c.b, 1f), Random.value * 360f);
        }

        /// <summary>Additive ice shards spraying outward and up, falling back.</summary>
        public static void Shards(Vector2 pos, int count, float speed, Color c)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Random.value * Mathf.PI * 2f, sp = speed * Random.Range(0.4f, 1f);
                Emit(shards, pos, new Vector2(Mathf.Cos(a) * sp, Mathf.Abs(Mathf.Sin(a)) * sp * 1.2f + 14f), Random.Range(0.3f, 0.6f), Random.Range(1.6f, 3.2f), c);
            }
        }

        /// <summary>Six painted frames "name-1..6" from Resources/Sprites/folder with the pivot at (anchorX, anchorY) px from the top-left; unitsPerPx scales the world size.</summary>
        public static Sprite[] Strip(string folder, string name, float anchorX, float anchorY, float unitsPerPx)
        {
            string key = folder + "/" + name;
            Sprite[] s;
            if (Strips.TryGetValue(key, out s)) return s;
            s = new Sprite[6];
            for (int i = 0; i < 6; i++)
            {
                Texture2D tex = SpriteSheet.Load("Sprites/" + folder + "/" + name + "-" + (i + 1));
                if (tex == null) { s = null; break; }
                s[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(anchorX / tex.width, (tex.height - anchorY) / tex.height), 1f / unitsPerPx, 0, SpriteMeshType.FullRect);
            }
            Strips[key] = s;
            return s;
        }

        /// <summary>Adds a normal sprite layer plus an additive copy child (bloom core); returns the normal renderer.</summary>
        public static SpriteRenderer Layer(Transform parent, string name, out SpriteRenderer glow)
        {
            SpriteRenderer sr = ProceduralSprites.Child(parent, name, null);
            glow = ProceduralSprites.Child(sr.transform, name + "Glow", null);
            glow.sharedMaterial = FeelMaterials.Additive;
            return sr;
        }
    }
}
