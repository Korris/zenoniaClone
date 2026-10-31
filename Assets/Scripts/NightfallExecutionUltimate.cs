using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The picture of the assassin's NIGHTFALL EXECUTION; its eight strikes stay in the skill data (SkillRun on the authority), so this runs only where RendersFx.
    /// Timeline (ms): crossed daggers and a shadow vortex with a rising column 0-240, the hero spins inside a slash ring 200-640 while shadow figures rush in and slash 240-900,
    /// lunge 640-800, the crossing X-slash lands at 800 and the giant purple X bursts 800-1250, recover until 1350. The ring and vortex are cut at their centre line: the upper half
    /// draws behind the hero and the lower half in front. Screen effects are for the local caster only.
    /// </summary>
    public class NightfallExecutionUltimate : MonoBehaviour
    {
        public const string SkillId = "assassin.nightfall-execution";
        public const string PoseAnim = "nightfall";
        const float CastToMs = 260f, SpinFromMs = 200f, SpinToMs = 860f, SpinLoopMs = 320f, CrossMs = 800f, CrossLenMs = 450f, TotalMs = 1350f, FadeMs = 50f;
        const float CrouchToMs = 200f, SpinPoseToMs = 640f, LungeToMs = 800f, SlashToMs = 1120f, TurnMs = 70f, ShadowLenMs = 300f;
        const float CastUnitsPerPx = 0.13f, RingUnitsPerPx = 0.285f, ShadowUnitsPerPx = 0.11f, CrossUnitsPerPx = 0.27f; // the ring's outer edge spans the real 3 tile radius
        const float CastAnchorY = 370f, RingAnchorY = 150f, RingLift = 4f, CrossLift = 10f, ShadowRadius = 34f;
        const int Frames = 6, Shadows = 6, PoseCrouch = 0, PoseSpin = 1, PoseLunge = 3, PoseSlash = 4, PoseRecover = 5;
        const string Folder = "Sprites/fx/nightfall/";
        static readonly int[] TurnOrder = { 0, 3, 1, 2 }; // down, right, up, left: one clockwise turn
        static readonly float[] ShadowAt = { 240f, 340f, 430f, 520f, 600f, 680f };
        static readonly float[] ShadowDeg = { 200f, 20f, 300f, 130f, 250f, 70f };
        static readonly Color Violet = new Color(0.72f, 0.3f, 1f), Dark = new Color(0.3f, 0.08f, 0.5f);

        /// <summary>One frame strip cut in two at `anchorY`: Back above it, Front below.</summary>
        sealed class Strip { public Sprite[] Back = new Sprite[Frames], Front = new Sprite[Frames]; }
        sealed class Pair { public SpriteRenderer Back, BackGlow, Front, FrontGlow; }

        static Strip castArt, spinArt;
        static Sprite[] shadowArt, crossArt;

        HeroController hero;
        ISkillBody body; // the zone pump finds a skill object's caster through this field: a portal transfer ends the picture with its hero
        float age;
        bool local, burst;
        Pair cast, spin;
        SpriteRenderer cross, crossGlow, groundGlow;
        readonly SpriteRenderer[] shadows = new SpriteRenderer[Shadows];
        readonly Vector2[] shadowPos = new Vector2[Shadows];
        readonly bool[] shadowHit = new bool[Shadows];

        public static void Cast(SkillContext ctx)
        {
            if (!NetMode.RendersFx) return;
            var go = new GameObject("NightfallExecution");
            go.transform.SetParent(ctx.Body.Hero.transform.parent, false);
            go.AddComponent<NightfallExecutionUltimate>().Begin(ctx);
        }

        static Strip LoadCut(string name, float anchorY, float unitsPerPx)
        {
            var s = new Strip();
            for (int i = 0; i < Frames; i++)
            {
                Texture2D tex = SpriteSheet.Load(Folder + name + "-" + (i + 1));
                if (tex == null) return null;
                float below = tex.height - anchorY, ppu = 1f / unitsPerPx;
                s.Back[i] = Sprite.Create(tex, new Rect(0f, below, tex.width, anchorY), new Vector2(0.5f, 0f), ppu, 0, SpriteMeshType.FullRect);
                s.Front[i] = Sprite.Create(tex, new Rect(0f, 0f, tex.width, below), new Vector2(0.5f, 1f), ppu, 0, SpriteMeshType.FullRect);
            }
            return s;
        }

        static Sprite[] LoadWhole(string name, float unitsPerPx, float pivotY)
        {
            var s = new Sprite[Frames];
            for (int i = 0; i < Frames; i++)
            {
                Texture2D tex = SpriteSheet.Load(Folder + name + "-" + (i + 1));
                if (tex == null) return null;
                s[i] = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, pivotY), 1f / unitsPerPx, 0, SpriteMeshType.FullRect);
            }
            return s;
        }

        /// <summary>Loads and slices the 24 painted frames ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload()
        {
            if (!NetMode.RendersFx) return;
            if (castArt == null) castArt = LoadCut("cast", CastAnchorY, CastUnitsPerPx);
            if (spinArt == null) spinArt = LoadCut("spin", RingAnchorY, RingUnitsPerPx);
            if (shadowArt == null) shadowArt = LoadWhole("shadow", ShadowUnitsPerPx, 34f / 384f); // feet line of the figure
            if (crossArt == null) crossArt = LoadWhole("cross", CrossUnitsPerPx, 0.5f);
        }

        void Begin(SkillContext ctx)
        {
            hero = ctx.Body.Hero;
            body = ctx.Body;
            local = body.IsLocal;
            Preload();
            groundGlow = ProceduralSprites.Child(transform, "GroundGlow", FxSprites.Dot());
            groundGlow.sortingOrder = UltimateFx.GroundFxOrder + 1;
            cast = MakePair("Cast"); spin = MakePair("Spin");
            cross = UltimateFx.Layer(transform, "Cross", out crossGlow);
            for (int i = 0; i < Shadows; i++)
            {
                shadows[i] = ProceduralSprites.Child(transform, "Shadow" + i, null);
                float a = ShadowDeg[i] * Mathf.Deg2Rad;
                shadowPos[i] = hero.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.62f) * ShadowRadius;
                shadows[i].flipX = shadowPos[i].x > hero.Position.x; // the art rushes to the right: everyone rushes at the hero
            }
            UltimateFx.Dust(hero.Position, 8, 12f);
            if (local) CameraFollow.AddTrauma(0.1f);
            Apply();
        }

        Pair MakePair(string name)
        {
            var p = new Pair();
            p.Back = UltimateFx.Layer(transform, name + "Back", out p.BackGlow);
            p.Front = UltimateFx.Layer(transform, name + "Front", out p.FrontGlow);
            return p;
        }

        void OnDestroy() { if (hero != null) hero.ClearSkillPose(); }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= TotalMs) { Destroy(gameObject); return; } // a fallen caster ends the picture
            age += GameClock.Dt * 1000f;
            for (int i = 0; i < Shadows; i++)
                if (!shadowHit[i] && age >= ShadowAt[i] + ShadowLenMs * 0.55f) ShadowSlash(i);
            if (!burst && age >= CrossMs) Burst();
            Apply();
        }

        void ShadowSlash(int i)
        {
            shadowHit[i] = true;
            UltimateFx.Embers(shadowPos[i], 10, 8f, 60f, new Color(Violet.r, Violet.g, Violet.b, 1f));
            if (local) CameraFollow.AddTrauma(0.1f);
        }

        void Burst()
        {
            burst = true;
            Vector2 at = hero.Position + new Vector2(0f, CrossLift);
            UltimateFx.Embers(at, 50, 30f, 120f, new Color(Violet.r, Violet.g, Violet.b, 1f));
            UltimateFx.Debris(at, 18, 26f, 110f);
            ShockRing.Spawn(at, 60f, 0.36f, new Color(Violet.r, Violet.g, Violet.b, 0.85f), UltimateFx.GroundFxOrder + 6);
            if (!local) return; // another player's finisher never moves this player's camera
            CameraFollow.AddTrauma(0.5f);
            GameClock.HitStop(70f);
            CameraFollow.Punch(0.5f);
            ScreenFlash.Trigger(new Color(0.65f, 0.25f, 1f, 0.12f), 0.2f);
        }

        /// <summary>1 inside [from, to], easing over `ms` at both ends.</summary>
        static float Fade(float age, float from, float to, float ms) { return Mathf.Clamp01(Mathf.Min((age - from) / ms, (to - age) / ms)); }

        static int ByProgress(float t) { return Mathf.Clamp(Mathf.FloorToInt(t * Frames), 0, Frames - 1); }

        static void Show(Pair p, Strip art, int frame, float alpha, float glowAlpha, Vector2 at, int backOrder)
        {
            bool on = art != null && alpha > 0.01f;
            p.Back.enabled = p.Front.enabled = on;
            p.BackGlow.enabled = p.FrontGlow.enabled = on && FeelConfig.FxOn;
            if (!on) return;
            p.Back.sprite = p.BackGlow.sprite = art.Back[frame];
            p.Front.sprite = p.FrontGlow.sprite = art.Front[frame];
            p.Back.color = p.Front.color = new Color(1f, 1f, 1f, alpha);
            p.BackGlow.color = p.FrontGlow.color = new Color(Violet.r, Violet.g, Violet.b, glowAlpha * alpha);
            p.Back.transform.position = p.Front.transform.position = at;
            p.Back.sortingOrder = backOrder; p.BackGlow.sortingOrder = backOrder + 1;
            p.Front.sortingOrder = FeelConfig.FxOrder - 3; p.FrontGlow.sortingOrder = FeelConfig.FxOrder - 2;
        }

        void ApplyPose()
        {
            if (age < CrouchToMs) hero.SetSkillPose(PoseAnim, PoseCrouch, 0f);
            else if (age < SpinPoseToMs) hero.SetSkillPose(PoseAnim, PoseSpin, 0f, TurnOrder[Mathf.FloorToInt((age - CrouchToMs) / TurnMs) % TurnOrder.Length]);
            else if (age < LungeToMs) hero.SetSkillPose(PoseAnim, PoseLunge, 0f);
            else if (age < SlashToMs) hero.SetSkillPose(PoseAnim, PoseSlash, 0f);
            else hero.SetSkillPose(PoseAnim, PoseRecover, 0f);
        }

        void Apply()
        {
            if (hero == null) return;
            ApplyPose();
            Vector2 feet = hero.Position, ring = feet + new Vector2(0f, RingLift);
            int behind = GameConfig.OrderBase(feet.y) - 2;
            Show(cast, castArt, ByProgress(age / CastToMs), Fade(age, -FadeMs, CastToMs, FadeMs), 0.3f, ring, behind);
            // The ring builds through the sheet once, then keeps cycling its last three, thickest frames.
            float spun = (age - SpinFromMs) / SpinLoopMs;
            int frame = spun < 1f ? ByProgress(spun) : Frames - 3 + Mathf.FloorToInt((spun - 1f) * 9f) % 3;
            Show(spin, spinArt, frame, Fade(age, SpinFromMs, SpinToMs, FadeMs), 0.35f, ring, behind);
            for (int i = 0; i < Shadows; i++)
            {
                float t = (age - ShadowAt[i]) / ShadowLenMs;
                bool on = shadowArt != null && t >= 0f && t < 1f;
                shadows[i].enabled = on;
                if (!on) continue;
                shadows[i].sprite = shadowArt[ByProgress(t)];
                shadows[i].transform.position = shadowPos[i];
                shadows[i].sortingOrder = GameConfig.OrderBase(shadowPos[i].y);
            }
            float c = (age - CrossMs) / CrossLenMs;
            bool showCross = crossArt != null && c >= 0f && c < 1f;
            cross.enabled = showCross;
            crossGlow.enabled = showCross && FeelConfig.FxOn;
            if (showCross)
            {
                cross.sprite = crossGlow.sprite = crossArt[ByProgress(c)];
                cross.transform.position = feet + new Vector2(0f, CrossLift);
                cross.color = new Color(1f, 1f, 1f, Mathf.Min(1f, (1f - c) * 5f));
                crossGlow.color = new Color(Violet.r, Violet.g, Violet.b, 0.4f * cross.color.a);
                cross.sortingOrder = FeelConfig.FxOrder - 3; crossGlow.sortingOrder = FeelConfig.FxOrder - 2;
            }
            groundGlow.transform.position = feet;
            groundGlow.transform.localScale = new Vector3(120f, 70f, 1f);
            float glow = age < CrossMs ? 0.3f * Fade(age, 0f, CrossMs, 100f) : 0.42f * Mathf.Clamp01(1f - c);
            groundGlow.color = new Color(Dark.r, Dark.g, Dark.b, glow);
        }
    }
}
