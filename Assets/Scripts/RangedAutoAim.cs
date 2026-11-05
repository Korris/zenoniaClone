using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Unity side of the ranged auto-aim: gathers the zone's monsters as AimCandidates and lets RangedAimRules pick the one a combo bolt turns
    /// toward. Runs wherever FireBolt runs (server, offline, predicting client). The client only sees its snapshot proxies, so its predicted
    /// bolt may aim a little differently from the server's; the BoltEnd event still puts the impact where the server saw it.
    /// </summary>
    public static class RangedAutoAim
    {
        static readonly List<AimCandidate> candidates = new List<AimCandidate>(32);

        /// <summary>
        /// Turns `dir` / `angleDeg` (the 4-way facing shot) toward the closest live monster within `range` of `muzzle` in the front half-plane of
        /// `facingDir`. Leaves them untouched when nothing qualifies. Returns true when the shot was aimed.
        /// </summary>
        public static bool Steer(Vector2 feet, Vector2 muzzle, Vector2 facingDir, float range, ref Vector2 dir, ref float angleDeg)
        {
            IReadOnlyList<Monster> monsters = Monster.All;
            if (monsters.Count == 0) return false;
            candidates.Clear();
            for (int i = 0; i < monsters.Count; i++)
            {
                Monster m = monsters[i];
                Rect box = m.Box;
                Vector2 c = m.Center;
                candidates.Add(new AimCandidate(c.x, c.y, box.xMin, box.yMin, box.xMax, box.yMax, m.NetId, m.IsDying));
            }
            int pick = RangedAimRules.Pick(feet.x, feet.y, facingDir.x, facingDir.y, muzzle.x, muzzle.y, range, candidates);
            if (pick < 0) return false;
            float dx, dy, a;
            if (!RangedAimRules.Direction(muzzle.x, muzzle.y, candidates[pick].Cx, candidates[pick].Cy, out dx, out dy, out a)) return false;
            dir = new Vector2(dx, dy);
            angleDeg = a;
            return true;
        }
    }
}
