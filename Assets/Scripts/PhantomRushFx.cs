using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The picture of the assassin's PHANTOM RUSH; its three dashes and three cuts stay in the skill data (SkillRun on the authority), so this runs only where RendersFx.
    /// Timeline (ms): crouch and gather with smoke swirling behind 0-140, three straight dashes 140-320, 320-500 and 500-680, each dragging its streak of slashes and shadow figures with the hero,
    /// a cut landing at the end of each; the path behind the hero bursts at 680 until 1120 while the hero slides to a stop; the figure is released when the cast lock ends at 740.
    /// The dashes themselves come from the skill data, so this only reads where the hero is. Screen effects are for the local caster only.
    /// </summary>
    public class PhantomRushFx : MonoBehaviour
    {
        public const string SkillId = "assassin.phantom-rush";
        const float CrouchToMs = 70f, DashFromMs = 140f, DashMs = 180f, BurstMs = 680f, PoseEndMs = 760f, RushLenMs = 420f, GatherToMs = 240f, BurstToMs = 1120f, TotalMs = 1140f, RecoverFromMs = 680f;
        const float GatherUnitsPerPx = 0.16f, RushUnitsPerPx = 0.15f, BurstUnitsPerPx = 0.17f, BurstBehind = 26f, BodyLift = 8f, GroundLift = 1f;
        const string CrouchAnim = "nightfall", RushAnim = "melee4";
        const int CrouchFrame = 0, ReadyFrame = 1, LungeFrame = 2, ThrustFrame = 3, RecoverFrame = 4, Dashes = 3;
        const string Folder = "Sprites/fx/phantomrush/";
        static readonly Color Violet = new Color(0.72f, 0.3f, 1f), Dark = new Color(0.3f, 0.08f, 0.5f);

        static Sprite[] gatherArt, rushArt, burstArt;

        HeroController hero;
        ISkillBody body; // the zone pump finds a skill object's caster through this field: a portal transfer ends the picture with its hero
        Vector2 origin, dir, burstAt;
        float age;
        bool local, burst, posing = true;
        AimedLayer gather, burstLayer;
        readonly AimedLayer[] rush = new AimedLayer[Dashes];
        readonly Vector2[] head = new Vector2[Dashes];
        readonly bool[] cut = new bool[Dashes];
        SpriteRenderer groundGlow;

        public static void Cast(SkillContext ctx)
        {
            if (!NetMode.RendersFx) return;
            var go = new GameObject("PhantomRush");
            go.transform.SetParent(ctx.Body.Hero.transform.parent, false);
            go.AddComponent<PhantomRushFx>().Begin(ctx);
        }

        public static void Preload()
        {
            if (!NetMode.RendersFx) return;
            if (gatherArt == null) gatherArt = AssassinSkillArt.Load(Folder, "gather", GatherUnitsPerPx, 340f, 217f);
            if (rushArt == null) rushArt = AssassinSkillArt.Load(Folder, "rush", RushUnitsPerPx, 398f, 128f);
            if (burstArt == null) burstArt = AssassinSkillArt.Load(Folder, "burst", BurstUnitsPerPx, 278f, 189f);
        }

        void Begin(SkillContext ctx)
        {
            hero = ctx.Body.Hero;
            body = ctx.Body;
            local = body.IsLocal;
            AssassinSkillArt.ClaimPose(hero, this);
            origin = burstAt = hero.Position;
            dir = ctx.Dir;
            Preload();
            groundGlow = ProceduralSprites.Child(transform, "GroundGlow", FxSprites.Dot());
            groundGlow.sortingOrder = UltimateFx.GroundFxOrder + 1;
            gather = new AimedLayer(transform, "Gather");
            burstLayer = new AimedLayer(transform, "Burst");
            for (int i = 0; i < Dashes; i++) { rush[i] = new AimedLayer(transform, "Rush" + i); head[i] = origin; }
            UltimateFx.Dust(origin, 4, 6f);
            Apply();
        }

        void OnDestroy() { if (hero != null) AssassinSkillArt.ReleasePose(hero, this); }

        static float DashStart(int i) { return DashFromMs + i * DashMs; }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= TotalMs) { Destroy(gameObject); return; } // a fallen caster ends the picture
            age += GameClock.Dt * 1000f;
            for (int i = 0; i < Dashes; i++)
                if (age < DashStart(i) + DashMs) head[i] = hero.Position;
            for (int i = 0; i < Dashes - 1; i++)
                if (!cut[i] && age >= DashStart(i) + DashMs) Cut(i);
            if (!burst && age >= BurstMs) Burst();
            Apply();
        }

        void Cut(int i)
        {
            cut[i] = true;
            Vector2 at = hero.Position + dir * 6f + new Vector2(0f, BodyLift);
            UltimateFx.Embers(at, 14, 8f, 70f, AssassinSkillArt.Solid(Violet));
            UltimateFx.Dust(hero.Position, 3, 5f);
            if (local) CameraFollow.AddTrauma(0.14f);
        }

        void Burst()
        {
            burst = true;
            Vector2 feet = hero.Position;
            float ran = Mathf.Max(0f, Vector2.Dot(feet - origin, dir));
            burstAt = feet - dir * Mathf.Min(BurstBehind, ran) + new Vector2(0f, BodyLift);
            UltimateFx.Embers(burstAt, 40, 26f, 120f, AssassinSkillArt.Solid(Violet));
            UltimateFx.Debris(burstAt, 16, 24f, 110f);
            UltimateFx.Dust(feet, 8, 10f);
            ShockRing.Spawn(burstAt, 44f, 0.34f, new Color(Violet.r, Violet.g, Violet.b, 0.8f), UltimateFx.GroundFxOrder + 6);
            if (!local) return; // another player's rush never moves this player's camera
            CameraFollow.AddTrauma(0.42f);
            GameClock.HitStop(40f);
            CameraFollow.ZoomPulse(0.04f, 150f);
            ScreenFlash.Trigger(new Color(0.65f, 0.25f, 1f, 0.1f), 0.18f);
        }

        void ApplyPose()
        {
            if (!posing) return;
            if (!AssassinSkillArt.OwnsPose(hero, this)) { posing = false; return; }
            if (age >= PoseEndMs) { AssassinSkillArt.ReleasePose(hero, this); posing = false; return; }
            hero.SetSkillPoseLook(1f, 0f);
            if (age < CrouchToMs) hero.SetSkillPose(CrouchAnim, CrouchFrame, 0f);
            else if (age < DashFromMs) hero.SetSkillPose(RushAnim, ReadyFrame, 0f);
            else if (age >= RecoverFromMs) hero.SetSkillPose(RushAnim, RecoverFrame, 0f);
            else
            {
                float into = Mathf.Repeat(age - DashFromMs, DashMs) / DashMs;
                hero.SetSkillPose(RushAnim, into < 0.4f ? LungeFrame : ThrustFrame, 0f);
            }
        }

        void Apply()
        {
            if (hero == null) return;
            ApplyPose();
            Vector2 lift = new Vector2(0f, BodyLift);
            int behind = GameConfig.OrderBase(hero.Position.y) - 2;
            float g = age / GatherToMs;
            gather.Show(gatherArt != null && g < 1f ? gatherArt[AssassinSkillArt.ByProgress(g)] : null, origin + new Vector2(0f, GroundLift), dir, Mathf.Min(1f, (1f - g) * 4f), Violet, 0.3f, behind);
            for (int i = 0; i < Dashes; i++)
            {
                float t = (age - DashStart(i)) / RushLenMs;
                bool on = rushArt != null && t >= 0f && t < 1f;
                rush[i].Show(on ? rushArt[AssassinSkillArt.ToPeak(t, 2, 0.25f)] : null, head[i] + lift, dir, Mathf.Min(1f, (1f - t) * 5f), Violet, 0.35f, behind - 2 * (Dashes - i));
            }
            float b = (age - BurstMs) / (BurstToMs - BurstMs);
            bool showBurst = burstArt != null && b >= 0f && b < 1f;
            burstLayer.Show(showBurst ? burstArt[AssassinSkillArt.ToPeak(b, 2, 0.12f)] : null, burstAt, dir, Mathf.Min(1f, (1f - b) * 5f), Violet, 0.4f, FeelConfig.FxOrder - 3);
            groundGlow.transform.position = burst ? burstAt - lift : hero.Position;
            groundGlow.transform.localScale = burst ? new Vector3(120f, 72f, 1f) : new Vector3(70f, 40f, 1f);
            float glow = age < BurstMs ? 0.28f * Mathf.Clamp01(age / 100f) : 0.42f * Mathf.Clamp01(1f - (age - BurstMs) / (TotalMs - BurstMs));
            groundGlow.color = new Color(Dark.r, Dark.g, Dark.b, glow);
        }
    }
}
