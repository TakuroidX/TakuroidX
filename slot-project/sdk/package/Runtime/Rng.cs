using System;
using System.Security.Cryptography;

namespace SlotSdk
{
    public interface IRng
    {
        /// <summary>Uniform integer in [0, maxExclusive)</summary>
        int NextInt(int maxExclusive);
    }

    /// <summary>
    /// xoshiro256** (reproducible from a seed, fast). For simulation, tests, and replays.
    /// Range generation uses rejection sampling, so there is no modulo bias.
    /// </summary>
    public sealed class Xoshiro256StarStar : IRng
    {
        private ulong _s0, _s1, _s2, _s3;

        public Xoshiro256StarStar(ulong seed)
        {
            // Expand the state with SplitMix64 (avoids an all-zero state)
            _s0 = SplitMix(ref seed);
            _s1 = SplitMix(ref seed);
            _s2 = SplitMix(ref seed);
            _s3 = SplitMix(ref seed);
        }

        private static ulong SplitMix(ref ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            var z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        private static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));

        public ulong NextULong()
        {
            var result = Rotl(_s1 * 5, 7) * 9;
            var t = _s1 << 17;
            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;
            _s2 ^= t;
            _s3 = Rotl(_s3, 45);
            return result;
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            var bound = (ulong)maxExclusive;
            // Reject values at or above the largest multiple of bound
            var limit = ulong.MaxValue - ulong.MaxValue % bound;
            ulong r;
            do { r = NextULong(); } while (r >= limit);
            return (int)(r % bound);
        }
    }

    /// <summary>
    /// Uses the OS cryptographic RNG. Default for real play (not reproducible).
    /// Note: this is not a certified gaming RNG.
    /// </summary>
    public sealed class CryptoRng : IRng, IDisposable
    {
        private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
        private readonly byte[] _buf = new byte[4];

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            var bound = (uint)maxExclusive;
            var limit = uint.MaxValue - uint.MaxValue % bound;
            uint r;
            do
            {
                _rng.GetBytes(_buf);
                r = BitConverter.ToUInt32(_buf, 0);
            } while (r >= limit);
            return (int)(r % bound);
        }

        public void Dispose() => _rng.Dispose();
    }
}
