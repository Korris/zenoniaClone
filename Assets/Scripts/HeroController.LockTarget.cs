using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Lock-target basic attack (Warrior since 2026-11-20, HeroClassDef.LockTargetMelee): every slash locks ONE monster (the one it locked before
    /// while that one lives and stays in reach, else the nearest live monster in reach), turns the hero's 4-way facing to it as the windup starts,
    /// and the blow damages only that monster. The picture (WarriorSlashLightFx) is aimed at it. Runs the same on the offline sim, the zone server
    /// (authority: damage) and the predicting client (facing + picture only; the server decides the hit). A proxy aims its picture at the monster
    /// it most likely swung at (nearest in reach along its replicated facing).
    /// </summary>
    public partial class HeroController
    {
        Monster lockTarget;      // the monster the current / last slash is locked on (null = none in reach: the slash whiffs)
        bool lockAcquired;       // this swing's windup already picked its target
        bool slashConnected;     // this swing's blow found a monster: the picture holds the impact frame (WarriorSlashFeel.HoldMs)

        static readonly List<float> LockDist = new List<float>();
        static readonly List<bool> LockAlive = new List<bool>();

        float lockAt = -99f;     // Time.time the lock last landed on a monster (TargetMarker shows it this long after)

        /// <summary>The monster this hero's basic attack is locked on, or null (read by captures / HUD).</summary>
        public Monster LockTarget { get { return lockTarget; } }

        /// <summary>The locked monster while it lives and was struck or picked within `seconds` (TargetMarker), else null.</summary>
        public Monster RecentLockTarget(float seconds)
        {
            Monster m = lockTarget;
            return m != null && !m.IsDying && m.Hp > 0f && Time.time - lockAt <= seconds ? m : null;
        }

        /// <summary>True for a lock-target class (Warrior): AttackChase walks it to an out-of-reach monster.</summary>
        public bool LocksTarget { get { return cls != null && cls.LockTargetMelee; } }

        /// <summary>The monster a slash started now would lock (sim position, reach shrunk by `margin`), or null = nothing in reach.</summary>
        public Monster LockCandidateInReach(float margin)
        {
            if (!LocksTarget || cls.Hits.Length == 0) return null;
            return PickLockTarget(LockFrom, lockTarget, LockRange(cls.Hits[0]) - margin, -1);
        }

        /// <summary>True when `m` is within the slash's reach (shrunk by `margin`) of the hero's sim position.</summary>
        public bool InLockReach(Monster m, float margin)
        {
            if (!LocksTarget || m == null || cls.Hits.Length == 0) return false;
            Rect b = m.MeleeBody;
            float r = LockRange(cls.Hits[0]) - margin;
            Vector2 f = LockFrom;
            return LockTargetRules.BoxDistSq(f.x, f.y, b.xMin, b.yMin, b.xMax, b.yMax) <= r * r;
        }

        /// <summary>Point of `m`'s melee body nearest `from` (where the sword meets it).</summary>
        public static Vector2 ContactPoint(Monster m, Vector2 from)
        {
            Rect b = m.MeleeBody;
            return new Vector2(Mathf.Clamp(from.x, b.xMin, b.xMax), Mathf.Clamp(from.y, b.yMin, b.yMax));
        }

        /// <summary>Hero chest (sim position), where reach is measured from.</summary>
        public Vector2 ChestPoint { get { return LockFrom; } }

        /// <summary>AttackChase reached `m`: lock it, so the slash it presses next keeps it even when another monster stands nearer.</summary>
        public void SetLockTarget(Monster m) { lockTarget = m; lockAt = Time.time; }

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
            slashConnected = false;
            ComboHit h = combo.CurrentHit;
            if (h == null) return;
            lockTarget = PickLockTarget(LockFrom, lockTarget, LockRange(h), -1);
            if (lockTarget != null) { FaceLockTarget(lockTarget); lockAt = Time.time; }
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
                Rect b = alive ? m.MeleeBody : default(Rect); // the painted body edge: the sword has to touch it
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
            slashConnected = lockTarget != null;
            if (predicted) { if (!replaying) { PlayLockSlashFx(step, lockTarget); if (slashConnected) LockImpactShake(false); } return; } // online: picture now, the server decides the hit
            PlayLockSlashFx(step, lockTarget);
            NetEventBus.AttackStep(this, step, false);
            if (lockTarget == null) return; // whiff: nothing in reach
            bool crit = Random.value < CritChance + CritBonus;
            float mult = combo.MultiplierFor(step, 1);
            int dmg = CombatMath.Damage(AtkStat, mult * (crit ? 1.6f : 1f), lockTarget.Def);
            Vector2 v = DirVec[facing];
            lockTarget.TakeHit(dmg, v * h.Knockback, crit, false, false, this); // finisher false: no finisher flash / shake on every slash
            if (NetMode.RendersFx) ClassSfx.MeleeHit(cls.Id, crit);
            LockImpactShake(crit);
            // 22/11 V2: no global GameClock freeze (it lengthened the 400 ms swing offline); the hit-stop is the picture holding the impact frame
            // WarriorSlashFeel.HoldMs inside the cycle (HeroController.Animation.cs), the monster flashes WarriorSlashFeel.FlashMs (Monster.Visuals.cs).
        }

        /// <summary>Camera kick on this player's own connecting slash (1.8-2.2 px, 75-85 ms; was trauma 0.05, invisible).</summary>
        void LockImpactShake(bool crit)
        {
            if (!NetMode.RendersFx || (object)this != (object)LocalPlayer.Hero) return;
            CameraFollow.Kick(crit ? WarriorSlashFeel.CritShakePx : WarriorSlashFeel.ShakePx, crit ? WarriorSlashFeel.CritShakeMs : WarriorSlashFeel.ShakeMs);
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
                // Aim at the contact point (the body edge nearest the chest) and land the crescent there, not at the monster's centre (big bosses).
                Vector2 c = LockFrom, hit = ContactPoint(target, c);
                Vector2 d = hit - c;
                if (d.sqrMagnitude > 1f) angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                else { Vector2 g = target.Feet - feet; if (g.sqrMagnitude > 1f) angle = Mathf.Atan2(g.y, g.x) * Mathf.Rad2Deg; }
                dist = Mathf.Max(1f, d.magnitude);
            }
            HeroFx.LockSlash(cls, feet, angle, dist, view != null ? view.FigureScale : 1f, this);
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
