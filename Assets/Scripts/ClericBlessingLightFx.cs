using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Light picture of Spirit Blessing (+ATK ally buff). ClericBuffFx still runs the caster's raise-hand pose; times are its (cast 620 ms, hand up at
    /// 110 ms, streams 180-460 ms), the buff itself is skill data (applied at the cast, atMs 0). Redone 2026-11-20 evening with the Midgard Senki /
    /// RO3 layering (docs/reference/cleric-fx-ro3/midgard-senki-vs-cleric-fx-chatgpt.md, ro3-midgard-fx-observation-chatgpt.md): the first light pass
    /// was pale mint / white like the snow. Now, per ClericLightLook's Buff* set:
    ///  - caster: mint halo pool, two teal rune arcs turning slowly (only their bright arcs glow), a deep-teal ring band counter-turning, a small
    ///    cream light gathering over the raised hand, teal motes + cream sparks spiralling up;
    ///  - one stream per ally (cream head + teal ribbon), landing at StreamToMs; the caster receives at StreamFromMs;
    ///  - receive: teal ring with a running bright arc, cream column + cross BEHIND the hero (never over the face), a short cream flash behind the chest;
    ///  - while the buff lasts: one small cream star beside the back shoulder + a rare teal mote, faded over the last 300 ms.
    /// Crowd tiers (ClericLightFx.DimFor): own / on me full, party 70 %, others 35 % with a quarter of the particles.
    /// </summary>
    public static class ClericBlessingLightFx
    {
        const float CastMs = 620f, RaiseMs = 110f, StreamFromMs = 180f, StreamToMs = 460f, HandLift = 25f, ChestLift = 9f;
        public const int MaxStreams = 8;

        public static void Cast(HeroController caster, List<HeroController> targets, float buffMs)
        {
            if (caster == null) return;
            float dim = ClericLightFx.DimFor(caster, null);
            Color teal = ClericLightLook.Teal2, gold = ClericLightLook.Gold;
            var fx = ClericLightFx.Create("Blessing", caster.Position, caster, CastMs, dim);
            fx.Ground(ClericLightLook.BuffHalo, 0f, 560f, 22f, 36f, 1f)
              .Ground(ClericLightLook.BuffRune, 0f, 600f, 26f, 34f, 0.85f, 0.9f, 40f, 5).Arc(4f, 0f, 2)
              .Ground(ClericLightLook.BuffRing, 60f, 560f, 30f, 40f, 0.7f, 0.9f, 0f, 6).Arc(-7f, 1.6f)
              .Upright(ClericLightLook.BuffFlash, ClericLightFx.Band.Over, false, RaiseMs, 480f, 4f, 9f, 4f, 9f, 0.6f, new Vector2(0f, HandLift))
              .Upright(ClericLightLook.BuffCross, ClericLightFx.Band.Over, false, RaiseMs + 60f, 470f, 6f, 10f, 9f, 14f, 0.65f, new Vector2(0f, HandLift), 1);
            for (int i = 0; i < 12; i++)
            {
                int k = i;
                fx.At(RaiseMs + k * 38f, f =>
                {
                    if (!f.Own && k % 3 != 0) return;
                    float a = k * 2.4f;
                    Vector2 p = f.Anchor + new Vector2(Mathf.Cos(a) * 10f, Mathf.Sin(a) * 4f + 2f);
                    bool spark = k % 2 == 1;
                    f.Emit(spark, p, new Vector2(-Mathf.Sin(a) * 8f, 26f), spark ? UnityEngine.Random.Range(1.6f, 2.4f) : UnityEngine.Random.Range(2f, 2.8f),
                        spark ? new Color(gold.r, gold.g, gold.b, 0.9f) : new Color(teal.r, teal.g, teal.b, 0.85f), 0.5f);
                });
            }
            int streams = 0;
            foreach (HeroController t in targets)
            {
                if (t == null) continue;
                float dimT = ClericLightFx.DimFor(caster, t);
                if (t == caster) { fx.At(StreamFromMs, f => Receive(t, dimT)); Aura.Wear(t, buffMs, StreamFromMs, dimT); continue; }
                if (streams++ >= MaxStreams) { Aura.Wear(t, buffMs, StreamToMs, dimT); continue; }
                bool left = streams % 2 == 0;
                fx.At(StreamFromMs, f => Stream.Launch(caster, t, left, dimT, StreamToMs - StreamFromMs, h => Receive(h, dimT)));
                Aura.Wear(t, buffMs, StreamToMs, dimT);
            }
        }

        /// <summary>The moment a hero takes the blessing: teal ring with a running arc, cream column + cross behind them, a short cream flash, sparks.</summary>
        static void Receive(HeroController t, float dim)
        {
            if (t == null) return;
            Color gold = ClericLightLook.Gold;
            ClericLightFx.Create("BlessingReceive", t.Position, t, 380f, dim)
                .Ground(ClericLightLook.BuffHalo, 0f, 380f, 14f, 26f, 1f)
                .Ground(ClericLightLook.BuffRing, 0f, 360f, 12f, 28f, 0.75f, 0.9f, 0f, 5).Arc(12f)
                .Upright(ClericLightLook.BuffColumn, ClericLightFx.Band.Behind, true, 0f, 340f, 12f, 8f, 28f, 34f, 0.7f, new Vector2(0f, -1f))
                .Upright(ClericLightLook.BuffCross, ClericLightFx.Band.Behind, false, 20f, 320f, 10f, 15f, 14f, 21f, 0.6f, new Vector2(0f, 13f), 1)
                .Upright(ClericLightLook.BuffFlash, ClericLightFx.Band.Behind, false, 0f, 130f, 6f, 12f, 6f, 12f, 0.55f, new Vector2(0f, ChestLift), 1)
                .At(0f, f => SideSparks(f, 6, gold, 0.4f));
        }

        /// <summary>`n` cream sparks rising beside the body (left / right of the silhouette, never across the face).</summary>
        public static void SideSparks(ClericLightFx f, int n, Color c, float life)
        {
            n = f.Count(n);
            for (int i = 0; i < n; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                Vector2 p = f.Anchor + new Vector2(side * UnityEngine.Random.Range(6f, 10f), UnityEngine.Random.Range(1f, 14f));
                f.Emit(true, p, new Vector2(side * UnityEngine.Random.Range(0f, 3f), UnityEngine.Random.Range(16f, 26f)), UnityEngine.Random.Range(1.5f, 2.4f),
                    new Color(c.r, c.g, c.b, 0.95f), life * UnityEngine.Random.Range(0.8f, 1.15f));
            }
        }

        /// <summary>One light stream from the caster's raised hand to an ally's chest: arcing cream head + teal ribbon, `onLand` when it arrives.</summary>
        public sealed class Stream : MonoBehaviour
        {
            HeroController from, to;
            float age, side, flightMs;
            MeshRenderer head;
            TrailRenderer trail;
            Action<HeroController> onLand;

            public static void Launch(HeroController from, HeroController to, bool left, float dim, float flightMs, Action<HeroController> onLand)
            {
                if (from == null || to == null || Zone.Main == null) return;
                var go = new GameObject("ClericBuffStream");
                go.transform.SetParent(Zone.Main.Root, false);
                var s = go.AddComponent<Stream>();
                s.from = from; s.to = to; s.side = left ? 1f : -1f; s.flightMs = Mathf.Max(1f, flightMs); s.onLand = onLand;
                go.transform.position = s.At(0f);
                s.head = LightSkillAssets.Quad(go.transform, "Head", ClericLightLook.BuffHead, false, FeelConfig.FxOrder - 12);
                s.head.transform.localScale = new Vector3(6f, 6f, 1f);
                LightSkillAssets.Set(s.head, dim);
                s.trail = ClericAttackLightFx.MakeTrail(go.transform, dim >= 0.99f ? 2.6f : 1.4f, 0.16f, dim);
                s.trail.sharedMaterial = ClericLightLook.BuffTrail;
                var g = new Gradient(); // cream at the head, teal along the ribbon (the attack trail's white -> jade is too pale on snow)
                g.SetKeys(
                    new[] { new GradientColorKey(ClericLightLook.Gold, 0f), new GradientColorKey(ClericLightLook.Teal2, 0.3f), new GradientColorKey(ClericLightLook.Teal, 1f) },
                    new[] { new GradientAlphaKey(dim, 0f), new GradientAlphaKey(0.7f * dim, 0.35f), new GradientAlphaKey(0f, 1f) });
                s.trail.colorGradient = g;
            }

            Vector3 At(float t)
            {
                Vector2 a = from.Position + new Vector2(0f, HandLift), b = to.Position + new Vector2(0f, ChestLift + 2f), d = b - a;
                Vector2 n = new Vector2(-d.y, d.x).normalized * side;
                Vector2 p = Vector2.Lerp(a, b, t) + n * Mathf.Sin(t * Mathf.PI) * Mathf.Min(14f, d.magnitude * 0.25f) + new Vector2(0f, Mathf.Sin(t * Mathf.PI) * 8f);
                return new Vector3(p.x, p.y, 0f);
            }

            void Update()
            {
                if (from == null || to == null) { Destroy(gameObject); return; }
                age += GameClock.Dt * 1000f;
                float t = Mathf.Clamp01(age / flightMs);
                if (t < 1f) { transform.position = At(t * t * (3f - 2f * t)); return; }
                if (head.enabled) { head.enabled = false; trail.emitting = false; transform.position = At(1f); if (onLand != null) onLand(to); }
                if (age > flightMs + 200f) Destroy(gameObject);
            }
        }

        /// <summary>What a blessed hero wears for the buff's time: a small pulsing cream star beside the back shoulder and a rare rising teal mote.</summary>
        sealed class Aura : MonoBehaviour
        {
            const float FadeMs = 300f, MoteEveryMs = 1300f;
            static readonly Dictionary<HeroController, Aura> Live = new Dictionary<HeroController, Aura>();
            HeroController hero;
            MeshRenderer star;
            ParticleSystem motes;
            float age, delay, total, nextMote, dim;

            public static void Wear(HeroController t, float buffMs, float delayMs, float dim)
            {
                if (t == null) return;
                Aura a;
                if (!Live.TryGetValue(t, out a) || a == null)
                {
                    var go = new GameObject("ClericBlessingAura");
                    go.transform.SetParent(t.transform.parent, false);
                    a = go.AddComponent<Aura>();
                    a.hero = t;
                    a.star = LightSkillAssets.Quad(go.transform, "Star", ClericLightLook.BuffCross, false, 0);
                    a.motes = WantedFxParticles.Create(go.transform, "Motes", ClericLightLook.BuffMote, FeelConfig.FxOrder - 21, 8, 0f);
                    Live[t] = a;
                }
                a.age = 0f; a.delay = delayMs; a.total = buffMs; a.nextMote = delayMs + 400f;
                a.dim = dim >= 0.99f ? 1f : dim >= 0.6f ? 0.6f : 0.25f; // a buff that lasts 12 s: others' star stays faint (crowd)
                a.Lay();
            }

            void Update()
            {
                if (hero == null || hero.IsDead || age >= total) { Destroy(gameObject); return; }
                age += GameClock.Dt * 1000f;
                if (age >= nextMote && FeelConfig.FxOn)
                {
                    nextMote += MoteEveryMs;
                    if (dim < 0.3f) return; // others: no sustained particles at all
                    Color m = ClericLightLook.Teal2;
                    WantedFxParticles.Emit(motes, hero.Position + new Vector2(UnityEngine.Random.Range(-6f, 6f), UnityEngine.Random.Range(0f, 4f)), new Vector2(0f, 14f), 2f, new Color(m.r, m.g, m.b, 0.6f * dim), 1.1f);
                }
            }

            void LateUpdate() { Lay(); }

            void Lay()
            {
                if (hero == null || star == null) return;
                float t = age - delay, k = Mathf.Clamp01((total - age) / FadeMs) * Mathf.Clamp01(t / 250f);
                star.enabled = t > 0f && FeelConfig.FxOn;
                if (!star.enabled) return;
                float pulse = 0.75f + 0.25f * Mathf.Sin(age * 0.004f);
                // Beside the back shoulder, just OUTSIDE the silhouette (tucked behind the hero it hid behind the hair), in the FX band.
                float side = (hero.Facing & 3) == 2 ? 11f : -11f;                                 // facing 2 = left: the back is to the right
                star.transform.position = new Vector3(hero.Position.x + side, hero.Position.y + 17f + Mathf.Sin(age * 0.0025f) * 0.8f, 0f);
                float s = 6.5f * (0.9f + 0.1f * pulse);
                star.transform.localScale = new Vector3(s, s * 1.25f, 1f);
                star.sortingOrder = FeelConfig.FxOrder - 22;
                LightSkillAssets.Set(star, 0.9f * pulse * k * dim);
            }

            void OnDestroy()
            {
                Aura a;
                if ((object)hero != null && Live.TryGetValue(hero, out a) && a == this) Live.Remove(hero);
            }
        }
    }
}
