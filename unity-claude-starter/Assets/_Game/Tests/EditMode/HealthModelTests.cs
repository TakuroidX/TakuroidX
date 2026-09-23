using System;
using NUnit.Framework;

namespace Game.Tests
{
    public class HealthModelTests
    {
        [Test]
        public void StartsAtMax()
        {
            var h = new HealthModel(100);
            Assert.That(h.Current, Is.EqualTo(100));
            Assert.That(h.IsDead, Is.False);
        }

        [Test]
        public void DamageClampsAtZeroAndFiresDiedOnce()
        {
            var h = new HealthModel(10);
            var died = 0;
            h.Died += () => died++;

            h.TakeDamage(15);
            h.TakeDamage(5);

            Assert.That(h.Current, Is.EqualTo(0));
            Assert.That(died, Is.EqualTo(1));
        }

        [Test]
        public void HealClampsAtMax()
        {
            var h = new HealthModel(10);
            h.TakeDamage(3);
            h.Heal(100);
            Assert.That(h.Current, Is.EqualTo(10));
        }

        [Test]
        public void NegativeValuesThrow()
        {
            var h = new HealthModel(10);
            Assert.Throws<ArgumentOutOfRangeException>(() => h.TakeDamage(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => h.Heal(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HealthModel(0));
        }
    }
}
