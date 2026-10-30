using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Engine-free hit-shape math (floats only, so dotnet can test it). A target is an axis-aligned box (monster hit box); a shape hits when
    /// the box overlaps it. Directions are unit vectors (dx, dy); angles are FULL opening angles in degrees. Cone and Arc share one sector test.
    /// Convex shapes are tested by clipping the box polygon against the shape's half-planes (exact, no sampling).
    /// </summary>
    public static class SkillGeometry
    {
        const float Eps = 1e-4f;
        [ThreadStatic] static float[] bufA, bufB; // x,y pairs; 4 corners + at most 1 vertex per clipping plane stays below 16 points

        /// <summary>Dispatch by shape. Single behaves like a circle (Radius = pick radius). Cone / Arc: Radius + AngleDeg. Line: Length + Width.</summary>
        public static bool Hits(AreaShape shape, float ox, float oy, float dx, float dy, float radius, float angleDeg, float length, float width,
            float xMin, float yMin, float xMax, float yMax)
        {
            switch (shape)
            {
                case AreaShape.Cone:
                case AreaShape.Arc: return InSector(ox, oy, dx, dy, radius, angleDeg, xMin, yMin, xMax, yMax);
                case AreaShape.Line: return InLine(ox, oy, dx, dy, length, width, xMin, yMin, xMax, yMax);
                default: return InCircle(ox, oy, radius, xMin, yMin, xMax, yMax);
            }
        }

        /// <summary>Distance from a point to the box (0 when inside).</summary>
        public static float DistToBox(float px, float py, float xMin, float yMin, float xMax, float yMax)
        {
            float nx = Clamp(px, xMin, xMax) - px, ny = Clamp(py, yMin, yMax) - py;
            return (float)Math.Sqrt(nx * nx + ny * ny);
        }

        public static bool InCircle(float cx, float cy, float radius, float xMin, float yMin, float xMax, float yMax)
        {
            return DistToBox(cx, cy, xMin, yMin, xMax, yMax) <= radius;
        }

        /// <summary>Sector with apex (ox, oy), facing (dx, dy), radius and full opening angle. 360 = circle.</summary>
        public static bool InSector(float ox, float oy, float dx, float dy, float radius, float angleDeg, float xMin, float yMin, float xMax, float yMax)
        {
            if (angleDeg >= 360f) return InCircle(ox, oy, radius, xMin, yMin, xMax, yMax);
            if (!InCircle(ox, oy, radius, xMin, yMin, xMax, yMax)) return false;
            float half = angleDeg * 0.5f * (float)Math.PI / 180f;
            if (half > (float)Math.PI * 0.5f - Eps)
            {
                // Wider than a half-plane pair can express: union of two sectors whose halves are each <= 90 degrees.
                float h2 = half * 0.5f, c = (float)Math.Cos(h2), s = (float)Math.Sin(h2);
                float ux = dx * c - dy * s, uy = dx * s + dy * c, vx = dx * c + dy * s, vy = -dx * s + dy * c;
                float deg = h2 * 2f * 180f / (float)Math.PI;
                return InSector(ox, oy, ux, uy, radius, deg, xMin, yMin, xMax, yMax) || InSector(ox, oy, vx, vy, radius, deg, xMin, yMin, xMax, yMax);
            }
            float ca = (float)Math.Cos(half), sa = (float)Math.Sin(half);
            float e1x = dx * ca - dy * sa, e1y = dx * sa + dy * ca; // left edge
            float e2x = dx * ca + dy * sa, e2y = -dx * sa + dy * ca; // right edge
            float[] p = Load(ref bufA, xMin - ox, yMin - oy, xMax - ox, yMax - oy);
            int n = 4;
            n = Clip(ref p, n, -e1y, e1x, 0f);  // keep the side clockwise of the left edge
            n = Clip(ref p, n, e2y, -e2x, 0f);  // keep the side counter-clockwise of the right edge
            if (n == 0) return false;
            // The clipped piece lies inside the wedge; it reaches the disc when its nearest point to the apex is within the radius.
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                best = Math.Min(best, SegmentDist(p[i * 2], p[i * 2 + 1], p[j * 2], p[j * 2 + 1]));
            }
            return best <= radius;
        }

        /// <summary>Oriented rectangle: starts at the origin, runs `length` along (dx, dy), `width` wide (centred on the axis).</summary>
        public static bool InLine(float ox, float oy, float dx, float dy, float length, float width, float xMin, float yMin, float xMax, float yMax)
        {
            float hw = width * 0.5f;
            float[] p = Load(ref bufA, xMin - ox, yMin - oy, xMax - ox, yMax - oy);
            int n = 4;
            n = Clip(ref p, n, -dx, -dy, 0f);        // behind the start
            n = Clip(ref p, n, dx, dy, length);      // beyond the end
            n = Clip(ref p, n, -dy, dx, hw);         // left side
            n = Clip(ref p, n, dy, -dx, hw);         // right side
            return n > 0;
        }

        /// <summary>True when the attacker stands behind a victim facing `facing` (0 down, 1 up, 2 left, 3 right): inside the rear 120 degree cone.</summary>
        public static bool IsBehind(int facing, float toAttackerX, float toAttackerY)
        {
            float fx = facing == 2 ? -1f : facing == 3 ? 1f : 0f, fy = facing == 0 ? -1f : facing == 1 ? 1f : 0f;
            float len = (float)Math.Sqrt(toAttackerX * toAttackerX + toAttackerY * toAttackerY);
            if (len < Eps) return false;
            return (fx * toAttackerX + fy * toAttackerY) / len < -0.5f;
        }

        static float Clamp(float v, float lo, float hi) { return v < lo ? lo : v > hi ? hi : v; }

        static float[] Load(ref float[] buf, float x0, float y0, float x1, float y1)
        {
            if (buf == null) buf = new float[32];
            buf[0] = x0; buf[1] = y0; buf[2] = x1; buf[3] = y0; buf[4] = x1; buf[5] = y1; buf[6] = x0; buf[7] = y1;
            return buf;
        }

        /// <summary>Sutherland-Hodgman against the half-plane (n . p at most d). Result is written to the other thread buffer; returns the new vertex count.</summary>
        static int Clip(ref float[] poly, int n, float nx, float ny, float d)
        {
            float[] src = poly, dst = ReferenceEquals(poly, bufA) ? (bufB ?? (bufB = new float[32])) : bufA;
            int m = 0;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                float ax = src[i * 2], ay = src[i * 2 + 1], bx = src[j * 2], by = src[j * 2 + 1];
                float da = nx * ax + ny * ay - d, db = nx * bx + ny * by - d;
                bool ina = da <= Eps, inb = db <= Eps;
                if (ina) { dst[m * 2] = ax; dst[m * 2 + 1] = ay; m++; }
                if (ina != inb)
                {
                    float t = da / (da - db);
                    dst[m * 2] = ax + (bx - ax) * t; dst[m * 2 + 1] = ay + (by - ay) * t; m++;
                }
            }
            poly = dst;
            return m;
        }

        static float SegmentDist(float ax, float ay, float bx, float by)
        {
            float vx = bx - ax, vy = by - ay, l2 = vx * vx + vy * vy;
            float t = l2 < 1e-9f ? 0f : Clamp(-(ax * vx + ay * vy) / l2, 0f, 1f);
            float cx = ax + vx * t, cy = ay + vy * t;
            return (float)Math.Sqrt(cx * cx + cy * cy);
        }
    }
}
