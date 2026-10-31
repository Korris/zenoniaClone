using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The picture of the assassin's SHADOW STEP; the blink (180 ms) and the backstab (300 ms) stay in the skill data (SkillRun on the authority), so this runs only where RendersFx.
    /// Timeline (ms): the hero crouches and dissolves into smoke over a ring at the start point 0-180, a shadow streak runs to the target 150-400 while the hero is gone,
    /// the hero fades back in out of a smoke ring behind the target 230-300, the two daggers stab at 300 with the crossing rays bursting until 640; the figure is released when the cast lock ends at 480.
    /// The landing point follows the hero, or the target's far side until the authority's blink has arrived. Screen effects are for the local caster only.
    /// </summary>
    public class ShadowStepFx : MonoBehaviour
    {
        public const string SkillId = "assassin.shadow-step";
        const float DissolveFromMs = 90f, BlinkMs = 180f, ReturnFromMs = 230f, HitMs = 300f, PoseEndMs = 500f, TotalMs = 660f;
        const float VanishToMs = 340f, TrailFromMs = 150f, TrailToMs = 400f, AppearFromMs = 220f, AppearToMs = 540f, StabToMs = 640f;
        const float VanishUnitsPerPx = 0.128f, TrailUnitsPerPx = 0.1f, TrailLenPx = 420f, StabUnitsPerPx = 0.11f, BehindUnits = 12f, MovedUnits = 12f, StabAhead = 16f, BodyLift = 8f, RingLift = 1f;
        const string CrouchAnim = "nightfall", StabAnim = "melee4";
        const int CrouchFrame = 0, ReadyFrame = 1, StabFrame = 2, ThrustFrame = 3, RecoverFrame = 4;
        const float ThrustFromMs = 380f, RecoverFromMs = 450f;
        const string Folder = "Sprites/fx/shadowstep/";
        static readonly Color Violet = new Color(0.72f, 0.3f, 1f), Dark = new Color(0.3f, 0.08f, 0.5f);

        static CutStrip vanishArt, appearArt;
        static Sprite[] trailArt, stabArt;

        SkillContext ctx;
        HeroController hero;
        ISkillBody body; // the zone pump finds a skill object's caster through this field: a portal transfer ends the picture with its hero
        Monster target;
        Vector2 origin, dest;
        float age;
        bool local, stabbed, posing = true;
        CutLayer vanish, appear;
        AimedLayer trail, stab;
        SpriteRenderer groundGlow;

        public static void Cast(SkillContext ctx)
        {
            if (!NetMode.RendersFx) return;
            var go = new GameObject("ShadowStep");
            go.transform.SetParent(ctx.Body.Hero.transform.parent, false);
            go.AddComponent<ShadowStepFx>().Begin(ctx);
        }

        public static void Preload()
        {
            if (!NetMode.RendersFx) return;
            if (vanishArt == null) vanishArt = AssassinSkillArt.LoadCut(Folder, "vanish", VanishUnitsPerPx, 278f, 375f);
            if (appearArt == null) appearArt = AssassinSkillArt.LoadCut(Folder, "appear", VanishUnitsPerPx, 278f, 270f);
            if (trailArt == null) trailArt = AssassinSkillArt.Load(Folder, "trail", TrailUnitsPerPx, 82f, 81f);
            if (stabArt == null) stabArt = AssassinSkillArt.Load(Folder, "stab", StabUnitsPerPx, 304f, 234f);
        }

        void Begin(SkillContext c)
        {
            ctx = c;
            hero = c.Body.Hero;
            body = c.Body;
            target = c.Target;
            local = body.IsLocal;
            AssassinSkillArt.ClaimPose(hero, this);
            origin = dest = hero.Position;
            Preload();
            groundGlow = ProceduralSprites.Child(transform, "GroundGlow", FxSprites.Dot());
            groundGlow.sortingOrder = UltimateFx.GroundFxOrder + 1;
            vanish = new CutLayer(transform, "Vanish");
            appear = new CutLayer(transform, "Appear");
            trail = new AimedLayer(transform, "Trail");
            stab = new AimedLayer(transform, "Stab");
            UltimateFx.Dust(origin, 4, 6f);
            Apply();
        }

        void OnDestroy() { if (hero != null) AssassinSkillArt.ReleasePose(hero, this); }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= TotalMs) { Destroy(gameObject); return; } // a fallen caster ends the picture
            age += GameClock.Dt * 1000f;
            if (age < HitMs) dest = Landing();
            if (!stabbed && age >= HitMs) Stabbed();
            Apply();
        }

        /// <summary>Where the hero is, once the blink has moved it; before that the target's far side, the spot the authority picks.</summary>
        Vector2 Landing()
        {
            Vector2 p = hero.Position;
            if ((p - origin).sqrMagnitude > MovedUnits * MovedUnits || target == null || target.IsDying) return p;
            Vector2 f = target.Feet, d = f - origin;
            return f + (d.sqrMagnitude > 0.01f ? d.normalized : ctx.Dir) * BehindUnits;
        }

        void Stabbed()
        {
            stabbed = true;
            Vector2 at = dest + ctx.Dir * StabAhead + new Vector2(0f, BodyLift);
            UltimateFx.Embers(at, 26, 10f, 90f, AssassinSkillArt.Solid(Violet));
            UltimateFx.Debris(at, 10, 12f, 80f);
            ShockRing.Spawn(dest, 20f, 0.28f, new Color(Violet.r, Violet.g, Violet.b, 0.8f), UltimateFx.GroundFxOrder + 6);
            if (!local) return; // another player's stab never moves this player's camera
            CameraFollow.AddTrauma(0.36f);
            GameClock.HitStop(45f);
            CameraFollow.ZoomPulse(0.025f, 120f);
            ScreenFlash.Trigger(new Color(0.65f, 0.25f, 1f, 0.09f), 0.16f);
        }

        void ApplyPose()
        {
            if (!posing) return;
            if (!AssassinSkillArt.OwnsPose(hero, this)) { posing = false; return; }
            if (age >= PoseEndMs) { AssassinSkillArt.ReleasePose(hero, this); posing = false; return; }
            int facing = HeroSkillBody.FacingOf(ctx.Dir);
            float alpha;
            if (age < BlinkMs)
            {
                alpha = 1f - Mathf.Clamp01((age - DissolveFromMs) / (BlinkMs - DissolveFromMs));
                hero.SetSkillPose(CrouchAnim, CrouchFrame, 0f, facing);
            }
            else if (age < HitMs)
            {
                alpha = Mathf.Clamp01((age - ReturnFromMs) / (HitMs - ReturnFromMs));
                hero.SetSkillPose(StabAnim, ReadyFrame, 0f, facing);
            }
            else
            {
                alpha = 1f;
                hero.SetSkillPose(StabAnim, age < ThrustFromMs ? StabFrame : age < RecoverFromMs ? ThrustFrame : RecoverFrame, 0f, facing);
            }
            hero.SetSkillPoseLook(alpha, 0f);
        }

        void Apply()
        {
            if (hero == null) return;
            ApplyPose();
            int behind = GameConfig.OrderBase(origin.y) - 2, behindDest = GameConfig.OrderBase(dest.y) - 2;
            float v = age / VanishToMs;
            vanish.Show(vanishArt, AssassinSkillArt.ByProgress(v), origin + new Vector2(0f, RingLift), v < 1f ? Mathf.Min(1f, (1f - v) * 5f) : 0f, Violet, 0.3f, behind);
            float a = (age - AppearFromMs) / (AppearToMs - AppearFromMs);
            appear.Show(appearArt, AssassinSkillArt.ByProgress(a), dest + new Vector2(0f, RingLift), a >= 0f && a < 1f ? Mathf.Min(1f, (1f - a) * 5f) : 0f, Violet, 0.3f, behindDest);
            Vector2 run = dest - origin;
            float dist = run.magnitude, tr = (age - TrailFromMs) / (TrailToMs - TrailFromMs);
            bool showTrail = trailArt != null && dist > MovedUnits && tr >= 0f && tr < 1f;
            trail.Show(showTrail ? trailArt[AssassinSkillArt.ByProgress(tr)] : null, origin + new Vector2(0f, RingLift + 2f), dist > 0.01f ? run / dist : ctx.Dir, 1f, Violet, 0.3f, UltimateFx.GroundFxOrder + 4, dist / (TrailLenPx * TrailUnitsPerPx));
            float s = (age - HitMs) / (StabToMs - HitMs);
            bool showStab = stabArt != null && s >= 0f && s < 1f;
            stab.Show(showStab ? stabArt[AssassinSkillArt.ToPeak(s, 2, 0.1f)] : null, dest + ctx.Dir * StabAhead + new Vector2(0f, BodyLift), ctx.Dir, Mathf.Min(1f, (1f - s) * 5f), Violet, 0.55f, GameConfig.OrderBase(dest.y) + 2, 1f, 1f, FeelConfig.FxOrder - 2); // the rays pass behind the figure, their flash lights it from in front
            groundGlow.transform.position = stabbed || age >= BlinkMs ? dest : origin;
            groundGlow.transform.localScale = new Vector3(72f, 44f, 1f);
            float glow = age < HitMs ? 0.28f : 0.38f * Mathf.Clamp01(1f - (age - HitMs) / (TotalMs - HitMs));
            groundGlow.color = new Color(Dark.r, Dark.g, Dark.b, glow);
        }
    }
}
