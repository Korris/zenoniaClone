using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The picture of the warrior's INFERNO CRASH; damage, burn and the leap itself stay in the skill data (SkillRun on the authority), so this runs only where RendersFx.
    /// Timeline (ms): wind-up flames at the take-off spot 0-180, the hero rises on an arc with a flame trail 60-340, a flame slash falls to the landing spot 290-430,
    /// the strike lands at 400 and the crater explodes 400-1150. The hero's pose frames and height come from here; the sim position and the shadow never leave the ground.
    /// Every crater frame is cut at the ring's centre line: the upper half draws behind the hero and the lower half in front. Screen effects are for the local caster only.
    /// </summary>
    public class InfernoCrashUltimate : MonoBehaviour
    {
        public const string SkillId = "warrior.inferno-crash";
        public const string PoseAnim = "inferno";
        const float LandMs = 400f, TotalMs = 1150f, PoseEndMs = 700f;
        const float CastToMs = 180f, AirFromMs = 60f, AirToMs = 340f, SlamFromMs = 290f, SlamToMs = 430f, BoomMs = 750f, CrossMs = 40f;
        const float LiftUnits = 30f, RingLift = 3f;
        const float CastUnitsPerPx = 0.135f, AirUnitsPerPx = 0.125f, SlamUnitsPerPx = 0.17f, BoomUnitsPerPx = 0.234f; // the crater's outer ring spans its real 3 tile radius at the peak frame
        const float CastAnchorY = 405f, AirAnchorY = 200f, SlamAnchorY = 420f, BoomAnchorY = 240f;
        const int Frames = 6, Land = 4;
        const string Folder = "Sprites/fx/infernocrash/";
        static readonly float[] PoseUntil = { 70f, 190f, 300f, LandMs };
        static readonly Color Hot = new Color(1f, 0.56f, 0.16f), Red = new Color(1f, 0.3f, 0.08f);

        /// <summary>One frame strip cut in two at `anchorY`: Back above it, Front below.</summary>
        sealed class Strip { public Sprite[] Back = new Sprite[Frames], Front = new Sprite[Frames]; }
        sealed class Pair { public SpriteRenderer Back, BackGlow, Front, FrontGlow; }

        static Strip castArt, airArt, slamArt, boomArt;

        HeroController hero;
        ISkillBody body; // the zone pump finds a skill object's caster through this field: a portal transfer ends the picture with its hero
        float age;
        bool local, landed;
        Vector2 takeOff, landing, flip;
        Pair cast, air, slam, boom;
        SpriteRenderer groundGlow;

        public static void Cast(SkillContext ctx)
        {
            if (!NetMode.RendersFx) return;
            var go = new GameObject("InfernoCrash");
            go.transform.SetParent(ctx.Body.Hero.transform.parent, false);
            go.AddComponent<InfernoCrashUltimate>().Begin(ctx);
        }

        static Strip Load(string name, float anchorY, float unitsPerPx, float anchorX)
        {
            var s = new Strip();
            for (int i = 0; i < Frames; i++)
            {
                Texture2D tex = SpriteSheet.Load(Folder + name + "-" + (i + 1));
                if (tex == null) return null;
                float below = tex.height - anchorY, ppu = 1f / unitsPerPx, px = anchorX / tex.width;
                s.Back[i] = Sprite.Create(tex, new Rect(0f, below, tex.width, anchorY), new Vector2(px, 0f), ppu, 0, SpriteMeshType.FullRect);
                s.Front[i] = Sprite.Create(tex, new Rect(0f, 0f, tex.width, below), new Vector2(px, 1f), ppu, 0, SpriteMeshType.FullRect);
            }
            return s;
        }

        /// <summary>Loads and slices the 24 painted frames ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload()
        {
            if (!NetMode.RendersFx) return;
            if (castArt == null) castArt = Load("cast", CastAnchorY, CastUnitsPerPx, 192f);
            if (airArt == null) airArt = Load("air", AirAnchorY, AirUnitsPerPx, 192f);
            if (slamArt == null) slamArt = Load("slam", SlamAnchorY, SlamUnitsPerPx, 192f);
            if (boomArt == null) boomArt = Load("boom", BoomAnchorY, BoomUnitsPerPx, 240f);
        }

        void Begin(SkillContext ctx)
        {
            hero = ctx.Body.Hero;
            body = ctx.Body;
            local = body.IsLocal;
            takeOff = hero.Position;
            landing = SkillAim.ClampToWalls(takeOff, takeOff + ctx.Dir * ctx.Def.Effects[0].MoveTiles * ctx.Units);
            flip = new Vector2(ctx.Dir.x < -0.1f ? -1f : 1f, 1f);
            Preload();
            groundGlow = ProceduralSprites.Child(transform, "GroundGlow", FxSprites.Dot());
            groundGlow.sortingOrder = UltimateFx.GroundFxOrder + 1;
            cast = MakePair("Cast"); air = MakePair("Air"); slam = MakePair("Slam"); boom = MakePair("Boom");
            UltimateFx.Dust(takeOff, 8, 10f);
            if (local) CameraFollow.AddTrauma(0.08f);
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
            float dt = GameClock.Dt * 1000f;
            age += dt;
            if (!landed && age >= LandMs) Impact();
            Apply();
        }

        void Impact()
        {
            landed = true;
            landing = hero.Position; // where the sim really stopped: walls may have cut the leap short
            Vector2 ring = landing + new Vector2(0f, RingLift);
            float r = BoomFrameRadius;
            UltimateFx.Embers(ring, 60, r * 0.7f, 110f, new Color(1f, 0.5f, 0.12f, 1f));
            UltimateFx.Debris(ring, 28, r * 0.6f, 130f);
            UltimateFx.Dust(landing, 18, r * 0.7f);
            ShockRing.Spawn(ring, r * 1.15f, 0.34f, new Color(1f, 0.55f, 0.18f, 0.85f), UltimateFx.GroundFxOrder + 6);
            ShockRing.Spawn(ring, r * 1.45f, 0.5f, new Color(1f, 0.32f, 0.1f, 0.7f), UltimateFx.GroundFxOrder + 6);
            if (!local) return; // another player's slam never moves this player's camera
            CameraFollow.AddTrauma(0.62f);
            GameClock.HitStop(90f);
            CameraFollow.Punch(0.6f);
            ScreenFlash.Trigger(new Color(1f, 0.45f, 0.14f, 0.1f), 0.2f);
        }

        const float BoomFrameRadius = 48f;

        static void Show(Pair p, Strip art, int frame, float alpha, float glowAlpha, Vector2 at, Vector2 scale, int backOrder, bool allInFront)
        {
            bool on = art != null && alpha > 0.01f;
            p.Back.enabled = p.Front.enabled = on;
            p.BackGlow.enabled = p.FrontGlow.enabled = on && FeelConfig.FxOn;
            if (!on) return;
            Sprite b = art.Back[frame], f = art.Front[frame];
            p.Back.sprite = p.BackGlow.sprite = b;
            p.Front.sprite = p.FrontGlow.sprite = f;
            Color c = new Color(1f, 1f, 1f, alpha), g = new Color(Hot.r, Hot.g, Hot.b, glowAlpha * alpha);
            p.Back.color = p.Front.color = c;
            p.BackGlow.color = p.FrontGlow.color = g;
            p.Back.transform.position = p.Front.transform.position = at;
            p.Back.transform.localScale = p.Front.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            int back = allInFront ? FeelConfig.FxOrder - 3 : backOrder;
            p.Back.sortingOrder = back; p.BackGlow.sortingOrder = back + 1;
            p.Front.sortingOrder = FeelConfig.FxOrder - 3; p.FrontGlow.sortingOrder = FeelConfig.FxOrder - 2;
        }

        /// <summary>1 inside [from, to], easing over `ms` at both ends.</summary>
        static float Fade(float age, float from, float to, float ms) { return Mathf.Clamp01(Mathf.Min((age - from) / ms, (to - age) / ms)); }

        static int ByProgress(float t) { return Mathf.Clamp(Mathf.FloorToInt(t * Frames), 0, Frames - 1); }

        int PoseFrame()
        {
            for (int i = 0; i < PoseUntil.Length; i++) if (age < PoseUntil[i]) return i;
            return Land;
        }

        void Apply()
        {
            if (hero == null) return;
            float arc = Mathf.Clamp01(age / LandMs), lift = age < LandMs ? Mathf.Sin(arc * Mathf.PI) * LiftUnits : 0f;
            if (age < PoseEndMs) hero.SetSkillPose(PoseAnim, PoseFrame(), lift); else hero.ClearSkillPose();
            Vector2 feet = hero.Position;
            int behind = GameConfig.OrderBase(landed ? landing.y : takeOff.y) - 2;
            Show(cast, castArt, ByProgress(age / CastToMs), Fade(age, -CrossMs, CastToMs, CrossMs), 0.3f, takeOff + new Vector2(0f, RingLift), Vector2.one, behind, false);
            // The trail hangs behind a hero in the air, so it draws under the raised figure.
            Show(air, airArt, ByProgress((age - AirFromMs) / (AirToMs - AirFromMs)), Fade(age, AirFromMs, AirToMs, CrossMs), 0.35f, feet + new Vector2(0f, lift + 8f), flip, GameConfig.OrderBase(feet.y - lift) - 2, false);
            float slamMirror = ((age - SlamFromMs) / (SlamToMs - SlamFromMs)) < 0.34f ? flip.x : 1f; // the first frames fall on a slant, along the leap
            Show(slam, slamArt, ByProgress((age - SlamFromMs) / (SlamToMs - SlamFromMs)), Fade(age, SlamFromMs, SlamToMs, CrossMs), 0.5f, landing + new Vector2(0f, RingLift), new Vector2(slamMirror, 1f), behind, true);
            float t = (age - LandMs) / BoomMs;
            Show(boom, boomArt, ByProgress(t), t < 0f || t >= 1f ? 0f : Mathf.Min(1f, (1f - t) * 4f), 0.42f, landing + new Vector2(0f, RingLift), Vector2.one, GameConfig.OrderBase(landing.y) - 2, false);
            groundGlow.transform.position = landed ? landing : takeOff;
            float glow = landed ? 0.42f * Mathf.Clamp01(1f - t) : 0.2f * Fade(age, 0f, LandMs, 80f);
            float size = landed ? BoomFrameRadius * 2.5f : 56f;
            groundGlow.transform.localScale = new Vector3(size, size * 0.58f, 1f);
            groundGlow.color = new Color(Red.r, Red.g, Red.b, glow);
        }
    }
}
