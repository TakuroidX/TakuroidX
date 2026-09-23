using System;
using Xunit;

namespace SlotSdk.Tests
{
    public class MathModelTests
    {
        [Fact]
        public void LoadsNeonFortune()
        {
            var m = TestModels.Neon();
            Assert.Equal("neon_fortune", m.Id);
            Assert.Equal(5, m.ReelCount);
            Assert.Equal(3, m.RowCount);
            Assert.Equal(20, m.Paylines.Length);
            Assert.Equal(1000, m.Paytable[m.SymbolIndex("SEVEN")][5]);
            Assert.Equal(2, m.Paytable[m.SymbolIndex("SEVEN")][2]);
            Assert.Equal(50, m.ScatterPays[5]);
            Assert.Empty(m.Validate());
        }

        [Fact]
        public void RejectsWildOnFirstReel()
        {
            var json = TestModels.WithReels(new[]
            {
                new[] { "WILD", "A", "K" }, new[] { "A", "K", "Q" }, new[] { "A", "K", "Q" }, new[] { "A", "K", "Q" }, new[] { "A", "K", "Q" },
            });
            var ex = Assert.Throws<FormatException>(() => MathModel.FromJson(json));
            Assert.Contains("reel 1 must not contain WILD", ex.Message);
        }

        [Fact]
        public void RejectsScattersCloserThanRowCount()
        {
            var json = TestModels.WithReels(new[]
            {
                new[] { "A", "K", "Q", "J" }, new[] { "SCATTER", "K", "SCATTER", "J", "A", "Q" }, new[] { "A", "K", "Q" },
                new[] { "A", "K", "Q" }, new[] { "A", "K", "Q" },
            });
            var ex = Assert.Throws<FormatException>(() => MathModel.FromJson(json));
            Assert.Contains("SCATTERs closer than 3", ex.Message);
        }
    }
}
