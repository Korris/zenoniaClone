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

        /// <summary>Bolt start height above the feet (ComboProjectile draws it 6 higher, so the picture sits on the torso) and how far ahead of the hero the staff / bow muzzle is.</summary>
        public const float BoltLift = 4f, MuzzleAhead = 12f;

        /// <summary>
        /// Where a ranged combo bolt starts, server and client alike (the server's point travels in BoltFired). (hx, hy) = hero feet, (fx, fy) = unit facing.
        /// `fromBody`: every Mechanic shot leaves the middle of the body (user 2026-11-08: not from the guns; the cannon shot, hit 4, too since the same day);
        /// every other class starts MuzzleAhead in front. The Mechanic's ranges carry MuzzleAhead extra (ComboConfigs), so they still reach as far ahead of the hero.
        /// </summary>
        public static void BoltOrigin(bool fromBody, float hx, float hy, float fx, float fy, out float ox, out float oy)
        {
            float ahead = fromBody ? 0f : MuzzleAhead;
            ox = hx + fx * ahead;
            oy = hy + BoltLift + fy * ahead;
        }

        /// <summary>How much higher ComboProjectile draws a bolt than its sim point (ComboProjectile.VisualLift).</summary>
        public const float BoltDrawLift = 6f;

        // Mechanic muzzles, combo steps 1-4 x facing (down, up, left, right): drawn offset from the feet in world units (8 frame px = 1 unit, figure
        // scale 1), on the firing frame (frame 1): the shooting pistol (T0 art) of attack1-3, hit 3 = midpoint of both guns, the cannon of attack4.
        // From the anchors of held-mechanic-dual.json (2026-11-08 board repaint): re-measure with Tools/measure-mechanic-attack-muzzles.py when they change.
        static readonly float[] MuzzleX = { -2.2f, 5.8f, -15.5f, 15.8f, 2.0f, -5.8f, -15.8f, 15.5f, -0.7f, -0.4f, -12.8f, 12.8f, 2.4f, 5.8f, -18.1f, 18.1f };
        static readonly float[] MuzzleY = { 12.8f, 17.5f, 15.3f, 15.3f, 12.9f, 17.5f, 15.3f, 15.3f, 11.3f, 16.9f, 14.1f, 14.1f, -2.5f, 27.8f, 12.5f, 12.5f };

        /// <summary>Drawn muzzle of the Mechanic's combo step `step` (1-4) facing `facing` (0 down, 1 up, 2 left, 3 right), offset from the feet.</summary>
        public static bool MechanicMuzzle(int step, int facing, out float dx, out float dy)
        {
            dx = dy = 0f;
            if (step < 1 || step > 4 || facing < 0 || facing > 3) return false;
            int i = (step - 1) * 4 + facing;
            dx = MuzzleX[i]; dy = MuzzleY[i];
            return true;
        }

        /// <summary>
        /// Mechanic shot (steps 1-4): the bolt leaves the shooting gun's muzzle (hit 3: between the two pistols, hit 4: the cannon), approved board
        /// 2026-11-08, server and client alike. `rangeCut` = how far that muzzle is ahead of the body point BoltOrigin(fromBody) uses: the bolt flies its
        /// range minus that, so it still ends as far from the hero as before. False for any other step (body point, nothing cut).
        /// </summary>
        public static bool MechanicBoltOrigin(int step, int facing, float hx, float hy, float fx, float fy, out float ox, out float oy, out float rangeCut)
        {
            float dx, dy;
            if (!MechanicMuzzle(step, facing, out dx, out dy)) { ox = hx; oy = hy + BoltLift; rangeCut = 0f; return false; }
            TipOrigin(dx, dy, hx, hy, fx, fy, out ox, out oy, out rangeCut);
            return true;
        }

        // Sim start of a bolt drawn leaving (dx, dy) from the feet; rangeCut = how far that is ahead of the body point (hx, hy + BoltLift).
        static void TipOrigin(float dx, float dy, float hx, float hy, float fx, float fy, out float ox, out float oy, out float rangeCut)
        {
            ox = hx + dx;
            oy = hy + dy - BoltDrawLift;
            rangeCut = (ox - hx) * fx + (oy - hy - BoltLift) * fy;
        }

        // Cleric staff crystal (hits 1, 3, 4) / shield centre (hit 2), combo steps 1-4 x facing (down, up, left, right), drawn offset from the feet in
        // world units (8 frame px = 1 unit, figure scale 1) on the release frame (frame 1) of attack1-4, T0 staff / shield (board 2026-11-08).
        // Generated by Tools/build-cleric-attack-cast-tips.py from held-cleric-melee.json: re-run and paste when the attack poses change.
        static readonly float[] ClericTipX = { 0.7f, 8.1f, -13.5f, 12.8f, 3.8f, -6.2f, -4.2f, 3.5f, -5.0f, 3.8f, -2.6f, 2.7f, -6.1f, 2.3f, -7.3f, 7.0f };
        static readonly float[] ClericTipY = { -0.4f, 18.5f, 11.8f, 11.8f, 7.0f, 9.7f, 7.1f, 7.7f, 23.5f, 24.2f, 24.4f, 23.3f, 18.0f, 17.1f, 18.2f, 18.2f };

        /// <summary>Drawn staff crystal (hit 2: shield centre) of the Cleric's combo step `step` (1-4) facing `facing` (0 down, 1 up, 2 left, 3 right), offset from the feet.</summary>
        public static bool ClericTip(int step, int facing, out float dx, out float dy)
        {
            dx = dy = 0f;
            if (step < 1 || step > 4 || facing < 0 || facing > 3) return false;
            int i = (step - 1) * 4 + facing;
            dx = ClericTipX[i]; dy = ClericTipY[i];
            return true;
        }

        /// <summary>
        /// Cleric shot (steps 1-4), server and client alike: the light arrow / halo lance / holy orb leaves the staff crystal, the Aegis Ray (hit 2) the
        /// shield (fix 2026-11-08: from the old point 12 ahead of the body the lance and orb crossed her face). `rangeCut` = how far that point is ahead of
        /// the old start (BoltOrigin(fromBody: false), MuzzleAhead in front), negative when it is behind it: the bolt flies Range minus that, so it still
        /// ends where it did. False for any other step (old start, nothing cut).
        /// </summary>
        public static bool ClericBoltOrigin(int step, int facing, float hx, float hy, float fx, float fy, out float ox, out float oy, out float rangeCut)
        {
            float dx, dy;
            if (!ClericTip(step, facing, out dx, out dy)) { BoltOrigin(false, hx, hy, fx, fy, out ox, out oy); rangeCut = 0f; return false; }
            TipOrigin(dx, dy, hx, hy, fx, fy, out ox, out oy, out rangeCut);
            rangeCut -= MuzzleAhead;
            return true;
        }

        /// <summary>
        /// Picture only: whether a Cleric bolt fired from ClericTip is drawn behind her while it is over her figure (from her feet to ClericBehindTop above
        /// them, ComboProjectile.KeepBehindShooter). Facing up her staff and shield are behind her body, so is every bolt until it clears her hair. Facing
        /// down a bolt leaving the raised staff (crystal above the waist: Halo Lance, Judgment Bloom) would cross her face and body on its way down.
        /// Sideways the bolt leaves in front of her, away from the body; facing down the thrust (crystal at her feet) and the shield ray stay in front.
        /// </summary>
        public static bool ClericStartsBehind(int step, int facing)
        {
            float dx, dy;
            if (!ClericTip(step, facing, out dx, out dy)) return false;
            return facing == 1 || (facing == 0 && dy > BehindDownMinLift);
        }

        /// <summary>Height over the feet up to which a Cleric bolt counts as over her figure: hair top (~18) plus the holy orb's radius.</summary>
        public const float ClericBehindTop = 26f;
        const float BehindDownMinLift = 12f;

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
