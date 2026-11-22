using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Mechanic ultimate ROCKET BARRAGE (web carpetBomb / UltimateEffect rocketBarrage, rank 3: 10 rockets, blast radius 18, 400% ATK each, range 70). Timeline (ms, same as the web):
    /// lock-on 0-1400, rockets launch in three groups at 400 / 440 / 480 (600 ms climb), each one descends over the 520 ms before its impact at 1550 + 85 * i,
    /// every impact damages its own blast circle (35 knockback). The targets are a fixed snapshot of the forward fan: they do not follow the caster.
    /// Picture since 2026-11-21 (RO3 Soft Fantasy V1, no sprite frames; the painted rocketbarrage sheet is gone): cyan lock-on reticle over the fan + a small honey
    /// marker on every landing spot, brass rockets with short honey trails and exhaust puffs (climb into the sky, then dive onto the spot), and a soft fantasy
    /// explosion per impact (MechanicLightBursts.Explosion: warm core, ring on the blast radius, dissolving smoke, sparks). Shake per rocket, hit-stop / punch / flash
    /// on the last, the caster's screen only.
    /// </summary>
    public class RocketBarrageUltimate : MonoBehaviour
    {
        public const float Radius = Rpg.LegacySkillNumbers.RocketBarrageRadius, Mult = Rpg.LegacySkillNumbers.RocketBarrageMult, Range = Rpg.LegacySkillNumbers.RocketBarrageRange;
        public const float LockMs = 1400f, LaunchMs = 400f, LaunchGapMs = 40f, ClimbMs = 600f, ImpactMs = 1550f, ImpactGapMs = 85f, DescendMs = 520f, TailMs = 850f;
        const int Rockets = Rpg.LegacySkillNumbers.RocketBarrageRockets;
        const float TotalMs = ImpactMs + (Rockets - 1) * ImpactGapMs + TailMs;
        const float MuzzleHeight = 9f, CenterLift = 6f;
        static readonly Vector2[] DirVec = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };
        static readonly float[] DirAngle = { -90f, 90f, 180f, 0f };
        static readonly Vector2 DescendVec = new Vector2(-25f, 85f);   // the dive starts this far from its impact point (web, y flipped)
        static readonly Vector2 ApexVec = new Vector2(-10f, 120f);     // the climb ends this far above the target, out of the top of the picture

        sealed class Rocket { public Vector2 Target; public float LaunchMs, ImpactMs; }

        HeroController hero;
        Vector2 caster, center;
        int dir;
        float age, atk, dim;
        bool launched, dived;
        int impacts, dives;
        readonly Rocket[] rockets = new Rocket[Rockets];
        ClericLightFx host;

        public static void Cast(HeroController owner) { Cast(owner, 1f); }

        /// <summary>Skill-tree cast: `damageScale` is the rank multiplier (SkillDef.DamageMul) applied to the caster ATK of this cast.</summary>
        public static void Cast(HeroController owner, float damageScale)
        {
            var go = new GameObject("RocketBarrage");
            go.transform.SetParent(owner.transform.parent, false);
            go.AddComponent<RocketBarrageUltimate>().Begin(owner, damageScale);
        }

        /// <summary>Shader materials ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload() { if (NetMode.RendersFx) MechanicLightLook.Load(); }

        void Begin(HeroController owner, float damageScale)
        {
            hero = owner;
            caster = owner.Position;
            dir = owner.Facing;
            center = caster + DirVec[dir] * Range * 0.78f + new Vector2(0f, CenterLift);
            atk = owner.AtkStat * damageScale;
            for (int i = 0; i < Rockets; i++) rockets[i] = MakeRocket(i);
            if (!NetMode.RendersFx || !MechanicLightLook.Active) return; // headless server: damage timeline only
            dim = MechanicLightLook.Dim(owner);
            LockOn();
        }

        Rocket MakeRocket(int i)
        {
            // web angle + (i / (n - 1) - 0.5) * 60 deg with y down: the minus keeps the same rocket on the same side after the flip
            float a = (DirAngle[dir] - (i / (Rockets - 1f) - 0.5f) * Rpg.LegacySkillNumbers.RocketBarrageFanDeg) * Mathf.Deg2Rad;
            float dist = Range * (Rpg.LegacySkillNumbers.RocketBarrageNear + (i % 3) * Rpg.LegacySkillNumbers.RocketBarrageStep);
            return new Rocket
            {
                Target = caster + new Vector2(Mathf.Cos(a) * dist, Mathf.Sin(a) * dist + CenterLift),
                LaunchMs = LaunchMs + (i % 3) * LaunchGapMs, ImpactMs = ImpactMs + i * ImpactGapMs,
            };
        }

        /// <summary>0-1400 ms: a cyan reticle over the fan centre and a honey marker ring on each landing spot (shown until its rocket lands).</summary>
        void LockOn()
        {
            MechanicLightBursts.Reticle(center - new Vector2(0f, CenterLift), 64f, 320f, LockMs, dim, MechanicLightLook.Tech);
            host = MechanicLightBursts.Host("RocketBarrage", center, TotalMs, dim);
            Material mark = MechanicLightLook.Line(MechanicLightLook.Band, MechanicLightLook.Fire);
            for (int i = 0; i < Rockets; i++)
            {
                Rocket r = rockets[i];
                float t0 = 120f + i * 40f;
                host.Ground(mark, t0, r.ImpactMs, 6f, MechanicLightLook.BandQuad(Radius * 0.7f), 0.9f, ClericLightFx.Squash, 0f, 5, r.Target - center).Env(120f, 60f);
            }
        }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= TotalMs) { if (host != null) Destroy(host.gameObject); Destroy(gameObject); return; } // a fallen caster ends the timeline (web)
            age += GameClock.Dt * 1000f;
            if (!launched && age >= LaunchMs) { launched = true; Launch(); }
            while (dives < Rockets && age >= rockets[dives].ImpactMs - DescendMs) Dive(dives++);
            while (impacts < Rockets && age >= rockets[impacts].ImpactMs) Explode(impacts++);
        }

        void Launch()
        {
            GameSfx.Play("skills/mechanic/homing-rockets-cast", 0.9f);
            if (host == null) return;
            Vector2 muzzle = caster + new Vector2(0f, MuzzleHeight);
            MechanicLightBursts.Muzzle(muzzle, 90f, 32f, dim, MechanicLightLook.Honey, 14);
            host.Upright(MechanicLightLook.SmokeMat, ClericLightFx.Band.Over, false, host.AgeMs, host.AgeMs + 700f, 8f, 20f, 8f, 20f, 0.75f, caster + new Vector2(-6f, 4f) - center, -3).Floor(4f).Rise(10f).Dissolve()
                .Upright(MechanicLightLook.SmokeMat, ClericLightFx.Band.Over, false, host.AgeMs + 40f, host.AgeMs + 760f, 7f, 18f, 7f, 18f, 0.7f, caster + new Vector2(7f, 2f) - center, -3).Floor(2f).Rise(10f).Dissolve();
            if (hero == LocalPlayer.Hero) CameraFollow.SkillShake(0.25f);
            for (int i = 0; i < Rockets; i++)
            {
                Rocket r = rockets[i];
                Vector2 apex = r.Target + ApexVec, side = new Vector2((i - (Rockets - 1) * 0.5f) * 3f, 0f);
                float delay = r.LaunchMs - LaunchMs;
                host.At(host.AgeMs + delay, f =>
                    MechanicLightBolt.Fire(f, u => { float e = u * u; return Vector2.Lerp(muzzle, apex + side, e) + new Vector2(0f, Mathf.Sin(u * Mathf.PI) * 6f); }, ClimbMs,
                        MechanicLightLook.Glyph(MechanicLightLook.Core, MechanicLightLook.Honey), new Vector2(13f, 6f), MechanicLightLook.Fire, 4.6f, 0.18f)
                        .Halo(MechanicLightLook.Flash(MechanicLightLook.Fire), 13f).Exhaust(6f, 4.6f));
            }
        }

        /// <summary>Rocket `i` dives from above onto its spot over the last DescendMs before its impact.</summary>
        void Dive(int i)
        {
            if (host == null) return;
            Rocket r = rockets[i];
            Vector2 from = r.Target + DescendVec, to = r.Target;
            MechanicLightBolt.Fire(host, u => Vector2.Lerp(from, to, u * u * (2f - u)), DescendMs,
                MechanicLightLook.Glyph(MechanicLightLook.Core, MechanicLightLook.Honey), new Vector2(14f, 6.5f), MechanicLightLook.Fire, 5f, 0.2f)
                .Halo(MechanicLightLook.Flash(MechanicLightLook.Fire), 15f).Exhaust(6f, 4.8f);
            dived = true;
        }

        void Explode(int i)
        {
            Rocket r = rockets[i];
            bool last = i == Rockets - 1;
            SkillDamage.Radial(r.Target, Radius, atk, Mult, Rpg.LegacySkillNumbers.RocketBarrageKnockback, last, ClassPalette.Orange, 0f, hero);
            if (!NetMode.RendersFx) return;
            if (i % 2 == 0 || last) GameSfx.Play("skills/mechanic/homing-rockets-impact", last ? 0.9f : 0.7f); // every other rocket: 85 ms apart they would stack into noise
            if (dived)
            {
                MechanicLightBursts.Explosion(r.Target - new Vector2(0f, CenterLift * 0.5f), Radius, dim, last);
                MechanicLightBursts.SplitPulses("mechanic.rocket-barrage", r.Target, Radius, dim, MechanicLightLook.Honey); // flashes with the split damage numbers
            }
            bool local = hero == LocalPlayer.Hero; // stop, shake, flash and zoom are the caster's own
            if (local) CameraFollow.SkillShake(last ? 0.6f : 0.22f);
            if (!last || !local) return;
            GameClock.HitStop(60f);
            CameraFollow.SkillPunch(0.6f);
            ScreenFlash.Trigger(new Color(1f, 0.85f, 0.5f, 0.1f), 0.2f);
        }
    }
}
