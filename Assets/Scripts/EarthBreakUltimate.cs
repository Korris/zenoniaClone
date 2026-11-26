using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Warrior ultimate EARTH BREAK (web bladeStorm / UltimateEffect earthBreak, rank 3: radius 54, 380% ATK, range 36).
    /// Timeline (ms, LegacySkillNumbers.EarthBreak*Ms; 22/11 user: "gần như 2 s mới rớt kiếm" -> sped up from slam 1450 / total 2500): charge 0-225,
    /// forward golden sweep 200-430 (picture only), giant light sword descends 350-650. Every hit lands once the sword is in the ground (user 17/11: the
    /// numbers must burst when the sword stabs down, not while it falls): the SLAM at 650 opens 8 small hits (60 % every 30 ms, Multi-Hit Barrage,
    /// ChatGPT V3), 100 % at 930, the 300 % finisher at 1030 (non-boss monsters launched for 1.5 s), fissure burns of 20 % at 1130/1200/1270/1340, end
    /// 1400. DEF is subtracted once per cast, split by each hit's share of the 880 % chain.
    /// Picture: WarriorEarthBreakLightFx (RO3 Soft Fantasy V1 shader FX, 2026-11-21; the painted fx/earthbreak frames are gone), laid once at the cast
    /// on these same times. Hit-stop, trauma shake, camera punch and the soft screen flash stay here, for the local caster only.
    /// </summary>
    public class EarthBreakUltimate : MonoBehaviour
    {
        public const float Radius = Rpg.LegacySkillNumbers.EarthBreakRadius, Range = Rpg.LegacySkillNumbers.EarthBreakRange, TotalMs = Rpg.LegacySkillNumbers.EarthBreakTotalMs,
            SlamMs = Rpg.LegacySkillNumbers.EarthBreakSlamMs;
        static readonly float[] BurnAt = BurnTimes();
        const float SmallStepMs = Rpg.LegacySkillNumbers.EarthBreakStepMs, PenultMs = Rpg.LegacySkillNumbers.EarthBreakPenultMs,
            FinishMs = Rpg.LegacySkillNumbers.EarthBreakFinishMs; // small hits start at SlamMs
        static readonly Vector2[] DirVec = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };

        HeroController hero;
        Vector2 caster, center;
        int dir;
        float age, atk;
        bool penult, slammed, finished;
        int smalls, burns;
        const string SwingAnim = "melee1"; // the combo's horizontal slash
        const int CutFrame = 4; // frames before it hold the sword back; it and the next are forward
        const float SwingFromMs = 200f, SwingFollowMs = 270f; // windup over the charge, the cut with the sweep (was 450 / 600 with the 1450 slam)
        bool poseDone;

        static float[] BurnTimes()
        {
            var t = new float[Rpg.LegacySkillNumbers.EarthBreakBurns];
            for (int i = 0; i < t.Length; i++) t[i] = Rpg.LegacySkillNumbers.EarthBreakBurnMs + i * Rpg.LegacySkillNumbers.EarthBreakBurnStepMs;
            return t;
        }

        public static void Cast(HeroController owner) { Cast(owner, 1f); }

        /// <summary>Skill-tree cast: `damageScale` is the rank multiplier (SkillDef.DamageMul) applied to the caster ATK of this cast.</summary>
        public static void Cast(HeroController owner, float damageScale) { Cast(owner, damageScale, null); }

        /// <summary>`at` = the chosen centre (Ground skill since 26/11/2026); null = the old spot Range ahead of the facing.</summary>
        public static void Cast(HeroController owner, float damageScale, Vector2? at)
        {
            var go = new GameObject("EarthBreak");
            go.transform.SetParent(owner.transform.parent, false);
            go.AddComponent<EarthBreakUltimate>().Begin(owner, damageScale, at);
        }

        /// <summary>Loads the light FX masks / shaders ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload() { WarriorLightLook.Load(); }

        void Begin(HeroController owner, float damageScale, Vector2? at)
        {
            hero = owner;
            caster = owner.Position;
            dir = owner.Facing;
            center = at ?? caster + DirVec[dir] * Range + new Vector2(0f, -6f);
            Vector2 toward = center - caster;
            toward = toward.sqrMagnitude > 1f ? toward.normalized : DirVec[dir]; // the sword flies from the hero toward the chosen spot
            atk = owner.AtkStat * damageScale;
            if (!NetMode.RendersFx) return; // headless server: damage timeline only, no picture
            WarriorEarthBreakLightFx.Build(owner, caster, center, toward, Radius, SlamMs, SmallStepMs, Rpg.LegacySkillNumbers.EarthBreakSmallHits, PenultMs, FinishMs, BurnAt, TotalMs);
            Pose();
        }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= TotalMs) { Destroy(gameObject); return; } // a fallen caster ends the timeline (web)
            age += GameClock.Dt * 1000f;
            if (!slammed && age >= SlamMs) { slammed = true; Slam(); }
            while (slammed && smalls < Rpg.LegacySkillNumbers.EarthBreakSmallHits && age >= SlamMs + smalls * SmallStepMs) Small(smalls++);
            if (!penult && age >= PenultMs) { penult = true; Penult(); }
            if (!finished && age >= FinishMs) { finished = true; Finish(); }
            while (burns < BurnAt.Length && age >= BurnAt[burns]) { burns++; Burn(); }
            if (NetMode.RendersFx) Pose();
        }

        /// <summary>Shake, hit-stop, punch and flash are for the caster's own screen only (sprite-rules VFX 12): never another player's, never the server.</summary>
        bool Local { get { return NetMode.RendersFx && hero != null && hero == LocalPlayer.Hero; } }

        /// <summary>DEF share of a hit: the chain subtracts DEF once per cast (SkillDef.DefShare for the hand-made ultimates).</summary>
        static float Share(float mult) { return mult / Rpg.LegacySkillNumbers.EarthBreakTotalMult; }

        /// <summary>The sword lands: the impact sound and a medium shake; the barrage starts on this frame.</summary>
        void Slam()
        {
            if (NetMode.RendersFx) GameSfx.Play("skills/warrior/earth-break-impact", 0.9f);
            if (Local) { CameraFollow.SkillShake(0.6f); GameClock.HitStop(55f); } // grand pass 21/11: the sword landing is a heavy hit
        }

        /// <summary>Small hit i of 8 around the planted sword: no shake, no push (ChatGPT V3).</summary>
        void Small(int i)
        {
            float m = Rpg.LegacySkillNumbers.EarthBreakSmallMult;
            SkillDamage.Radial(center, Radius, atk, m, 0f, false, null, 0f, hero, Share(m));
        }

        /// <summary>The stronger hit: a light shake and a 15 ms stop.</summary>
        void Penult()
        {
            float m = Rpg.LegacySkillNumbers.EarthBreakPenultMult;
            SkillDamage.Radial(center, Radius, atk, m, 0f, false, null, 0f, hero, Share(m));
            if (!Local) return;
            CameraFollow.SkillShake(0.12f);
            GameClock.HitStop(15f);
        }

        /// <summary>The 300 % finisher: the ground bursts, non-boss monsters are launched, the biggest shake / punch / flash of the skill.</summary>
        void Finish()
        {
            float m = Rpg.LegacySkillNumbers.EarthBreakFinishMult;
            List<Monster> hit = SkillDamage.Radial(center, Radius, atk, m, 0f, true, null, 0f, hero, Share(m));
            for (int i = 0; i < hit.Count; i++) hit[i].Launch();
            if (!Local) return;
            GameClock.HitStop(80f); // grand pass 21/11 (ChatGPT spec 85 ms; was 45)
            CameraFollow.SkillShake(1f);
            CameraFollow.SkillPunch(1f);
            ScreenFlash.Trigger(WarriorLightLook.IvoryFlash(0.07f), 0.16f); // RO trailer style: ivory, never orange-red
        }

        void Burn()
        {
            SkillDamage.Radial(center, Radius, atk, Rpg.LegacySkillNumbers.EarthBreakBurn, 0f, false, null, 0f, hero);
            if (Local) CameraFollow.SkillShake(0.14f);
        }

        /// <summary>The hero swings with the golden sweep: melee1's windup (sword back) over the charge, its forward cut as the sweep starts, held until the slam. View only.</summary>
        void Pose()
        {
            if (poseDone) return;
            if (age >= SlamMs) { poseDone = true; hero.ClearSkillPose(); return; }
            int frame = age < SwingFromMs ? Mathf.FloorToInt(age / SwingFromMs * CutFrame) : age < SwingFollowMs ? CutFrame : CutFrame + 1;
            hero.SetSkillPose(SwingAnim, frame, 0f);
        }

        void OnDestroy() { if (hero != null && !poseDone) hero.ClearSkillPose(); }
    }
}
