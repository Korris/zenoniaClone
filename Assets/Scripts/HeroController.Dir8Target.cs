using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// View-only: a hero that swings or casts while standing turns its 8-way view toward the monster it fights, so a crowd around a boss reads as a ring
    /// of heroes all facing in (target picture docs/reference/ro-soft-fantasy-5-class-redesign/40-players-new-classes-vs-mire-hydra-queen.png).
    /// Works the same for the own hero, AI heroes and proxies (a proxy's pose comes from the snapshot): the monster is the nearest live one whose 8-way
    /// direction agrees with the hero's 4-dir facing (the sim faced it to attack), within TargetReach of its hit box. The sim, hit boxes and the network
    /// keep the 4-dir facing; nothing here is sent anywhere.
    /// </summary>
    public partial class HeroController
    {
        const float Dir8TargetReach = 150f; // world units from the hero to the monster's hit box: a ranged bolt (60-82) or a skill reach, with room to spare

        /// <summary>True while this hero swings or casts (proxy: the replicated pose).</summary>
        bool FightingPose()
        {
            if (proxy) return proxyPose == HeroPose.Attack || proxyPose == HeroPose.Cast;
            return combo.IsAttacking || castLeft > 0f;
        }

        /// <summary>Dir8 row toward the monster this hero fights, or -1 (no live monster in reach along the facing).</summary>
        int CombatDir8()
        {
            var at = new Vector2(x, y + 6f); // about chest height of the hero, so a monster level with the feet still reads as straight ahead
            if (lockTarget != null && !lockTarget.IsDying && lockTarget.Hp > 0f) // lock-target slash: look straight at the locked monster
            {
                int ld = Dir8Util.FromVector(lockTarget.Feet - new Vector2(x, y)); // ground direction, as the lock facing (HeroController.LockTarget.cs)
                if (ld >= 0 && Dir8Util.Matches(ld, facing)) return ld;
            }
            float best = Dir8TargetReach * Dir8TargetReach;
            int row = -1;
            var all = Monster.All;
            for (int i = 0; i < all.Count; i++)
            {
                Monster m = all[i];
                if (m == null || m.IsDying || m.Hp <= 0f) continue;
                Rect box = m.Box;
                float cx = Mathf.Clamp(at.x, box.xMin, box.xMax), cy = Mathf.Clamp(at.y, box.yMin, box.yMax);
                float d2 = (cx - at.x) * (cx - at.x) + (cy - at.y) * (cy - at.y);
                if (d2 >= best) continue;
                int d = Dir8Util.FromVector(box.center - at); // aim at the body's middle, not the nearest corner: a ring of heroes points at one spot
                if (d < 0 || !Dir8Util.Matches(d, facing)) continue;
                best = d2;
                row = d;
            }
            return row;
        }
    }
}
