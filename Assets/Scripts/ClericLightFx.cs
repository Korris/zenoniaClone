using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Small timeline player for the Cleric light FX (ClericLightLook): a list of shader quads, each with a start / end time (ms), a size that eases
    /// from s0 to s1, an offset that can move (Move / Rise), a peak alpha with an envelope (default: fast fade in, smooth fade out over the second half;
    /// Env = explicit fade in / out for sustained layers), optional dissolve and a mesh trail, plus timed callbacks that emit particles. No sprite frames.
    /// Ground quads lie on the ground band (under every hero and monster), "over" quads sit in the FX band, "behind" quads just behind the followed hero.
    /// Anchored on a world point or on a hero's feet (follows them; ends with them). ClearAll on a map change (ProxySpawner.ClearForTransfer).
    /// Crowd LOD (RO3 rule 12, ClericLightFxLod): the tier comes from the dim passed at creation; Glow layers and particles shrink for allies and
    /// strangers, Detail layers are dropped for strangers.
    /// </summary>
    public sealed partial class ClericLightFx : MonoBehaviour
    {
        public enum Band { Ground, Over, Behind, Front } // Front: just over the followed hero's own layers (sorts with that hero, not over the whole crowd)

        sealed class Layer
        {
            public MeshRenderer R; public Band Band; public int Order; public LayerKind Kind;
            public float T0, T1, W0, W1, H0, H1, Alpha, Spin, Rot, InMs = -1f, OutMs = -1f;
            public Vector2 Offset, Move;
            public bool Arc, Dissolve, WasOn; public float ArcPhase, ArcSpeed, ArcFloor, ArcCount;
            public TrailRenderer Trail;
        }

        struct Cue { public float At; public Action<ClericLightFx> Fire; public bool Done; }

        static readonly List<ClericLightFx> Live = new List<ClericLightFx>(32);
        static int arcId = -1; // _Arc, looked up on first use (not in a MonoBehaviour's static initialiser)
        readonly List<Layer> layers = new List<Layer>(8);
        readonly List<Cue> cues = new List<Cue>(8);
        HeroController follow;
        bool following;
        Vector2 anchor;
        float age, endMs;
        ParticleSystem motes, sparkles;

        /// <summary>The crowd dim given at creation: 1 = the local player's own cast (or one landing on them), 0.7 = party, lower = anyone else.</summary>
        public float Dim { get; private set; }
        public FxTier Lod { get { return TierOf(Dim); } }
        public bool Own { get { return Lod == FxTier.Own; } }
        public float AgeMs { get { return age; } }

        /// <summary>Feet of the followed hero, else the fixed anchor.</summary>
        public Vector2 Anchor { get { return following && follow != null ? follow.Position : anchor; } }

        public static ClericLightFx Create(string name, Vector2 at, HeroController followHero, float endMs, bool own)
        {
            return Create(name, at, followHero, endMs, own ? 1f : 0.35f);
        }

        public static ClericLightFx Create(string name, Vector2 at, HeroController followHero, float endMs, float dim)
        {
            Transform root = Zone.Main != null ? Zone.Main.Root : null;
            var go = new GameObject("ClericLight_" + name);
            if (root != null) go.transform.SetParent(root, false);
            var fx = go.AddComponent<ClericLightFx>();
            fx.anchor = at; fx.follow = followHero; fx.following = followHero != null; fx.endMs = endMs;
            fx.Dim = Mathf.Clamp01(dim);
            Live.Add(fx);
            return fx;
        }

        public static void ClearAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--) if (Live[i] != null) Destroy(Live[i].gameObject);
            Live.Clear();
        }

        /// <summary>The last added layer (a ring with an Arc material) gets a bright arc running round it at `speedRadPerSec`: only ~30 % of
        /// the ring glows at once (`count` arcs share that length). `floor` = glow left outside the arc (negative = the material's).</summary>
        public ClericLightFx Arc(float speedRadPerSec, float phase = 0f, int count = 1, float floor = -1f)
        {
            if (layers.Count == 0) return this;
            Layer l = Last;
            l.Arc = true; l.ArcSpeed = speedRadPerSec; l.ArcPhase = phase; l.ArcCount = Mathf.Max(1, count); l.ArcFloor = floor;
            return this;
        }

        Layer Last { get { return layers[layers.Count - 1]; } }

        /// <summary>The last added layer drifts up `dy` units over its life (same ease-out as its size): emblems, rings rising round the body.</summary>
        public ClericLightFx Rise(float dy) { return Move(new Vector2(0f, dy)); }

        /// <summary>The last added layer's offset moves by `d` units over its life (ease-out): flying specks, a bolt from the hand to the target.</summary>
        public ClericLightFx Move(Vector2 d) { if (layers.Count > 0) Last.Move = d; return this; }

        /// <summary>Explicit envelope of the last added layer: fade in over `inMs`, full, fade out over the last `outMs` (sustained layers).</summary>
        public ClericLightFx Env(float inMs, float outMs) { if (layers.Count > 0) { Last.InMs = inMs; Last.OutMs = outMs; } return this; }

        /// <summary>The last added layer breaks up (LightBlob _Dissolve) over the second half of its life instead of fading evenly.</summary>
        public ClericLightFx Dissolve() { if (layers.Count > 0) Last.Dissolve = true; return this; }

        /// <summary>Marks the last added layer as soft glow (dimmed for allies, nearly off for strangers) or detail (dropped for strangers).</summary>
        public ClericLightFx Kind(LayerKind k) { if (layers.Count > 0) Last.Kind = k; return this; }

        /// <summary>A short mesh trail (LightTrail) behind the last added layer as it moves: `width` units, `seconds` long, light head -> main tail.</summary>
        public ClericLightFx Trail(ClericLightLook.Pal p, float width, float seconds)
        {
            if (layers.Count == 0) return this;
            float k = TrailAlpha(Lod);
            if (k <= 0f) return this;
            TrailRenderer t = ClericAttackLightFx.MakeTrail(Last.R.transform, width, seconds, k, p);
            t.emitting = false;
            Last.Trail = t;
            return this;
        }

        /// <summary>A quad flat on the ground: diameter d0 -> d1 (units), squash = height / width of the ellipse (ClericLightFxLod.Squash by default).</summary>
        public ClericLightFx Ground(Material m, float t0, float t1, float d0, float d1, float alpha, float squash = Squash, float spinDegPerSec = 0f, int order = 4, Vector2 offset = default(Vector2))
        {
            return Add(m, Band.Ground, false, t0, t1, d0, d1, d0 * squash, d1 * squash, alpha, offset, order, spinDegPerSec);
        }

        /// <summary>An upright quad (centred, or standing on its foot) in the FX band or just behind / in front of the followed hero.</summary>
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
                ps = WantedFxParticles.Create(transform, sparkle ? "Sparkles" : "Motes", sparkle ? ClericLightLook.SparkMat : ClericLightLook.DotMat, FeelConfig.FxOrder - 19, 96, 0f);
                if (sparkle) sparkles = ps; else motes = ps;
            }
            WantedFxParticles.Emit(ps, pos, vel, size, new Color(c.r, c.g, c.b, c.a * ParticleAlpha(Lod)), life);
        }

        /// <summary>`n` particles spread in a disc of radius `spread` around `at`, drifting up `rise` units / s (+- jitter).</summary>
        public void Puff(bool sparkle, Vector2 at, int n, float spread, float rise, float sizeMin, float sizeMax, Color c, float life)
        {
            n = Count(n);
            for (int i = 0; i < n; i++)
            {
                Vector2 off = UnityEngine.Random.insideUnitCircle * spread;
                off.y *= Squash;
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
            FxTier tier = Lod;
            int behind = follow != null ? GameConfig.OrderBase(follow.Position.y) - 2 : 0, front = follow != null ? behind + 10 : FeelConfig.FxOrder - 20;
            for (int i = 0; i < layers.Count; i++)
            {
                Layer l = layers[i];
                float span = Mathf.Max(1f, l.T1 - l.T0), u = (age - l.T0) / span;
                float kind = KindAlpha(l.Kind, tier);
                bool on = u >= 0f && u < 1f && FeelConfig.FxOn && kind > 0f;
                float grow = 1f - (1f - Mathf.Clamp01(u)) * (1f - Mathf.Clamp01(u)) * (1f - Mathf.Clamp01(u)); // ease-out cubic
                l.R.transform.position = new Vector3(at.x + l.Offset.x + l.Move.x * grow, at.y + l.Offset.y + l.Move.y * grow, 0f);
                if (l.Trail != null)
                {
                    if (on && !l.WasOn) l.Trail.Clear(); // placed first: the ribbon must not streak in from where the quad sat before
                    l.Trail.emitting = on;
                    l.Trail.sortingOrder = l.Band == Band.Ground ? UltimateFx.GroundFxOrder + l.Order : FeelConfig.FxOrder - 21 + l.Order;
                }
                l.WasOn = on;
                l.R.enabled = on;
                if (!on) continue;
                float local = age - l.T0, fadeIn, fadeOut;
                if (l.InMs >= 0f)
                {
                    fadeIn = l.InMs > 0f ? Mathf.Clamp01(local / l.InMs) : 1f;
                    fadeOut = l.OutMs > 0f ? Mathf.Clamp01((span - local) / l.OutMs) : 1f;
                    fadeOut = fadeOut * fadeOut * (3f - 2f * fadeOut);
                }
                else
                {
                    fadeIn = Mathf.Clamp01(local / Mathf.Min(70f, span * 0.25f));
                    float k = Mathf.Clamp01((u - 0.45f) / 0.55f);
                    fadeOut = 1f - k * k * (3f - 2f * k);
                }
                l.R.transform.localScale = new Vector3(Mathf.Lerp(l.W0, l.W1, grow), Mathf.Lerp(l.H0, l.H1, grow), 1f);
                if (l.Spin != 0f) l.R.transform.localRotation = Quaternion.Euler(0f, 0f, l.Rot + l.Spin * age / 1000f);
                l.R.sortingOrder = l.Band == Band.Ground ? UltimateFx.GroundFxOrder + l.Order : l.Band == Band.Over ? FeelConfig.FxOrder - 20 + l.Order : l.Band == Band.Front ? front + l.Order : behind + l.Order;
                LightSkillAssets.Set(l.R, l.Alpha * fadeIn * fadeOut * kind, l.Dissolve ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.35f) / 0.65f)) : -1f);
                if (l.Arc)
                {
                    if (arcId < 0) arcId = Shader.PropertyToID("_Arc");
                    Vector4 m = l.R.sharedMaterial.GetVector(arcId);
                    LightSkillAssets.SetArc(l.R, l.ArcPhase + l.ArcSpeed * age / 1000f, ClericLightLook.ArcHalfWidth / l.ArcCount, l.ArcFloor >= 0f ? l.ArcFloor : m.z, l.ArcCount);
                }
            }
        }

        void OnDestroy() { Live.Remove(this); }
    }
}
