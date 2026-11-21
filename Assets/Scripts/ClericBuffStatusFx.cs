using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// What a hero wears WHILE Spirit Blessing (+ATK) and / or Divine Aegis (+DEF) last (2026-11-21, user: "hình 1 và hình 2 là lúc vừa buff,
    /// còn sau khi buff chỉ cần show hiệu ứng khi đang có cả 2 hiệu ứng trên người"; concept docs/reference/cleric-buff-status-concept/tay-buff-concept-4.png,
    /// tuned after the in-game review ingame-review-chatgpt.md). Small and light, no columns, no emblems:
    ///  - ATK: a gold compass rune circle under the feet (a bright arc runs round it, 1 turn / 8 s) + one gold swirl arc circling the hips
    ///    (hidden while the hero runs: no trail dragged behind);
    ///  - DEF: a small cyan-jade dome close round the hero, fresnel style (face behind the hero ~0.1, rim 0.4; in front of the hero only the rim,
    ///    so the face stays clear) + a cyan ring on the ground; a hit taken under it laps a bright arc over the dome for 180 ms;
    ///  - both: two concentric rings, gold inside (~1.2 x hero width, 1 turn / 8 s, alpha 0.35) and cyan outside (~1.5 x, counter-turning
    ///    1 turn / 12 s, alpha 0.3), ~3 small motes / s, the dome reduced to faint side rim arcs (0.12) that flash on a hit; no swirl.
    /// The time is the buff's real one (ClericBuffStatusSource: own state / SelfState / AllyBuff), so it follows refreshes, deaths and the
    /// end on every hero, whatever the buff's length; it fades in after the receive burst (hold) and out over the last 300 ms. One per hero.
    /// Rings are laid in LateUpdate on the hero's feet (no lag). Crowd: own hero full, party 60 %, anyone else 30 % with no sustained particles.
    /// </summary>
    public sealed class ClericBuffStatusFx : MonoBehaviour
    {
        /// <summary>Hold a status created by a cast until its receive burst has played (stream lands at 460 ms + most of the burst).</summary>
        public const float HoldForCastMs = 1060f, HoldAfterReceiveMs = 600f;
        const float FadeInMs = 250f, FadeOutMs = 300f, GraceMs = 1500f, BlockMs = 180f, Squash = 0.6f;
        // World units (hero ~15 wide, ~20 tall). Gold circle 26 alone (review: -12 %), 19 inside the cyan 23.5 when both run.
        const float CircleD = 26f, CircleBothD = 19f, RingD = 26f, RingBothD = 23.5f, DomeW = 32.5f, DomeH = 35.5f, DomeBaseV = 0.28f;
        const float GoldTurn = Mathf.PI * 2f / 8f, CyanTurn = -Mathf.PI * 2f / 12f, SparkEveryMs = 900f, BothSparkEveryMs = 330f;
        static readonly Dictionary<HeroController, ClericBuffStatusFx> Live = new Dictionary<HeroController, ClericBuffStatusFx>();

        HeroController hero;
        MeshRenderer circle, ring, domeFace, domeRimBack, domeRimFront, domeSide;
        ClericBuffOrbitTrail swirl;
        ParticleSystem sparks;
        float age, holdUntil, atkK, defK, dim = 1f, lastHp, blockAt = -1e6f, blockAngle, nextSpark, still = 1f;
        Vector2 lastFeet;
        bool sparkGold;

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
            lastFeet = h.Position;
            ClericBuffStatusLook.Set a = ClericBuffStatusLook.Atk, d = ClericBuffStatusLook.Def;
            circle = LightSkillAssets.Quad(transform, "AtkCircle", a.Compass, false, 0);
            ring = LightSkillAssets.Quad(transform, "DefRing", d.Ring, false, 0);
            domeFace = LightSkillAssets.Quad(transform, "DomeFace", d.Dome, false, 0);
            domeRimBack = LightSkillAssets.Quad(transform, "DomeRimBack", d.DomeRim, false, 0);
            domeRimFront = LightSkillAssets.Quad(transform, "DomeRimFront", d.DomeRim, false, 0);
            domeSide = LightSkillAssets.Quad(transform, "DomeSide", d.DomeSide, false, 0);
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

            // Running: the swirl fades out (a mesh trail would smear behind the hero) and comes back when the hero stands
            Vector2 feetNow = hero.Position;
            bool moving = (feetNow - lastFeet).sqrMagnitude > 0.0025f;
            lastFeet = feetNow;
            still = Mathf.MoveTowards(still, moving ? 0f : 1f, dt / (moving ? 120f : 400f));

            float hp = hero.Hp;
            if (hp < lastHp - 0.5f && defK > 0.3f && age - blockAt > BlockMs) { blockAt = age; blockAngle = Random.value * Mathf.PI * 2f; } // HP is replicated: a client sees the block too
            lastHp = hp;

            if (age >= nextSpark && FeelConfig.FxOn)
            {
                bool both = atkK > 0.5f && defK > 0.5f;
                nextSpark = age + (both ? BothSparkEveryMs : SparkEveryMs);                    // both: ~3 small motes / s, gold and cyan in turn
                if (dim < 0.5f) return; // strangers: no sustained particles (crowd)
                sparkGold = !sparkGold;
                bool gold = both ? sparkGold : atkK > 0.5f;
                if (gold && atkK > 0.5f) Emit(ClericBuffStatusLook.AtkCream, new Vector2(Random.Range(-9f, 9f), Random.Range(1f, 8f)), 0.9f);
                else if (defK > 0.5f) Emit(ClericBuffStatusLook.DefIce, new Vector2(Random.Range(-11f, 11f), Random.Range(2f, 14f)), 0.8f);
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
            float pulse = 0.9f + 0.1f * Mathf.Sin(age * 0.0035f);
            float both = Mathf.Min(atkK, defK);                                                // 0 = one buff, 1 = both (blends while one fades)
            float sinceBlock = age - blockAt, block = sinceBlock < BlockMs ? 1f - sinceBlock / BlockMs : 0f;

            // Gold compass circle: alone 26 / alpha 0.75, inner ring 19 / alpha 0.35 when both run; its bright arcs turn once per 8 s
            float ca = atkK * Mathf.Lerp(0.75f, 0.35f, both) * pulse * dim;
            circle.enabled = fx && ca > 0.01f;
            if (circle.enabled)
            {
                float d = Mathf.Lerp(CircleD, CircleBothD, both);
                circle.transform.position = new Vector3(feet.x, feet.y, 0f);
                circle.transform.localScale = new Vector3(d, d * Squash, 1f);
                circle.sortingOrder = UltimateFx.GroundFxOrder + 3;
                LightSkillAssets.Set(circle, ca);
                LightSkillAssets.SetArc(circle, age * 0.001f * GoldTurn, ClericLightLook.ArcHalfWidth * 0.5f, 0.35f, 2f);
            }

            // Cyan ring: alone 26 / alpha 0.6, outer ring 23.5 / alpha 0.3 round the gold one when both run; counter-turns once per 12 s
            float ra = defK * Mathf.Lerp(0.6f, 0.3f, both) * pulse * dim;
            ring.enabled = fx && ra > 0.01f;
            if (ring.enabled)
            {
                float d = Mathf.Lerp(RingD, RingBothD, both) / 0.78f;                         // the band sits at 78 % of the quad
                ring.transform.position = new Vector3(feet.x, feet.y, 0f);
                ring.transform.localScale = new Vector3(d, d * Squash, 1f);
                ring.sortingOrder = UltimateFx.GroundFxOrder + 2;
                LightSkillAssets.Set(ring, ra);
                LightSkillAssets.SetArc(ring, age * 0.001f * CyanTurn, ClericLightLook.ArcHalfWidth, 0.3f, 1f);
            }

            // Swirl: ATK alone only, feet to hips, hidden while running
            float sw = atkK * (1f - both) * still * 0.97f;
            if (sw > 0.01f && swirl == null) swirl = ClericBuffOrbitTrail.Spawn(transform, hero, ClericBuffStatusLook.Atk, 12f, 4f, 3f, 9f, 1900f, 0f, Random.value * 6.28f, 1.85f, 0.32f, dim);
            if (swirl != null)
            {
                swirl.Alpha = sw;
                if (atkK <= 0f) { Destroy(swirl.gameObject); swirl = null; }
            }

            // Dome (base ellipse centred on the feet; mask base line at 28 % of the quad height). DEF alone: face + rims; both: side arcs only.
            float k = defK * dim, solo = k * (1f - both), side = k * both;
            bool dome = fx && k > 0.01f;
            domeFace.enabled = domeRimBack.enabled = domeRimFront.enabled = dome && solo > 0.01f;
            domeSide.enabled = dome && side > 0.01f;
            if (!dome) return;
            float breathe = 1f + 0.015f * Mathf.Sin(age * 0.0025f);
            Vector3 c = new Vector3(feet.x, feet.y + (0.5f - DomeBaseV) * DomeH * breathe, 0f);
            Vector3 size = new Vector3(DomeW * breathe, DomeH * breathe, 1f);
            float lap = block > 0f ? blockAngle + (1f - block) * Mathf.PI * 2f : -1f;
            Place(domeFace, c, size, order - 4, (0.35f + 0.15f * block) * solo, -1f);
            Place(domeRimBack, c, size, order - 3, (0.4f + 0.3f * block) * solo, lap);
            Place(domeRimFront, c, size, order + 8, (0.25f + 0.3f * block) * solo, lap);
            Place(domeSide, c, size, order + 8, (0.12f + 0.35f * block) * side, lap);
        }

        /// <summary>One dome layer; `lap` &gt;= 0 runs the bright hit arc at that angle, else an even faint glow.</summary>
        static void Place(MeshRenderer r, Vector3 c, Vector3 size, int order, float alpha, float lap)
        {
            if (!r.enabled) return;
            r.transform.position = c;
            r.transform.localScale = size;
            r.sortingOrder = order;
            LightSkillAssets.Set(r, alpha);
            bool hit = lap >= 0f;
            LightSkillAssets.SetArc(r, hit ? lap : 0f, hit ? ClericLightLook.ArcHalfWidth : 0.01f, hit ? 0.2f : 0.45f, 1f);
        }

        void OnDestroy()
        {
            ClericBuffStatusFx s;
            if ((object)hero != null && Live.TryGetValue(hero, out s) && s == this) Live.Remove(hero);
        }
    }
}
