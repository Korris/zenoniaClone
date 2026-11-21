using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The picture of the warrior's INFERNO CRASH; damage, burn and the leap itself stay in the skill data (SkillRun on the authority), so this runs only where RendersFx.
    /// Timeline (ms): wind-up at the take-off spot, the hero rises on an arc 0-400 (pose frames + height from here; the sim position and the shadow never
    /// leave the ground), lands at 400 (= the leap's DurationMs in SkillTable) while the Multi-Hit Barrage lands 450-1100; the finisher time is read from
    /// the skill data (last step of SkillSchedule) so the big shake can never drift from the damage.
    /// Shapes: WarriorInfernoLightFx (RO3 Soft Fantasy V1 shader FX + a fire Mesh Trail along the leap, 2026-11-21; the painted fx/infernocrash frames
    /// are gone), laid once at the cast on the same strike steps. Screen effects are for the local caster only.
    /// </summary>
    public class InfernoCrashUltimate : MonoBehaviour
    {
        public const string SkillId = "warrior.inferno-crash";
        public const string PoseAnim = "inferno";
        const float LandMs = 400f, BoomMs = 900f, TotalMs = LandMs + BoomMs, PoseEndMs = 700f; // the boom outlasts the last hit (1100)
        const float LiftUnits = 30f;
        const int Land = 4;
        static readonly float[] PoseUntil = { 70f, 190f, 300f, LandMs };

        HeroController hero;
        ISkillBody body; // the zone pump finds a skill object's caster through this field: a portal transfer ends the picture with its hero
        float age;
        bool local, landed, finished;
        float finishMs;

        public static void Cast(SkillContext ctx)
        {
            if (!NetMode.RendersFx) return;
            var go = new GameObject("InfernoCrash");
            go.transform.SetParent(ctx.Body.Hero.transform.parent, false);
            go.AddComponent<InfernoCrashUltimate>().Begin(ctx);
        }

        /// <summary>Loads the light FX masks / shaders ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload() { WarriorLightLook.Load(); }

        void Begin(SkillContext ctx)
        {
            hero = ctx.Body.Hero;
            body = ctx.Body;
            local = body.IsLocal;
            Vector2 takeOff = hero.Position;
            Vector2 landing = SkillAim.ClampToWalls(takeOff, takeOff + ctx.Dir * ctx.Def.Effects[0].MoveTiles * ctx.Units);
            finishMs = Rpg.SkillSchedule.LastStepMs(Rpg.SkillSchedule.Build(ctx.Def.Effects));
            float crater = 3f * ctx.Units;
            foreach (Rpg.SkillEffect e in ctx.Def.Effects) if (e.Kind == Rpg.EffectKind.Strike) { crater = e.Radius * ctx.Units; break; }
            var fin = new System.Collections.Generic.List<bool>();
            System.Collections.Generic.List<float> strikes = WarriorLightSkills.StrikeTimes(ctx.Def, fin);
            WarriorInfernoLightFx.Build(hero, ctx.Def.Id, takeOff, landing, ctx.Dir, crater, LandMs, LiftUnits, strikes, fin, TotalMs);
            UltimateFx.Dust(takeOff, 8, 10f);
            if (local) CameraFollow.SkillShake(0.08f);
            Apply();
        }

        void OnDestroy() { if (hero != null) hero.ClearSkillPose(); }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= TotalMs) { Destroy(gameObject); return; } // a fallen caster ends the picture
            age += GameClock.Dt * 1000f;
            if (!landed && age >= LandMs) Impact();
            if (landed && !finished && age >= finishMs) Finish();
            Apply();
        }

        void Impact()
        {
            landed = true;
            if (!local) return; // another player's slam never moves this player's camera
            CameraFollow.SkillShake(0.2f); // the landing only opens the barrage; the big shake waits for the finisher (Finish)
            GameClock.HitStop(20f);
        }

        /// <summary>The 350 % finisher: the strongest shake, hit-stop, punch-zoom and flash of the skill (ChatGPT V3: 60 ms, 5 px).</summary>
        void Finish()
        {
            finished = true;
            if (!local) return;
            CameraFollow.SkillShake(0.62f);
            GameClock.HitStop(60f);
            CameraFollow.SkillPunch(0.6f);
            ScreenFlash.Trigger(new Color(1f, 0.45f, 0.14f, 0.1f), 0.2f);
        }

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
        }
    }
}
