using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Runs one choreography: turns the phase list into time-stamped layers once at cast time (projectile skills shorten the flight to the actual hit
    /// and slide the later phases earlier; piercing impacts start when the head passes each target), then ticks them on the game clock (hit-stop freezes it).
    /// Render only: never touches damage or the sim timeline. Sustain skills end through End(broken) or when their sustain phase runs out.
    /// </summary>
    public sealed class SkillChoreoPlayer : MonoBehaviour
    {
        Choreo def;
        ChoreoEnv env;
        readonly List<ChoreoLayer> layers = new List<ChoreoLayer>();
        float age;
        bool switched, hadFollow;

        public static SkillChoreoPlayer Begin(Choreo def, ChoreoEnv env, Vector2 target, IList<Vector2> hits, Transform parent)
        {
            var go = new GameObject("Choreo_" + def.SkillId);
            go.transform.SetParent(parent, false);
            SkillChoreoPlayer p = go.AddComponent<SkillChoreoPlayer>();
            p.def = def; p.env = env; p.hadFollow = env.Follow != null;
            p.Build(target, hits ?? new Vector2[0]);
            p.Update();
            return p;
        }

        void Build(Vector2 target, IList<Vector2> hits)
        {
            float tile = SkillChoreoPrimitives.Tile, reach = def.TravelTiles * tile;
            bool proj = reach > 0f, pierce = false;
            float tFrom = 0f, tTo = 0f;
            foreach (ChoreoPhase p in def.Phases)
            {
                pierce |= p.PerHit;
                if (proj && p.At == ChoreoAnchor.Travel && tTo == 0f) { tFrom = p.From; tTo = p.To; }
            }
            Vector2 end = target;
            if (proj) end = !pierce && hits.Count > 0 ? hits[0] : env.Origin + env.Dir * reach;
            float frac = proj ? Mathf.Clamp(Vector2.Distance(env.Origin, end) / reach, 0.15f, 1f) : 1f;
            float shift = pierce ? 0f : (tTo - tFrom) * (frac - 1f);
            foreach (ChoreoPhase p in def.Phases)
            {
                if (p.PerHit)
                {
                    int n = Mathf.Max(1, hits.Count);
                    for (int i = 0; i < n; i++)
                    {
                        Vector2 at = hits.Count > 0 ? hits[i] : end;
                        float along = Mathf.Clamp(Vector2.Dot(at - env.Origin, env.Dir), 0f, reach) / reach;
                        float s = tFrom + (tTo - tFrom) * along + (p.From - tTo);
                        layers.Add(new ChoreoLayer(p, s, s + p.To - p.From, at, env, transform));
                    }
                }
                else if (proj && p.At == ChoreoAnchor.Travel) layers.Add(new ChoreoLayer(p, p.From, p.From + (p.To - p.From) * frac, end, env, transform));
                else layers.Add(new ChoreoLayer(p, p.From + (proj && p.From >= tTo ? shift : 0f), p.To + (proj && p.From >= tTo ? shift : 0f), proj ? end : target, env, transform));
            }
        }

        /// <summary>Ends a sustained skill now: plays its break (shield shattered) or expire art, then removes itself. Other skills: just stops.</summary>
        public void End(bool broken)
        {
            if (switched) return;
            List<ChoreoPhase> next = broken ? def.OnBreak : def.OnExpire;
            if (next.Count == 0) { Destroy(gameObject); return; }
            switched = true;
            foreach (ChoreoLayer l in layers) l.Release();
            layers.Clear(); age = 0f;
            Vector2 at = env.Follow != null ? (Vector2)env.Follow.position : env.Origin;
            foreach (ChoreoPhase p in next) layers.Add(new ChoreoLayer(p, p.From, p.To, at, env, transform));
        }

        void Update()
        {
            if (!NetMode.RendersFx || (hadFollow && env.Follow == null)) { Destroy(gameObject); return; } // caster gone: end quietly
            float dt = GameClock.Dt * 1000f;
            age += dt;
            bool live = false;
            foreach (ChoreoLayer l in layers) { l.Tick(age, dt); live |= !l.Done; }
            if (live) return;
            if (!switched && def.OnExpire.Count > 0) End(false); else Destroy(gameObject);
        }

        void OnDestroy() { foreach (ChoreoLayer l in layers) l.Release(); }
    }
}
