using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public class HealthPlayModeTests
    {
        [UnityTest]
        public IEnumerator ComponentInitializesModelOnAwake()
        {
            var go = new GameObject("Health");
            var health = go.AddComponent<Health>();
            yield return null;

            Assert.That(health.Model, Is.Not.Null);
            health.TakeDamage(30);
            Assert.That(health.Model.Current, Is.EqualTo(70));

            Object.Destroy(go);
        }
    }
}
