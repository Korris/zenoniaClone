using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// What a hero wears WHILE Spirit Blessing (+ATK) and / or Divine Aegis (+DEF) last (2026-11-21, user: "hình 1 và hình 2 là lúc vừa buff,
    /// còn sau khi buff chỉ cần show hiệu ứng khi đang có cả 2 hiệu ứng trên người"; concept docs/reference/cleric-buff-status-concept/tay-buff-concept-4.png).
    /// Small and light, no columns, no emblems:
    ///  - ATK: a gold compass rune circle under the feet (slow spin, a bright arc running round) + one gold swirl arc circling the waist;
    ///  - DEF: a small see-through ice-blue dome close round the hero (back half behind the hero, a faint front half over it: rim only, the face
    ///    stays clear) + a blue ring on the ground; a hit taken under it laps a bright arc over the dome for 180 ms;
    ///  - both: dome + gold circle + gold swirl (the blue ring steps aside), one combined look, not two full sets.
    /// The time is the buff's real one (ClericBuffStatusSource: own state / SelfState / AllyBuff), so it follows refreshes, deaths and the
    /// end on every hero; it fades in after the receive burst (hold) and out over the last 300 ms. One per hero. Crowd: own hero full,
    /// party 60 %, anyone else 30 % with no sustained particles.
    /// </summary>
    public sealed class ClericBuffStatusFx : MonoBehaviour
    {
        /// <summary>Hold a status created by a cast until its receive burst has played (stream lands at 460 ms + most of the burst).</summary>
        public const float HoldForCastMs = 1060f, HoldAfterReceiveMs = 600f;
        const float FadeInMs = 250f, FadeOutMs = 300f, GraceMs = 1500f, BlockMs = 180f, Squash = 0.6f;
        const float CircleD = 30f, RingD = 26f, DomeW = 36f, DomeH = 38f, DomeBaseV = 0.28f, SparkEveryMs = 900f;
        static readonly Dictionary<HeroController, ClericBuffStatusFx> Live = new Dictionary<HeroController, ClericBuffStatusFx>();

        HeroController hero;
        MeshRenderer circle, ring, domeBack, domeFront;
        ClericBuffOrbitTrail swirl;
        ParticleSystem sparks;
        float age, holdUntil, atkK, defK, dim = 1f, lastHp, blockAt = -1e6f, blockAngle, nextSpark;

        /// <summary>Makes sure `h` has a status picture (it then reads the real buff time itself); `holdMs` keeps it hidden that long from now.</summary>
        public static void Ensure(HeroController h, float holdMs)
        {
            if (h == null || !NetMode.RendersFx || !ClericBuffStatusLook.Ready) return;
            ClericBuffStatusFx s;
            if (!Live.TryGetValue(h, out s) || s == null)
            {
                var go = new GameObject("ClericBuffStatus");
                go.transform.SetParent(h.transform.parent, false);
                s = go.AddComponent<ClericBuffStatusFx>();
                s.Build(h);
                Live[h] = s;
            }
            s.holdUntil = Mathf.Max(s.holdUntil, s.age + holdMs);
            s.dim = DimOf(h);
        }

        public static void ClearAll()
        {
            foreach (ClericBuffStatusFx s in new List<ClericBuffStatusFx>(Live.Values)) if (s != null) Destroy(s.gameObject);
            Live.Clear();
        }

        static float DimOf(HeroController h)
        {
            HeroController me = LocalPlayer.Hero;
            if (me == null || h == me) return 1f;
            return PartyAllies.Are(me, h) ? 0.6f : 0.3f;
        }

        void Build(HeroController h)
        {
            hero = h;
            lastHp = h.Hp;
            ClericBuffStatusLook.Set a = ClericBuffStatusLook.Atk, d = ClericBuffStatusLook.Def;
            circle = LightSkillAssets.Quad(transform, "AtkCircle", a.Compass, false, 0);
            ring = LightSkillAssets.Quad(transform, "DefRing", d.Ring, false, 0);
            domeBack = LightSkillAssets.Quad(transform, "DomeBack", d.Dome, false, 0);
            domeFront = LightSkillAssets.Quad(transform, "DomeFront", d.Dome, false, 0);
            sparks = WantedFxParticles.Create(transform, "Sparks", ClericBuffStatusLook.Spark, FeelConfig.FxOrder - 21, 12, 0f);
            Lay();
        }

        void Update()
        {
            if (hero == null) { Destroy(gameObject); return; }
            float dt = GameClock.Dt * 1000f;
            age += dt;
            float atk = ClericBuffStatusSource.LeftMs(hero, ClericBuffStatusSource.Atk), def = ClericBuffStatusSource.LeftMs(hero, ClericBuffStatusSource.Def);
            bool show = age >= holdUntil;
            atkK = Mathf.MoveTowards(atkK, show ? Mathf.Clamp01(atk / FadeOutMs) : 0f, dt / FadeInMs);
            defK = Mathf.MoveTowards(defK, show ? Mathf.Clamp01(def / FadeOutMs) : 0f, dt / FadeInMs);
            if (atk <= 0f && def <= 0f && atkK <= 0f && defK <= 0f && age > Mathf.Max(GraceMs, holdUntil)) { Destroy(gameObject); return; }

            float hp = hero.Hp;
            if (hp < lastHp - 0.5f && defK > 0.3f && age - blockAt > BlockMs) { blockAt = age; blockAngle = Random.value * Mathf.PI * 2f; } // HP is replicated: a client sees the block too
            lastHp = hp;

            if (age >= nextSpark && FeelConfig.FxOn)
            {
                nextSpark = age + SparkEveryMs;
                if (dim < 0.5f) return; // strangers: no sustained particles (crowd)
                if (atkK > 0.5f) Emit(ClericBuffStatusLook.AtkCream, new Vector2(Random.Range(-9f, 9f), Random.Range(2f, 10f)), 0.9f);
                if (defK > 0.5f) Emit(ClericBuffStatusLook.DefIce, new Vector2(Random.Range(-11f, 11f), Random.Range(4f, 18f)), 0.8f);
            }
        }

        void Emit(Color c, Vector2 off, float a)
        {
            WantedFxParticles.Emit(sparks, hero.Position + off, new Vector2(0f, 12f), Random.Range(1.4f, 2.1f), new Color(c.r, c.g, c.b, a * dim), 0.9f);
        }

        void LateUpdate() { Lay(); }

        void Lay()
        {
            if (hero == null || circle == null) return;
            bool fx = FeelConfig.FxOn;
            Vector2 feet = hero.Position;
            int order = GameConfig.OrderBase(feet.y);
            float pulse = 0.88f + 0.12f * Mathf.Sin(age * 0.0035f);

            // ATK: gold compass circle on the ground + the swirl
            float ca = 0.75f * atkK * pulse * dim;
            circle.enabled = fx && ca > 0.01f;
            if (circle.enabled)
            {
                circle.transform.position = new Vector3(feet.x, feet.y, 0f);
                circle.transform.localScale = new Vector3(CircleD, CircleD * Squash, 1f);
                circle.sortingOrder = UltimateFx.GroundFxOrder + 3;
                LightSkillAssets.Set(circle, ca);
                LightSkillAssets.SetArc(circle, age * 0.0022f, ClericLightLook.ArcHalfWidth * 0.5f, 0.35f, 2f);
            }
            if (atkK > 0.01f && swirl == null) swirl = ClericBuffOrbitTrail.Spawn(transform, hero, ClericBuffStatusLook.Atk, 13f, 4.5f, 6f, 11f, 1900f, 0f, Random.value * 6.28f, 1.5f, 0.32f, dim);
            if (swirl != null)
            {
                swirl.Alpha = atkK * 0.85f;
                if (atkK <= 0f) { Destroy(swirl.gameObject); swirl = null; }
            }

            // DEF only: blue ring on the ground (with ATK the gold circle takes the ground)
            float ra = 0.6f * defK * (1f - atkK) * pulse * dim;
            ring.enabled = fx && ra > 0.01f;
            if (ring.enabled)
            {
                ring.transform.position = new Vector3(feet.x, feet.y, 0f);
                ring.transform.localScale = new Vector3(RingD / 0.78f, RingD / 0.78f * Squash, 1f); // the band sits at 78 % of the quad
                ring.sortingOrder = UltimateFx.GroundFxOrder + 2;
                LightSkillAssets.Set(ring, ra);
                LightSkillAssets.SetArc(ring, -age * 0.003f, ClericLightLook.ArcHalfWidth, 0.3f, 1f);
            }

            // DEF: the dome, base ellipse centred on the feet (mask base line at 28 % of the quad height)
            float sinceBlock = age - blockAt, block = sinceBlock < BlockMs ? 1f - sinceBlock / BlockMs : 0f;
            bool dome = fx && defK * dim > 0.01f;
            domeBack.enabled = domeFront.enabled = dome;
            if (!dome) return;
            float breathe = 1f + 0.015f * Mathf.Sin(age * 0.0025f);
            Vector3 c = new Vector3(feet.x, feet.y + (0.5f - DomeBaseV) * DomeH * breathe, 0f);
            Vector3 size = new Vector3(DomeW * breathe, DomeH * breathe, 1f);
            domeBack.transform.position = domeFront.transform.position = c;
            domeBack.transform.localScale = domeFront.transform.localScale = size;
            domeBack.sortingOrder = order - 3;
            domeFront.sortingOrder = order + 8;
            LightSkillAssets.Set(domeBack, (0.5f + 0.3f * block) * defK * dim);
            LightSkillAssets.Set(domeFront, (0.2f + 0.3f * block) * defK * dim);
            if (block > 0f)
            {
                float lap = blockAngle + (1f - block) * Mathf.PI * 2f;
                LightSkillAssets.SetArc(domeBack, lap, ClericLightLook.ArcHalfWidth, 0.2f, 1f);
                LightSkillAssets.SetArc(domeFront, lap, ClericLightLook.ArcHalfWidth, 0.2f, 1f);
            }
            else
            {
                LightSkillAssets.SetArc(domeBack, 0f, 0.01f, 0.45f, 1f);                   // at rest: no bright arc, an even faint glow
                LightSkillAssets.SetArc(domeFront, 0f, 0.01f, 0.45f, 1f);
            }
        }

        void OnDestroy()
        {
            ClericBuffStatusFx s;
            if ((object)hero != null && Live.TryGetValue(hero, out s) && s == this) Live.Remove(hero);
        }
    }
}
