using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Lock-target basic attack (Warrior since 2026-11-20, HeroClassDef.LockTargetMelee): every slash locks ONE monster (the one it locked before
    /// while that one lives and stays in reach, else the nearest live monster in reach), turns the hero's 4-way facing to it as the windup starts,
    /// and the blow damages only that monster. The picture (WarriorLockSlashFx) is aimed at it. Runs the same on the offline sim, the zone server
    /// (authority: damage) and the predicting client (facing + picture only; the server decides the hit). A proxy aims its picture at the monster
    /// it most likely swung at (nearest in reach along its replicated facing).
    /// </summary>
    public partial class HeroController
    {
        Monster lockTarget;      // the monster the current / last slash is locked on (null = none in reach: the slash whiffs)
        bool lockAcquired;       // this swing's windup already picked its target

        static readonly List<float> LockDist = new List<float>();
        static readonly List<bool> LockAlive = new List<bool>();

        /// <summary>The monster this hero's basic attack is locked on, or null (read by captures / HUD).</summary>
        public Monster LockTarget { get { return lockTarget; } }

        /// <summary>Hero chest, the point lock distances are measured from (same height as the old cleave box centre).</summary>
        Vector2 LockFrom { get { return new Vector2(x, y + 6f); } }

        float LockRange(ComboHit h) { return h.Reach + LockTargetRules.ReachSlack; }

        /// <summary>Called every sim step after the combo advanced: a new windup picks its target and turns the hero to it.</summary>
        void UpdateLockTarget()
        {
            if (!cls.LockTargetMelee) return;
            if (!combo.IsAttacking || combo.Phase != ComboPhase.Windup) { lockAcquired = false; return; }
            if (lockAcquired) return;
            lockAcquired = true;
            ComboHit h = combo.CurrentHit;
            if (h == null) return;
            lockTarget = PickLockTarget(LockFrom, lockTarget, LockRange(h), -1);
            if (lockTarget != null) FaceLockTarget(lockTarget);
        }

        void FaceLockTarget(Monster m)
        {
            Vector2 d = m.Feet - new Vector2(x, y); // ground direction (feet to feet): chest to body centre read a monster level with the hero as up-left / up-right
            facing = LockTargetRules.Facing4(d.x, d.y, facing);
        }

        /// <summary>The live monster to strike from `from` within `range` (keeps `locked` while valid). onlyFacing &gt;= 0 = only monsters whose
        /// direction agrees with that 4-way facing (a proxy's picture, where the server already turned the hero).</summary>
        static Monster PickLockTarget(Vector2 from, Monster locked, float range, int onlyFacing)
        {
            IReadOnlyList<Monster> all = Monster.All;
            LockDist.Clear(); LockAlive.Clear();
            int lockedIdx = -1;
            for (int i = 0; i < all.Count; i++)
            {
                Monster m = all[i];
                bool alive = m != null && !m.IsDying && m.Hp > 0f;
                Rect b = alive ? m.Box : default(Rect);
                if (alive && onlyFacing >= 0) { Vector2 d = m.Feet - (from - new Vector2(0f, 6f)); alive = LockTargetRules.Facing4(d.x, d.y, onlyFacing) == onlyFacing; }
                LockAlive.Add(alive);
                LockDist.Add(alive ? LockTargetRules.BoxDistSq(from.x, from.y, b.xMin, b.yMin, b.xMax, b.yMax) : float.MaxValue);
                if (alive && (object)m == (object)locked) lockedIdx = i;
            }
            int pick = LockTargetRules.Pick(lockedIdx, LockDist, LockAlive, range);
            return pick >= 0 ? all[pick] : null;
        }

        /// <summary>The blow of a lock slash (combo Hit event): damage to the locked monster only, if it is still alive and in reach.</summary>
        void LockStrike(ComboHit h, int step)
        {
            // The target can die or step out between the windup and the blow: pick again (keeps the lock when still valid).
            lockTarget = PickLockTarget(LockFrom, lockTarget, LockRange(h), -1);
            if (predicted) { if (!replaying) PlayLockSlashFx(step, lockTarget); return; } // online: picture now, the server decides the hit
            PlayLockSlashFx(step, lockTarget);
            NetEventBus.AttackStep(this, step, false);
            if (lockTarget == null) return; // whiff: nothing in reach
            bool crit = Random.value < CritChance + CritBonus;
            float mult = combo.MultiplierFor(step, 1);
            int dmg = CombatMath.Damage(AtkStat, mult * (crit ? 1.6f : 1f), lockTarget.Def);
            Vector2 v = DirVec[facing];
            lockTarget.TakeHit(dmg, v * h.Knockback, crit, false, false, this); // finisher false: no finisher flash / shake on every slash
            HeroFx.MeleeLanded(cls, crit, false);
            GameClock.HitStop(FeelConfig.HitStopMs(crit)); // the heavy hit-stop only on a crit, so a steady 2.5 slashes / s does not stutter
        }

        /// <summary>Slash picture of this hero's lock slash toward `target` (null = straight along the facing).</summary>
        void PlayLockSlashFx(int step, Monster target)
        {
            if (!NetMode.RendersFx) return;
            Vector2 feet = new Vector2(x, y);
            float angle = DirAngle[facing];
            float dist = 0f;
            if (target != null)
            {
                Vector2 d = target.Feet - feet; // ground direction, like the facing
                if (d.sqrMagnitude > 1f) angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                dist = d.magnitude;
            }
            HeroFx.LockSlash(cls, feet, angle, dist, view != null ? view.FigureScale : 1f);
        }

        /// <summary>Proxy / replay picture of a lock slash: aim at the monster in reach along the replicated facing.</summary>
        void PlayLockSlashFxFromFacing(int step, int facingDir)
        {
            ComboHit h = cls.Hits[step - 1];
            Monster m = PickLockTarget(new Vector2(x, y + 6f), lockTarget, LockRange(h), facingDir & 3);
            lockTarget = m;
            int keep = facing;
            facing = facingDir & 3;
            PlayLockSlashFx(step, m);
            facing = keep;
        }
    }
}
