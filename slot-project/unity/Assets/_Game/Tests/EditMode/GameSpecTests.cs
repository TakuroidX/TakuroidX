using NUnit.Framework;

namespace Game.Tests
{
    public class GameSpecTests
    {
        [TestCase(0, 20, WinTier.None)]
        [TestCase(1, 20, WinTier.Normal)]
        [TestCase(199, 20, WinTier.Normal)]
        [TestCase(200, 20, WinTier.Big)]
        [TestCase(500, 20, WinTier.Mega)]
        [TestCase(1000, 20, WinTier.Epic)]
        public void WinTierThresholds(long win, long bet, WinTier expected)
        {
            Assert.That(GameSpec.TierOf(win, bet), Is.EqualTo(expected));
        }

        [Test]
        public void TurboIsFasterThanNormal()
        {
            Assert.That(GameSpec.Turbo.SpeedSymbolsPerSec, Is.GreaterThan(GameSpec.Normal.SpeedSymbolsPerSec));
            Assert.That(GameSpec.Turbo.MinSpinSeconds, Is.LessThan(GameSpec.Normal.MinSpinSeconds));
            Assert.That(GameSpec.Turbo.StopIntervalSeconds, Is.LessThan(GameSpec.Normal.StopIntervalSeconds));
        }
    }
}
