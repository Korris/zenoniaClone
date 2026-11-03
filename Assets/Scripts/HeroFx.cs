using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Cosmetic half of the hero: slash / trail / dagger FX, cast flashes, floating text, sound, camera kicks, level-up and potion effects.
    /// Gameplay code (HeroController) calls these with plain data and never touches an FX class itself; every entry returns at once when this
    /// process renders nothing (dedicated server), so the sim runs without a single sprite, AudioSource or camera.
    /// </summary>
    public static class HeroFx
    {
        /// <summary>Melee swing visuals at the hit box centre: dagger flurry, or slash arc + class whoosh + sword trail. `echo` = a later stab (mirrored).</summary>
        public static void Swing(HeroClassDef cls, Vector2 center, Vector2 feet, float angle, ComboHit h, int step, bool finisher, bool echo, string weaponGlow = null)
        {
            if (!NetMode.RendersFx) return;
            bool flip = (step == 2) != echo;
            if (!echo) ClassSfx.Swing(cls.Id, step, finisher); // one whoosh per combo step (+ finisher shout), not again for the echo stab
            if (cls.Id == HeroClass.Assassin) { DaggerFx.Strike(center, angle, h.Reach, finisher, echo, step); return; }
            // One swing effect only (user decision 2026-11-03): the sword trail ribbon in the class colours, as before. The painted slash arc
            // (SlashEffect, weapon-coloured) was tried and dropped; the weapon colour is still available through weaponGlow if it comes back.
            SwordTrail.Spawn(feet.x, feet.y + 6f, angle, h.Reach, finisher, flip, cls.Palette);
        }

        /// <summary>A swing connected: impact sound and camera trauma (hit-stop is the sim's call, offline only).</summary>
        public static void MeleeLanded(HeroClassDef cls, bool anyCrit, bool finisher)
        {
            if (!NetMode.RendersFx) return;
            ClassSfx.MeleeHit(cls.Id, anyCrit);
            CameraFollow.AddTrauma(finisher ? FeelConfig.TraumaFinisher : anyCrit ? FeelConfig.TraumaCrit : FeelConfig.TraumaNormal);
        }

        /// <summary>Painted cast animation (swirl at the staff, muzzle blast at the gun) once per ranged step, as the windup starts.</summary>
        public static void CastWindup(HeroClassDef cls, ComboHit h, Vector2 tip, float facingAngle, bool finisher, HeroController shooter = null)
        {
            if (!NetMode.RendersFx) return;
            if (finisher && cls.Id == HeroClass.Mechanic && MechanicCannonBlastFx.Play(shooter, tip, facingAngle)) return;
            VfxSet set = VfxSprites.For(h.Bolt.Style);
            if (set == null) return;
            float angle = h.Bolt.Style == BoltStyle.Plasma ? facingAngle : 0f; // the muzzle blast points along the aim, swirls are round
            VfxAnim.Cast(set, tip, angle, finisher);
        }

        // Mechanic: where each muzzle is, world units from the feet (down, up, left, right), measured by Tools/build-mechanic-dual.py muzzles. Pictures only: the bolt's own start stays in FireBolt.
        static readonly Vector2[] MuzzleLeftHand = { new Vector2(7.6f, 12.4f), new Vector2(-8.6f, 14.3f), new Vector2(-9.9f, 13.5f), new Vector2(10.7f, 12.1f) };
        static readonly Vector2[] MuzzleRightHand = { new Vector2(-9.1f, 13.3f), new Vector2(8f, 15.8f), new Vector2(-9.6f, 13.1f), new Vector2(10.8f, 12.5f) };
        static readonly Vector2[] MuzzleBothRight = { new Vector2(-9.6f, 10.9f), new Vector2(8.8f, 13.3f), new Vector2(-8.8f, 11.4f), new Vector2(8.8f, 11.4f) };
        static readonly Vector2[] MuzzleBothLeft = { new Vector2(9.8f, 10.9f), new Vector2(-9f, 12f), new Vector2(-9.3f, 12f), new Vector2(9.3f, 12f) };
        static readonly Vector2[] MuzzleCannon = { new Vector2(3.4f, -2.2f), new Vector2(4.9f, 27.3f), new Vector2(-13f, 10.5f), new Vector2(13f, 10.5f) };

        /// <summary>The flash of combo step `step` (1-4): left pistol, right pistol, both (the right one here), cannon. `fallback` for every other class.</summary>
        public static Vector2 CastTipFor(HeroClassDef cls, int step, int facing, Vector2 feet, Vector2 fallback)
        {
            if (cls.Id != HeroClass.Mechanic || step < 1 || step > 4) return fallback;
            Vector2[] table = step == 1 ? MuzzleLeftHand : step == 2 ? MuzzleRightHand : step == 3 ? MuzzleBothRight : MuzzleCannon;
            return feet + table[facing & 3];
        }

        /// <summary>Hit 3's other pistol: a flash and a tracer, no damage.</summary>
        static void SecondPistol(HeroClassDef cls, ComboHit h, Vector2 feet, Vector2 dir, float angle)
        {
            if (cls.Id != HeroClass.Mechanic || cls.Hits.Length < 3 || !ReferenceEquals(h, cls.Hits[2])) return;
            int facing = Mathf.Abs(dir.x) > Mathf.Abs(dir.y) ? (dir.x < 0f ? 2 : 3) : (dir.y > 0f ? 1 : 0);
            Vector2 at = feet + MuzzleBothLeft[facing];
            VfxSet set = VfxSprites.For(h.Bolt.Style);
            if (set != null) VfxAnim.Cast(set, at, angle, false);
            MechanicTracer.Fire(at, dir, angle, h.Bolt.Speed);
        }

        /// <summary>Shot sounds, the procedural flash when the painted cast art is missing, and the recoil kick.</summary>
        public static void BoltLaunched(HeroClassDef cls, ComboHit h, bool finisher, ClassPalette pal, Vector2 feet, Vector2 tip, Vector2 dir, float angle, bool paintedCastArt)
        {
            if (!NetMode.RendersFx) return;
            ClassSfx.BoltLaunched(cls.Id, finisher);
            SecondPistol(cls, h, feet, dir, angle);
            if (!paintedCastArt) FallbackCast(cls, h, feet, tip, dir, angle, pal);
            if (!finisher || cls.Id != HeroClass.Mechanic) CameraFollow.AddTrauma(finisher ? 0.2f : 0.06f); // recoil kick; the Mechanic's cannon shakes in MechanicCannonBlastFx, for its shooter only
        }

        /// <summary>Procedural cast flash, used only when the painted VFX sprites are missing.</summary>
        static void FallbackCast(HeroClassDef cls, ComboHit h, Vector2 feet, Vector2 tip, Vector2 dir, float angle, ClassPalette pal)
        {
            bool big = h.Bolt.ExplodeRadius > 0f;
            switch (h.Bolt.Style)
            {
                case BoltStyle.Arcane: RuneFx.CastFlash(tip, big, pal); break;
                case BoltStyle.HolyArrow:
                    RuneFx.CastFlash(tip, big, pal);
                    UltimateFx.Embers(tip, big ? 12 : 7, 5f, 26f, new Color(0.7f, 1f, 0.45f, 1f));
                    break;
                case BoltStyle.Plasma: MechanicFx.Muzzle(tip, angle, big, pal); break;
                default:
                    if (h.Bolt.FlashFx != null)
                        BurstFx.Spawn(cls.FxFolder, h.Bolt.FlashFx, new Vector2(feet.x + dir.x * 10f, feet.y + 10f + dir.y * 6f), angle, big ? 1.6f : 1f, 0.14f, false, pal.Tint);
                    break;
            }
        }

        /// <summary>Red damage number over the hero and a camera shake.</summary>
        public static void Hurt(Vector2 feet, int dmg, bool shake = true)
        {
            if (!NetMode.RendersFx) return;
            if (shake) DamageNumber.Spawn(feet.x, feet.y + 26f, dmg.ToString(), new Color(1f, 0.35f, 0.35f), false);
            else DamageNumber.Spawn(feet.x, feet.y + 26f, dmg.ToString(), new Color(1f, 0.6f, 0.6f), false, false, true); // another hero was hit: small, faded
            if (shake) CameraFollow.AddTrauma(FeelConfig.TraumaHurt);
        }

        /// <summary>Violet afterimage streak along the blink line plus smoke where the hero vanished.</summary>
        public static void ShadowTrail(Vector2 from, Vector2 to)
        {
            if (!NetMode.RendersFx) return;
            int n = Mathf.Max(6, Mathf.RoundToInt(Vector2.Distance(from, to) / 4f));
            for (int i = 0; i <= n; i++)
                UltimateFx.Embers(Vector2.Lerp(from, to, i / (float)n) + new Vector2(0f, 8f), 2, 3f, 6f, new Color(0.6f, 0.35f, 1f, 0.9f));
            ShockRing.Spawn(from, 14f, 0.28f, new Color(0.5f, 0.25f, 0.9f, 0.9f), UltimateFx.GroundFxOrder + 6, 0.72f, 0.2f);
            UltimateFx.Dust(from, 4, 5f);
        }

        /// <summary>The backstab itself: dagger flurry on the target, landing ring, sparks, shake and the skill callout.</summary>
        public static void ShadowStrike(Vector2 hitAt, float angle, Vector2 landed, Vector2 heroFeet)
        {
            if (!NetMode.RendersFx) return;
            DaggerFx.Strike(hitAt, angle, 30f, true, false, 4);
            ShockRing.Spawn(landed, 16f, 0.3f, new Color(0.62f, 0.3f, 1f, 0.95f), UltimateFx.GroundFxOrder + 6, 0.72f, 0.15f);
            UltimateFx.Embers(hitAt, 26, 8f, 40f, new Color(0.7f, 0.45f, 1f, 1f));
            CameraFollow.AddTrauma(0.45f);
            DamageNumber.Spawn(heroFeet.x, heroFeet.y + 30f, "Shadow Step!", new Color(0.85f, 0.66f, 1f), false);
        }

        /// <summary>Small green sparkle of the cleric finisher's heal pulse.</summary>
        public static void HealSparkle(Vector2 feet)
        {
            if (!NetMode.RendersFx) return;
            UltimateFx.Embers(new Vector2(feet.x, feet.y + 6f), 8, 7f, 30f, new Color(0.7f, 1f, 0.5f, 1f));
        }

        /// <summary>Cleric Heal skill: green number, holy rings, rising sparkles and a soft screen tint.</summary>
        public static void Heal(Vector2 feet, int healed)
        {
            if (!NetMode.RendersFx) return;
            DamageNumber.Spawn(feet.x, feet.y + 30f, "+" + healed, new Color(0.55f, 1f, 0.55f), true);
            ShockRing.Spawn(feet, 24f, 0.55f, new Color(0.45f, 0.85f, 0.35f, 0.95f), UltimateFx.GroundFxOrder + 6, 0.72f, 0.3f);
            ShockRing.Spawn(feet, 15f, 0.4f, new Color(1f, 0.92f, 0.5f, 0.8f), UltimateFx.GroundFxOrder + 7, 0.72f, 0.2f, true);
            UltimateFx.Embers(feet + new Vector2(0f, 4f), 40, 14f, 48f, new Color(0.7f, 1f, 0.45f, 1f));
            UltimateFx.Embers(feet + new Vector2(0f, 8f), 14, 8f, 70f, new Color(1f, 0.95f, 0.55f, 1f));
            ScreenFlash.Trigger(new Color(0.5f, 1f, 0.5f, 0.07f), 0.25f);
        }

        /// <summary>Potion drunk: "HP +n" over the hero and red / blue embers.</summary>
        public static void Potion(Vector2 feet, bool hp, string text)
        {
            if (!NetMode.RendersFx) return;
            DamageNumber.Spawn(feet.x, feet.y + 28f, text, hp ? new Color(0.5f, 1f, 0.55f) : new Color(0.5f, 0.75f, 1f), false);
            UltimateFx.Embers(feet + new Vector2(0f, 6f), 10, 6f, 30f, hp ? new Color(1f, 0.45f, 0.45f, 1f) : new Color(0.45f, 0.65f, 1f, 1f));
        }

        /// <summary>Level-up: HUD banner, gold rings, embers and a flash.</summary>
        public static void LevelUp(Vector2 feet, int level, bool banner = true)
        {
            if (!NetMode.RendersFx) return;
            if (banner) RpgHud.LevelUp(level);
            ShockRing.Spawn(feet, 26f, 0.6f, new Color(1f, 0.85f, 0.35f, 0.95f), UltimateFx.GroundFxOrder + 6, 0.72f, 0.25f);
            ShockRing.Spawn(feet, 16f, 0.45f, new Color(1f, 1f, 0.7f, 0.85f), UltimateFx.GroundFxOrder + 7, 0.72f, 0.2f, true);
            UltimateFx.Embers(feet + new Vector2(0f, 6f), 36, 12f, 60f, new Color(1f, 0.9f, 0.45f, 1f));
            ScreenFlash.Trigger(new Color(1f, 0.95f, 0.6f, 0.08f), 0.3f);
        }
    }
}
