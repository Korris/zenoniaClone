using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Ambient life of a 2.5D map (ChatGPT RO3 comparison section 8), Particle System only, no sprite FX sheets:
    ///  - motes: warm dust specks (#FFF4D7) floating 4-70 units above the ground around the camera focus, slow (0.1-0.3 m/s), ~16 visible at once
    ///    (the perspective gives them parallax); on a 2.5D map without the RO3 look they keep the first prototype's green-gold;
    ///  - falling leaves (RO3 look only): a burst of 2-4 leaves from a tree crown in view every 10-18 s, at most 6 visible, fluttering down to the ground;
    ///    the leaf is a small procedural texture made at runtime.
    /// Sun flecks under the canopy live in the ground-shadow composite and tree sway in Sprite25DLit (both shader only).
    /// Crowd LOD (40-player fights): counts x1 up to 8 heroes, x0.5 up to 20, x0.25 beyond; nothing with effects off.
    /// </summary>
    public sealed class View25DAmbientFx
    {
        const float MoteAreaW = 520f, MoteAreaD = 380f, MoteLife = 5f;
        readonly Transform parent;
        ParticleSystem motes, leaves;
        float moteDebt, nextBurst = 4f;

        public View25DAmbientFx(Transform parent) { this.parent = parent; }

        /// <summary>Per frame while a 2.5D map is on screen.</summary>
        public void Tick(Camera cam, int heroes)
        {
            if (cam == null || !FeelConfig.FxOn) return;
            Vector3 c = cam.transform.position, f = cam.transform.forward;
            if (f.z <= 0.01f) return;
            Vector3 g = c + f * (-c.z / f.z); // centre of the visible ground: straight ahead of the camera on z = 0
            float lod = heroes <= 8 ? 1f : heroes <= 20 ? 0.5f : 0.25f;
            bool ro3 = View25D.Ro3Look;
            TickMotes(g, lod, ro3);
            if (ro3 && View25D.Profile.LeavesMax > 0) TickLeaves(cam, lod); // no leaves underground
            if (ro3 && View25D.Profile.Dungeon) { TickDrips(g, lod); TickSplashes(); } // cave: water drips from the dark ceiling
            if (ro3 && View25D.Profile.Snowfall > 0) TickSnow(g, lod);
        }

        float snowDebt;

        /// <summary>Light snowfall (snow maps, ChatGPT snow art direction 21/11/2026 section 7: sparse, slow, small, alpha 0.10-0.25, never a white haze):
        /// flakes fall from 60-110 units above the visible ground at ~14 units/s with a slow sideways drift. Uses the mote system (no extra material).</summary>
        void TickSnow(Vector3 g, float lod)
        {
            if (motes == null) return;
            const float Fall = 14f;
            snowDebt += View25D.Profile.Snowfall * Fall / 85f * lod * Time.deltaTime; // visible ~= Snowfall (life = height / fall speed)
            while (snowDebt >= 1f)
            {
                snowDebt -= 1f;
                float h = Random.Range(60f, 110f);
                var p = new ParticleSystem.EmitParams
                {
                    position = new Vector3(g.x + Random.Range(-0.55f, 0.55f) * MoteAreaW, g.y + Random.Range(-0.4f, 0.6f) * MoteAreaD, -h),
                    velocity = new Vector3(Random.Range(-4f, -1f), Random.Range(-1f, 1f), Fall * Random.Range(0.8f, 1.2f)),
                    startSize = Random.Range(1.2f, 2.4f),
                    startColor = new Color(0.93f, 0.96f, 1f, Random.Range(0.18f, 0.32f)),
                    startLifetime = h / Fall,
                    applyShapeToPosition = false,
                };
                motes.Emit(p, 1);
            }
        }

        public void Destroy()
        {
            if (motes != null) Object.Destroy(motes.gameObject);
            if (leaves != null) Object.Destroy(leaves.gameObject);
            motes = leaves = null;
        }

        void TickMotes(Vector3 g, float lod, bool ro3)
        {
            if (motes == null)
            {
                Material m = MoteMaterial();
                if (m == null) return;
                motes = WantedFxParticles.Create(parent, "View25DMotes", m, FeelConfig.FxOrder - 40, 200, 0f);
                var main = motes.main;
                main.loop = true; // emitted by hand forever: keep the system alive past its 5 s duration
            }
            float rate = (ro3 ? View25D.Profile.Motes : 18) / MoteLife * lod; // visible ~= rate x life
            moteDebt += rate * Time.deltaTime;
            while (moteDebt >= 1f)
            {
                moteDebt -= 1f;
                float height = Random.Range(4f, 70f);
                Color col = ro3 ? (Random.value < 0.7f ? View25D.Profile.MoteColorA : View25D.Profile.MoteColorB) // dungeon: amber spores / cyan wisps
                                : (Random.value < 0.6f ? new Color(0.75f, 1f, 0.55f, 0.9f) : new Color(1f, 0.9f, 0.55f, 0.9f));
                var p = new ParticleSystem.EmitParams
                {
                    position = new Vector3(g.x + Random.Range(-0.5f, 0.5f) * MoteAreaW, g.y + Random.Range(-0.45f, 0.55f) * MoteAreaD, -height),
                    velocity = new Vector3(Random.Range(-3.5f, 3.5f), Random.Range(-1.5f, 2.5f), -Random.Range(0.8f, 3f)),
                    startSize = Random.Range(1.4f, 3.0f),
                    startColor = col,
                    startLifetime = Random.Range(MoteLife * 0.7f, MoteLife * 1.3f),
                    applyShapeToPosition = false,
                };
                motes.Emit(p, 1);
            }
        }

        float nextDrip = 1f;

        /// <summary>Cave drips (dark dungeon, user 21/11 "giống ở trong hang"): every 0.8-2.2 s a pale drop falls ~70 units from the unseen ceiling
        /// somewhere in view and lands as a tiny splash of 4 specks (TickSplashes). Uses the mote system (no extra material).</summary>
        void TickDrips(Vector3 g, float lod)
        {
            nextDrip -= Time.deltaTime;
            if (nextDrip > 0f || motes == null) return;
            nextDrip = Random.Range(0.8f, 2.2f) / Mathf.Max(0.25f, lod);
            Vector3 at = new Vector3(g.x + Random.Range(-0.45f, 0.45f) * MoteAreaW, g.y + Random.Range(-0.35f, 0.45f) * MoteAreaD, 0f);
            const float H = 70f, Fall = 95f;
            var drop = new ParticleSystem.EmitParams
            {
                position = at + new Vector3(0f, 0f, -H), velocity = new Vector3(0f, 0f, Fall), startSize = 1.6f,
                startColor = new Color(0.75f, 0.92f, 1f, 0.9f), startLifetime = H / Fall, applyShapeToPosition = false,
            };
            motes.Emit(drop, 1);
            splashes.Add(new Vector4(at.x, at.y, 0f, Time.time + H / Fall));
        }

        readonly List<Vector4> splashes = new List<Vector4>(4); // x, y, -, time the drop lands

        void TickSplashes()
        {
            for (int k = splashes.Count - 1; k >= 0; k--)
            {
                if (Time.time < splashes[k].w) continue;
                Vector3 at = new Vector3(splashes[k].x, splashes[k].y, 0f);
                splashes.RemoveAt(k);
                if (motes == null) continue;
                for (int i = 0; i < 4; i++)
                {
                    float a = Random.value * Mathf.PI * 2f;
                    var sp = new ParticleSystem.EmitParams
                    {
                        position = at, velocity = new Vector3(Mathf.Cos(a) * 9f, Mathf.Sin(a) * 9f, -Random.Range(6f, 12f)), startSize = 1.1f,
                        startColor = new Color(0.75f, 0.92f, 1f, 0.75f), startLifetime = 0.3f, applyShapeToPosition = false,
                    };
                    motes.Emit(sp, 1);
                }
            }
        }

        void TickLeaves(Camera cam, float lod)
        {
            nextBurst -= Time.deltaTime;
            if (nextBurst > 0f) return;
            View25DProfile prof = View25D.Profile;
            nextBurst = Random.Range(prof.LeafBurstMin, prof.LeafBurstMax);
            if (leaves == null && !CreateLeaves()) return;
            Vector4 tree;
            if (!View25DLighting.RandomTreeInView(cam, out tree)) return;
            int max = Mathf.Max(1, Mathf.RoundToInt(prof.LeavesMax * lod));
            int n = Mathf.Min(Random.Range(2, 5), max - leaves.particleCount);
            for (int i = 0; i < n; i++)
            {
                // from the crown: 55-90 % of the tree height, across ~60 % of its width; falls 9-13 units / s, drifting with the wind (to the right)
                float h = tree.z * Random.Range(0.55f, 0.9f), fall = Random.Range(9f, 13f);
                Vector3 pos = View25D.Above(tree.x + Random.Range(-0.3f, 0.3f) * tree.w, tree.y - Random.Range(2f, 14f), h);
                var p = new ParticleSystem.EmitParams
                {
                    position = pos,
                    velocity = new Vector3(Random.Range(3f, 9f), Random.Range(-3f, 1f), fall),
                    startSize = Random.Range(3.2f, 4.6f),
                    startColor = LeafColor(),
                    startLifetime = Mathf.Max(1.5f, -pos.z / fall),
                    rotation = Random.Range(0f, 360f),
                    angularVelocity = Random.Range(-140f, 140f),
                    applyShapeToPosition = false,
                };
                leaves.Emit(p, 1);
            }
        }

        static Color LeafColor()
        {
            float r = Random.value;
            return r < 0.45f ? new Color(0.66f, 0.75f, 0.38f, 1f) : r < 0.8f ? new Color(0.84f, 0.77f, 0.42f, 1f) : new Color(0.55f, 0.69f, 0.31f, 1f);
        }

        bool CreateLeaves()
        {
            Shader s = Shader.Find("Sprites/Default");
            if (s == null) return false;
            var m = new Material(s) { name = "View25D_Leaf", hideFlags = HideFlags.DontSave, mainTexture = LeafTexture() };
            leaves = WantedFxParticles.Create(parent, "View25DLeaves", m, FeelConfig.FxOrder - 41, 24, 0f);
            var main = leaves.main;
            main.loop = true;
            var noise = leaves.noise; // flutter: side to side as it falls
            noise.enabled = true;
            noise.strength = 7f;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.3f;
            noise.damping = false;
            var col = leaves.colorOverLifetime;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var size = leaves.sizeOverLifetime;
            size.enabled = false;
            return true;
        }

        /// <summary>32 x 32 white leaf (pointed oval with a midrib), tinted per particle.</summary>
        static Texture2D LeafTexture()
        {
            const int N = 32;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "View25D_LeafTex", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                    float halfW = 0.42f * Mathf.Sin(Mathf.Clamp01((v + 0.95f) / 1.9f) * Mathf.PI); // pointed at both ends
                    float edge = halfW - Mathf.Abs(u);
                    float a = Mathf.Clamp01(edge * N * 0.5f) * (Mathf.Abs(v) < 0.95f ? 1f : 0f);
                    float rib = Mathf.Abs(u) < 0.05f ? 0.8f : 1f;
                    byte k = (byte)(255 * rib);
                    px[y * N + x] = new Color32(k, k, k, (byte)(255 * a));
                }
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }

        static Material moteMat;
        static Material MoteMaterial()
        {
            if (moteMat != null) return moteMat;
            Shader s = Resources.Load<Shader>("Shaders/LightParticle");
            Texture2D tex = Resources.Load<Texture2D>("Sprites/fx/light-skill/portal-mote");
            if (s == null || !s.isSupported || tex == null) return null;
            moteMat = new Material(s) { name = "View25D_Mote" };
            moteMat.SetTexture("_MainTex", tex);
            moteMat.SetColor("_EmissionColor", new Color(1.1f, 1.05f, 0.85f, 1f));
            moteMat.SetFloat("_CoreWhite", 0.45f);
            moteMat.SetColor("_BodyColor", new Color(1f, 0.96f, 0.8f, 0.5f));
            return moteMat;
        }
    }
}
