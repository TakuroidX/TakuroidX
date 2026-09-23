using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public class SlotSmokeTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp() => SaveStore.Enabled = false; // don't touch the real save data

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go);
            SaveStore.Enabled = true;
        }

        [UnityTest]
        public IEnumerator SpinCompletesAndBalanceIsConsistent()
        {
            _go = new GameObject("NeonFortune");
            var c = _go.AddComponent<SlotController>();
            c.Turbo = true;

            var t = 0f;
            while (!c.IsIdle && t < 5f) { t += Time.deltaTime; yield return null; }
            Assert.That(c.IsIdle, "controller never became idle");

            var start = c.Game.Balance;
            c.RequestSpin();
            yield return null;
            t = 0f;
            while ((!c.IsIdle || c.Game.IsInFreeSpins || c.PaidSpins == 0) && t < 60f)
            {
                c.OnOverlayClicked(); // skip presentations
                t += Time.deltaTime;
                yield return null;
            }

            Assert.That(c.PaidSpins, Is.EqualTo(1));
            Assert.That(c.IsIdle, "spin did not finish within 60s");
            Assert.That(c.Game.Balance, Is.GreaterThanOrEqualTo(start - c.Game.TotalBet));
        }
    }
}
