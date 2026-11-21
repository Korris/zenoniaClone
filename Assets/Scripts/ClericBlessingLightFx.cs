using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Light picture of Spirit Blessing (+ATK ally buff), RO3 Soft Fantasy V1 (spec 4.2 CLERIC_SPIRIT_BLESSING_V1, Blessing + Impositio Manus),
    /// Holy palette gold #F2D078 / ivory #FFF1BC / core #FFFBE5. ClericBuffFx still runs the caster's raise-hand pose; times are its (cast 620 ms,
    /// hand up at 110 ms, streams 180-460 ms), the buff itself is skill data (applied at the cast, atMs 0, 60 s).
    ///  - caster: a faint gold pool + a thin gold ring 1.3 H under the feet with a bright arc running round, a small ivory light over the raised
    ///    hand, 6 gold motes / sparks spiralling up (no light column: RO3 keeps none for the +ATK time);
    ///  - one stream per ally (ivory head + gold ribbon, mesh trail), landing at StreamToMs; the caster receives at StreamFromMs;
    ///  - receive burst: ClericBuffReceiveFx (star over the head, ring, two light lines up the body); then ClericBuffStatusFx keeps the dim ring.
    /// Crowd tiers: ClericLightFx.DimFor / ClericLightFxLod.
    /// </summary>
    public static class ClericBlessingLightFx
    {
        public const float CastMs = 620f, RaiseMs = 110f, StreamFromMs = 180f, StreamToMs = 460f, HandLift = 25f, ChestLift = 9f;
        public const int MaxStreams = 8;

        public static void Cast(HeroController caster, List<HeroController> targets, float buffMs)
        {
            if (caster == null || !ClericBuffStatusLook.Ready) return;
            ClericLightLook.Pal p = ClericLightLook.Holy;
            var fx = ClericLightFx.Create("Blessing", caster.Position, caster, CastMs, ClericLightFx.DimFor(caster, null));
            fx.Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Fill), 0f, 560f, 16f, 28f, 1f).Kind(LayerKind.Glow)
              .Ground(ClericLightLook.M(ClericLightLook.RuneRing, p, ClericLightLook.Role.Arc), 0f, CastMs, 18f / 0.955f, 27f / 0.955f, 0.9f, ClericLightFx.Squash, 0f, 5).Arc(5f, 0f, 2).Env(60f, 260f)
              .Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, RaiseMs, 480f, 4f, 8f, 4f, 8f, 0.65f, new Vector2(0f, HandLift));
            for (int i = 0; i < 6; i++)
            {
                int k = i;
                fx.At(RaiseMs + k * 50f, f =>
                {
                    if (!f.Own && k % 3 != 0) return;
                    float a = k * 2.4f;
                    Vector2 at = f.Anchor + new Vector2(Mathf.Cos(a) * 10f, Mathf.Sin(a) * 4f + 2f);
                    bool spark = k % 2 == 1;
                    f.Emit(spark, at, new Vector2(-Mathf.Sin(a) * 8f, 24f), spark ? UnityEngine.Random.Range(1.6f, 2.2f) : UnityEngine.Random.Range(1.8f, 2.6f),
                        spark ? ClericLightLook.A(p.Light, 0.95f) : ClericLightLook.A(p.Main, 0.9f), 0.5f);
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

        /// <summary>`n` sparks rising beside the body (left / right of the silhouette, never across the face). Also used by Heal, Guardian Spirit.</summary>
        public static void SideSparks(ClericLightFx f, int n, Color c, float life)
        {
            n = f.Count(n);
            for (int i = 0; i < n; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                Vector2 at = f.Anchor + new Vector2(side * UnityEngine.Random.Range(6f, 10f), UnityEngine.Random.Range(1f, 14f));
                f.Emit(true, at, new Vector2(side * UnityEngine.Random.Range(0f, 3f), UnityEngine.Random.Range(16f, 26f)), UnityEngine.Random.Range(1.5f, 2.3f),
                    new Color(c.r, c.g, c.b, 0.95f), life * UnityEngine.Random.Range(0.8f, 1.15f));
            }
        }

        /// <summary>One light stream from the caster's raised hand to an ally's chest: arcing light head + short ribbon in the buff's colours, `onLand` when it arrives.</summary>
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
                s.head.transform.localScale = new Vector3(5f, 5f, 1f);
                bool own = ClericLightFx.TierOf(dim) == FxTier.Own;
                LightSkillAssets.Set(s.head, own ? 1f : 0.6f);
                s.trail = ClericAttackLightFx.MakeTrail(go.transform, own ? 2.4f : 1.3f, 0.15f, own ? 1f : 0.5f, look.P); // 0.06-0.12 H, 0.15 s tail (rule 9)
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
