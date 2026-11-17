using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Warrior ultimate EARTH BREAK (web bladeStorm / UltimateEffect earthBreak, rank 3: radius 54, 380% ATK, range 36).
    /// Timeline (ms): charge ring 0-500, forward golden sweep 450-950 (picture only), giant sword descends 850-1450. Every hit lands once the sword is in the ground
    /// (user 17/11: the numbers must burst when the sword stabs down, not while it falls): the SLAM at 1450 opens 8 small hits (60 % every 45 ms, Multi-Hit
    /// Barrage, ChatGPT V3), 100 % at 1850, the 300 % finisher at 2000 (non-boss monsters launched for 1.5 s), fissure burns of 20 % at 2150/2250/2350/2450.
    /// DEF is subtracted once per cast, split by each hit's share of the 880 % chain.
    /// Unity extras: converging embers, red ground telegraph, additive gold bloom layers, rock debris + dust + ember GPU particles,
    /// shockwave rings, hit-stop, trauma shake, camera punch-zoom and a soft orange screen flash.
    /// </summary>
    public class EarthBreakUltimate : MonoBehaviour
    {
        public const float Radius = Rpg.LegacySkillNumbers.EarthBreakRadius, Range = Rpg.LegacySkillNumbers.EarthBreakRange, TotalMs = 2500f, SlamMs = 1450f;
        const string Folder = "classes/warrior/fx/earthbreak";
        const float Scale = 0.245f * 1.1f; // world units per source px (web: size 62.4 / 256 at the radius 54 slam, slightly bigger for impact)
        static readonly float[] BurnAt = { 2150f, 2250f, 2350f, 2450f };
        const float SmallStepMs = 45f, PenultMs = 1850f, FinishMs = 2000f; // small hits start at SlamMs
        static readonly Vector2[] DirVec = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };
        static readonly float[] DirAngle = { -90f, 90f, 180f, 0f };
        static readonly Color Gold = new Color(1f, 0.62f, 0.16f);

        HeroController hero;
        Vector2 caster, center;
        int dir;
        float age, atk;
        bool penult, slammed, finished, sweptFx;
        int smalls, burns;
        const string SwingAnim = "melee1"; // the combo's horizontal slash
        const int CutFrame = 4; // frames before it hold the sword back; it and the next are forward
        const float SwingFromMs = 450f, SwingFollowMs = 600f;
        bool poseDone;
        Sprite[] charge, sweep, slam, fissure;
        SpriteRenderer chargeSr, chargeGlow, sweepSr, sweepGlow, slamSr, slamGlow, fissureSr, fissureGlow, telegraph, groundGlow;

        public static void Cast(HeroController owner) { Cast(owner, 1f); }

        /// <summary>Skill-tree cast: `damageScale` is the rank multiplier (SkillDef.DamageMul) applied to the caster ATK of this cast.</summary>
        public static void Cast(HeroController owner, float damageScale)
        {
            var go = new GameObject("EarthBreak");
            go.transform.SetParent(owner.transform.parent, false);
            go.AddComponent<EarthBreakUltimate>().Begin(owner, damageScale);
        }

        static Sprite[] ChargeArt() { return UltimateFx.Strip(Folder, "charge", 192f, 320f, Scale * 0.62f); }
        static Sprite[] SweepArt() { return UltimateFx.Strip(Folder, "sweep", 192f, 192f, Scale * 1.05f); }
        static Sprite[] SlamArt() { return UltimateFx.Strip(Folder, "slam", 192f, 340f, Scale); }
        static Sprite[] FissuresArt() { return UltimateFx.Strip(Folder, "fissures", 192f, 192f, Scale * 1.35f); }

        /// <summary>Loads and slices the 24 painted frames ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload() { ChargeArt(); SweepArt(); SlamArt(); FissuresArt(); }

        void Begin(HeroController owner, float damageScale)
        {
            hero = owner;
            caster = owner.Position;
            dir = owner.Facing;
            center = caster + DirVec[dir] * Range + new Vector2(0f, -6f);
            atk = owner.AtkStat * damageScale;
            if (!NetMode.RendersFx) return; // headless server: damage timeline only, no art / layers
            charge = ChargeArt(); sweep = SweepArt(); slam = SlamArt(); fissure = FissuresArt();
            int g = UltimateFx.GroundFxOrder;
            telegraph = ProceduralSprites.Child(transform, "Telegraph", FxSprites.Ring());
            telegraph.sortingOrder = g; // normal blend: additive washes out to white on snow
            groundGlow = ProceduralSprites.Child(transform, "GroundGlow", FxSprites.Dot());
            groundGlow.sortingOrder = g + 1;
            chargeSr = UltimateFx.Layer(transform, "Charge", out chargeGlow); chargeSr.sortingOrder = g + 2; chargeGlow.sortingOrder = g + 3;
            fissureSr = UltimateFx.Layer(transform, "Fissure", out fissureGlow); fissureSr.sortingOrder = g + 4; fissureGlow.sortingOrder = g + 5;
            sweepSr = UltimateFx.Layer(transform, "Sweep", out sweepGlow); sweepSr.sortingOrder = FeelConfig.FxOrder - 3; sweepGlow.sortingOrder = FeelConfig.FxOrder - 2;
            slamSr = UltimateFx.Layer(transform, "Slam", out slamGlow); slamSr.sortingOrder = FeelConfig.FxOrder - 1; slamGlow.sortingOrder = FeelConfig.FxOrder;
            sweepSr.transform.position = (Vector3)((caster + center) * 0.5f + new Vector2(0f, 4f));
            sweepSr.transform.rotation = Quaternion.Euler(0f, 0f, DirAngle[dir]);
            slamSr.transform.position = center;
            fissureSr.transform.position = center;
            chargeSr.transform.position = caster;
            telegraph.transform.position = center;
            groundGlow.transform.position = center;
            Apply();
        }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= TotalMs) { Destroy(gameObject); return; } // a fallen caster ends the timeline (web)
            float dt = GameClock.Dt * 1000f;
            age += dt;
            if (age < 500f && dt > 0f) UltimateFx.Embers(caster, 3, 20f, 30f, new Color(1f, 0.66f, 0.2f, 0.9f), 38f);
            if (!sweptFx && age >= 560f) { sweptFx = true; SweepFx(); }
            if (!slammed && age >= SlamMs) { slammed = true; Slam(); }
            while (slammed && smalls < Rpg.LegacySkillNumbers.EarthBreakSmallHits && age >= SlamMs + smalls * SmallStepMs) Small(smalls++);
            if (!penult && age >= PenultMs) { penult = true; Penult(); }
            if (!finished && age >= FinishMs) { finished = true; Finish(); }
            while (burns < BurnAt.Length && age >= BurnAt[burns]) { burns++; Burn(); }
            Apply();
        }

        /// <summary>DEF share of a hit: the chain subtracts DEF once per cast (SkillDef.DefShare for the hand-made ultimates).</summary>
        /// <summary>Shake, hit-stop, punch and flash are for the caster's own screen only (sprite-rules VFX 12): never another player's, never the server.</summary>
        bool Local { get { return NetMode.RendersFx && hero != null && hero == LocalPlayer.Hero; } }

        static float Share(float mult) { return mult / Rpg.LegacySkillNumbers.EarthBreakTotalMult; }

        /// <summary>The golden sweep's embers (no damage: every hit waits for the sword to land).</summary>
        void SweepFx()
        {
            if (!NetMode.RendersFx) return;
            UltimateFx.Embers((caster + center) * 0.5f + new Vector2(0f, 6f), 26, 16f, 40f, new Color(1f, 0.72f, 0.25f, 1f));
        }

        /// <summary>The sword lands: rings, rocks, the impact sound and a medium shake; the barrage starts on this frame.</summary>
        void Slam()
        {
            if (NetMode.RendersFx) GameSfx.Play("skills/warrior/earth-break-impact", 0.9f);
            if (Local) { CameraFollow.SkillShake(0.35f); GameClock.HitStop(20f); }
            if (!NetMode.RendersFx) return;
            int g = UltimateFx.GroundFxOrder + 6;
            ShockRing.Spawn(center, Radius * 1.2f, 0.4f, new Color(1f, 0.62f, 0.18f, 0.9f), g);
            UltimateFx.Debris(center, 24, Radius * 0.6f, 110f);
            UltimateFx.Dust(center, 16, Radius * 0.8f);
            UltimateFx.Embers(center, 40, Radius * 0.6f, 80f, new Color(1f, 0.6f, 0.15f, 1f));
        }

        /// <summary>Small hit i of 8 around the planted sword: no shake, no push (ChatGPT V3), a few embers every third.</summary>
        void Small(int i)
        {
            float m = Rpg.LegacySkillNumbers.EarthBreakSmallMult;
            SkillDamage.Radial(center, Radius, atk, m, 0f, false, null, 0f, hero, Share(m));
            if (NetMode.RendersFx && i % 3 == 2) UltimateFx.Embers(center, 12, Radius * 0.6f, 60f, new Color(1f, 0.6f, 0.15f, 1f));
        }

        /// <summary>The stronger hit: a light shake and a 15 ms stop.</summary>
        void Penult()
        {
            float m = Rpg.LegacySkillNumbers.EarthBreakPenultMult;
            SkillDamage.Radial(center, Radius, atk, m, 0f, false, null, 0f, hero, Share(m));
            if (NetMode.RendersFx) ShockRing.Spawn(center, Radius, 0.28f, new Color(1f, 0.55f, 0.15f, 0.8f), UltimateFx.GroundFxOrder + 6);
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
            if (Local)
            {
                GameClock.HitStop(45f); // ChatGPT V3: 45 ms for the Earth Break finisher
                CameraFollow.SkillShake(1f);
                CameraFollow.SkillPunch(1f);
                ScreenFlash.Trigger(new Color(1f, 0.55f, 0.15f, 0.1f), 0.18f);
            }
            if (!NetMode.RendersFx) return;
            int g = UltimateFx.GroundFxOrder + 6;
            ShockRing.Spawn(center, Radius * 1.45f, 0.5f, new Color(1f, 0.62f, 0.18f, 0.9f), g);
            ShockRing.Spawn(center, Radius * 0.9f, 0.32f, new Color(1f, 0.8f, 0.3f, 0.6f), g + 1, 0.72f, 0.1f, true);
            UltimateFx.Debris(center, 34, Radius * 0.6f, 120f);
            UltimateFx.Dust(center, 22, Radius * 0.8f);
            UltimateFx.Embers(center, 70, Radius * 0.6f, 95f, new Color(1f, 0.6f, 0.15f, 1f));
        }

        void Burn()
        {
            SkillDamage.Radial(center, Radius, atk, Rpg.LegacySkillNumbers.EarthBreakBurn, 0f, false, null, 0f, hero);
            if (Local) CameraFollow.SkillShake(0.14f);
            UltimateFx.Embers(center, 22, Radius * 0.7f, 60f, new Color(1f, 0.5f, 0.12f, 1f));
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

        static int Frame(float t, int n = 6) { return Mathf.Clamp(Mathf.FloorToInt(t * n), 0, n - 1); }

        void Apply()
        {
            if (!NetMode.RendersFx) return;
            Pose();
            // charge ring (0-500) + red ground telegraph for the coming slam (grows 500-1450, then fades out)
            ShowStrip(chargeSr, chargeGlow, charge, age / 500f, 1f, 0.45f);
            float tel = Mathf.Clamp01((age - 400f) / 1050f);
            float telFade = age < SlamMs ? 1f : Mathf.Clamp01(1f - (age - SlamMs) / 250f);
            telegraph.enabled = age >= 400f && telFade > 0f;
            telegraph.transform.localScale = new Vector3(Radius * 2f * (0.4f + 0.6f * tel), Radius * 2f * (0.4f + 0.6f * tel) * 0.72f, 1f);
            telegraph.color = new Color(1f, 0.28f, 0.08f, (0.3f + 0.6f * tel) * telFade);
            // forward sweep
            float sw = (age - 450f) / 500f;
            ShowStrip(sweepSr, sweepGlow, sweep, sw, 1f, 0.4f);
            // sword: lingers on the descent frames until the slam, then plays the eruption
            float sl = age < SlamMs ? (age - 850f) / 1200f : 0.5f + (age - SlamMs) / 900f;
            ShowStrip(slamSr, slamGlow, slam, sl, 1f, age >= SlamMs ? 0.18f : 0.4f);
            // fissure patch: normal art + a gold ground glow that pulses and dies away
            float fi = (age - SlamMs) / 1000f;
            ShowStrip(fissureSr, fissureGlow, fissure, fi, 1f, 0.5f);
            bool burning = fi >= 0f && fi < 1f;
            groundGlow.enabled = burning;
            if (burning)
            {
                float pulse = 0.75f + 0.25f * Mathf.Sin(age * 0.03f);
                groundGlow.transform.localScale = new Vector3(Radius * 1.9f, Radius * 1.2f, 1f);
                groundGlow.color = new Color(1f, 0.3f, 0.05f, 0.13f * pulse * (1f - fi * fi));
            }
        }

        void ShowStrip(SpriteRenderer sr, SpriteRenderer glow, Sprite[] frames, float t, float alpha, float glowAlpha)
        {
            bool on = frames != null && t >= 0f && t < 1f;
            sr.enabled = on;
            glow.enabled = on && FeelConfig.FxOn;
            if (!on) return;
            int f = Frame(t);
            float fade = Mathf.Clamp01((1f - t) / 0.25f);
            sr.sprite = frames[f];
            sr.color = new Color(1f, 1f, 1f, alpha * fade);
            glow.sprite = frames[f];
            glow.color = new Color(Gold.r, Gold.g, Gold.b, glowAlpha * fade);
        }
    }
}
