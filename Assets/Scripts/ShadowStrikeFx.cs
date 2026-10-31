using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The picture of the assassin's SHADOW STRIKE; its two hits (0 and 120 ms) stay in the skill data (SkillRun on the authority), so this runs only where RendersFx.
    /// Timeline (ms): step in and the right dagger's slash arc 0-240, the left dagger's reverse arc 120-360, the two arcs cross into a giant X 210-640 (flash at 250), the figure holds until the cast lock ends at 300.
    /// Screen effects are for the local caster only.
    /// </summary>
    public class ShadowStrikeFx : MonoBehaviour
    {
        public const string SkillId = "assassin.shadow-strike";
        const float Slash1To = 240f, Slash2From = 120f, Slash2To = 360f, CrossFrom = 210f, CrossPeak = 250f, CrossTo = 640f, TotalMs = 660f, PoseEndMs = 320f;
        const float SlashUnitsPerPx = 0.127f, CrossUnitsPerPx = 0.165f, BodyLift = 8f, CrossAhead = 25f, StepUnits = 7f, StepInMs = 90f, StepBackFromMs = 240f;
        const string Folder = "Sprites/fx/shadowstrike/";
        static readonly Color Violet = new Color(0.72f, 0.3f, 1f), Dark = new Color(0.3f, 0.08f, 0.5f);
        // Right slash, left slash, then the crossed-arms frame of the X.
        static readonly float[] PoseTo = { 30f, 80f, 120f, 150f, 200f, 240f };
        static readonly string[] PoseAnim = { "melee1", "melee1", "melee1", "melee2", "melee2", "melee2" };
        static readonly int[] PoseFrame = { 1, 2, 3, 1, 2, 3 };
        const string CrossPoseAnim = "melee3";
        const int CrossPoseFrame = 2;

        static Sprite[] slash1Art, slash2Art, crossArt;

        HeroController hero;
        ISkillBody body; // the zone pump finds a skill object's caster through this field: a portal transfer ends the picture with its hero
        Vector2 feet, dir;
        float age;
        bool local, secondHit, crossed, posing = true;
        AimedLayer slash1, slash2, cross;
        SpriteRenderer groundGlow;

        public static void Cast(SkillContext ctx)
        {
            if (!NetMode.RendersFx) return;
            var go = new GameObject("ShadowStrike");
            go.transform.SetParent(ctx.Body.Hero.transform.parent, false);
            go.AddComponent<ShadowStrikeFx>().Begin(ctx);
        }

        public static void Preload()
        {
            if (!NetMode.RendersFx) return;
            if (slash1Art == null) slash1Art = AssassinSkillArt.Load(Folder, "slash1", SlashUnitsPerPx, 180f, 128f);
            if (slash2Art == null) slash2Art = AssassinSkillArt.Load(Folder, "slash2", SlashUnitsPerPx, 180f, 161f);
            if (crossArt == null) crossArt = AssassinSkillArt.Load(Folder, "cross", CrossUnitsPerPx, 326f, 170f);
        }

        void Begin(SkillContext ctx)
        {
            hero = ctx.Body.Hero;
            body = ctx.Body;
            local = body.IsLocal;
            AssassinSkillArt.ClaimPose(hero, this);
            feet = hero.Position;
            dir = ctx.Dir;
            Preload();
            groundGlow = ProceduralSprites.Child(transform, "GroundGlow", FxSprites.Dot());
            groundGlow.sortingOrder = UltimateFx.GroundFxOrder + 1;
            slash1 = new AimedLayer(transform, "Slash1");
            slash2 = new AimedLayer(transform, "Slash2");
            cross = new AimedLayer(transform, "Cross");
            UltimateFx.Dust(feet, 4, 6f);
            UltimateFx.Embers(Ahead(18f), 10, 10f, 50f, AssassinSkillArt.Solid(Violet));
            if (local) CameraFollow.AddTrauma(0.1f);
            Apply();
        }

        Vector2 Ahead(float units) { return feet + dir * units + new Vector2(0f, BodyLift); }

        void OnDestroy() { if (hero != null) AssassinSkillArt.ReleasePose(hero, this); }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= TotalMs) { Destroy(gameObject); return; } // a fallen caster ends the picture
            age += GameClock.Dt * 1000f;
            if (!secondHit && age >= Slash2From) SecondHit();
            if (!crossed && age >= CrossPeak) Crossed();
            Apply();
        }

        void SecondHit()
        {
            secondHit = true;
            UltimateFx.Embers(Ahead(20f), 12, 12f, 60f, AssassinSkillArt.Solid(Violet));
            if (local) CameraFollow.AddTrauma(0.14f);
        }

        void Crossed()
        {
            crossed = true;
            Vector2 at = Ahead(CrossAhead);
            UltimateFx.Embers(at, 26, 18f, 90f, AssassinSkillArt.Solid(Violet));
            UltimateFx.Debris(at, 10, 16f, 80f);
            ShockRing.Spawn(feet + dir * CrossAhead, 34f, 0.3f, new Color(Violet.r, Violet.g, Violet.b, 0.75f), UltimateFx.GroundFxOrder + 6);
            if (!local) return; // another player's strike never moves this player's camera
            CameraFollow.AddTrauma(0.26f);
            GameClock.HitStop(35f);
            CameraFollow.ZoomPulse(0.02f, 110f);
            ScreenFlash.Trigger(new Color(0.65f, 0.25f, 1f, 0.07f), 0.14f);
        }

        void ApplyPose()
        {
            if (!posing) return;
            if (!AssassinSkillArt.OwnsPose(hero, this)) { posing = false; return; }
            if (age >= PoseEndMs) { AssassinSkillArt.ReleasePose(hero, this); posing = false; return; }
            float step = age < StepBackFromMs ? Mathf.Clamp01(age / StepInMs) : 1f - Mathf.Clamp01((age - StepBackFromMs) / (PoseEndMs - StepBackFromMs));
            string anim = CrossPoseAnim;
            int frame = CrossPoseFrame;
            for (int i = 0; i < PoseTo.Length; i++)
                if (age < PoseTo[i]) { anim = PoseAnim[i]; frame = PoseFrame[i]; break; }
            hero.SetSkillPose(anim, frame, dir.y * StepUnits * step);
            hero.SetSkillPoseLook(1f, dir.x * StepUnits * step);
        }

        void Apply()
        {
            if (hero == null) return;
            ApplyPose();
            Vector2 body0 = feet + new Vector2(0f, BodyLift);
            int front = FeelConfig.FxOrder - 3;
            float t1 = age / Slash1To;
            slash1.Show(slash1Art != null && t1 < 1f ? slash1Art[AssassinSkillArt.ByProgress(t1)] : null, body0, dir, 1f, Violet, 0.35f, front);
            float t2 = (age - Slash2From) / (Slash2To - Slash2From);
            slash2.Show(slash2Art != null && t2 >= 0f && t2 < 1f ? slash2Art[AssassinSkillArt.ByProgress(t2)] : null, body0, dir, 1f, Violet, 0.35f, front + 2);
            float c = (age - CrossFrom) / (CrossTo - CrossFrom);
            bool showCross = crossArt != null && c >= 0f && c < 1f;
            cross.Show(showCross ? crossArt[AssassinSkillArt.ToPeak(c, 2, (CrossPeak - CrossFrom) / (CrossTo - CrossFrom))] : null, Ahead(CrossAhead), dir, Mathf.Min(1f, (1f - c) * 5f), Violet, 0.4f, front + 4);
            groundGlow.transform.position = feet + dir * 14f;
            groundGlow.transform.localScale = new Vector3(84f, 52f, 1f);
            float glow = age < CrossPeak ? 0.26f * Mathf.Clamp01(age / 80f) : 0.36f * Mathf.Clamp01(1f - (age - CrossPeak) / (TotalMs - CrossPeak));
            groundGlow.color = new Color(Dark.r, Dark.g, Dark.b, glow);
        }
    }
}
