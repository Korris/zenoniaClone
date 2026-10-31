using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The picture of the assassin's BLADE DANCE; its four strikes stay in the skill data (SkillRun on the authority, at 0, 150, 300 and 450 ms), so this runs only where RendersFx.
    /// Timeline (ms): the hero turns on the spot with both arms out 0-480 while the slash rings build, each strike adds a brighter wider ring (frames 0-4 span 0-450); at 450 he stops
    /// in a low stance and the ring breaks into rays and dark shards 450-800. The rings are cut at their centre line: the upper half draws behind the hero and the lower half in front,
    /// and their widest frame spans the real 2 tile radius. One object per hero: a recast restarts it. Screen effects are for the local caster only.
    /// </summary>
    public class BladeDanceFx : MonoBehaviour
    {
        public const string SkillId = "assassin.blade-dance";
        const string Folder = "Sprites/fx/bladedance/", SpinAnim = NightfallExecutionUltimate.PoseAnim, StopAnim = "melee1";
        const float RingToMs = 520f, HitEveryMs = 150f, BurstMs = 450f, FinishLenMs = 350f, SpinToMs = 480f, TurnMs = 70f, PoseToMs = 600f, TotalMs = 800f;
        const float RingUnitsPerPx = 0.165f, FinishUnitsPerPx = 0.155f, AnchorY = 160f, Lift = 2f;
        const int Hits = 4, SpinFrame = 1, StopFrame = 4;
        static readonly int[] TurnOrder = { 0, 3, 1, 2 }; // down, right, up, left: one clockwise turn
        static readonly Color Violet = new Color(0.68f, 0.28f, 1f), Dark = new Color(0.3f, 0.08f, 0.5f);
        static readonly Dictionary<HeroController, BladeDanceFx> Live = new Dictionary<HeroController, BladeDanceFx>();
        static AssassinBoardFx.Strip ringArt, finishArt;

        HeroController hero;
        ISkillBody body; // the zone pump finds a skill object's caster through this field: a portal transfer ends the picture with its hero
        float age;
        bool local, posing, burst;
        int hitsShown;
        AssassinBoardFx.Pair ring, finish;
        SpriteRenderer groundGlow;

        public static void Cast(SkillContext ctx)
        {
            if (!NetMode.RendersFx) return;
            HeroController caster = ctx.Body.Hero;
            BladeDanceFx old;
            if (Live.TryGetValue(caster, out old) && old != null) { old.posing = false; Destroy(old.gameObject); } // a recast restarts instead of stacking
            var go = new GameObject("BladeDance");
            go.transform.SetParent(caster.transform.parent, false);
            BladeDanceFx fx = go.AddComponent<BladeDanceFx>();
            Live[caster] = fx;
            fx.Begin(ctx);
        }

        /// <summary>Loads and slices the 12 painted frames ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload()
        {
            if (!NetMode.RendersFx) return;
            if (ringArt == null) ringArt = AssassinBoardFx.LoadCut(Folder, "ring", AnchorY, RingUnitsPerPx);
            if (finishArt == null) finishArt = AssassinBoardFx.LoadCut(Folder, "finish", AnchorY, FinishUnitsPerPx);
        }

        void Begin(SkillContext ctx)
        {
            hero = ctx.Body.Hero;
            body = ctx.Body;
            local = body.IsLocal;
            posing = true;
            Preload();
            ring = new AssassinBoardFx.Pair(transform, "Ring");
            finish = new AssassinBoardFx.Pair(transform, "Finish");
            groundGlow = ProceduralSprites.Child(transform, "GroundGlow", FxSprites.Dot());
            groundGlow.sortingOrder = UltimateFx.GroundFxOrder + 1;
            Apply();
        }

        void OnDestroy()
        {
            if (hero == null) return;
            if (posing) hero.ClearSkillPose();
            BladeDanceFx fx;
            if (Live.TryGetValue(hero, out fx) && fx == this) Live.Remove(hero);
        }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= TotalMs) { Destroy(gameObject); return; } // a fallen caster ends the picture
            age += GameClock.Dt * 1000f;
            while (hitsShown < Hits && age >= hitsShown * HitEveryMs) StrikePulse(hitsShown++);
            if (!burst && age >= BurstMs) Burst();
            Apply();
        }

        /// <summary>Each strike of the skill data flares once: a little harder every time.</summary>
        void StrikePulse(int i)
        {
            Vector2 at = hero.Position + new Vector2(0f, Lift);
            UltimateFx.Embers(at, 6 + i * 2, 18f + i * 4f, 40f, new Color(Violet.r, Violet.g, Violet.b, 1f));
            ShockRing.Spawn(at, 20f + i * 4f, 0.22f, new Color(Violet.r, Violet.g, Violet.b, 0.5f), UltimateFx.GroundFxOrder + 6);
            if (local) CameraFollow.AddTrauma(0.05f + i * 0.02f);
        }

        void Burst()
        {
            burst = true;
            Vector2 at = hero.Position + new Vector2(0f, Lift);
            UltimateFx.Embers(at, 28, 26f, 90f, new Color(Violet.r, Violet.g, Violet.b, 1f));
            UltimateFx.Debris(at, 10, 24f, 70f);
            UltimateFx.Dust(hero.Position, 5, 18f);
            if (!local) return; // another player's strikes never move this player's camera
            CameraFollow.AddTrauma(0.28f);
            GameClock.HitStop(35f);
            CameraFollow.Punch(0.25f);
            ScreenFlash.Trigger(new Color(0.65f, 0.3f, 1f, 0.07f), 0.14f);
        }

        void ApplyPose()
        {
            if (!posing) return;
            if (age >= PoseToMs) { hero.ClearSkillPose(); posing = false; return; }
            if (age < SpinToMs) hero.SetSkillPose(SpinAnim, SpinFrame, 0f, TurnOrder[Mathf.FloorToInt(age / TurnMs) % TurnOrder.Length]);
            else hero.SetSkillPose(StopAnim, StopFrame, 0f);
        }

        void Apply()
        {
            if (hero == null) return;
            ApplyPose();
            Vector2 at = hero.Position + new Vector2(0f, Lift);
            if (age < RingToMs) ring.Show(ringArt, AssassinBoardFx.ByProgress(age / RingToMs), Mathf.Clamp01(Mathf.Min(age + 40f, RingToMs - age) / 40f), 0.34f, Violet, at);
            else ring.Hide();
            float f = (age - BurstMs) / FinishLenMs;
            if (f >= 0f && f < 1f) finish.Show(finishArt, AssassinBoardFx.ByProgress(f), Mathf.Min(1f, (1f - f) * 3f), 0.36f, Violet, at);
            else finish.Hide();
            groundGlow.transform.position = hero.Position;
            groundGlow.transform.localScale = new Vector3(80f, 50f, 1f);
            float glow = age < BurstMs ? 0.34f * Mathf.Clamp01(age / 120f) : 0.34f * Mathf.Clamp01(1f - f);
            groundGlow.color = new Color(Dark.r, Dark.g, Dark.b, glow);
        }
    }
}
