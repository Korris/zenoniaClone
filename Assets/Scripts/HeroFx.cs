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
            if (cls.Id == HeroClass.Assassin) { DaggerFx.Swing(center, feet, angle, h, step, finisher, echo); return; }
            // Both swing effects in the class colours, as originally (user decision 2026-11-03 after trying weapon-coloured variants):
            // pass weaponGlow to SlashEffect.Spawn / ClassPalette.ForWeapon(weaponGlow, cls.Palette) to colour them by the worn weapon again.
            bool rising = step == cls.RisingStep && !finisher, chop = finisher && cls.RisingStep > 0; // Warrior: hit 3 rises, hit 4 (jump cleave) falls
            float lift = 6f, trailAngle = angle;
            bool trailFlip = flip;
            if (rising || chop)
            {
                RisingArc(angle, out trailAngle, out trailFlip);
                if (chop) trailFlip = !trailFlip; // same arc on the weapon side, run from high to low
                lift = RisingLift;
                if (rising) { angle = trailAngle; flip = trailFlip; }
            }
            SlashEffect.Spawn(center.x, center.y, angle, h.FxScale, finisher, flip, FeelConfig.FxOrder); // the chop art picks its own upright pose from the facing angle
            SwordTrail.Spawn(feet.x, feet.y + lift, trailAngle, h.Reach, finisher, trailFlip, cls.Palette);
        }

        /// <summary>
        /// Rising slash (Warrior hit 3): the arc stands on the weapon side and runs from low to high, instead of across the facing.
        /// The trail ribbon runs from +span to -span (clockwise) unless reversed. Facing down the sword is on screen-left (art of melee3up),
        /// facing up on screen-right; sideways it rises in front.
        /// </summary>
        const float RisingLift = 12f; // ribbon centre at mid-body, so the rising arc climbs from the feet to above the head

        static void RisingArc(float facingAngle, out float angle, out bool reverse)
        {
            bool leftSide = facingAngle < -45f || facingAngle > 135f; // down or left
            angle = leftSide ? 180f : 0f;
            reverse = !leftSide; // left side: 252 -> 108 deg is clockwise; right side: -72 -> 72 is counter-clockwise
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
            bool big = finisher || (h.Bolt.Style == BoltStyle.Plasma && h.Bolt.Scale > 1f); // the Mechanic's big hit-3 bullet gets the big flash
            // Cleric: the flash at the staff crystal / shield goes behind her whenever her bolt does (RangedAimRules.ClericStartsBehind): facing up the
            // staff and shield are behind her body, facing down the raised staff's crystal is beside her face (hits 3, 4; the big hit-4 flash covered an eye)
            int clericFacing = facingAngle > 45f && facingAngle < 135f ? 1 : facingAngle < -45f && facingAngle > -135f ? 0 : 3;
            if (cls.Id == HeroClass.Cleric && shooter != null && Rpg.RangedAimRules.ClericStartsBehind(System.Array.IndexOf(cls.Hits, h) + 1, clericFacing))
                VfxAnim.Cast(set, tip, angle, big, GameConfig.OrderBase(shooter.Position.y) - 1);
            else VfxAnim.Cast(set, tip, angle, big);
        }

        // Mechanic flashes. Pistol shots (hits 1-3) flash at the shooting gun's muzzle, where the bolt starts (RangedAimRules.MechanicMuzzle; approved
        // board 2026-11-08: muzzle flash at the gun, hit 3 one at each gun with one big blue bolt between them; the painted v2 flash is a small star,
        // not the old cone that read as extra shots). The cannon blast (hit 4) stays in the middle of the body ("pháo nhát 4 cũng bắn từ người ra"),
        // since the board it leaves the cannon muzzle like the pistol bolts (RangedAimRules.MechanicMuzzle step 4), MechanicCannonBlastFx grows the shell
        // and the discharge out of it. Offsets are for figure scale 1: the callers pass HeroRenderer.FigureScale (dev-start.json heroScale) so the flash stays on the gun.
        const float BodyFlashLift = Rpg.RangedAimRules.BoltLift + Rpg.RangedAimRules.BoltDrawLift;
        // Hit 3, each gun's muzzle (gun = right hand, gunL = left hand), facing down, up, left, right: same measure as RangedAimRules.MechanicMuzzle.
        static readonly Vector2[] Hit3MuzzleGun = { new Vector2(-3.0f, 11.3f), new Vector2(5.1f, 16.9f), new Vector2(-12.4f, 13.7f), new Vector2(12.4f, 13.7f) };
        static readonly Vector2[] Hit3MuzzleGunL = { new Vector2(1.6f, 11.3f), new Vector2(-6.0f, 16.9f), new Vector2(-13.1f, 14.5f), new Vector2(13.1f, 14.5f) };

        // Wizard cast flash (board 2026-11-08), per hit 1-4 and facing down, up, left, right, units from the feet at figure scale 1: hits 1 and 3 at the
        // staff crystal, hit 2 at the book, hit 4 between the fists, at the release frame. Generated by Tools/build-wizard-attack-board-repaint.py cast-tips
        // (re-run after changing the attack poses); the flash used to sit on the chest, where its star covered the body facing down. Picture only.
        // Facing down since 10/11 (combo v2, Tools/build-wizard-combo-v2-attacks.py cast-tips): hit 1 the crystal of the down-thrust below the feet,
        // hit 2 the crystal of the sweep on the right, hit 3 the crystal slammed into the ground, hit 4 the crystal of the planted staff beside the head (between the hands it covered the face). Up / left / right keep the older poses' points.
        static readonly Vector2[][] WizardCastTip = {
            new[] { new Vector2(6.7f, -12.4f), new Vector2(10.1f, 19.6f), new Vector2(-15.7f, 11.0f), new Vector2(16.3f, 11.4f) },
            new[] { new Vector2(8.3f, 9.3f), new Vector2(-7.8f, 13.7f), new Vector2(-10.6f, 12.4f), new Vector2(9.8f, 12.2f) },
            new[] { new Vector2(-0.1f, -4.5f), new Vector2(8.9f, 22.5f), new Vector2(9.0f, 20.4f), new Vector2(-7.2f, 21.1f) },
            new[] { new Vector2(-5.2f, 16.1f), new Vector2(0.0f, 13.1f), new Vector2(-6.9f, 11.1f), new Vector2(6.9f, 11.1f) },
        };

        /// <summary>The flash of combo step `step` (1-4): Mechanic pistols at the muzzle, the cannon in the middle of the body, Wizard at the staff / book, Cleric at the staff crystal / shield (where her bolt starts). `fallback` for every other class.</summary>
        public static Vector2 CastTipFor(HeroClassDef cls, int step, int facing, Vector2 feet, Vector2 fallback, float figure = 1f)
        {
            if (cls.Id == HeroClass.Wizard && step >= 1 && step <= 4 && facing >= 0 && facing <= 3) return feet + WizardCastTip[step - 1][facing] * figure;
            float tx, ty;
            if (cls.Id == HeroClass.Cleric) return Rpg.RangedAimRules.ClericTip(step, facing, out tx, out ty) ? feet + new Vector2(tx, ty) * figure : fallback;
            if (cls.Id != HeroClass.Mechanic || step < 1 || step > 4) return fallback;
            float dx, dy;
            if (Rpg.RangedAimRules.MechanicMuzzle(step, facing, out dx, out dy)) return feet + new Vector2(dx, dy) * figure;
            return feet + new Vector2(0f, BodyFlashLift);
        }

        /// <summary>Mechanic pistol shot (step 1-3): the painted muzzle flash at the shooting gun (hit 3: at both guns). False = not this case, nothing played.</summary>
        public static bool MechanicPistolFlash(HeroClassDef cls, ComboHit h, int step, int facing, Vector2 feet, float angle, float figure = 1f)
        {
            if (!NetMode.RendersFx) return cls.Id == HeroClass.Mechanic && step >= 1 && step <= 3;
            if (cls.Id != HeroClass.Mechanic || step < 1 || step > 3 || h.Bolt == null || facing < 0 || facing > 3) return false;
            VfxSet set = VfxSprites.For(h.Bolt.Style);
            if (set == null) return false;
            if (step == 3)
            {
                VfxAnim.Cast(set, feet + Hit3MuzzleGun[facing] * figure, angle, false);
                VfxAnim.Cast(set, feet + Hit3MuzzleGunL[facing] * figure, angle, false);
                return true;
            }
            VfxAnim.Cast(set, CastTipFor(cls, step, facing, feet, feet, figure), angle, false);
            return true;
        }

        /// <summary>Shot sounds, the procedural flash when the painted cast art is missing, and the recoil kick.</summary>
        public static void BoltLaunched(HeroClassDef cls, ComboHit h, bool finisher, ClassPalette pal, Vector2 feet, Vector2 tip, Vector2 dir, float angle, bool paintedCastArt)
        {
            if (!NetMode.RendersFx) return;
            ClassSfx.BoltLaunched(cls.Id, finisher);
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
            DaggerFx.Stab(hitAt, angle, 30f, false);
            DaggerFx.Stab(hitAt, angle, 30f, true);
            ShockRing.Spawn(landed, 16f, 0.3f, new Color(0.62f, 0.3f, 1f, 0.95f), UltimateFx.GroundFxOrder + 6, 0.72f, 0.15f);
            UltimateFx.Embers(hitAt, 26, 8f, 40f, new Color(0.7f, 0.45f, 1f, 1f));
            CameraFollow.SkillShake(0.45f);
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
