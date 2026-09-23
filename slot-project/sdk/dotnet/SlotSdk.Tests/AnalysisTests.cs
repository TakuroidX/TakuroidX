using System;
using SlotSdk.Analysis;
using Xunit;

namespace SlotSdk.Tests
{
    public class AnalysisTests
    {
        [Fact]
        public void TheoryMatchesPythonReference()
        {
            // Reference values from tools/slotmath.py
            var t = TheoreticalCalculator.Compute(TestModels.Neon());
            Assert.Equal(0.759959, t.LineRtp, 6);
            Assert.Equal(0.017520, t.ScatterRtp, 6);
            Assert.Equal(0.959794, t.TotalRtp, 6);
            Assert.Equal(137.934386, 1 / t.TriggerProbability, 5);
        }

        [Fact]
        public void FullCycleEqualsTheoryOnSmallModel()
        {
            // A small model that satisfies the premises (fast full-cycle enumeration)
            var m = MathModel.FromJson(TestModels.WithReels(new[]
            {
                new[] { "SEVEN", "A", "SCATTER", "K", "Q", "J", "BELL", "A", "K" },
                new[] { "WILD", "SEVEN", "Q", "BELL", "A", "K", "J", "BELL" },
                new[] { "A", "WILD", "SEVEN", "K", "SCATTER", "Q", "J", "SEVEN", "A" },
                new[] { "SEVEN", "BELL", "K", "WILD", "A", "J", "Q" },
                new[] { "SEVEN", "A", "K", "SCATTER", "Q", "J", "SEVEN", "BELL", "A" },
            }));
            var th = TheoreticalCalculator.Compute(m);
            var full = FullCycleCalculator.Run(m);
            Assert.Equal(9L * 8 * 9 * 7 * 9, full.Combinations);
            Assert.Equal(th.BaseRtp, full.BaseRtp, 12);

            long trig = 0;
            for (var k = m.ScatterTriggerCount; k < full.ScatterCountHistogram.Length; k++) trig += full.ScatterCountHistogram[k];
            Assert.Equal(th.TriggerProbability, (double)trig / full.Combinations, 12);
        }

        [Fact]
        public void MonteCarloIsReproducibleAndWithinConfidence()
        {
            var m = TestModels.Neon();
            var a = MonteCarloSimulator.Run(m, 400_000, 7);
            var b = MonteCarloSimulator.Run(m, 400_000, 7);
            Assert.Equal(a.TotalWin, b.TotalWin);

            var th = TheoreticalCalculator.Compute(m);
            var z = (a.Rtp - th.TotalRtp) / (a.StdDev / Math.Sqrt(a.Rounds));
            Assert.True(Math.Abs(z) < 4, $"z={z}");
        }
    }
}
