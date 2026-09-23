using NUnit.Framework;

namespace Game.Tests
{
    public class ReelMotionTests
    {
        private const int Rows = 3;

        [Test]
        public void InitialMappingIsIdentity()
        {
            var m = new ReelMotion(40, 7);
            Assert.That(m.StripIndexOf(7), Is.EqualTo(7));
            Assert.That(m.StripIndexOf(-1), Is.EqualTo(39));
        }

        [Test]
        public void PlanStopLandsOnTargetWithoutChangingVisibleSymbols()
        {
            var m = new ReelMotion(40, 10);
            m.Position = 3.4; // mid-spin
            var visibleBefore = new int[Rows + 2];
            for (var k = 0; k < visibleBefore.Length; k++) visibleBefore[k] = m.StripIndexOf(3 + k); // top=3 .. bottom

            var target = m.PlanStop(25, 1, Rows);

            // Symbols visible right now (index >= top) don't change
            for (var k = 0; k < visibleBefore.Length; k++) Assert.That(m.StripIndexOf(3 + k), Is.EqualTo(visibleBefore[k]));
            // The target window is 25, 26, 27
            for (var r = 0; r < Rows; r++) Assert.That(m.StripIndexOf(target + r), Is.EqualTo(25 + r));
            // It moves down (Position decreases) and travels at least rows + minTravel
            Assert.That(target, Is.LessThanOrEqualTo((long)System.Math.Floor(3.4) - Rows - 1));
        }

        [Test]
        public void RepeatedSpinsStayConsistent()
        {
            var rng = new System.Random(1);
            var m = new ReelMotion(43, 0);
            for (var spin = 0; spin < 200; spin++)
            {
                m.Position -= rng.NextDouble() * 30; // spinning
                var stop = rng.Next(43);
                var target = m.PlanStop(stop, rng.Next(0, 3), Rows);
                m.Settle(target);
                for (var r = 0; r < Rows; r++) Assert.That(m.StripIndexOf(target + r), Is.EqualTo((stop + r) % 43));
                // After settling the mapping is continuous (one symbol above = the previous strip index)
                Assert.That(m.StripIndexOf(target - 1), Is.EqualTo((stop + 42) % 43));
            }
        }

        [Test]
        public void ModHandlesNegatives()
        {
            Assert.That(ReelMotion.Mod(-1, 5), Is.EqualTo(4));
            Assert.That(ReelMotion.Mod(-10, 5), Is.EqualTo(0));
            Assert.That(ReelMotion.Mod(12, 5), Is.EqualTo(2));
        }
    }
}
