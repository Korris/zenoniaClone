using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Light picture of Spirit Blessing (+ATK ally buff). ClericBuffFx still runs the caster's raise-hand pose; times are its (cast 620 ms, hand up at
    /// 110 ms, streams 180-460 ms), the buff itself is skill data (applied at the cast, atMs 0). 2026-11-21: warm gold / cream after the concept the
    /// user picked (docs/reference/cleric-buff-status-concept/tay-buff-concept-1.png, ClericBuffStatusLook.Atk), replacing the teal pass of 20/11:
    ///  - caster: gold halo pool + gold compass rune circle with a running bright arc, a small cream light over the raised hand, cream sparks
    ///    spiralling up;
    ///  - one stream per ally (cream head + gold ribbon, mesh trail), landing at StreamToMs; the caster receives at StreamFromMs;
    ///  - receive burst: ClericBuffReceiveFx (column, compass circle, swirls, sword emblem); after it ClericBuffStatusFx keeps the small maintained
    ///    look for the buff's real time.
    /// Crowd tiers (ClericLightFx.DimFor): own / on me full, party 70 %, others 35 % with a quarter of the particles.
    /// </summary>
    public static class ClericBlessingLightFx
    {
        public const float CastMs = 620f, RaiseMs = 110f, StreamFromMs = 180f, StreamToMs = 460f, HandLift = 25f, ChestLift = 9f;
        public const int MaxStreams = 8;

        public static void Cast(HeroController caster, List<HeroController> targets, float buffMs)
        {
            if (caster == null || !ClericBuffStatusLook.Ready) return;
            ClericBuffStatusLook.Set g = ClericBuffStatusLook.Atk;
            Color cream = g.Light, gold = g.Main;
            var fx = ClericLightFx.Create("Blessing", caster.Position, caster, CastMs, ClericLightFx.DimFor(caster, null)).Particles(ClericBuffStatusLook.Spark, ClericBuffStatusLook.Mote);
            fx.Ground(g.Halo, 0f, 560f, 22f, 38f, 1f, 0.6f)
              .Ground(g.Compass, 0f, 600f, 26f, 36f, 0.85f, 0.6f, 0f, 5).Arc(4f, 0f, 2)
              .Upright(g.Flash, ClericLightFx.Band.Over, false, RaiseMs, 480f, 4f, 9f, 4f, 9f, 0.6f, new Vector2(0f, HandLift));
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
                        spark ? new Color(cream.r, cream.g, cream.b, 0.95f) : new Color(gold.r, gold.g, gold.b, 0.85f), 0.5f);
                });
            }
            LaunchAll(fx, caster, targets, true);
        }

        /// <summary>Streams to every ally (the caster receives at StreamFromMs), each target's receive burst + maintained status. Shared with Divine Aegis.</summary>
        public static void LaunchAll(ClericLightFx fx, HeroController caster, List<HeroController> targets, bool atk)
        {
            ClericBuffStatusLook.Set look = atk ? ClericBuffStatusLook.Atk : ClericBuffStatusLook.Def;
            int streams = 0;
            foreach (HeroController t in targets)
            {
                if (t == null) continue;
                float dimT = ClericLightFx.DimFor(caster, t);
                if (t == caster) { fx.At(StreamFromMs, f => ClericBuffReceiveFx.Receive(t, atk, dimT)); ClericBuffStatusFx.Ensure(t, ClericBuffStatusFx.HoldForCastMs); continue; }
                if (streams++ >= MaxStreams) { ClericBuffStatusFx.Ensure(t, StreamToMs); continue; } // crowd: no stream / burst past 8, only the status
                bool left = streams % 2 == 0;
                fx.At(StreamFromMs, f => Stream.Launch(caster, t, left, dimT, StreamToMs - StreamFromMs, look, h => ClericBuffReceiveFx.Receive(h, atk, dimT)));
                ClericBuffStatusFx.Ensure(t, ClericBuffStatusFx.HoldForCastMs);
            }
        }

        /// <summary>`n` sparks rising beside the body (left / right of the silhouette, never across the face). Also used by Heal.</summary>
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

        /// <summary>One light stream from the caster's raised hand to an ally's chest: arcing light head + ribbon in the buff's colours, `onLand` when it arrives.</summary>
        public sealed class Stream : MonoBehaviour
        {
            HeroController from, to;
            float age, side, flightMs;
            MeshRenderer head;
            TrailRenderer trail;
            Action<HeroController> onLand;

            public static void Launch(HeroController from, HeroController to, bool left, float dim, float flightMs, ClericBuffStatusLook.Set look, Action<HeroController> onLand)
            {
                if (from == null || to == null || Zone.Main == null) return;
                var go = new GameObject("ClericBuffStream");
                go.transform.SetParent(Zone.Main.Root, false);
                var s = go.AddComponent<Stream>();
                s.from = from; s.to = to; s.side = left ? 1f : -1f; s.flightMs = Mathf.Max(1f, flightMs); s.onLand = onLand;
                go.transform.position = s.At(0f);
                s.head = LightSkillAssets.Quad(go.transform, "Head", look.Head, false, FeelConfig.FxOrder - 12);
                s.head.transform.localScale = new Vector3(6f, 6f, 1f);
                LightSkillAssets.Set(s.head, dim);
                s.trail = ClericAttackLightFx.MakeTrail(go.transform, dim >= 0.99f ? 2.6f : 1.4f, 0.16f, dim);
                s.trail.sharedMaterial = look.Trail;
                var g = new Gradient(); // light tone at the head, main colour along the ribbon
                g.SetKeys(
                    new[] { new GradientColorKey(look.Light, 0f), new GradientColorKey(look.Main, 0.3f), new GradientColorKey(look.Main, 1f) },
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
    }
}
