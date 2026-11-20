using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Light picture of Divine Aegis (+DEF ally buff, 2026-11-20 evening), replacing its painted blue frames (`--cleric-fx legacy` keeps them).
    /// ClericBuffFx runs the caster's raise-hand pose; times are its (cast 620 ms, hand up 110 ms, streams 180-460 ms), the buff is skill data.
    /// Layers (docs/reference/cleric-fx-ro3/ro3-midgard-fx-observation-chatgpt.md, Divine Aegis): a teal shield rim round each blessed hero (upright
    /// ellipse just outside the silhouette, alpha 0.55-0.75), a very transparent face (0.12-0.25, hollow: the centre, where the face is, stays clear),
    /// three small cream runes on the rim; a bright arc runs round the rim while the shield rises (300-450 ms) and again for 180 ms whenever the
    /// hero loses HP under it (the "block"), otherwise the rim only glows faintly. The caster also gets a teal ring + mint halo on the ground and a
    /// small cream light over the raised hand; allies get a stream like Spirit Blessing. Crowd: ClericLightFx.DimFor (own full, party 70 %, others 35 %).
    /// </summary>
    public static class ClericAegisLightFx
    {
        const float CastMs = 620f, RaiseMs = 110f, StreamFromMs = 180f, StreamToMs = 460f, HandLift = 25f;

        public static void Cast(HeroController caster, List<HeroController> targets, float buffMs)
        {
            if (caster == null) return;
            float dim = ClericLightFx.DimFor(caster, null);
            Color gold = ClericLightLook.Gold;
            var fx = ClericLightFx.Create("Aegis", caster.Position, caster, CastMs, dim)
                .Ground(ClericLightLook.BuffHalo, 0f, 520f, 20f, 34f, 1f)
                .Ground(ClericLightLook.BuffRing, RaiseMs, 560f, 14f, 36f, 0.75f, 0.9f, 0f, 5).Arc(9f)
                .Upright(ClericLightLook.BuffFlash, ClericLightFx.Band.Over, false, RaiseMs, 430f, 4f, 8f, 4f, 8f, 0.55f, new Vector2(0f, HandLift))
                .At(RaiseMs + 40f, f => ClericBlessingLightFx.SideSparks(f, 4, gold, 0.45f));
            int streams = 0;
            foreach (HeroController t in targets)
            {
                if (t == null) continue;
                float dimT = ClericLightFx.DimFor(caster, t);
                if (t == caster) { Shield.Wear(t, buffMs, StreamFromMs, dimT); continue; }
                if (streams++ < ClericBlessingLightFx.MaxStreams)
                {
                    bool left = streams % 2 == 0;
                    fx.At(StreamFromMs, f => ClericBlessingLightFx.Stream.Launch(caster, t, left, dimT, StreamToMs - StreamFromMs, null));
                }
                Shield.Wear(t, buffMs, StreamToMs, dimT);
            }
        }

        /// <summary>The shield one hero wears while Divine Aegis lasts (one per hero: a recast restarts it).</summary>
        sealed class Shield : MonoBehaviour
        {
            const float RiseMs = 420f, BlockMs = 180f, FadeMs = 300f, RimHalfW = 11f, RimHalfH = 15f, RimLift = 11f, RingAt = 0.78f;
            static readonly Dictionary<HeroController, Shield> Live = new Dictionary<HeroController, Shield>();
            static readonly float[] RuneAngles = { 90f, 215f, 325f };     // top, lower left, lower right of the rim (outside the face and the body)
            HeroController hero;
            MeshRenderer rim, face;
            readonly MeshRenderer[] runes = new MeshRenderer[3];
            ParticleSystem sparks;
            float age, delay, total, dim, lastHp, blockAt = -1e6f, blockAngle;

            public static void Wear(HeroController t, float buffMs, float delayMs, float dim)
            {
                if (t == null) return;
                Shield s;
                if (!Live.TryGetValue(t, out s) || s == null)
                {
                    var go = new GameObject("ClericAegisShield");
                    go.transform.SetParent(t.transform.parent, false);
                    s = go.AddComponent<Shield>();
                    s.hero = t;
                    s.face = LightSkillAssets.Quad(go.transform, "Face", ClericLightLook.AegisFace, false, 0);
                    s.rim = LightSkillAssets.Quad(go.transform, "Rim", ClericLightLook.AegisRim, false, 0);
                    for (int i = 0; i < s.runes.Length; i++) s.runes[i] = LightSkillAssets.Quad(go.transform, "Rune", ClericLightLook.AegisRune, false, 0);
                    s.sparks = WantedFxParticles.Create(go.transform, "Sparks", ClericLightLook.BuffSpark, FeelConfig.FxOrder - 19, 16, 0f);
                    Live[t] = s;
                }
                s.age = 0f; s.delay = delayMs; s.total = buffMs; s.dim = dim; s.lastHp = t.Hp; s.blockAt = -1e6f;
                s.Lay();
            }

            void Update()
            {
                if (hero == null || hero.IsDead || age >= total) { Destroy(gameObject); return; }
                age += GameClock.Dt * 1000f;
                float hp = hero.Hp;
                if (hp < lastHp - 0.5f && age > delay && age - blockAt > BlockMs) Block(); // HP is replicated: a client sees the block too
                lastHp = hp;
            }

            /// <summary>A hit taken under the shield: the bright arc laps the rim once (180 ms) and a few cream sparks jump off it.</summary>
            void Block()
            {
                blockAt = age;
                blockAngle = Random.value * Mathf.PI * 2f;
                if (!FeelConfig.FxOn || dim < 0.3f) return;
                Color g = ClericLightLook.Gold;
                int n = dim >= 0.99f ? 4 : 2;
                for (int i = 0; i < n; i++)
                {
                    float a = blockAngle + i * 1.7f;
                    Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    Vector2 p = hero.Position + new Vector2(d.x * RimHalfW, RimLift + d.y * RimHalfH);
                    WantedFxParticles.Emit(sparks, p, d * 18f, Random.Range(1.6f, 2.4f), new Color(g.r, g.g, g.b, 0.95f * dim), 0.3f);
                }
            }

            void LateUpdate() { Lay(); }

            void Lay()
            {
                if (hero == null || rim == null) return;
                float t = age - delay;
                bool on = t > 0f && FeelConfig.FxOn;
                rim.enabled = face.enabled = on;
                for (int i = 0; i < runes.Length; i++) runes[i].enabled = on;
                if (!on) return;
                float rise = Mathf.Clamp01(t / RiseMs), ease = 1f - (1f - rise) * (1f - rise), end = Mathf.Clamp01((total - age) / FadeMs);
                float sinceBlock = age - blockAt, block = sinceBlock < BlockMs ? 1f - sinceBlock / BlockMs : 0f;
                bool lapping = rise < 1f || block > 0f;
                float grow = Mathf.Lerp(0.7f, 1f, ease), pulse = 0.92f + 0.08f * Mathf.Sin(age * 0.003f);
                Vector2 c = hero.Position + new Vector2(0f, RimLift);
                int order = GameConfig.OrderBase(hero.Position.y) + 8;                        // sorts with its hero, just over the hero's own layers

                // Rim: the burst-ring band sits at 78 % of the quad, so the quad is rim / 0.78. Bright on the rise and on a block, faint otherwise.
                rim.transform.position = new Vector3(c.x, c.y, 0f);
                rim.transform.localScale = new Vector3(2f * RimHalfW / RingAt * grow, 2f * RimHalfH / RingAt * grow, 1f);
                rim.sortingOrder = order;
                float rimA = (rise < 1f ? Mathf.Lerp(0.95f, 0.75f, rise) : 0.6f) + 0.3f * block;
                LightSkillAssets.Set(rim, Mathf.Clamp01(rimA * pulse * Mathf.Min(1f, t / 90f) * end * dim));
                if (lapping)
                {
                    float lap = rise < 1f ? rise * Mathf.PI * 2.4f : blockAngle + (1f - block) * Mathf.PI * 2f; // the flash runs round the rim
                    LightSkillAssets.SetArc(rim, Mathf.PI * 0.5f - lap, ClericLightLook.ArcHalfWidth, 0.15f, 1f);
                }
                else LightSkillAssets.SetArc(rim, 0f, 0.01f, 0.45f, 1f);                       // sustain: no bright arc, a faint even glow

                // Face: hollow, so only the band just inside the rim tints; the hero's face (rim centre area) stays clear.
                face.transform.position = rim.transform.position;
                face.transform.localScale = new Vector3(2f * RimHalfW * grow, 2f * RimHalfH * grow, 1f);
                face.sortingOrder = order - 1;
                LightSkillAssets.Set(face, Mathf.Clamp01((0.8f + 0.4f * block) * ease * end * dim));

                for (int i = 0; i < runes.Length; i++)
                {
                    float a = RuneAngles[i] * Mathf.Deg2Rad;
                    MeshRenderer r = runes[i];
                    r.transform.position = new Vector3(c.x + Mathf.Cos(a) * RimHalfW * grow, c.y + Mathf.Sin(a) * RimHalfH * grow + Mathf.Sin(age * 0.004f + i) * 0.5f, 0f);
                    float s = (i == 0 ? 4.6f : 3.6f) * (0.9f + 0.1f * pulse);
                    r.transform.localScale = new Vector3(s, s, 1f);
                    r.sortingOrder = order + 1;
                    LightSkillAssets.Set(r, Mathf.Clamp01((0.75f + 0.25f * block) * pulse * ease * end * dim));
                }
            }

            void OnDestroy()
            {
                Shield s;
                if ((object)hero != null && Live.TryGetValue(hero, out s) && s == this) Live.Remove(hero);
            }
        }
    }
}
