using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Soft unit collision, hero side (Rpg/BodyCollisionRules.cs, RO3 style): heroes pass through each other; a hero cannot walk into a
    /// monster's foot core (it slides around it: hard for bosses, a smaller soft core for normal monsters) and drifts out of an overlap over
    /// ~0.2 s instead of being teleported. Runs in Simulate, so the offline sim, the zone server and the predicting client all apply it.
    /// </summary>
    public partial class HeroController
    {
        /// <summary>A lock-target slash (Warrior) whose monster is already in sword reach does not lunge: four units a swing used to creep the hero
        /// into the boss until it stood behind its body (seen in game 2026-11-20, Frostfang King).</summary>
        bool LockedInReach()
        {
            Monster m = LocksTarget ? LockTarget : null;
            return m != null && !m.IsDying && m.Hp > 0f && InLockReach(m, 0f);
        }

        /// <summary>This step's move `move` (world units) clipped against monster feet, plus the separation drift out of any overlap.</summary>
        Vector2 BodyCollide(Vector2 move, float dt)
        {
            IReadOnlyList<Monster> all = Monster.All;
            float mx = move.x, my = move.y, sx = 0f, sy = 0f;
            for (int i = 0; i < all.Count; i++)
            {
                Monster m = all[i];
                if (m == null || m.IsDying || m.Hp <= 0f) continue;
                Vector2 f = m.Feet;
                float dx = x - f.x, dy = y - f.y, c = m.HeroContact;
                float reach = c + Mathf.Abs(mx) + Mathf.Abs(my) + 1f;
                if (dx > reach || dx < -reach || dy > reach || dy < -reach) continue; // cheap reject (ground y only shrinks the core)
                BodyCollisionRules.ClipMove(dx, dy, c, ref mx, ref my);
                float ax, ay;
                if (BodyCollisionRules.Separate(dx, dy, c, dt, out ax, out ay)) { sx += ax; sy += ay; }
            }
            return new Vector2(mx + sx, my + sy);
        }
    }
}
