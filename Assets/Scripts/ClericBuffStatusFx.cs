using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// What a hero wears WHILE Spirit Blessing (+ATK), Divine Aegis (+DEF) and / or Guardian Spirit (shield) last.
    /// 2026-11-21 10:30 (user: "khi buff dam và defense trên người sẽ có 1 cái vòng bao xung quanh chứ không phải icon trên đầu"; exception to
    /// RO3 rule 10 in docs/sprite-rules.md): no head emblem for these two, a clearly visible ring round the BODY each (ClericBuffBodyRing):
    ///  - ATK: gold-ivory ring at the waist (42 % of the hero height above the feet), 1.2 x the sprite width, depth 0.42, 1 turn / 6 s clockwise
    ///    seen from above, 2 short bright segments (30 % of the ring);
    ///  - DEF: pale-blue ring with gold segments at the low chest (56 %), 1.3 x the width, 1 turn / 8 s counter-clockwise; a hit taken under it
    ///    lights the whole ring for 150 ms. Both buffs = both rings.
    /// Far half behind the hero, near half in front (below the face, never over the face, hair, name or HP bar).
    ///  - Guardian Spirit: ClericGuardianStatus (small halo over the head at ~25 %, flash on a hit, wings flash once if the shield breaks early).
    /// ~1.5 motes / s per ring beside the body for the own hero and party (none for strangers).
    /// The time is the buff's real one (ClericBuffStatusSource: own state / SelfState / AllyBuff), so it follows refreshes, deaths and the end on every
    /// hero; it fades in after the receive burst (hold) and dissolves over 300 ms when its buff ends. Rings are laid in LateUpdate on the hero's feet
    /// (no lag). Crowd: own full (ribbon ~4 px), party 80 % and a little thinner, anyone else 55 % and thinner (~3 px), no motes.
    /// </summary>
    public sealed class ClericBuffStatusFx : MonoBehaviour
    {
        /// <summary>Hold a status created by a cast until its receive burst has played (stream lands at 460 ms + most of the burst).</summary>
        public const float HoldForCastMs = 1000f, HoldAfterReceiveMs = 800f; // receive burst 950 / 1050 ms: the ring fades in as it fades out
        /// <summary>Resting brightness of a small maintained sign (Guardian halo; RO3 rule 10: 15-30 % of the cast).</summary>
        public const float Rest = 0.28f;
        const float FadeInMs = 250f, DissolveMs = 300f, GraceMs = 1500f, HitFlashMs = 150f;
        // World units: hero body 20.5 tall (HeroLayeredSet.BodyUnits), ~15 wide; ribbon 1.25 wide = ~4 px at 1280x720 (0.3 units / px)
        const float HeroH = HeroLayeredSet.BodyUnits, HeroW = 15f, Depth = 0.42f;
        const float AtkY = 0.42f * HeroH, AtkRx = 0.6f * HeroW, DefY = 0.56f * HeroH, DefRx = 0.65f * HeroW;
        const float AtkTurnsPerMs = -1f / 6000f, DefTurnsPerMs = 1f / 8000f;  // clockwise from above = the angle decreases
        const float MoteEveryMs = 650f;
        // Crowd tiers: 0 own hero, 1 party, 2 anyone else
        static readonly float[] TierWidth = { 1.25f, 1.05f, 0.85f }, TierAlpha = { 1f, 0.8f, 0.55f };
        static readonly Dictionary<HeroController, ClericBuffStatusFx> Live = new Dictionary<HeroController, ClericBuffStatusFx>();

        /// <summary>Fade-in / dissolve state of one sign.</summary>
        public struct SignState { public float On, Gone; public float Visible { get { return On * (1f - Gone); } } }

        HeroController hero;
        ClericBuffBodyRing atkRing, defRing;
        ClericGuardianStatus guard;
        ParticleSystem motes;
        SignState atkS, defS, guardS;
        int tier = -1;
        float age, holdUntil, dim = 1f, lastHp, hitAt = -1e6f, nextAtkMote, nextDefMote = MoteEveryMs * 0.5f;

        /// <summary>Makes sure `h` has a status picture (it then reads the real buff time itself); `holdMs` keeps it hidden that long from now.</summary>
        public static void Ensure(HeroController h, float holdMs)
        {
            if (h == null || !ClericLightLook.Active || !ClericBuffStatusLook.Ready) return; // `--cleric-fx legacy`: the painted auras
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
            s.SetTier(TierOf(h));
        }

        public static void ClearAll()
        {
            foreach (ClericBuffStatusFx s in new List<ClericBuffStatusFx>(Live.Values)) if (s != null) Destroy(s.gameObject);
            Live.Clear();
        }

        static int TierOf(HeroController h)
        {
            HeroController me = LocalPlayer.Hero;
            if (me == null || h == me) return 0;
            return PartyAllies.Are(me, h) ? 1 : 2;
        }

        void Build(HeroController h)
        {
            hero = h;
            lastHp = h.Hp;
            guard = new ClericGuardianStatus(transform);
            motes = WantedFxParticles.Create(transform, "Motes", ClericLightLook.DotMat, FeelConfig.FxOrder - 21, 12, 0f);
            SetTier(TierOf(h));
            Lay();
        }

        /// <summary>(Re)builds the two rings at the crowd tier's ribbon width; `dim` (Guardian halo, motes) follows the tier too.</summary>
        void SetTier(int t)
        {
            dim = t == 0 ? 1f : t == 1 ? 0.6f : 0.3f;
            if (t == tier) return;
            tier = t;
            if (atkRing != null) atkRing.Destroy();
            if (defRing != null) defRing.Destroy();
            atkRing = defRing = null;
            Material a = ClericBuffStatusLook.Atk.BodyRing, d = ClericBuffStatusLook.Def.BodyRing;
            float w = TierWidth[t];
            if (a != null) atkRing = new ClericBuffBodyRing(transform, "AtkRing", a, AtkRx, AtkRx * Depth, w);
            if (d != null) defRing = new ClericBuffBodyRing(transform, "DefRing", d, DefRx, DefRx * Depth, w);
        }

        /// <summary>Fades a sign in while its buff runs (after the hold), dissolves it over the buff's last 300 ms or 300 ms after it vanished.</summary>
        static void Step(ref SignState r, float leftMs, bool show, float dt)
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

        void Update()
        {
            if (hero == null) { Destroy(gameObject); return; }
            float dt = GameClock.Dt * 1000f;
            age += dt;
            float atk = ClericBuffStatusSource.LeftMs(hero, ClericBuffStatusSource.Atk), def = ClericBuffStatusSource.LeftMs(hero, ClericBuffStatusSource.Def),
                shieldLeft = ClericBuffStatusSource.LeftMs(hero, ClericBuffStatusSource.Guard);
            bool show = age >= holdUntil;
            Step(ref atkS, atk, show, dt);
            Step(ref defS, def, show, dt);
            Step(ref guardS, shieldLeft, show, dt);
            guard.Track(hero, shieldLeft, age);
            if (atk <= 0f && def <= 0f && shieldLeft <= 0f && atkS.On <= 0f && defS.On <= 0f && guardS.On <= 0f && age > Mathf.Max(GraceMs, holdUntil)) { Destroy(gameObject); return; }

            float hp = hero.Hp;
            bool hit = hp < lastHp - 0.5f || guard.Soaked;                                     // HP is replicated: a client sees the hit too
            lastHp = hp;
            if (hit && age - hitAt > HitFlashMs) { hitAt = age; guard.Flash(age); }

            // Motes: one per ring every ~0.65 s, own hero and party only (strangers: rings only)
            if (!FeelConfig.FxOn || tier >= 2) return;
            if (age >= nextAtkMote) { nextAtkMote = age + MoteEveryMs * Random.Range(0.8f, 1.2f); if (atkS.Visible > 0.5f) Mote(ClericBuffStatusLook.Atk.Light, AtkRx, AtkY, 3.5f); }
            if (age >= nextDefMote) { nextDefMote = age + MoteEveryMs * Random.Range(0.8f, 1.2f); if (defS.Visible > 0.5f) Mote(ClericBuffStatusLook.Def.Light, DefRx, DefY, 3f); }
        }

        /// <summary>A small mote leaving the ring at one of its sides (never in front of the face), rising to about the shoulders, then fading.</summary>
        void Mote(Color c, float rx, float y, float rise)
        {
            float side = Random.value < 0.5f ? -1f : 1f, a = Random.Range(0.15f, 0.75f);           // |cos| 0.73-0.99: outside the face
            Vector2 at = hero.Position + new Vector2(side * Mathf.Cos(a) * rx, y - Mathf.Sin(a) * rx * Depth * 0.6f);
            WantedFxParticles.Emit(motes, at, new Vector2(side * 0.8f, rise / 0.7f), Random.Range(1.2f, 1.7f), new Color(c.r, c.g, c.b, 0.85f * dim), 0.7f);
        }

        void LateUpdate() { Lay(); }

        void Lay()
        {
            if (hero == null || guard == null) return;
            Vector2 feet = hero.Position;
            int order = GameConfig.OrderBase(feet.y);
            float since = age - hitAt, flash = since < HitFlashMs ? 1f - since / HitFlashMs : 0f, k = tier >= 0 ? TierAlpha[tier] : 1f;
            if (atkRing != null) atkRing.Lay(feet + new Vector2(0f, AtkY), order, atkS.On * k, atkS.Gone, age * AtkTurnsPerMs);
            if (defRing != null) defRing.Lay(feet + new Vector2(0f, DefY), order, defS.On * Mathf.Lerp(k, 1f, flash), defS.Gone, age * DefTurnsPerMs + 0.25f, flash);
            guard.Lay(feet, order, guardS.On * dim, guardS.Gone, age, dim);
        }

        void OnDestroy()
        {
            ClericBuffStatusFx s;
            if ((object)hero != null && Live.TryGetValue(hero, out s) && s == this) Live.Remove(hero);
        }
    }
}
