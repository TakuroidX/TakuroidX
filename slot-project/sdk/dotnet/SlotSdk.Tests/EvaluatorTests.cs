using System.Linq;
using Xunit;

namespace SlotSdk.Tests
{
    public class EvaluatorTests
    {
        // columns: {top, mid, bottom}. Top/bottom rows are set so they don't make wins.
        private static string[] Col(string top, string mid, string bottom) => new[] { top, mid, bottom };

        [Fact]
        public void FiveSevensOnCenterLine()
        {
            var m = TestModels.FixedWindow(
                Col("J", "SEVEN", "Q"), Col("K", "SEVEN", "A"), Col("CHERRY", "SEVEN", "BELL"),
                Col("J", "SEVEN", "Q"), Col("K", "SEVEN", "A"));
            var r = new SpinEvaluator(m).Evaluate(TestModels.Zeros, lineBet: 2, multiplier: 1);

            var center = r.LineWins.Single(w => w.LineIndex == 0);
            Assert.Equal(m.SymbolIndex("SEVEN"), center.Symbol);
            Assert.Equal(5, center.Count);
            Assert.Equal(1000 * 2, center.Win);
            Assert.Equal(40, r.TotalBet);
        }

        [Fact]
        public void WildSubstitutesAndRunStopsAtMismatch()
        {
            var m = TestModels.FixedWindow(
                Col("J", "SEVEN", "Q"), Col("K", "WILD", "A"), Col("CHERRY", "SEVEN", "BELL"),
                Col("J", "DIAMOND", "Q"), Col("K", "SEVEN", "A"));
            var r = new SpinEvaluator(m).Evaluate(TestModels.Zeros, 1, 1);
            var center = r.LineWins.Single(w => w.LineIndex == 0);
            Assert.Equal(3, center.Count);
            Assert.Equal(50, center.Win);
        }

        [Fact]
        public void ScatterBreaksLineAndDoesNotSubstitute()
        {
            var m = TestModels.FixedWindow(
                Col("J", "BELL", "Q"), Col("K", "SCATTER", "A"), Col("CHERRY", "BELL", "DIAMOND"),
                Col("J", "BELL", "Q"), Col("K", "BELL", "A"));
            var r = new SpinEvaluator(m).Evaluate(TestModels.Zeros, 1, 1);
            Assert.DoesNotContain(r.LineWins, w => w.LineIndex == 0);
            Assert.Equal(1, r.ScatterCount);
            Assert.Equal(0, r.ScatterWin);
        }

        [Fact]
        public void ScatterPaysTotalBetTimesMultiplier()
        {
            var m = TestModels.FixedWindow(
                Col("SCATTER", "J", "Q"), Col("K", "A", "J"), Col("CHERRY", "SCATTER", "BELL"),
                Col("A", "K", "Q"), Col("K", "A", "SCATTER"));
            var r = new SpinEvaluator(m).Evaluate(TestModels.Zeros, lineBet: 5, multiplier: 3);
            Assert.Equal(3, r.ScatterCount);
            Assert.Equal(2L * 100 * 3, r.ScatterWin); // 2x total bet(100) x3
        }

        [Fact]
        public void EvaluateUnitsMatchesDetailedEvaluation()
        {
            var m = TestModels.Neon();
            var ev = new SpinEvaluator(m);
            var rng = new Xoshiro256StarStar(99);
            var stops = new int[5];
            for (var i = 0; i < 20000; i++)
            {
                for (var k = 0; k < 5; k++) stops[k] = rng.NextInt(m.Reels[k].Length);
                var units = ev.EvaluateUnits(stops, out var sc);
                var detail = ev.Evaluate(stops, 1, 1);
                Assert.Equal(detail.TotalWin, units);
                Assert.Equal(detail.ScatterCount, sc);
            }
        }
    }
}
