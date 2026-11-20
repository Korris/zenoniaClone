using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// RO3-style light picture of Spirit Blessing (+ATK ally buff, 2026-11-20), replacing its painted gold frames; ClericBuffFx still runs the caster's
    /// raise-hand pose and keeps Divine Aegis painted. Times are ClericBuffFx's (cast 620 ms, hand up at 110 ms, streams 180-460 ms), the buff itself is
    /// skill data. ChatGPT spec (ClericLightLook): "Blessing = light sent out":
    ///  - caster: turning holy rune + soft pool under the feet, a light gathering over the raised hand, mint motes spiralling up;
    ///  - one light stream per ally (glowing head + LightTrail ribbon, arcing), landing at StreamToMs; the caster receives at StreamFromMs;
    ///  - receive: holy cross behind the hero, ring on the ground, sparkles;
    ///  - while the buff lasts: one small pulsing holy star behind the shoulder + a rare rising mote (one per hero, a recast restarts it), faded out over
    ///    the buff's last 300 ms. Never over the face; another hero's star at 30 %.
    /// </summary>
    public static class ClericBlessingLightFx
    {
        const float CastMs = 620f, RaiseMs = 110f, StreamFromMs = 180f, StreamToMs = 460f, HandLift = 25f, ChestLift = 11f;

        public static void Cast(HeroController caster, List<HeroController> targets, float buffMs)
        {
            if (caster == null) return;
            HeroController me = LocalPlayer.Hero;
            bool own = me == null || caster == me;
            Color mint = ClericLightLook.Leaf;
            var fx = ClericLightFx.Create("Blessing", caster.Position, caster, CastMs, own);
            fx.Ground(ClericLightLook.GroundGlow, 0f, 520f, 20f, 34f, 0.8f)
              .Ground(ClericLightLook.Rune, 0f, 560f, 24f, 38f, 0.8f, 0.9f, 60f, 5)
              .Upright(ClericLightLook.Flash, ClericLightFx.Band.Over, false, RaiseMs, 500f, 7f, 16f, 7f, 16f, 0.9f, new Vector2(0f, HandLift))
              .Upright(ClericLightLook.Cross, ClericLightFx.Band.Over, false, RaiseMs + 60f, 480f, 9f, 15f, 12f, 21f, 0.85f, new Vector2(0f, HandLift), 1);
            for (int i = 0; i < 12; i++)
            {
                int k = i;
                fx.At(RaiseMs + k * 38f, f =>
                {
                    float a = k * 2.4f;
                    Vector2 p = f.Anchor + new Vector2(Mathf.Cos(a) * 9f, Mathf.Sin(a) * 4f + 2f);
                    if (f.Own || k % 3 == 0) f.Emit(false, p, new Vector2(-Mathf.Sin(a) * 8f, 26f), Random.Range(2f, 3f), new Color(mint.r, mint.g, mint.b, 0.85f), 0.5f);
                });
            }
            int streams = 0;
            foreach (HeroController t in targets)
            {
                if (t == null) continue;
                bool ownT = own || t == me;
                if (t == caster) { fx.At(StreamFromMs, f => Receive(t, ownT)); Aura.Wear(t, buffMs, StreamFromMs, ownT); continue; }
                if (streams++ >= 8) { Aura.Wear(t, buffMs, StreamToMs, ownT); continue; }
                bool left = streams % 2 == 0;
                fx.At(StreamFromMs, f => Stream.Launch(caster, t, left, own));
                Aura.Wear(t, buffMs, StreamToMs, ownT);
            }
        }

        /// <summary>The moment a hero takes the blessing: holy cross behind them, a ring at the feet, a few sparkles.</summary>
        static void Receive(HeroController t, bool own)
        {
            if (t == null) return;
            Color white = ClericLightLook.Spark;
            ClericLightFx.Create("BlessingReceive", t.Position, t, 350f, own)
                .Ground(ClericLightLook.Ring, 0f, 350f, 14f, 30f, 0.8f, 0.9f, 0f, 5)
                .Upright(ClericLightLook.Column, ClericLightFx.Band.Behind, true, 0f, 320f, 12f, 7f, 34f, 38f, 0.9f, new Vector2(0f, -1f))
                .Upright(ClericLightLook.Cross, ClericLightFx.Band.Behind, false, 0f, 300f, 16f, 26f, 22f, 36f, 0.9f, new Vector2(0f, ChestLift), 1)
                .At(0f, f => f.Puff(true, f.Anchor + new Vector2(0f, 6f), 6, 8f, 22f, 2.2f, 3.4f, white, 0.4f));
        }

        /// <summary>One light stream from the caster's raised hand to an ally's chest: arcing head + ribbon trail, lands at StreamToMs.</summary>
        sealed class Stream : MonoBehaviour
        {
            HeroController from, to;
            float age, side;
            bool own;
            MeshRenderer head;
            TrailRenderer trail;

            public static void Launch(HeroController from, HeroController to, bool left, bool own)
            {
                if (from == null || to == null || Zone.Main == null) return;
                var go = new GameObject("ClericBlessingStream");
                go.transform.SetParent(Zone.Main.Root, false);
                var s = go.AddComponent<Stream>();
                s.from = from; s.to = to; s.side = left ? 1f : -1f; s.own = own;
                go.transform.position = s.At(0f);
                s.head = LightSkillAssets.Quad(go.transform, "Head", ClericLightLook.BoltCore, false, FeelConfig.FxOrder - 12);
                s.head.transform.localScale = new Vector3(8f, 8f, 1f);
                LightSkillAssets.Set(s.head, own ? 1f : 0.45f);
                s.trail = ClericAttackLightFx.MakeTrail(go.transform, own ? 3f : 1.6f, 0.14f, own ? 1f : 0.4f);
            }

            Vector3 At(float t)
            {
                Vector2 a = from.Position + new Vector2(0f, HandLift), b = to.Position + new Vector2(0f, ChestLift), d = b - a;
                Vector2 n = new Vector2(-d.y, d.x).normalized * side;
                Vector2 p = Vector2.Lerp(a, b, t) + n * Mathf.Sin(t * Mathf.PI) * Mathf.Min(14f, d.magnitude * 0.25f) + new Vector2(0f, Mathf.Sin(t * Mathf.PI) * 8f);
                return new Vector3(p.x, p.y, 0f);
            }

            void Update()
            {
                if (from == null || to == null) { Destroy(gameObject); return; }
                age += GameClock.Dt * 1000f;
                float t = Mathf.Clamp01(age / (StreamToMs - StreamFromMs));
                if (t < 1f) { transform.position = At(t * t * (3f - 2f * t)); return; }
                if (head.enabled) { head.enabled = false; trail.emitting = false; transform.position = At(1f); Receive(to, own || to == LocalPlayer.Hero); }
                if (age > StreamToMs - StreamFromMs + 200f) Destroy(gameObject);
            }
        }

        /// <summary>What a blessed hero wears for the buff's time: a small pulsing holy star behind the shoulder and a rare rising mote.</summary>
        sealed class Aura : MonoBehaviour
        {
            const float FadeMs = 300f, MoteEveryMs = 1300f;
            static readonly Dictionary<HeroController, Aura> Live = new Dictionary<HeroController, Aura>();
            HeroController hero;
            MeshRenderer star;
            ParticleSystem motes;
            float age, delay, total, nextMote, dim;

            public static void Wear(HeroController t, float buffMs, float delayMs, bool own)
            {
                if (t == null) return;
                Aura a;
                if (!Live.TryGetValue(t, out a) || a == null)
                {
                    var go = new GameObject("ClericBlessingAura");
                    go.transform.SetParent(t.transform.parent, false);
                    a = go.AddComponent<Aura>();
                    a.hero = t;
                    a.star = LightSkillAssets.Quad(go.transform, "Star", ClericLightLook.Cross, false, 0);
                    a.motes = WantedFxParticles.Create(go.transform, "Motes", ClericLightLook.Mote, FeelConfig.FxOrder - 21, 8, 0f);
                    Live[t] = a;
                }
                a.age = 0f; a.delay = delayMs; a.total = buffMs; a.nextMote = delayMs + 400f; a.dim = own ? 1f : 0.3f;
                a.Lay();
            }

            void Update()
            {
                if (hero == null || hero.IsDead || age >= total) { Destroy(gameObject); return; }
                age += GameClock.Dt * 1000f;
                if (age >= nextMote && FeelConfig.FxOn)
                {
                    nextMote += MoteEveryMs;
                    Color m = ClericLightLook.Leaf;
                    WantedFxParticles.Emit(motes, hero.Position + new Vector2(Random.Range(-6f, 6f), Random.Range(0f, 4f)), new Vector2(0f, 14f), 2f, new Color(m.r, m.g, m.b, 0.5f * dim), 1.1f);
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
                // Beside the back shoulder, just OUTSIDE the silhouette (first capture: tucked behind the hero it hid behind the hair the whole time),
                // so it sits over the hero band without ever touching the face.
                float side = (hero.Facing & 3) == 2 ? 11f : -11f;                                 // facing 2 = left: the back is to the right
                star.transform.position = new Vector3(hero.Position.x + side, hero.Position.y + 17f + Mathf.Sin(age * 0.0025f) * 0.8f, 0f);
                float s = 6.5f * (0.9f + 0.1f * pulse);
                star.transform.localScale = new Vector3(s, s * 1.25f, 1f);
                star.sortingOrder = FeelConfig.FxOrder - 22;                                      // FX band: a monster or hero standing beside must not hide it (2nd capture)
                LightSkillAssets.Set(star, 0.85f * pulse * k * dim);
            }

            void OnDestroy()
            {
                Aura a;
                if ((object)hero != null && Live.TryGetValue(hero, out a) && a == this) Live.Remove(hero);
            }
        }
    }
}
