using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Small timeline player for the Cleric light FX (ClericLightLook): a list of shader quads, each with a start / end time (ms), a size that eases
    /// from s0 to s1 and a peak alpha (fast fade in, smooth fade out over the second half), plus timed callbacks that emit particles. No sprite frames.
    /// Ground quads lie on the ground band (under every hero and monster), "over" quads sit in the FX band, "behind" quads just behind the followed hero.
    /// Anchored on a world point or on a hero's feet (follows them; ends with them). Dim: another player's cast draws at 45 % alpha and 35 % particles
    /// (ChatGPT raid rule), so 40 Clerics do not bury the screen. ClearAll on a map change (ProxySpawner.ClearForTransfer).
    /// </summary>
    public sealed class ClericLightFx : MonoBehaviour
    {
        public enum Band { Ground, Over, Behind }

        sealed class Layer
        {
            public MeshRenderer R; public Band Band; public int Order;
            public float T0, T1, W0, W1, H0, H1, Alpha, Spin, Rot;
            public Vector2 Offset;
        }

        struct Cue { public float At; public Action<ClericLightFx> Fire; public bool Done; }

        static readonly List<ClericLightFx> Live = new List<ClericLightFx>(32);
        readonly List<Layer> layers = new List<Layer>(8);
        readonly List<Cue> cues = new List<Cue>(8);
        HeroController follow;
        bool following;
        Vector2 anchor;
        float age, endMs;
        ParticleSystem motes, sparkles;

        /// <summary>1 for the local player's own cast (or one landing on them), lower for everyone else.</summary>
        public float Dim { get; private set; }
        public bool Own { get { return Dim >= 0.99f; } }
        public float AgeMs { get { return age; } }

        /// <summary>Feet of the followed hero, else the fixed anchor.</summary>
        public Vector2 Anchor { get { return following && follow != null ? follow.Position : anchor; } }

        public static ClericLightFx Create(string name, Vector2 at, HeroController followHero, float endMs, bool own)
        {
            Transform root = Zone.Main != null ? Zone.Main.Root : null;
            var go = new GameObject("ClericLight_" + name);
            if (root != null) go.transform.SetParent(root, false);
            var fx = go.AddComponent<ClericLightFx>();
            fx.anchor = at; fx.follow = followHero; fx.following = followHero != null; fx.endMs = endMs;
            fx.Dim = own ? 1f : 0.45f;
            Live.Add(fx);
            return fx;
        }

        public static void ClearAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--) if (Live[i] != null) Destroy(Live[i].gameObject);
            Live.Clear();
        }

        /// <summary>Count of particles for this cast: all of them for the local player, about a third for others (at least one).</summary>
        public int Count(int n) { return Own ? n : Mathf.Max(1, Mathf.RoundToInt(n * 0.35f)); }

        /// <summary>A quad flat on the ground: diameter d0 -> d1 (units), squash = height / width of the ellipse.</summary>
        public ClericLightFx Ground(Material m, float t0, float t1, float d0, float d1, float alpha, float squash = 0.9f, float spinDegPerSec = 0f, int order = 4, Vector2 offset = default(Vector2))
        {
            return Add(m, Band.Ground, false, t0, t1, d0, d1, d0 * squash, d1 * squash, alpha, offset, order, spinDegPerSec);
        }

        /// <summary>An upright quad (centred, or standing on its foot) in the FX band or just behind the followed hero.</summary>
        public ClericLightFx Upright(Material m, Band band, bool standing, float t0, float t1, float w0, float w1, float h0, float h1, float alpha, Vector2 offset, int order = 0)
        {
            return Add(m, band, standing, t0, t1, w0, w1, h0, h1, alpha, offset, order, 0f);
        }

        ClericLightFx Add(Material m, Band band, bool standing, float t0, float t1, float w0, float w1, float h0, float h1, float alpha, Vector2 offset, int order, float spin)
        {
            MeshRenderer r = LightSkillAssets.Quad(transform, m.name, m, standing, 0);
            r.enabled = false;
            layers.Add(new Layer { R = r, Band = band, Order = order, T0 = t0, T1 = t1, W0 = w0, W1 = w1, H0 = h0, H1 = h1, Alpha = alpha, Offset = offset, Spin = spin, Rot = UnityEngine.Random.value * 90f });
            return this;
        }

        /// <summary>Runs `fire` once when the FX is `ms` old (particle bursts, follow-up pieces).</summary>
        public ClericLightFx At(float ms, Action<ClericLightFx> fire) { cues.Add(new Cue { At = ms, Fire = fire }); return this; }

        /// <summary>One particle (mote = soft dot, else a 4-point sparkle) at world `pos`, moving at `vel` units / s.</summary>
        public void Emit(bool sparkle, Vector2 pos, Vector2 vel, float size, Color c, float life)
        {
            if (!FeelConfig.FxOn) return;
            ParticleSystem ps = sparkle ? sparkles : motes;
            if (ps == null)
            {
                ps = WantedFxParticles.Create(transform, sparkle ? "Sparkles" : "Motes", sparkle ? ClericLightLook.Sparkle : ClericLightLook.Mote, FeelConfig.FxOrder - 19, 96, 0f);
                if (sparkle) sparkles = ps; else motes = ps;
            }
            WantedFxParticles.Emit(ps, pos, vel, size, new Color(c.r, c.g, c.b, c.a * Dim), life);
        }

        /// <summary>`n` particles spread in a disc of radius `spread` around `at`, drifting up `rise` units / s (+- jitter).</summary>
        public void Puff(bool sparkle, Vector2 at, int n, float spread, float rise, float sizeMin, float sizeMax, Color c, float life)
        {
            n = Count(n);
            for (int i = 0; i < n; i++)
            {
                Vector2 off = UnityEngine.Random.insideUnitCircle * spread;
                off.y *= 0.8f;
                Emit(sparkle, at + off, new Vector2(UnityEngine.Random.Range(-3f, 3f), rise * UnityEngine.Random.Range(0.6f, 1.3f)), UnityEngine.Random.Range(sizeMin, sizeMax), c, life * UnityEngine.Random.Range(0.75f, 1.15f));
            }
        }

        /// <summary>`n` particles flying out radially from `at` at `speed` units / s.</summary>
        public void Ring(bool sparkle, Vector2 at, int n, float speed, float sizeMin, float sizeMax, Color c, float life)
        {
            n = Count(n);
            float a0 = UnityEngine.Random.value * Mathf.PI * 2f;
            for (int i = 0; i < n; i++)
            {
                float a = a0 + i * Mathf.PI * 2f / n + UnityEngine.Random.Range(-0.2f, 0.2f);
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.85f);
                Emit(sparkle, at + d * 2f, d * speed * UnityEngine.Random.Range(0.7f, 1.1f), UnityEngine.Random.Range(sizeMin, sizeMax), c, life * UnityEngine.Random.Range(0.8f, 1.1f));
            }
        }

        void Update()
        {
            if (following && (follow == null || follow.IsDead)) { Destroy(gameObject); return; }
            age += GameClock.Dt * 1000f;
            if (age > endMs + 700f) { Destroy(gameObject); return; } // + the last particles' life
            for (int i = 0; i < cues.Count; i++)
            {
                Cue c = cues[i];
                if (c.Done || age < c.At) continue;
                c.Done = true; cues[i] = c;
                c.Fire(this);
            }
            Lay();
        }

        void LateUpdate() { Lay(); } // after the hero moved this frame, so a followed piece never lags a frame behind

        void Lay()
        {
            Vector2 at = Anchor;
            int behind = follow != null ? GameConfig.OrderBase(follow.Position.y) - 2 : 0;
            for (int i = 0; i < layers.Count; i++)
            {
                Layer l = layers[i];
                float u = (age - l.T0) / Mathf.Max(1f, l.T1 - l.T0);
                bool on = u >= 0f && u < 1f && FeelConfig.FxOn;
                l.R.enabled = on;
                if (!on) continue;
                float grow = 1f - (1f - u) * (1f - u) * (1f - u);                                 // ease-out cubic
                float fadeIn = Mathf.Clamp01((age - l.T0) / Mathf.Min(70f, (l.T1 - l.T0) * 0.25f));
                float k = Mathf.Clamp01((u - 0.45f) / 0.55f), fadeOut = 1f - k * k * (3f - 2f * k);
                l.R.transform.position = new Vector3(at.x + l.Offset.x, at.y + l.Offset.y, 0f);
                l.R.transform.localScale = new Vector3(Mathf.Lerp(l.W0, l.W1, grow), Mathf.Lerp(l.H0, l.H1, grow), 1f);
                if (l.Spin != 0f) l.R.transform.localRotation = Quaternion.Euler(0f, 0f, l.Rot + l.Spin * age / 1000f);
                l.R.sortingOrder = l.Band == Band.Ground ? UltimateFx.GroundFxOrder + l.Order : l.Band == Band.Over ? FeelConfig.FxOrder - 20 + l.Order : behind + l.Order;
                LightSkillAssets.Set(l.R, l.Alpha * fadeIn * fadeOut * Dim);
            }
        }

        void OnDestroy() { Live.Remove(this); }
    }
}
