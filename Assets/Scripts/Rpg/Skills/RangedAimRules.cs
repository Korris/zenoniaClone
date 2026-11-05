using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>A monster as the auto-aim sees it: hit box, centre, wire id (tie-break) and whether it is already dying.</summary>
    public struct AimCandidate
    {
        public float Cx, Cy, XMin, YMin, XMax, YMax;
        public ushort Id;
        public bool Dying;

        public AimCandidate(float cx, float cy, float xMin, float yMin, float xMax, float yMax, ushort id, bool dying = false)
        {
            Cx = cx; Cy = cy; XMin = xMin; YMin = yMin; XMax = xMax; YMax = yMax; Id = id; Dying = dying;
        }
    }

    /// <summary>
    /// Engine-free aim pick of a ranged combo shot (floats only, so dotnet can test it). The bolt would fly straight along the hero's 4-way
    /// facing; when a live monster stands within the bolt's range in the front half-plane of that facing, the shot turns toward it instead.
    /// Deterministic for the same candidate set: closest hit box wins, ties go to the lower wire id, then the lower index. Server and
    /// predicting client each run it on their own monster list; the server's angle travels in the BoltFired event.
    /// </summary>
    public static class RangedAimRules
    {
        const float TieEps = 1e-3f, MinAimDist = 1e-3f;

        /// <summary>
        /// Index of the monster to aim at, -1 for a straight shot. (hx, hy) = hero feet, (fx, fy) = unit facing, (ox, oy) = muzzle the bolt starts
        /// from: range is measured from the muzzle to the hit box (the bolt flies `range` units from there), the half-plane from the hero.
        /// </summary>
        public static int Pick(float hx, float hy, float fx, float fy, float ox, float oy, float range, IList<AimCandidate> all)
        {
            int best = -1;
            float bestDist = 0f;
            for (int i = 0; i < all.Count; i++)
            {
                AimCandidate c = all[i];
                if (c.Dying) continue;
                if ((c.Cx - hx) * fx + (c.Cy - hy) * fy < 0f) continue; // behind the hero
                float dist = SkillGeometry.DistToBox(ox, oy, c.XMin, c.YMin, c.XMax, c.YMax);
                if (dist > range) continue;
                if (best >= 0 && dist > bestDist - TieEps && (dist > bestDist + TieEps || c.Id >= all[best].Id)) continue;
                best = i; bestDist = dist;
            }
            return best;
        }

        /// <summary>Unit direction from the muzzle to the point (tx, ty) and its angle in degrees. False when the point sits on the muzzle (keep the straight shot).</summary>
        public static bool Direction(float ox, float oy, float tx, float ty, out float dx, out float dy, out float angleDeg)
        {
            dx = tx - ox; dy = ty - oy;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < MinAimDist) { dx = dy = angleDeg = 0f; return false; }
            dx /= len; dy /= len;
            angleDeg = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
            return true;
        }
    }
}
