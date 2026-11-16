using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>Injectable RNG, same convention as the web `rand = Math.random` parameters: returns a double in [0, 1).</summary>
    public delegate double Rand();

    /// <summary>Deterministic RNG (mulberry32) for tests and reproducible loot.</summary>
    public sealed class SeededRng
    {
        uint state;
        public SeededRng(uint seed) { state = seed; }

        public double Next()
        {
            unchecked
            {
                state += 0x6D2B79F5u;
                uint t = state;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }

        public Rand AsRand() { return Next; }
    }

    /// <summary>Default non-seeded source.</summary>
    public static class RpgRandom
    {
        static readonly Random Shared = new Random();
        public static double Default() { return Shared.NextDouble(); }

        static readonly System.Security.Cryptography.RandomNumberGenerator Crypto = System.Security.Cryptography.RandomNumberGenerator.Create();
        static readonly byte[] CryptoBytes = new byte[8];

        /// <summary>Cryptographic source for what players must not predict (the pet gacha on the zone server): 53 random bits in [0, 1). Main thread only.</summary>
        public static double Secure()
        {
            Crypto.GetBytes(CryptoBytes);
            ulong v = BitConverter.ToUInt64(CryptoBytes, 0) >> 11;
            return v / (double)(1UL << 53);
        }
    }

    /// <summary>JavaScript-compatible numeric helpers (the web uses Math.round, which rounds .5 up, unlike C# banker's rounding).</summary>
    public static class RpgMath
    {
        public static int Round(double v) { return (int)Math.Floor(v + 0.5); }
        public static float Round3(double v) { return (float)(Math.Floor(v * 1000.0 + 0.5) / 1000.0); }
        public static int Clamp(int v, int lo, int hi) { return v < lo ? lo : v > hi ? hi : v; }
    }
}
