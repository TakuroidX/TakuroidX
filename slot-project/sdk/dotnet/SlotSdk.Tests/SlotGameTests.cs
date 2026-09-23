using System;
using Xunit;

namespace SlotSdk.Tests
{
    public class SlotGameTests
    {
        [Fact]
        public void BaseSpinDeductsBetAndAddsWin()
        {
            var m = TestModels.Neon();
            var g = new SlotGame(m, new Xoshiro256StarStar(1), 1000);
            var r = g.Spin();
            Assert.False(r.IsFreeSpin);
            Assert.Equal(1000 - 20 + r.TotalWin, g.Balance);
            Assert.Equal(g.Balance, r.BalanceAfter);
        }

        [Fact]
        public void ThreeScattersTriggerFreeSpinsWithMultiplierAndNoDeduction()
        {
            var m = TestModels.Neon();
            var g = new SlotGame(m, new Xoshiro256StarStar(1), 1000);
            var trigger = TestModels.ScatterStops(m, 0, 2, 4);

            var t = g.SpinWithStops(trigger);
            Assert.True(t.FeatureTriggered);
            Assert.Equal(10, t.FreeSpinsAwarded);
            Assert.Equal(10, g.FreeSpinsRemaining);
            Assert.Equal(2 * 20, t.ScatterWin);

            // Spinning the same stops in FS gives x3 and a +10 retrigger
            var before = g.Balance;
            var f = g.SpinWithStops(trigger);
            Assert.True(f.IsFreeSpin);
            Assert.Equal(3, f.Multiplier);
            Assert.Equal(t.TotalWin * 3, f.TotalWin);
            Assert.Equal(before + f.TotalWin, g.Balance); // no bet deducted
            Assert.Equal(10, f.FreeSpinsAwarded);
            Assert.Equal(19, g.FreeSpinsRemaining);
        }

        [Fact]
        public void FreeSpinsEndAndReportFeatureTotal()
        {
            var m = TestModels.Neon();
            var g = new SlotGame(m, new Xoshiro256StarStar(1), 1000);
            g.SpinWithStops(TestModels.ScatterStops(m, 0, 2, 4));
            var none = TestModels.ScatterStops(m);
            long sum = 0;
            SpinResult last = null;
            for (var i = 0; i < 10; i++)
            {
                last = g.SpinWithStops(none);
                sum += last.TotalWin;
                Assert.Equal(i == 9, last.FeatureEnded);
            }
            Assert.Equal(sum, last.FeatureTotalWin);
            Assert.False(g.IsInFreeSpins);
        }

        [Fact]
        public void BetIsLockedDuringFreeSpinsAndBalanceIsChecked()
        {
            var m = TestModels.Neon();
            var g = new SlotGame(m, new Xoshiro256StarStar(1), 1000);
            g.SetBetLevel(3); // line bet 10 → total 200
            Assert.Equal(200, g.TotalBet);
            g.SpinWithStops(TestModels.ScatterStops(m, 0, 2, 4));
            Assert.Throws<InvalidOperationException>(() => g.SetBetLevel(0));

            var poor = new SlotGame(m, new Xoshiro256StarStar(1), 19);
            Assert.False(poor.CanSpin);
            Assert.Throws<InvalidOperationException>(() => poor.Spin());
        }
    }
}
