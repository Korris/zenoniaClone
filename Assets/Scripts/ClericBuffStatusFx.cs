using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// What a hero wears WHILE Spirit Blessing (+ATK) and / or Divine Aegis (+DEF) last. 2026-11-21 08:00 (user: "hiệu ứng sau khi buff xong cũng
    /// mờ nhạt quá, nên làm 2 cái vòng trên người thay vì chỉ dưới chân"; spec docs/reference/cleric-buff-status-concept/midgard-fx-map-rings-shadows-chatgpt.md
    /// section 3): no ground circle, no swirl, no permanent dome; two thin energy rings round the BODY (ClericBuffBodyRing), solid colour:
    ///  - ATK: amber gold ring at the waist (42 % of the hero height above the feet), 1.2 x the sprite width, depth 0.42, 1 turn / 6 s clockwise
    ///    seen from above, 2 bright segments (30 % of the ring);
    ///  - DEF: cyan ring at the low chest (56 %), 1.3 x the width, 1 turn / 8 s counter-clockwise; a hit taken under it flashes the dome rim 200 ms.
    /// Far half behind the hero, near half in front (below the face). 1-2 motes / s per ring rising beside the body to the shoulders (max ~3 / s
    /// with both). The time is the buff's real one (ClericBuffStatusSource: own state / SelfState / AllyBuff), so it follows refreshes, deaths and
    /// the end on every hero; it fades in after the receive burst (hold) and a ring dissolves over 300 ms when its buff ends. Rings are laid in
    /// LateUpdate on the hero's feet (no lag), no trail. Crowd: own hero full, party 60 %, anyone else 30 % rings only (no motes).
    /// </summary>
    public sealed class ClericBuffStatusFx : MonoBehaviour
    {
        /// <summary>Hold a status created by a cast until its receive burst has played (stream lands at 460 ms + most of the burst).</summary>
        public const float HoldForCastMs = 1060f, HoldAfterReceiveMs = 600f;
        const float FadeInMs = 250f, DissolveMs = 300f, GraceMs = 1500f, BlockMs = 200f;
        // World units: hero body 20.5 tall (HeroLayeredSet.BodyUnits), ~15 wide; ribbon 1.25 wide = ~4 px at 1280x720 (0.3 units / px), ~3.5 px seen
        const float HeroH = HeroLayeredSet.BodyUnits, HeroW = 15f, Depth = 0.42f, RingWidth = 1.25f;
        const float AtkY = 0.42f * HeroH, AtkRx = 0.6f * HeroW, DefY = 0.56f * HeroH, DefRx = 0.65f * HeroW;
        const float AtkTurnsPerMs = -1f / 6000f, DefTurnsPerMs = 1f / 8000f;  // clockwise from above = the angle decreases
        const float MoteEveryMs = 650f, DomeW = 32.5f, DomeH = 35.5f, DomeBaseV = 0.28f;
        static readonly Dictionary<HeroController, ClericBuffStatusFx> Live = new Dictionary<HeroController, ClericBuffStatusFx>();

        /// <summary>Fade-in / dissolve state of one ring.</summary>
        struct RingState { public float On, Gone; }

        HeroController hero;
        ClericBuffBodyRing atkRing, defRing;
        MeshRenderer domeBack, domeFront;
        ParticleSystem motes;
        RingState atkS, defS;
        float age, holdUntil, dim = 1f, lastHp, blockAt = -1e6f, blockAngle, nextAtkMote, nextDefMote = MoteEveryMs * 0.5f;

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
            atkRing = new ClericBuffBodyRing(transform, "AtkRing", a.BodyRing, AtkRx, AtkRx * Depth, RingWidth);
            defRing = new ClericBuffBodyRing(transform, "DefRing", d.BodyRing, DefRx, DefRx * Depth, RingWidth);
            domeBack = LightSkillAssets.Quad(transform, "DomeFlashBack", d.DomeRim, false, 0);
            domeFront = LightSkillAssets.Quad(transform, "DomeFlashFront", d.DomeRim, false, 0);
            domeBack.enabled = domeFront.enabled = false;
            motes = WantedFxParticles.Create(transform, "Motes", ClericBuffStatusLook.Mote, FeelConfig.FxOrder - 21, 12, 0f);
            Lay();
        }

        /// <summary>Fades a ring in while its buff runs (after the hold), dissolves it over the buff's last 300 ms or 300 ms after it vanished.</summary>
        static void Step(ref RingState r, float leftMs, bool show, float dt)
        {
            if (leftMs > 0f)
            {
                if (r.Gone >= 0.999f) r.On = 0f;                                              // a new buff after a dissolve fades in again
                r.Gone = 1f - Mathf.Clamp01(leftMs / DissolveMs);
                r.On = Mathf.MoveTowards(r.On, show ? 1f : 0f, dt / FadeInMs);
            }
            else if (r.On > 0f) r.Gone = Mathf.MoveTowards(r.Gone, 1f, dt / DissolveMs);
            if (r.Gone >= 0.999f && leftMs <= 0f) r.On = 0f;
        }

        static float Visible(RingState r) { return r.On * (1f - r.Gone); }

        void Update()
        {
            if (hero == null) { Destroy(gameObject); return; }
            float dt = GameClock.Dt * 1000f;
            age += dt;
            float atk = ClericBuffStatusSource.LeftMs(hero, ClericBuffStatusSource.Atk), def = ClericBuffStatusSource.LeftMs(hero, ClericBuffStatusSource.Def);
            bool show = age >= holdUntil;
            Step(ref atkS, atk, show, dt);
            Step(ref defS, def, show, dt);
            if (atk <= 0f && def <= 0f && atkS.On <= 0f && defS.On <= 0f && age > Mathf.Max(GraceMs, holdUntil)) { Destroy(gameObject); return; }

            float hp = hero.Hp;
            if (hp < lastHp - 0.5f && Visible(defS) > 0.3f && age - blockAt > BlockMs) { blockAt = age; blockAngle = Random.value * Mathf.PI * 2f; } // HP is replicated: a client sees the block too
            lastHp = hp;

            // Motes: one per ring every ~0.65 s (1.5 / s, ~3 / s with both), own hero and party only (strangers: rings only)
            if (!FeelConfig.FxOn || dim < 0.5f) return;
            if (age >= nextAtkMote) { nextAtkMote = age + MoteEveryMs * Random.Range(0.8f, 1.2f); if (Visible(atkS) > 0.5f) Mote(ClericBuffStatusLook.AtkHi, AtkRx, AtkY, 3.5f); }
            if (age >= nextDefMote) { nextDefMote = age + MoteEveryMs * Random.Range(0.8f, 1.2f); if (Visible(defS) > 0.5f) Mote(ClericBuffStatusLook.DefHi, DefRx, DefY, 3f); }
        }

        /// <summary>A small mote leaving the ring at one of its sides (never in front of the face) and rising to about the shoulders, then fading.</summary>
        void Mote(Color c, float rx, float y, float rise)
        {
            float side = Random.value < 0.5f ? -1f : 1f, a = Random.Range(0.15f, 0.75f);           // |cos| 0.73-0.99: outside the face
            Vector2 at = hero.Position + new Vector2(side * Mathf.Cos(a) * rx, y - Mathf.Sin(a) * rx * Depth * 0.6f);
            WantedFxParticles.Emit(motes, at, new Vector2(side * 0.8f, rise / 0.7f), Random.Range(1.3f, 1.9f), new Color(c.r, c.g, c.b, 0.95f * dim), 0.7f);
        }

        void LateUpdate() { Lay(); }

        void Lay()
        {
            if (hero == null || atkRing == null) return;
            Vector2 feet = hero.Position;
            int order = GameConfig.OrderBase(feet.y);
            atkRing.Lay(feet + new Vector2(0f, AtkY), order, Visible(atkS) * dim, atkS.Gone, age * AtkTurnsPerMs);
            defRing.Lay(feet + new Vector2(0f, DefY), order, Visible(defS) * dim, defS.Gone, age * DefTurnsPerMs + 0.25f);

            // DEF hit: the dome rim flashes 200 ms (a bright arc laps it), no dome otherwise
            float since = age - blockAt, block = since < BlockMs ? 1f - since / BlockMs : 0f, k = block * Visible(defS) * dim;
            bool dome = FeelConfig.FxOn && k > 0.01f;
            domeBack.enabled = domeFront.enabled = dome;
            if (!dome) return;
            Vector3 c = new Vector3(feet.x, feet.y + (0.5f - DomeBaseV) * DomeH, 0f), size = new Vector3(DomeW, DomeH, 1f);
            float lap = blockAngle + (1f - block) * Mathf.PI * 2f;
            Place(domeBack, c, size, order - 4, 0.8f * k, lap);
            Place(domeFront, c, size, order + 8, 0.55f * k, lap);
        }

        static void Place(MeshRenderer r, Vector3 c, Vector3 size, int order, float alpha, float lap)
        {
            r.transform.position = c;
            r.transform.localScale = size;
            r.sortingOrder = order;
            LightSkillAssets.Set(r, alpha);
            LightSkillAssets.SetArc(r, lap, ClericLightLook.ArcHalfWidth, 0.35f, 1f);
        }

        void OnDestroy()
        {
            ClericBuffStatusFx s;
            if ((object)hero != null && Live.TryGetValue(hero, out s) && s == this) Live.Remove(hero);
        }
    }
}
