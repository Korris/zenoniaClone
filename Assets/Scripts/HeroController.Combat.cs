using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Hero combat: melee swing resolution, ranged bolt launch and incoming damage.</summary>
    public partial class HeroController
    {
        void DoHit(int step)
        {
            ComboHit h = combo.CurrentHit;
            if (h == null) return;
            if (h.Bolt != null) { FireBolt(h, step); return; }
            MeleeStrike(h, step, false);
            if (h.Stabs > 1) { stabLeft = StabGapMs; stabHit = h; stabStep = step; } // double stab: the next stab lands 90 ms later
        }

        /// <summary>One melee hit box + its FX. A multi-stab step splits its multiplier between the stabs; echo = a later stab (mirrored FX).</summary>
        void MeleeStrike(ComboHit h, int step, bool echo)
        {
            Vector2 v = DirVec[facing];
            float offset = h.Reach * 0.5f + 4f;
            float cx = x + v.x * offset, cy = y + 6f + v.y * offset;
            float w = v.x != 0f ? h.Reach : h.Width, ht = v.y != 0f ? h.Reach : h.Width;
            var box = new Rect(cx - w * 0.5f, cy - ht * 0.5f, w, ht);
            bool finisher = step == combo.HitCount;
            bool flip = (step == 2) != echo;
            if (cls.Id == HeroClass.Assassin) DaggerFx.Strike(new Vector2(cx, cy), DirAngle[facing], h.Reach, finisher, echo, step);
            else
            {
                SlashEffect.Spawn(cx, cy, DirAngle[facing], h.FxScale, finisher, flip, FeelConfig.FxOrder);
                SwordTrail.Spawn(x, y + 6f, DirAngle[facing], h.Reach, finisher, flip, cls.Palette);
            }

            float mult = combo.MultiplierFor(step, 1) / h.Stabs;
            int landed = 0;
            bool anyCrit = false;
            IReadOnlyList<Monster> monsters = Monster.All;
            for (int i = 0; i < monsters.Count; i++)
            {
                Monster m = monsters[i];
                if (m.IsDying || !box.Overlaps(m.Box)) continue;
                bool crit = Random.value < CritChance + CritBonus;
                int dmg = CombatMath.Damage(AtkStat, mult * (crit ? 1.6f : 1f), m.Def);
                m.TakeHit(dmg, v * h.Knockback * (crit ? 2f : 1f), crit, finisher);
                landed++;
                anyCrit |= crit;
            }
            if (landed > 0)
            {
                GameClock.HitStop(FeelConfig.HitStopMs(finisher));
                CameraFollow.AddTrauma(finisher ? FeelConfig.TraumaFinisher : anyCrit ? FeelConfig.TraumaCrit : FeelConfig.TraumaNormal);
            }
        }

        int castFxStep; // combo step whose painted cast FX already played (0 = none)

        /// <summary>Staff-tip / barrel position of the ranged cast and its aim angle.</summary>
        Vector2 CastTip { get { Vector2 v = DirVec[facing]; return new Vector2(x + v.x * 10f, y + 12f + v.y * 6f); } }

        /// <summary>Plays the painted cast animation (swirl at the staff, muzzle blast at the gun) once per ranged step, as the windup starts.</summary>
        void UpdateRangedCastFx()
        {
            if (!combo.IsAttacking) { castFxStep = 0; return; }
            ComboHit h = combo.CurrentHit;
            if (h == null || h.Bolt == null || combo.Phase != ComboPhase.Windup || castFxStep == combo.Step) return;
            PlayCastFx(h);
        }

        void PlayCastFx(ComboHit h)
        {
            castFxStep = combo.Step;
            VfxSet set = VfxSprites.For(h.Bolt.Style);
            if (set == null) return;
            float angle = h.Bolt.Style == BoltStyle.Plasma ? DirAngle[facing] : 0f; // the muzzle blast points along the aim, swirls are round
            VfxAnim.Cast(set, CastTip, angle, combo.IsFinisher);
        }

        /// <summary>Web fireComboBolt: the shot leaves 12 units in front of the hero, with a flash at the staff tip / gun barrel.</summary>
        void FireBolt(ComboHit h, int step)
        {
            Vector2 v = DirVec[facing];
            bool finisher = combo.IsFinisher;
            float mult = combo.MultiplierFor(step, 1);
            ClassPalette pal = cls.ActivePalette;
            var tip = CastTip;
            ComboProjectile.Fire(new Vector2(x + v.x * 12f, y + 4f + v.y * 12f), v, DirAngle[facing], h.Bolt, AtkStat, mult, h.Knockback, finisher, cls.FxFolder, pal, CritBonus);
            if (VfxSprites.For(h.Bolt.Style) != null)
            {
                if (castFxStep != combo.Step) PlayCastFx(h); // windup shorter than one frame: still show the cast
            }
            else FallbackCastFx(h, tip, finisher, pal); // painted art missing: procedural flash
            CameraFollow.AddTrauma(finisher ? 0.2f : 0.06f); // recoil kick
        }

        /// <summary>Procedural cast flash, used only when the painted VFX sprites are missing.</summary>
        void FallbackCastFx(ComboHit h, Vector2 tip, bool finisher, ClassPalette pal)
        {
            Vector2 v = DirVec[facing];
            bool big = h.Bolt.ExplodeRadius > 0f;
            switch (h.Bolt.Style)
            {
                case BoltStyle.Arcane: RuneFx.CastFlash(tip, big, pal); break;
                case BoltStyle.HolyArrow:
                    RuneFx.CastFlash(tip, big, pal);
                    UltimateFx.Embers(tip, big ? 12 : 7, 5f, 26f, new Color(0.7f, 1f, 0.45f, 1f));
                    break;
                case BoltStyle.Plasma: MechanicFx.Muzzle(tip, DirAngle[facing], big, pal); break;
                default:
                    if (h.Bolt.FlashFx != null)
                        BurstFx.Spawn(cls.FxFolder, h.Bolt.FlashFx, new Vector2(x + v.x * 10f, y + 10f + v.y * 6f), DirAngle[facing], big ? 1.6f : 1f, 0.14f, false, pal.Tint);
                    break;
            }
        }

        /// <summary>Called by monsters when a strike connects.</summary>
        public void TakeDamage(int dmg)
        {
            if (dead || DebugGod || invuln > 0f || dashLeft > 0f) return;
            hp -= dmg;
            invuln = InvulnMs;
            hurt = HurtMs;
            stabLeft = 0f;
            castLeft = 0f; // a landed hit breaks the cast root (the already launched skill keeps playing, like the web)
            DamageNumber.Spawn(x, y + 26f, dmg.ToString(), new Color(1f, 0.35f, 0.35f), false);
            CameraFollow.AddTrauma(FeelConfig.TraumaHurt);
            if (hp <= 0f)
            {
                hp = 0f;
                dead = true;
                deathTimer = 0f;
                dashLeft = 0f;
                combo.Reset();
            }
        }
    }
}
