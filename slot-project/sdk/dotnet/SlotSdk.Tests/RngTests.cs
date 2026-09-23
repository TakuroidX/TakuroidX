using System;
using System.Linq;
using Xunit;

namespace SlotSdk.Tests
{
    public class RngTests
    {
        [Fact]
        public void SameSeedSameSequence()
        {
            var a = new Xoshiro256StarStar(42);
            var b = new Xoshiro256StarStar(42);
            for (var i = 0; i < 1000; i++) Assert.Equal(a.NextInt(1000), b.NextInt(1000));
        }

        [Fact]
        public void DifferentSeedsDiffer()
        {
            var a = Enumerable.Range(0, 20).Select(_ => 0).ToArray();
            var ra = new Xoshiro256StarStar(1);
            var rb = new Xoshiro256StarStar(2);
            Assert.NotEqual(Enumerable.Range(0, 20).Select(_ => ra.NextInt(1 << 30)), Enumerable.Range(0, 20).Select(_ => rb.NextInt(1 << 30)));
        }

        [Theory]
        [InlineData(44)]
        [InlineData(7)]
        public void RoughlyUniform_ChiSquare(int n)
        {
            var rng = new Xoshiro256StarStar(7);
            var counts = new long[n];
            const int samples = 440000;
            for (var i = 0; i < samples; i++)
            {
                var v = rng.NextInt(n);
                Assert.InRange(v, 0, n - 1);
                counts[v]++;
            }
            var expected = (double)samples / n;
            var chi2 = counts.Sum(c => (c - expected) * (c - expected) / expected);
            // Upper 0.1% point for df=n-1 is at most about n+3.1*sqrt(2n)+10 (a loose bound)
            Assert.True(chi2 < n + 3.1 * Math.Sqrt(2 * n) + 10, $"chi2={chi2}");
        }

        [Fact]
        public void CryptoRngInRange()
        {
            using var rng = new CryptoRng();
            for (var i = 0; i < 10000; i++) Assert.InRange(rng.NextInt(43), 0, 42);
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(0));
        }
    }
}
