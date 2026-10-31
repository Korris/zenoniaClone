using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The picture of the assassin's DEATH MARK; the mark itself stays in the skill data (SkillRun on the authority applies the status at LandMs), so this runs only where RendersFx.
    /// Timeline (ms): the hero raises his dagger with a spark at its tip 0-170, points it at the target and a shadow beam shoots there 170-300, at 300 the star appears over the target's
    /// head and the seal locks under its feet, and both stay for as long as the Monster's Mark status lasts. The status is the only truth, on a client through the snapshot's mark bit:
    /// a mark that never shows on the target ends the picture, one that ends early (its charges are spent by the marker's hits) or a dying target shatters it, one that runs out fades.
    /// One picture per target: a recast takes over when its beam lands. While a picture covers a target the Monster's own reticle stays off. Screen effects are for the local caster only.
    /// </summary>
    public class DeathMarkFx : MonoBehaviour
    {
        public const string SkillId = "assassin.death-mark";
        public const float LandMs = 300f; // keep equal to the strike's AtMs in SkillTable.Assassin
        const string Folder = "Sprites/fx/deathmark/", RaiseAnim = "attack3", PointAnim = "melee4";
        const float RaiseToMs = 170f, PoseToMs = 350f, BeamFadeMs = 120f, StarFrameMs = 120f, RingFrameMs = 130f, SeenGraceMs = 900f, ShatterLenMs = 420f, FadeLenMs = 260f, ExpiryMarginMs = 300f, EndMarginMs = 800f;
        const float BeamUnitsPerPx = 0.04f, BeamFullPx = 1010f, BeamPivotX = 60f / 1086f, StarUnitsPerPx = 0.04f, RingUnitsPerPx = 0.065f, RingPivotY = 112f / 512f, ShatterUnitsPerPx = 0.11f, SparkScale = 0.42f, StarGap = 11f, TipRaise = 22f, TipOut = 10f, HandHeight = 9f, HandOut = 14f, RebindRadius = 40f;
        const int RaiseFrame = 1, PointFrame = 3;
        static readonly Color Violet = new Color(0.72f, 0.3f, 1f);
        static readonly Dictionary<Monster, DeathMarkFx> Live = new Dictionary<Monster, DeathMarkFx>();
        static readonly Dictionary<HeroController, DeathMarkFx> Posing = new Dictionary<HeroController, DeathMarkFx>();
        static Sprite[] beamArt, starArt, ringArt, shatterArt;

        enum Stage { Aim, Held, Shatter, Fade }

        HeroController hero;
        ISkillBody body; // the zone pump finds a skill object's caster through this field: a portal transfer ends the picture with its hero
        Monster target;
        Stage stage;
        float age, stageAge, markMs;
        bool local, posing, seen;
        int facing;
        Vector2 lastFeet, dir;
        float lastTop;
        SpriteRenderer beam, beamGlow, star, starGlow, ring, ringGlow, shatter, shatterGlow;

        /// <summary>True while a painted mark stands on this monster (its own reticle would only clutter it).</summary>
        public static bool Covers(Monster m) { DeathMarkFx fx; return Live.TryGetValue(m, out fx) && fx != null; }

        public static void Cast(SkillContext ctx)
        {
            if (!NetMode.RendersFx || ctx.Target == null) return;
            HeroController caster = ctx.Body.Hero;
            DeathMarkFx old;
            if (Posing.TryGetValue(caster, out old) && old != null) old.posing = false;
            var go = new GameObject("DeathMark");
            go.transform.SetParent(caster.transform.parent, false);
            DeathMarkFx fx = go.AddComponent<DeathMarkFx>();
            Posing[caster] = fx;
            fx.Begin(ctx);
        }

        /// <summary>Loads and slices the 24 painted frames ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload()
        {
            if (!NetMode.RendersFx) return;
            if (beamArt == null) beamArt = AssassinBoardFx.LoadWhole(Folder, "beam", BeamUnitsPerPx, BeamPivotX, 0.5f);
            if (starArt == null) starArt = AssassinBoardFx.LoadWhole(Folder, "star", StarUnitsPerPx);
            if (ringArt == null) ringArt = AssassinBoardFx.LoadWhole(Folder, "ring", RingUnitsPerPx, 0.5f, RingPivotY);
            if (shatterArt == null) shatterArt = AssassinBoardFx.LoadWhole(Folder, "shatter", ShatterUnitsPerPx);
        }

        void Begin(SkillContext ctx)
        {
            hero = ctx.Body.Hero;
            body = ctx.Body;
            local = body.IsLocal;
            target = ctx.Target;
            posing = true;
            markMs = ctx.Def.Effects[0].Status.Ms;
            dir = ctx.Dir;
            facing = AssassinBoardFx.FacingOf(dir);
            lastFeet = target.Feet;
            lastTop = target.OverheadUnits;
            Preload();
            beam = UltimateFx.Layer(transform, "Beam", out beamGlow);
            star = UltimateFx.Layer(transform, "Star", out starGlow);
            ring = UltimateFx.Layer(transform, "Ring", out ringGlow);
            shatter = UltimateFx.Layer(transform, "Shatter", out shatterGlow);
            beam.enabled = beamGlow.enabled = star.enabled = starGlow.enabled = ring.enabled = ringGlow.enabled = shatter.enabled = shatterGlow.enabled = false;
            Apply();
        }

        void OnDestroy()
        {
            ReleaseTarget();
            if (hero == null) return;
            if (posing) hero.ClearSkillPose();
            DeathMarkFx fx;
            if (Posing.TryGetValue(hero, out fx) && fx == this) Posing.Remove(hero);
        }

        void ReleaseTarget()
        {
            DeathMarkFx fx;
            if (target != null && Live.TryGetValue(target, out fx) && fx == this) Live.Remove(target);
        }

        void Update()
        {
            if (hero == null || hero.IsDead) { Destroy(gameObject); return; } // a fallen caster ends the picture; the Monster's reticle still shows a mark that outlives him
            float dt = GameClock.Dt * 1000f;
            age += dt;
            stageAge += dt;
            if (target != null) { lastFeet = target.Feet; lastTop = target.OverheadUnits; }
            switch (stage)
            {
                case Stage.Aim:
                    if (target == null) { Destroy(gameObject); return; }
                    if (age >= LandMs) Land();
                    break;
                case Stage.Held: Hold(); break;
                case Stage.Shatter: if (stageAge >= ShatterLenMs) { Destroy(gameObject); return; } break;
                case Stage.Fade: if (stageAge >= FadeLenMs) { Destroy(gameObject); return; } break;
            }
            Apply();
        }

        /// <summary>The beam arrives: the star and seal lock on. An older picture on this target hands over here, so a recast never stacks two.</summary>
        void Land()
        {
            stage = Stage.Held;
            stageAge = 0f;
            DeathMarkFx old;
            if (Live.TryGetValue(target, out old) && old != null && old != this) { old.target = null; Destroy(old.gameObject); }
            Live[target] = this;
            Vector2 at = target.Center;
            UltimateFx.Embers(at, 14, 8f, 50f, new Color(Violet.r, Violet.g, Violet.b, 1f));
            ShockRing.Spawn(target.Feet, 18f, 0.3f, new Color(Violet.r, Violet.g, Violet.b, 0.8f), UltimateFx.GroundFxOrder + 6);
            if (local) { CameraFollow.AddTrauma(0.12f); ScreenFlash.Trigger(new Color(0.6f, 0.25f, 1f, 0.05f), 0.1f); }
        }

        void Hold()
        {
            if (target == null || target.IsDying) { ToShatter(); return; }
            bool marked = target.Status.Has(StatusKind.Mark);
            if (marked) { seen = true; }
            else if (!seen)
            {
                if (stageAge > SeenGraceMs && !Rebind()) { Destroy(gameObject); }
                return;
            }
            else if (stageAge < markMs - ExpiryMarginMs) { ToShatter(); return; }
            else { ReleaseTarget(); stage = Stage.Fade; stageAge = 0f; return; }
            if (stageAge > markMs + EndMarginMs) { ReleaseTarget(); stage = Stage.Fade; stageAge = 0f; }
        }

        /// <summary>A client aims at the monster nearest in its own view; when the server marked another one close by, the picture moves there. Nothing marked: the picture ends.</summary>
        bool Rebind()
        {
            Monster best = null;
            float bestSq = RebindRadius * RebindRadius;
            IReadOnlyList<Monster> all = Monster.All;
            for (int i = 0; i < all.Count; i++)
            {
                Monster m = all[i];
                if (m == target || m.IsDying || !m.Status.Has(StatusKind.Mark) || Covers(m)) continue;
                float sq = (m.Feet - lastFeet).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = m; }
            }
            if (best == null) return false;
            ReleaseTarget();
            target = best;
            Live[target] = this;
            seen = true;
            return true;
        }

        void ToShatter()
        {
            ReleaseTarget();
            stage = Stage.Shatter;
            stageAge = 0f;
            Vector2 at = lastFeet + new Vector2(0f, lastTop * 0.5f);
            UltimateFx.Embers(at, 22, 12f, 80f, new Color(Violet.r, Violet.g, Violet.b, 1f));
            UltimateFx.Debris(at, 8, 12f, 60f);
            if (!local) return; // another player's mark never moves this player's camera
            CameraFollow.AddTrauma(0.3f);
            GameClock.HitStop(30f);
            CameraFollow.Punch(0.2f);
            ScreenFlash.Trigger(new Color(0.65f, 0.25f, 1f, 0.07f), 0.14f);
        }

        void ApplyPose()
        {
            if (!posing) return;
            if (age >= PoseToMs) { hero.ClearSkillPose(); posing = false; return; }
            if (age < RaiseToMs) hero.SetSkillPose(RaiseAnim, RaiseFrame, 0f, facing);
            else hero.SetSkillPose(PointAnim, PointFrame, 0f, facing);
        }

        static void Draw(SpriteRenderer s, SpriteRenderer g, Sprite art, Vector2 at, float alpha, float glowAlpha, Vector3 scale, float angle, int order)
        {
            bool on = art != null && alpha > 0.01f;
            s.enabled = on;
            g.enabled = on && FeelConfig.FxOn && glowAlpha > 0.01f;
            if (!on) return;
            s.sprite = g.sprite = art;
            s.color = new Color(1f, 1f, 1f, alpha);
            g.color = new Color(Violet.r, Violet.g, Violet.b, glowAlpha * alpha);
            s.transform.position = at;
            s.transform.localScale = scale;
            s.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            s.sortingOrder = g.sortingOrder = order;
        }

        void ApplyAim()
        {
            Vector2 feet = hero.Position;
            Vector2 hand = feet + new Vector2(0f, HandHeight) + dir * HandOut;
            if (age < RaiseToMs && starArt != null)
            {
                Vector2 tip = feet + new Vector2(dir.x >= 0f ? TipOut : -TipOut, TipRaise);
                float grow = Mathf.Clamp01(age / RaiseToMs);
                Draw(star, starGlow, starArt[AssassinBoardFx.ByProgress(age / RaiseToMs)], tip, Mathf.Clamp01(age / 50f), 0.5f, Vector3.one * SparkScale * (0.5f + 0.5f * grow), 0f, FeelConfig.FxOrder - 3);
            }
            else if (stage == Stage.Aim) star.enabled = starGlow.enabled = false;
            bool beamOn = beamArt != null && age >= RaiseToMs && age < LandMs + BeamFadeMs;
            if (!beamOn) { beam.enabled = beamGlow.enabled = false; return; }
            Vector2 end = target != null ? target.Center : lastFeet + new Vector2(0f, 6f), line = end - hand;
            // The painted beam grows by itself over frames 0-3 and thins over 4-5; it is stretched lengthwise only, so its full length lands exactly on the target.
            int frame = age < LandMs ? Mathf.Clamp(Mathf.FloorToInt((age - RaiseToMs) / (LandMs - RaiseToMs) * 4f), 0, 3) : Mathf.Clamp(4 + Mathf.FloorToInt((age - LandMs) / BeamFadeMs * 2f), 4, 5);
            float angle = Mathf.Atan2(line.y, line.x) * Mathf.Rad2Deg, a = age < LandMs ? 1f : Mathf.Clamp01(1.4f - (age - LandMs) / BeamFadeMs);
            Draw(beam, beamGlow, beamArt[frame], hand, a, 0.5f, new Vector3(line.magnitude / (BeamFullPx * BeamUnitsPerPx), 1f, 1f), angle, FeelConfig.FxOrder - 3);
        }

        void ApplyMark()
        {
            bool held = stage == Stage.Held, fading = stage == Stage.Fade;
            if (stage == Stage.Aim) { ring.enabled = ringGlow.enabled = false; return; }
            if (held || fading)
            {
                float a = fading ? Mathf.Clamp01(1f - stageAge / FadeLenMs) : Mathf.Clamp01(stageAge / 120f);
                Vector2 top = lastFeet + new Vector2(0f, lastTop + StarGap + Mathf.Sin(stageAge * 0.004f) * 1f);
                float pulse = 1f + 0.05f * Mathf.Sin(stageAge * 0.009f);
                int order = GameConfig.OrderBase(lastFeet.y);
                // The star keeps reading from the side of a crowd: above every y-sorted body.
                Draw(star, starGlow, starArt != null ? starArt[Mathf.FloorToInt(stageAge / StarFrameMs) % AssassinBoardFx.Frames] : null, top, a, 0.45f, Vector3.one * pulse, 0f, order + 8);
                Draw(ring, ringGlow, ringArt != null ? ringArt[Mathf.FloorToInt(stageAge / RingFrameMs) % AssassinBoardFx.Frames] : null, lastFeet, a * 0.95f, 0.4f, Vector3.one, 0f, order - 2);
                shatter.enabled = shatterGlow.enabled = false;
                return;
            }
            star.enabled = starGlow.enabled = ring.enabled = ringGlow.enabled = false;
            float f = stageAge / ShatterLenMs;
            Draw(shatter, shatterGlow, shatterArt != null ? shatterArt[AssassinBoardFx.ByProgress(f)] : null, lastFeet + new Vector2(0f, lastTop * 0.5f + 4f), Mathf.Min(1f, (1f - f) * 4f), 0.45f, Vector3.one, 0f, FeelConfig.FxOrder - 3);
        }

        void Apply()
        {
            if (hero == null) return;
            ApplyPose();
            ApplyAim();
            ApplyMark();
        }
    }
}
