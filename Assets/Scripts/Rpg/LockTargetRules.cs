using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Warrior basic attack since 2026-11-20 (user: "ko làm combo attack nữa giờ là attack cứng lock target nhân vật chém vào 1 con quái duy nhất"):
    /// one heavy slash that locks ONE monster, turns the hero to it and hits only that monster (no cleave box). Engine-free so the pick is unit-tested;
    /// HeroController.LockTarget.cs feeds it the live monsters. Shared by the offline sim, the zone server and the predicting client.
    /// </summary>
    public static class LockTargetRules
    {
        /// <summary>World units added to the swing's Reach when picking / keeping a target and when the blow lands: measured from the hero's chest
        /// to the nearest edge of the monster's hit box (the old cleave box reached Reach + 4 ahead of the hero, a little wider than its body).</summary>
        public const float ReachSlack = 8f;

        /// <summary>Squared distance from a point to an axis-aligned box (0 inside).</summary>
        public static float BoxDistSq(float px, float py, float xMin, float yMin, float xMax, float yMax)
        {
            float cx = px < xMin ? xMin : px > xMax ? xMax : px;
            float cy = py < yMin ? yMin : py > yMax ? yMax : py;
            return (cx - px) * (cx - px) + (cy - py) * (cy - py);
        }

        /// <summary>
        /// Index of the monster to strike, or -1. The locked one (index `locked`) is kept while it lives and stays within `range`, even when another
        /// monster is nearer: that is the lock. Otherwise the nearest live monster within `range`. distSq / alive are per monster.
        /// </summary>
        public static int Pick(int locked, IReadOnlyList<float> distSq, IReadOnlyList<bool> alive, float range)
        {
            float r2 = range * range;
            if (locked >= 0 && locked < distSq.Count && alive[locked] && distSq[locked] <= r2) return locked;
            int best = -1;
            float bestD = r2;
            for (int i = 0; i < distSq.Count; i++)
            {
                if (!alive[i] || distSq[i] > bestD) continue;
                if (best >= 0 && distSq[i] == bestD) continue; // tie: the first one wins, so the server and the client agree
                best = i;
                bestD = distSq[i];
            }
            return best;
        }

        /// <summary>4-way facing toward (dx, dy): 0 down, 1 up, 2 left, 3 right. A vector too short to mean anything keeps `current`.</summary>
        public static int Facing4(float dx, float dy, int current)
        {
            if (dx * dx + dy * dy < 1f) return current;
            float ax = dx < 0f ? -dx : dx, ay = dy < 0f ? -dy : dy;
            if (ax > ay) return dx < 0f ? 2 : 3;
            if (ay > ax) return dy < 0f ? 0 : 1;
            return current == 2 || current == 3 ? (dx < 0f ? 2 : 3) : (dy < 0f ? 0 : 1); // exact diagonal: stay on the current axis
        }
    }
}
