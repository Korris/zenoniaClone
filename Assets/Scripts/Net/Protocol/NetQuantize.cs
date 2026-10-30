using System;

namespace ZenoniaProto.Net
{
    /// <summary>Lossy packing used by snapshots: world units to 1/8 unit shorts, angles to 256 steps, ratios to bytes. Every helper clamps, never throws.</summary>
    public static class NetQuantize
    {
        public const float PosScale = 8f;
        /// <summary>Largest world coordinate a short position can carry (+-4095.875 units; Frost Hollow is 576 x 384).</summary>
        public const float MaxPos = short.MaxValue / PosScale;

        public static short Pos(float units)
        {
            if (float.IsNaN(units)) return 0;
            float v = (float)Math.Round(units * PosScale);
            return (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, v));
        }

        public static float Pos(short packed) { return packed / PosScale; }

        public static byte Angle(float degrees)
        {
            if (float.IsNaN(degrees) || float.IsInfinity(degrees)) return 0;
            float a = degrees % 360f;
            if (a < 0f) a += 360f;
            return (byte)((int)Math.Round(a / 360f * 256f) & 255);
        }

        public static float Angle(byte packed) { return packed * 360f / 256f; }

        /// <summary>HP as 0..100; a living entity never rounds down to 0 so a sliver of HP still shows a bar.</summary>
        public static byte HpPct(float hp, float maxHp)
        {
            if (maxHp <= 0f || hp <= 0f) return 0;
            return (byte)Math.Max(1, Math.Min(100, (int)Math.Round(hp / maxHp * 100f)));
        }

        public static float HpFraction(byte pct) { return Math.Min(100, (int)pct) / 100f; }

        /// <summary>Animation progress 0..1 as one byte.</summary>
        public static byte Progress(float p01)
        {
            if (float.IsNaN(p01)) return 0;
            return (byte)Math.Max(0, Math.Min(255, (int)Math.Round(p01 * 255f)));
        }

        public static float Progress(byte packed) { return packed / 255f; }

        /// <summary>Facing (0..3) in the low 2 bits, up to 6 flag / state bits above.</summary>
        public static byte PackFacing(int facing, int bits) { return (byte)(((bits & 0x3F) << 2) | (facing & 3)); }
        public static int Facing(byte packed) { return packed & 3; }
        public static int Bits(byte packed) { return packed >> 2; }

        /// <summary>Wrap-safe "a is later than b" for the uint millisecond server clock / sequence numbers.</summary>
        public static bool IsNewer(uint a, uint b) { return (int)(a - b) > 0; }
    }
}
