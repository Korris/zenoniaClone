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
        /// <summary>World units added to the swing's Reach (ComboConfigs.WarriorHits, 12) when picking / keeping a target and when the blow lands, measured
        /// from the hero's chest to the nearest edge of the monster's melee body (MeleeBody). 12 + 4 = 16 = where the sword's tip is in the swing frame
        /// (~14 units ahead at figure scale 1, ~16 at 1.15): contact, as in RO / MU (user 2026-11-20 "làm giống mấy game RPG khác đó"). Was 34 + 8 = 42.</summary>
        public const float ReachSlack = 4f;

        /// <summary>Melee body width per painted height: a monster drawn far larger than its hit box (event bosses) is still touched at its painted edge.</summary>
        public const float BodyWidthPerHeight = 0.35f;

        /// <summary>Half width of a monster's melee body: its hit box, or 0.35 x its drawn height when that is wider. From MonsterDef numbers only, so the
        /// zone server (no sprites) and the client agree.</summary>
        public static float BodyHalfWidth(float hitW, float drawnHeight)
        {
            float a = hitW * 0.5f, b = drawnHeight * BodyWidthPerHeight;
            return a > b ? a : b;
        }

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

        // ---- walk to the target (AttackChase, user OK 2026-11-20: an attack press with the monster out of reach walks to it, RO style, then slashes) ----
        /// <summary>An attack press with nothing in reach starts a walk to a monster at most this far (feet to feet, 8 tiles).</summary>
        public const float ChaseStartRange = 128f;
        /// <summary>The walk gives up when the target gets this far away (12 tiles: it fled or was knocked away).</summary>
        public const float ChaseLeash = 192f;
        /// <summary>... after this long without reaching it, or this long without moving (walled off).</summary>
        public const float ChaseMaxSeconds = 6f, ChaseStuckSeconds = 1f;
        /// <summary>Reach margin the walk stops inside, so a target that drifts a little (or the server's slightly newer monster position online) is still in reach when the blow lands.</summary>
        public const float ChaseReachMargin = 3f;

        /// <summary>8-way move (each of sx, sy is -1, 0 or 1) straight toward (dx, dy); an axis within `dead` units is not pressed (no zig-zag).</summary>
        public static void ChaseDir(float dx, float dy, float dead, out int sx, out int sy)
        {
            sx = dx > dead ? 1 : dx < -dead ? -1 : 0;
            sy = dy > dead ? 1 : dy < -dead ? -1 : 0;
            if (sx == 0 && sy == 0 && (dx * dx + dy * dy) > 0.01f) { if ((dx < 0f ? -dx : dx) >= (dy < 0f ? -dy : dy)) sx = dx < 0f ? -1 : 1; else sy = dy < 0f ? -1 : 1; }
        }

        /// <summary>True while the walk should go on: the target within the leash, not walking too long, not stuck.</summary>
        public static bool KeepChasing(float distToTarget, float chaseSeconds, float stillSeconds)
        {
            return distToTarget <= ChaseLeash && chaseSeconds <= ChaseMaxSeconds && stillSeconds <= ChaseStuckSeconds;
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
