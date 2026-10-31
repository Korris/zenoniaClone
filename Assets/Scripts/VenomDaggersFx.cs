using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The picture of the assassin's VENOM DAGGERS; the hit and the poison stay in the skill data (SkillRun on the authority, at LandMs), so this runs only where RendersFx.
    /// Timeline (ms): the hero leans back with a poison swirl around him 0-170, flings both arms forward at 170 and seven daggers fan out over the skill's real cone, reaching its edge
    /// at LandMs and no farther; a poison burst on each enemy in the cone, then a skull over every enemy for as long as its poison lasts (the Monster status, replicated online, is the
    /// only truth: no status, no skull). One skull per enemy: a recast takes it over. Screen effects are for the local caster only.
    /// </summary>
    public class VenomDaggersFx : MonoBehaviour
    {
        public const string SkillId = "assassin.venom-daggers";
        public const float LandMs = 330f; // keep equal to the strike's AtMs in SkillTable.Assassin
        const string Folder = "Sprites/fx/venomdaggers/", WindAnim = "melee3", ThrowAnim = "melee4";
        const float WindToMs = 170f, StabToMs = 230f, PoseToMs = 350f, CastToMs = 230f, DaggerFadeMs = 70f, StaggerMs = 14f, BurstLenMs = 380f, SkullDelayMs = 170f, SkullFrameMs = 130f, SkullFadeMs = 220f, SeenGraceMs = 900f, EndMarginMs = 400f;
        const float CastUnitsPerPx = 0.075f, DaggerUnitsPerPx = 0.042f, BurstUnitsPerPx = 0.085f, SkullUnitsPerPx = 0.04f, SkullGap = 7f, CastLift = 0f, HandHeight = 8f, HandOut = 6f;
        const int Daggers = 7, MaxTargets = 8, WindFrame = 1, StabFrame = 2, ThrowFrame = 3;
        static readonly Color Green = new Color(0.35f, 1f, 0.3f), Sour = new Color(0.5f, 1f, 0.35f, 1f);
        static readonly Dictionary<HeroController, VenomDaggersFx> Posing = new Dictionary<HeroController, VenomDaggersFx>();
        static readonly Dictionary<Monster, VenomDaggersFx> SkullOwner = new Dictionary<Monster, VenomDaggersFx>();
        static Sprite[] castArt, daggerArt, burstArt, skullArt;

        sealed class Victim { public Monster M; public SpriteRenderer Burst, Skull, SkullGlow; public Vector2 At; public float Fade = -1f; public bool Seen, Gone; }

        HeroController hero;
        ISkillBody body; // the zone pump finds a skill object's caster through this field: a portal transfer ends the picture with its hero
        float age, radiusUnits, angleDeg, poisonMs;
        bool local, posing, landed;
        Vector2 dir, apex;
        SpriteRenderer castBack, castBackGlow, castVeil, castVeilGlow;
        readonly SpriteRenderer[] daggers = new SpriteRenderer[Daggers], daggerGlows = new SpriteRenderer[Daggers];
        readonly List<Victim> victims = new List<Victim>(MaxTargets);

        public static void Cast(SkillContext ctx)
        {
            if (!NetMode.RendersFx) return;
            HeroController caster = ctx.Body.Hero;
            VenomDaggersFx old;
            if (Posing.TryGetValue(caster, out old) && old != null) old.posing = false; // a recast takes the pose; the old skulls live on
            var go = new GameObject("VenomDaggers");
            go.transform.SetParent(caster.transform.parent, false);
            VenomDaggersFx fx = go.AddComponent<VenomDaggersFx>();
            Posing[caster] = fx;
            fx.Begin(ctx);
        }

        /// <summary>Loads and slices the 24 painted frames ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload()
        {
            if (!NetMode.RendersFx) return;
            if (castArt == null) castArt = AssassinBoardFx.LoadWhole(Folder, "cast", CastUnitsPerPx, 0.5f, 0.12f);
            if (daggerArt == null) daggerArt = AssassinBoardFx.LoadWhole(Folder, "dagger", DaggerUnitsPerPx, 0.92f, 0.5f); // pivot near the tip: the tip is what reaches the cone's edge
            if (burstArt == null) burstArt = AssassinBoardFx.LoadWhole(Folder, "burst", BurstUnitsPerPx);
            if (skullArt == null) skullArt = AssassinBoardFx.LoadWhole(Folder, "skull", SkullUnitsPerPx);
        }

        void Begin(SkillContext ctx)
        {
            hero = ctx.Body.Hero;
            body = ctx.Body;
            local = body.IsLocal;
            posing = true;
            dir = ctx.Dir;
            SkillEffect e = ctx.Def.Effects[0];
            radiusUnits = e.Radius * ctx.Units;
            angleDeg = e.AngleDeg;
            poisonMs = e.Status.Ms;
            apex = ctx.CastFrom;
            Preload();
            castBack = UltimateFx.Layer(transform, "CastBack", out castBackGlow);
            castVeil = UltimateFx.Layer(transform, "CastVeil", out castVeilGlow);
            castVeilGlow.enabled = false;
            for (int i = 0; i < Daggers; i++) { daggers[i] = UltimateFx.Layer(transform, "Dagger" + i, out daggerGlows[i]); daggers[i].enabled = daggerGlows[i].enabled = false; }
            UltimateFx.Embers(hero.Position + new Vector2(0f, HandHeight), 8, 8f, 30f, Sour, 12f);
            Apply();
        }

        void OnDestroy()
        {
            foreach (Victim v in victims) DropSkull(v);
            if (hero == null) return;
            if (posing) hero.ClearSkillPose();
            VenomDaggersFx fx;
            if (Posing.TryGetValue(hero, out fx) && fx == this) Posing.Remove(hero);
        }

        void DropSkull(Victim v)
        {
            VenomDaggersFx o;
            if (v.M != null && SkullOwner.TryGetValue(v.M, out o) && o == this) SkullOwner.Remove(v.M);
        }

        void Update()
        {
            if (hero == null || hero.IsDead) { Destroy(gameObject); return; } // a fallen caster ends the picture
            age += GameClock.Dt * 1000f;
            if (!landed && age >= LandMs) Land();
            Apply();
            if (landed && age >= LandMs + BurstLenMs && AllGone()) Destroy(gameObject);
        }

        bool AllGone()
        {
            foreach (Victim v in victims) if (!v.Gone) return false;
            return true;
        }

        /// <summary>The daggers arrive: every enemy the cone covers (the same test as the skill data; here it only chooses where to draw, the status decides who keeps a skull).</summary>
        void Land()
        {
            landed = true;
            float u = radiusUnits;
            IReadOnlyList<Monster> all = Monster.All;
            for (int i = 0; i < all.Count && victims.Count < MaxTargets; i++)
            {
                Monster m = all[i];
                if (m.IsDying) continue;
                Rect b = m.Box;
                if (!SkillGeometry.Hits(AreaShape.Cone, apex.x, apex.y, dir.x, dir.y, u, angleDeg, 0f, 0f, b.xMin, b.yMin, b.xMax, b.yMax)) continue;
                var v = new Victim { M = m, At = m.Center };
                SpriteRenderer burstGlow;
                v.Burst = UltimateFx.Layer(transform, "Burst" + i, out burstGlow);
                v.Burst.sortingOrder = FeelConfig.FxOrder - 3; burstGlow.sortingOrder = FeelConfig.FxOrder - 2;
                burstGlow.color = new Color(Green.r, Green.g, Green.b, 0.4f);
                v.Skull = UltimateFx.Layer(transform, "Skull" + i, out v.SkullGlow);
                v.Skull.enabled = v.SkullGlow.enabled = false;
                victims.Add(v);
                SkullOwner[m] = this; // the newest cast draws this enemy's skull
                UltimateFx.Embers(v.At, 10, 8f, 45f, Sour);
            }
            if (local && victims.Count > 0) { CameraFollow.AddTrauma(0.18f); GameClock.HitStop(25f); CameraFollow.Punch(0.12f); }
        }

        void ApplyPose()
        {
            if (!posing) return;
            if (age >= PoseToMs) { hero.ClearSkillPose(); posing = false; return; }
            if (age < WindToMs) hero.SetSkillPose(WindAnim, WindFrame, 0f);
            else if (age < StabToMs) hero.SetSkillPose(ThrowAnim, StabFrame, 0f);
            else hero.SetSkillPose(ThrowAnim, ThrowFrame, 0f);
        }

        void ApplyCast()
        {
            bool on = castArt != null && age < CastToMs;
            castBack.enabled = castVeil.enabled = on;
            castBackGlow.enabled = on && FeelConfig.FxOn;
            if (!on) return;
            Sprite s = castArt[AssassinBoardFx.ByProgress(age / CastToMs)];
            float a = Mathf.Clamp01((CastToMs - age) / 60f);
            castBack.sprite = castBackGlow.sprite = castVeil.sprite = s;
            castBack.color = new Color(1f, 1f, 1f, 0.9f * a);
            castBackGlow.color = new Color(Green.r, Green.g, Green.b, 0.3f * a);
            castVeil.color = new Color(1f, 1f, 1f, 0.2f * a); // only a faint veil over the body: the hero stays readable inside the swirl
            Vector2 at = hero.Position + new Vector2(0f, CastLift);
            castBack.transform.position = castVeil.transform.position = at;
            int order = GameConfig.OrderBase(hero.Position.y);
            castBack.sortingOrder = order - 2; castBackGlow.sortingOrder = order - 1;
            castVeil.sortingOrder = order + 7;
        }

        /// <summary>The fan: the outer daggers leave a little later, every tip ends exactly on the cone's edge and fades there.</summary>
        void ApplyDaggers()
        {
            Vector2 hand = apex + new Vector2(0f, HandHeight) + dir * HandOut;
            float span = Mathf.Max(1f, LandMs - WindToMs);
            for (int i = 0; i < Daggers; i++)
            {
                int off = Mathf.Abs(i - Daggers / 2);
                float deg = (i - Daggers / 2) * angleDeg / (Daggers - 1), start = WindToMs + off * StaggerMs, t = (age - start) / Mathf.Max(1f, span - off * StaggerMs);
                bool on = daggerArt != null && t >= 0f && age < LandMs + DaggerFadeMs;
                daggers[i].enabled = on;
                daggerGlows[i].enabled = on && FeelConfig.FxOn;
                if (!on) continue;
                Vector2 way = Rotate(dir, deg), end = apex + way * radiusUnits;
                Vector2 from = hand + way * 2f;
                Vector2 at = Vector2.Lerp(from, end, Mathf.Clamp01(t));
                Vector2 line = end - from;
                float angle = Mathf.Atan2(line.y, line.x) * Mathf.Rad2Deg;
                daggers[i].transform.position = at;
                daggers[i].transform.rotation = Quaternion.Euler(0f, 0f, angle);
                daggers[i].sprite = daggerGlows[i].sprite = daggerArt[AssassinBoardFx.ByProgress(t)];
                float a = age < LandMs ? 1f : Mathf.Clamp01(1f - (age - LandMs) / DaggerFadeMs);
                daggers[i].color = new Color(1f, 1f, 1f, a);
                daggerGlows[i].color = new Color(Green.r, Green.g, Green.b, 0.35f * a);
                daggers[i].sortingOrder = FeelConfig.FxOrder - 3; daggerGlows[i].sortingOrder = FeelConfig.FxOrder - 2;
            }
        }

        void ApplyVictim(Victim v, float since)
        {
            float f = since / BurstLenMs;
            bool burstOn = burstArt != null && f < 1f;
            v.Burst.enabled = burstOn;
            SpriteRenderer burstGlow = v.Burst.transform.childCount > 0 ? v.Burst.transform.GetChild(0).GetComponent<SpriteRenderer>() : null;
            if (burstGlow != null) burstGlow.enabled = burstOn && FeelConfig.FxOn;
            if (burstOn)
            {
                v.Burst.sprite = burstArt[AssassinBoardFx.ByProgress(f)];
                if (burstGlow != null) burstGlow.sprite = v.Burst.sprite;
                v.Burst.transform.position = v.At;
                v.Burst.color = new Color(1f, 1f, 1f, Mathf.Min(1f, (1f - f) * 4f));
            }
            if (v.Gone) return;
            bool dead = v.M == null || v.M.IsDying;
            VenomDaggersFx owner;
            bool mine = !dead && SkullOwner.TryGetValue(v.M, out owner) && owner == this;
            bool poisoned = !dead && IsPoisoned(v.M);
            if (poisoned) v.Seen = true;
            if (!mine || (!poisoned && (v.Seen || since > SeenGraceMs)) || since > poisonMs + EndMarginMs)
            {
                if (v.Fade < 0f) v.Fade = v.Seen && mine && !dead ? SkullFadeMs : 0f;
                v.Fade -= GameClock.Dt * 1000f;
                if (v.Fade <= 0f) { v.Skull.enabled = v.SkullGlow.enabled = false; DropSkull(v); v.Gone = true; return; }
            }
            bool skullOn = skullArt != null && v.Seen && since >= SkullDelayMs;
            v.Skull.enabled = skullOn;
            v.SkullGlow.enabled = skullOn && FeelConfig.FxOn;
            if (!skullOn) return;
            float a = v.Fade >= 0f ? Mathf.Clamp01(v.Fade / SkullFadeMs) : Mathf.Clamp01((since - SkullDelayMs) / 120f);
            v.Skull.sprite = v.SkullGlow.sprite = skullArt[Mathf.FloorToInt(since / SkullFrameMs) % AssassinBoardFx.Frames];
            v.Skull.transform.position = v.M.Feet + new Vector2(0f, v.M.OverheadUnits + SkullGap + Mathf.Sin(since * 0.005f) * 0.8f);
            v.Skull.color = new Color(1f, 1f, 1f, a);
            v.SkullGlow.color = new Color(0.6f, 0.3f, 1f, 0.35f * a);
            int order = GameConfig.OrderBase(v.M.Feet.y) + 8;
            v.Skull.sortingOrder = order; v.SkullGlow.sortingOrder = order;
        }

        /// <summary>A client only hears the dominant status: a stronger one may hide the poison without ending it, so it does not take the skull away.</summary>
        static bool IsPoisoned(Monster m)
        {
            MonsterStatus st = m.Status;
            if (st.Has(StatusKind.Poison)) return true;
            if (!m.IsProxy) return false;
            StatusKind d = st.Dominant;
            return d == StatusKind.Freeze || d == StatusKind.Stun || d == StatusKind.Burn;
        }

        static Vector2 Rotate(Vector2 v, float deg)
        {
            float a = deg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        void Apply()
        {
            if (hero == null) return;
            ApplyPose();
            ApplyCast();
            ApplyDaggers();
            for (int i = 0; i < victims.Count; i++) ApplyVictim(victims[i], age - LandMs);
        }
    }
}
