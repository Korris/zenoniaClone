using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Warrior ultimate EARTH BREAK (web bladeStorm / UltimateEffect earthBreak, rank 3: radius 54, 380% ATK, range 36).
    /// Timeline (ms, same as the web): charge ring 0-500, forward golden sweep 450-950 (hit 560 for 20%), giant sword descends 850-1450,
    /// SLAM at 1450 (full damage, non-boss monsters launched for 1.5 s), fissure patch 1450-2450 ticking at 1700/1950/2200/2450 for 12%.
    /// Unity extras: converging embers, red ground telegraph, additive gold bloom layers, rock debris + dust + ember GPU particles,
    /// shockwave rings, hit-stop, trauma shake, camera punch-zoom and a soft orange screen flash.
    /// </summary>
    public class EarthBreakUltimate : MonoBehaviour
    {
        public const float Radius = 54f, Mult = 3.8f, Range = 36f, TotalMs = 2500f, SlamMs = 1450f;
        const string Folder = "fx/earthbreak";
        const float Scale = 0.245f * 1.1f; // world units per source px (web: size 62.4 / 256 at the radius 54 slam, slightly bigger for impact)
        static readonly float[] BurnAt = { 1700f, 1950f, 2200f, 2450f };
        static readonly Vector2[] DirVec = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };
        static readonly float[] DirAngle = { -90f, 90f, 180f, 0f };
        static readonly Color Gold = new Color(1f, 0.62f, 0.16f);

        HeroController hero;
        Vector2 caster, center;
        int dir;
        float age, atk;
        bool swept, slammed;
        int burns;
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
            if (!swept && age >= 560f) { swept = true; Sweep(); }
            if (!slammed && age >= SlamMs) { slammed = true; Slam(); }
            while (burns < BurnAt.Length && age >= BurnAt[burns]) { burns++; Burn(); }
            Apply();
        }

        void Sweep()
        {
            SkillDamage.Radial(center, Radius * 0.75f, atk, Mult * 0.2f, 20f, false, null, 0f, hero);
            CameraFollow.AddTrauma(0.3f);
            Vector2 mid = (caster + center) * 0.5f + new Vector2(0f, 6f);
            UltimateFx.Embers(mid, 26, 16f, 40f, new Color(1f, 0.72f, 0.25f, 1f));
        }

        void Slam()
        {
            if (NetMode.RendersFx) GameSfx.Play("skills/warrior/earth-break-impact", 0.9f);
            List<Monster> hit = SkillDamage.Radial(center, Radius, atk, Mult, 0f, true, null, 0f, hero);
            for (int i = 0; i < hit.Count; i++) hit[i].Launch();
            GameClock.HitStop(110f);
            CameraFollow.AddTrauma(1f);
            CameraFollow.Punch(1f);
            ScreenFlash.Trigger(new Color(1f, 0.55f, 0.15f, 0.1f), 0.18f);
            int g = UltimateFx.GroundFxOrder + 6;
            ShockRing.Spawn(center, Radius * 1.45f, 0.5f, new Color(1f, 0.62f, 0.18f, 0.9f), g);
            ShockRing.Spawn(center, Radius * 0.9f, 0.32f, new Color(1f, 0.8f, 0.3f, 0.6f), g + 1, 0.72f, 0.1f, true);
            UltimateFx.Debris(center, 34, Radius * 0.6f, 120f);
            UltimateFx.Dust(center, 22, Radius * 0.8f);
            UltimateFx.Embers(center, 70, Radius * 0.6f, 95f, new Color(1f, 0.6f, 0.15f, 1f));
        }

        void Burn()
        {
            SkillDamage.Radial(center, Radius, atk, Mult * 0.12f, 0f, false, null, 0f, hero);
            CameraFollow.AddTrauma(0.14f);
            UltimateFx.Embers(center, 22, Radius * 0.7f, 60f, new Color(1f, 0.5f, 0.12f, 1f));
        }

        static int Frame(float t, int n = 6) { return Mathf.Clamp(Mathf.FloorToInt(t * n), 0, n - 1); }

        void Apply()
        {
            if (!NetMode.RendersFx) return;
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
