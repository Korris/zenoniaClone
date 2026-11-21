using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Warrior tree skill BLADE CYCLONE. Follows the caster, who can keep walking after the short cast lock.
    /// Timeline (ms): cast 0-500, spin 450-1300, peak 1250-2250, finish 2250-2750. 18 hits land around the hero (Multi-Hit Barrage, ChatGPT V3): 16 small ones
    /// every 100 ms from 500, a stronger one at 2100 and the finisher at 2250; the finisher also knocks enemies back (bosses are too heavy, Monster.TakeHit).
    /// Picture: WarriorCycloneLightFx (RO3 Soft Fantasy V1 shader FX + Mesh Trail ribbons, 2026-11-21; the painted fx/bladecyclone frames are gone),
    /// laid once at the cast on these same times. The loop sound stays here; for the local caster only a light shake on the strong hit, then hit-stop,
    /// punch-zoom and a soft flash on the last.
    /// </summary>
    public class BladeCycloneUltimate : MonoBehaviour
    {
        public const float Radius = Rpg.LegacySkillNumbers.BladeCycloneRadius, Knockback = Rpg.LegacySkillNumbers.BladeCycloneKnockback;
        const float CastMs = 500f, SpinFromMs = 450f, SpinToMs = 1300f, PeakFromMs = 1250f, PeakToMs = 2250f, FinishMs = 500f, TotalMs = PeakToMs + FinishMs;
        const string LoopClip = "Audio/sfx/skills/warrior/blade-cyclone-loop";
        const float SmallFromMs = 500f, SmallStepMs = 100f, PenultMs = 2100f; // the finisher lands at PeakToMs
        static readonly float[] WaveAt = BuildWaveTimes();

        static float[] BuildWaveTimes()
        {
            var t = new float[Rpg.LegacySkillNumbers.BladeCycloneHits];
            for (int i = 0; i < Rpg.LegacySkillNumbers.BladeCycloneSmallHits; i++) t[i] = SmallFromMs + i * SmallStepMs;
            t[t.Length - 2] = PenultMs;
            t[t.Length - 1] = PeakToMs;
            return t;
        }

        /// <summary>x ATK of hit i: small, then the stronger second-to-last, then the finisher.</summary>
        static float MultOf(int i)
        {
            int n = WaveAt.Length;
            return i == n - 1 ? Rpg.LegacySkillNumbers.BladeCycloneFinishMult : i == n - 2 ? Rpg.LegacySkillNumbers.BladeCyclonePenultMult : Rpg.LegacySkillNumbers.BladeCycloneSmallMult;
        }

        HeroController hero;
        ISkillBody body; // the zone pump finds a skill object's caster through this field: a portal transfer ends the cyclone with its hero
        float age, atk;
        int waves;
        bool local;
        AudioSource loop;

        public static void Cast(HeroController owner) { Cast(owner, 1f); }

        /// <summary>Skill-tree cast: `damageScale` is the rank multiplier (SkillDef.DamageMul) applied to the caster ATK of this cast.</summary>
        public static void Cast(HeroController owner, float damageScale)
        {
            var go = new GameObject("BladeCyclone");
            go.transform.SetParent(owner.transform.parent, false);
            go.AddComponent<BladeCycloneUltimate>().Begin(owner, damageScale);
        }

        /// <summary>Loads the light FX masks / shaders ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload() { WarriorLightLook.Load(); }

        void Begin(HeroController owner, float damageScale)
        {
            hero = owner;
            body = new HeroSkillBody(owner);
            atk = owner.AtkStat * damageScale;
            local = body.IsLocal;
            if (!NetMode.RendersFx) return; // headless server: damage timeline only, no picture
            owner.PlaySpinPose(PeakToMs); // the cast lock's pose shows first, then the hero turns inside the rings until the finish
            WarriorCycloneLightFx.Build(owner, Radius, WaveAt, CastMs, SpinFromMs, SpinToMs, PeakFromMs, PeakToMs, TotalMs);
            AudioClip clip = Resources.Load<AudioClip>(LoopClip);
            if (clip != null && AudioPrefs.SfxOn) // sound effects switched off in Settings: no loop
            {
                loop = gameObject.AddComponent<AudioSource>();
                loop.clip = clip; loop.loop = true; loop.volume = local ? 0.55f : 0.25f; loop.Play();
            }
        }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= TotalMs) { Destroy(gameObject); return; } // a fallen caster ends the cyclone
            age += GameClock.Dt * 1000f;
            while (waves < WaveAt.Length && age >= WaveAt[waves]) Wave(waves++);
        }

        void Wave(int i)
        {
            bool last = i == WaveAt.Length - 1, penult = i == WaveAt.Length - 2, big = last || penult;
            float mult = MultOf(i);
            // DEF is subtracted once per cast, split by this hit's share of the chain (small hits would otherwise lose most of their damage to it).
            SkillDamage.Radial(hero.Position, Radius, atk, mult, last ? Knockback : 0f, last, ClassPalette.Orange, 0f, hero, mult / Rpg.LegacySkillNumbers.BladeCycloneTotalMult);
            if (!NetMode.RendersFx) return;
            if (last) GameSfx.Play("skills/warrior/earth-break-impact", 0.7f);
            if (!local || !big) return; // small hits: no shake, no hit-stop (ChatGPT V3); another player's cyclone never moves this camera
            if (penult) { CameraFollow.SkillShake(0.08f); GameClock.HitStop(15f); return; }
            CameraFollow.SkillShake(0.5f);
            GameClock.HitStop(45f);
            CameraFollow.SkillPunch(0.5f);
            ScreenFlash.Trigger(new Color(1f, 0.4f, 0.12f, 0.08f), 0.18f);
        }
    }
}
