using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Hero combat rules: melee swing resolution, ranged bolt launch and incoming damage. Visuals / sound live in HeroFx.</summary>
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
            if (predicted) { if (!replaying) PlaySwingFx(step, echo, facing); return; } // online: the swing shows now, the server decides what it hits
            Vector2 v = DirVec[facing];
            float offset = h.Reach * 0.5f + 4f;
            float cx = x + v.x * offset, cy = y + 6f + v.y * offset;
            float w = v.x != 0f ? h.Reach : h.Width, ht = v.y != 0f ? h.Reach : h.Width;
            var box = new Rect(cx - w * 0.5f, cy - ht * 0.5f, w, ht);
            bool finisher = step == combo.HitCount;
            HeroFx.Swing(cls, new Vector2(cx, cy), new Vector2(x, y), DirAngle[facing], h, step, finisher, echo);
            NetEventBus.AttackStep(this, step, echo);

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
                m.TakeHit(dmg, v * h.Knockback * (crit ? 2f : 1f), crit, finisher, false, this);
                landed++;
                anyCrit |= crit;
            }
            if (landed > 0)
            {
                HeroFx.MeleeLanded(cls, anyCrit, finisher);
                GameClock.HitStop(FeelConfig.HitStopMs(finisher));
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
            if (replaying) return;
            HeroFx.CastWindup(cls, h, CastTip, DirAngle[facing], combo.IsFinisher);
        }

        /// <summary>Web fireComboBolt: the shot leaves 12 units in front of the hero, with a flash at the staff tip / gun barrel.</summary>
        void FireBolt(ComboHit h, int step)
        {
            if (replaying) return; // a re-simulated step never fires a second picture
            Vector2 v = DirVec[facing];
            bool finisher = combo.IsFinisher;
            float mult = combo.MultiplierFor(step, 1);
            ClassPalette pal = cls.ActivePalette;
            Vector2 muzzle = new Vector2(x + v.x * 12f, y + 4f + v.y * 12f);
            ushort boltId = NetEventBus.BoltFired(this, step, muzzle, DirAngle[facing], finisher);
            ComboProjectile shot = ComboProjectile.Fire(muzzle, v, DirAngle[facing], h.Bolt, AtkStat, mult, h.Knockback, finisher, cls.FxFolder, pal, CritBonus, this, boltId);
            if (predicted) RememberPredictedBolt(shot);
            if (!NetMode.RendersFx) return;
            bool painted = VfxSprites.For(h.Bolt.Style) != null;
            if (painted && castFxStep != combo.Step) PlayCastFx(h); // windup shorter than one frame: still show the cast
            HeroFx.BoltLaunched(cls, h, finisher, pal, new Vector2(x, y), CastTip, v, DirAngle[facing], painted);
        }

        /// <summary>Called by monsters when a strike connects.</summary>
        public void TakeDamage(int dmg)
        {
            if (dead || God || invuln > 0f || dashLeft > 0f) return;
            dmg = HeroBuffs.Absorb(this, dmg); // Mana Shield / Guardian Spirit soak first
            if (dmg <= 0) { invuln = InvulnMs; return; } // fully absorbed: no HP loss, no hurt reaction
            hp -= dmg;
            invuln = InvulnMs;
            hurt = HurtMs;
            stabLeft = 0f;
            castLeft = 0f; // a landed hit breaks the cast root (the already launched skill keeps playing, like the web)
            HeroFx.Hurt(new Vector2(x, y), dmg);
            NetEventBus.HeroHit(this, dmg);
            if (hp <= 0f)
            {
                hp = 0f;
                dead = true;
                deathTimer = 0f;
                dashLeft = 0f;
                combo.Reset();
                NetEventBus.HeroDied(this);
            }
        }
    }
}
