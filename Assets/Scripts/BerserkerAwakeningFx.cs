using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The picture of the warrior's BERSERKER AWAKENING; the buffs stay in the skill data (SkillRun on the authority), so this runs only where RendersFx.
    /// Timeline (ms): clenched fists 0-180, the roar and a burst of fire from the feet 180-650, a thin fire around the body that follows the hero for the buff's time, embers at the end.
    /// The fire is hollow around the hero: each frame is cut at the ring's centre line, the upper half draws behind the hero and the lower half in front, with only a faint veil of the
    /// upper half over the body, so the hero always stays visible. One object per hero: a recast restarts it instead of stacking. BuffState is not replicated, so the end comes from the buff's time.
    /// </summary>
    public class BerserkerAwakeningFx : MonoBehaviour
    {
        public const string SkillId = "warrior.berserker-awakening";
        public const string PoseAnim = "berserker";
        const float RoarMs = 180f, BurstToMs = 650f, EmbersMs = 600f, EmberEveryMs = 450f, LoopFrameMs = 150f, FadeInMs = 200f, RingLift = 1f;
        const float BurstUnitsPerPx = 0.068f, LoopUnitsPerPx = 0.045f, EmbersUnitsPerPx = 0.055f, AnchorX = 256f, AnchorY = 428f;
        const int Frames = 6, FistsFrame = 0, RoarFrame = 1, SettleFrame = 2;
        static readonly int[] LoopOrder = { 5, 0, 1, 2, 1, 0 }; // back and forth over the even-height frames: painted frames 3 and 4 are much taller, so the fire jumped
        const string Folder = "Sprites/fx/berserker/";
        static readonly Color Hot = new Color(1f, 0.42f, 0.12f);
        static readonly Dictionary<HeroController, BerserkerAwakeningFx> Live = new Dictionary<HeroController, BerserkerAwakeningFx>();

        /// <summary>One frame strip cut in two at the ring's centre line: Back above it, Front below.</summary>
        sealed class Strip { public Sprite[] Back = new Sprite[Frames], Front = new Sprite[Frames]; }

        static Strip burstArt, loopArt, embersArt;

        HeroController hero;
        ISkillBody body; // the zone pump finds a skill object's caster through this field: a portal transfer ends the picture with its hero
        float age, poseEnd, fireEnd, emberLeft;
        bool local, roared, posing;
        SpriteRenderer back, backGlow, front, frontGlow, veil, veilGlow;

        public static void Cast(SkillContext ctx)
        {
            if (!NetMode.RendersFx) return;
            HeroController caster = ctx.Body.Hero;
            BerserkerAwakeningFx fx;
            if (!Live.TryGetValue(caster, out fx) || fx == null)
            {
                var go = new GameObject("BerserkerAwakening");
                go.transform.SetParent(caster.transform.parent, false);
                fx = go.AddComponent<BerserkerAwakeningFx>();
                Live[caster] = fx;
                fx.Build(caster);
            }
            fx.Begin(ctx);
        }

        static Strip Load(string name, float unitsPerPx)
        {
            var s = new Strip();
            for (int i = 0; i < Frames; i++)
            {
                Texture2D tex = SpriteSheet.Load(Folder + name + "-" + (i + 1));
                if (tex == null) return null;
                float below = tex.height - AnchorY, ppu = 1f / unitsPerPx, px = AnchorX / tex.width;
                s.Back[i] = Sprite.Create(tex, new Rect(0f, below, tex.width, AnchorY), new Vector2(px, 0f), ppu, 0, SpriteMeshType.FullRect);
                s.Front[i] = Sprite.Create(tex, new Rect(0f, 0f, tex.width, below), new Vector2(px, 1f), ppu, 0, SpriteMeshType.FullRect);
            }
            return s;
        }

        /// <summary>Loads and slices the 18 painted frames ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload()
        {
            if (!NetMode.RendersFx) return;
            if (burstArt == null) burstArt = Load("burst", BurstUnitsPerPx);
            if (loopArt == null) loopArt = Load("loop", LoopUnitsPerPx);
            if (embersArt == null) embersArt = Load("embers", EmbersUnitsPerPx);
        }

        void Build(HeroController owner)
        {
            hero = owner;
            Preload();
            back = UltimateFx.Layer(transform, "Back", out backGlow);
            front = UltimateFx.Layer(transform, "Front", out frontGlow);
            veil = UltimateFx.Layer(transform, "Veil", out veilGlow);
        }

        void Begin(SkillContext ctx)
        {
            body = ctx.Body;
            local = body.IsLocal;
            age = 0f;
            roared = false;
            posing = true;
            emberLeft = EmberEveryMs;
            poseEnd = Mathf.Max(BurstToMs * 0.5f, ctx.Def.CastLockMs);
            fireEnd = Mathf.Max(BurstToMs, ctx.Def.Effects[0].Buff.Ms - EmbersMs * 0.5f);
            Apply();
        }

        void OnDestroy()
        {
            if (hero == null) return;
            if (posing) hero.ClearSkillPose();
            BerserkerAwakeningFx fx;
            if (Live.TryGetValue(hero, out fx) && fx == this) Live.Remove(hero);
        }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= fireEnd + EmbersMs) { Destroy(gameObject); return; } // a fallen caster ends the picture
            float dt = GameClock.Dt * 1000f;
            age += dt;
            if (!roared && age >= RoarMs) Roar();
            if (age >= BurstToMs && age < fireEnd && (emberLeft -= dt) <= 0f)
            {
                emberLeft = EmberEveryMs;
                if (FeelConfig.FxOn) UltimateFx.Embers(hero.Position + new Vector2(0f, RingLift), 1, 9f, 26f, new Color(1f, 0.45f, 0.12f, 0.9f));
            }
            Apply();
        }

        void Roar()
        {
            roared = true;
            Vector2 feet = hero.Position + new Vector2(0f, RingLift);
            UltimateFx.Embers(feet, 26, 14f, 70f, new Color(1f, 0.45f, 0.12f, 1f));
            UltimateFx.Dust(hero.Position, 8, 12f);
            ShockRing.Spawn(feet, 26f, 0.36f, new Color(1f, 0.35f, 0.1f, 0.75f), UltimateFx.GroundFxOrder + 6);
            if (!local) return; // another player's roar never moves this player's camera
            CameraFollow.AddTrauma(0.22f);
            CameraFollow.Punch(0.25f);
            ScreenFlash.Trigger(new Color(1f, 0.3f, 0.1f, 0.06f), 0.16f);
        }

        void Show(Strip art, int frame, float backAlpha, float frontAlpha, float veilAlpha, float glowAlpha)
        {
            bool on = art != null && backAlpha > 0.01f;
            back.enabled = front.enabled = veil.enabled = on;
            bool glow = on && FeelConfig.FxOn && glowAlpha > 0.01f;
            backGlow.enabled = frontGlow.enabled = glow;
            veilGlow.enabled = false;
            if (!on) return;
            Sprite up = art.Back[frame], down = art.Front[frame];
            back.sprite = backGlow.sprite = veil.sprite = up;
            front.sprite = frontGlow.sprite = down;
            back.color = new Color(1f, 1f, 1f, backAlpha);
            front.color = new Color(1f, 1f, 1f, frontAlpha);
            veil.color = new Color(1f, 1f, 1f, veilAlpha);
            backGlow.color = frontGlow.color = new Color(Hot.r, Hot.g, Hot.b, glowAlpha);
            Vector2 at = hero.Position + new Vector2(0f, RingLift);
            back.transform.position = front.transform.position = veil.transform.position = at;
            int order = GameConfig.OrderBase(hero.Position.y);
            back.sortingOrder = order - 2; backGlow.sortingOrder = order - 1;
            front.sortingOrder = order + 7; frontGlow.sortingOrder = order + 7;
            veil.sortingOrder = order + 7;
        }

        void Apply()
        {
            if (hero == null) return;
            if (age < poseEnd) hero.SetSkillPose(PoseAnim, age < RoarMs ? FistsFrame : age < poseEnd - 120f ? RoarFrame : SettleFrame, 0f);
            else if (posing) { hero.ClearSkillPose(); posing = false; }
            if (age < BurstToMs)
            {
                int f = Mathf.Clamp(Mathf.FloorToInt(age / BurstToMs * Frames), 0, Frames - 1);
                Show(burstArt, f, 0.85f, 0.6f, 0.14f, 0.28f);
            }
            else if (age < fireEnd)
            {
                float k = Mathf.Clamp01((age - BurstToMs) / FadeInMs);
                int f = LoopOrder[Mathf.FloorToInt(age / LoopFrameMs) % LoopOrder.Length];
                Show(loopArt, f, 0.62f * k, 0.42f * k, 0.09f * k, 0.1f * k);
            }
            else
            {
                float t = Mathf.Clamp01((age - fireEnd) / EmbersMs);
                Show(embersArt, Mathf.Clamp(Mathf.FloorToInt(t * Frames), 0, Frames - 1), 0.8f * (1f - t * 0.4f), 0.6f * (1f - t * 0.4f), 0.1f * (1f - t), 0f);
            }
        }
    }
}
