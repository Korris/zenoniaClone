using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// What a hero wears WHILE Spirit Blessing (+ATK), Divine Aegis (+DEF) and / or Guardian Spirit (shield) last, RO3 Soft Fantasy V1 rule 10
    /// (2026-11-21): after the cast only a small sign stays, at 15-30 % of the cast's brightness, and it flashes back only on an event.
    ///  - ATK: a small gold 4-point star emblem over the head, ~28 % (user 21/11: small RO3 emblems instead of the two body rings);
    ///  - DEF: a small pale-blue shield emblem beside it, ~28 %; a hit taken under it flashes the emblem to full and the see-through shield in
    ///    front of the chest for 150 ms (spec 4.3 layer 5). With both buffs the two emblems sit side by side, one alone sits centred;
    ///  - Guardian Spirit: ClericGuardianStatus (small halo over the head at ~25 %, flash on a hit, wings flash once if the shield breaks early).
    /// Emblems in the FX band above the name tag. ~1.5 motes / s per buff beside the body for the own hero and party (none for strangers).
    /// The time is the buff's real one (ClericBuffStatusSource: own state / SelfState / AllyBuff), so it follows refreshes, deaths and the end on every
    /// hero; it fades in after the receive burst (hold) and dissolves over 300 ms when its buff ends. Crowd: own full, party 60 %, anyone else 30 %.
    /// </summary>
    public sealed class ClericBuffStatusFx : MonoBehaviour
    {
        /// <summary>Hold a status created by a cast until its receive burst has played (stream lands at 460 ms + most of the burst).</summary>
        public const float HoldForCastMs = 1000f, HoldAfterReceiveMs = 800f; // receive burst 950 / 1050 ms: the small emblem fades in as it fades out
        /// <summary>Resting brightness of a maintained sign (RO3 rule 10: 15-30 % of the cast).</summary>
        public const float Rest = 0.28f;
        const float FadeInMs = 250f, DissolveMs = 300f, GraceMs = 1500f, HitFlashMs = 150f;
        // World units: hero body 20.5 tall (HeroLayeredSet.BodyUnits), ~15 wide; emblem 6 units (~0.3 H, ~20 px at 720p), centre 40 above the
        // feet (name tag ~30), 4.5 units either side of the middle when both show
        const float HeroH = HeroLayeredSet.BodyUnits, HeroW = 15f, EmblemY = 40f, EmblemS = 6f, EmblemDx = 4.5f, AtkY = 0.42f * HeroH, DefY = 0.56f * HeroH;
        const float MoteEveryMs = 650f;
        static readonly Dictionary<HeroController, ClericBuffStatusFx> Live = new Dictionary<HeroController, ClericBuffStatusFx>();

        /// <summary>Fade-in / dissolve state of one sign.</summary>
        public struct SignState { public float On, Gone; public float Visible { get { return On * (1f - Gone); } } }

        HeroController hero;
        MeshRenderer atkEmblem, defEmblem, shield;
        ClericGuardianStatus guard;
        ParticleSystem motes;
        SignState atkS, defS, guardS;
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
            atkEmblem = LightSkillAssets.Quad(transform, "AtkEmblem", ClericLightLook.M(ClericLightLook.Star, ClericLightLook.Holy, ClericLightLook.Role.Glyph), false, 0);
            defEmblem = LightSkillAssets.Quad(transform, "DefEmblem", ClericLightLook.M(ClericLightLook.Shield, ClericLightLook.Guard, ClericLightLook.Role.Glyph), false, 0);
            atkEmblem.enabled = defEmblem.enabled = false;
            shield = LightSkillAssets.Quad(transform, "DefHitShield", ClericLightLook.M(ClericLightLook.Shield, ClericLightLook.Guard, ClericLightLook.Role.Line), false, 0);
            shield.enabled = false;
            guard = new ClericGuardianStatus(transform);
            motes = WantedFxParticles.Create(transform, "Motes", ClericLightLook.DotMat, FeelConfig.FxOrder - 21, 12, 0f);
            Lay();
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
            if (!FeelConfig.FxOn || dim < 0.5f) return;
            if (age >= nextAtkMote) { nextAtkMote = age + MoteEveryMs * Random.Range(0.8f, 1.2f); if (atkS.Visible > 0.5f) Mote(ClericBuffStatusLook.Atk.Light, HeroW * 0.55f, AtkY, 3.5f); }
            if (age >= nextDefMote) { nextDefMote = age + MoteEveryMs * Random.Range(0.8f, 1.2f); if (defS.Visible > 0.5f) Mote(ClericBuffStatusLook.Def.Light, HeroW * 0.6f, DefY, 3f); }
        }

        /// <summary>A small mote rising beside the body (never in front of the face) to about the shoulders, then fading.</summary>
        void Mote(Color c, float rx, float y, float rise)
        {
            float side = Random.value < 0.5f ? -1f : 1f, a = Random.Range(0.15f, 0.75f);           // |cos| 0.73-0.99: outside the face
            Vector2 at = hero.Position + new Vector2(side * Mathf.Cos(a) * rx, y - Mathf.Sin(a) * rx * 0.25f);
            WantedFxParticles.Emit(motes, at, new Vector2(side * 0.8f, rise / 0.7f), Random.Range(1.2f, 1.7f), new Color(c.r, c.g, c.b, 0.8f * dim), 0.7f);
        }

        void LateUpdate() { Lay(); }

        void Lay()
        {
            if (hero == null || atkEmblem == null) return;
            Vector2 feet = hero.Position;
            int order = GameConfig.OrderBase(feet.y);
            float since = age - hitAt, flash = since < HitFlashMs ? 1f - since / HitFlashMs : 0f;
            float atkV = atkS.Visible, defV = defS.Visible;
            Emblem(atkEmblem, feet + new Vector2(-EmblemDx * defV, EmblemY), atkV * dim * Rest, 1f);
            Emblem(defEmblem, feet + new Vector2(EmblemDx * atkV, EmblemY), defV * dim * Mathf.Lerp(Rest, 1f, flash), 1f + 0.2f * flash);
            guard.Lay(feet, order, guardS.On * dim, guardS.Gone, age, dim);

            // DEF hit: the see-through shield in front of the chest flashes 150 ms (rim + faint face, the hero stays visible)
            float k = flash * defV * dim;
            bool on = FeelConfig.FxOn && k > 0.01f;
            shield.enabled = on;
            if (!on) return;
            shield.transform.position = new Vector3(feet.x, feet.y + 11f, 0f);
            shield.transform.localScale = new Vector3(14f, 15.5f, 1f);
            shield.sortingOrder = order + 9;
            LightSkillAssets.Set(shield, 0.5f * k);
        }

        static void Emblem(MeshRenderer r, Vector2 at, float alpha, float scale)
        {
            bool on = FeelConfig.FxOn && alpha > 0.01f;
            r.enabled = on;
            if (!on) return;
            r.transform.position = new Vector3(at.x, at.y, 0f);
            r.transform.localScale = new Vector3(EmblemS * scale, EmblemS * scale, 1f);
            r.sortingOrder = FeelConfig.FxOrder - 22;
            LightSkillAssets.Set(r, alpha);
        }

        void OnDestroy()
        {
            ClericBuffStatusFx s;
            if ((object)hero != null && Live.TryGetValue(hero, out s) && s == this) Live.Remove(hero);
        }
    }
}
