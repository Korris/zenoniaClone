using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Deterministic randomness for skills: the server and every client derive the same rain points / spread angles from the cast event
    /// (caster id, skill index, quantised position, optional salt), so the picture matches the damage without sending coordinates.
    /// </summary>
    public static class SkillScatter
    {
        public static int Seed(int casterId, int skillIndex, float x, float y, int salt = 0)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ (uint)casterId) * 16777619u;
                h = (h ^ (uint)skillIndex) * 16777619u;
                h = (h ^ (uint)(int)Math.Round(x * 4f)) * 16777619u;
                h = (h ^ (uint)(int)Math.Round(y * 4f)) * 16777619u;
                h = (h ^ (uint)salt) * 16777619u;
                return (int)h;
            }
        }

        /// <summary>Hash of (seed, i, channel) mapped to [0,1): independent per index so the order of evaluation never matters.</summary>
        public static float Unit(int seed, int i, int channel)
        {
            unchecked
            {
                uint h = (uint)seed * 2654435761u + (uint)i * 40503u + (uint)channel * 9973u;
                h ^= h >> 15; h *= 2246822519u; h ^= h >> 13; h *= 3266489917u; h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        /// <summary>Point i of a uniform scatter inside a disc of the given radius (offset from the centre).</summary>
        public static void InDisc(int seed, int i, float radius, out float dx, out float dy)
        {
            float r = radius * (float)Math.Sqrt(Unit(seed, i, 0)), a = Unit(seed, i, 1) * 6.2831853f;
            dx = r * (float)Math.Cos(a); dy = r * (float)Math.Sin(a);
        }

        /// <summary>Angle offset (degrees) of projectile i of n fanned over spreadDeg, centred on the aim; a single projectile flies straight.</summary>
        public static float FanAngle(int i, int n, float spreadDeg)
        {
            return n <= 1 ? 0f : -spreadDeg * 0.5f + spreadDeg * i / (n - 1);
        }
    }
}
