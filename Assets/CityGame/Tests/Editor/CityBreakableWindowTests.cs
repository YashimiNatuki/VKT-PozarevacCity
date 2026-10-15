using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityBreakableWindowTests
    {
        [Test]
        public void PausedWeakOrInvalidImpactsDoNotBreakAndAValidImpactBreaksOnlyOnce()
        {
            float previous = Time.timeScale;
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                CityBreakableWindow window = obj.AddComponent<CityBreakableWindow>();
                Time.timeScale = 0; Assert.That(window.ReceiveImpact(12), Is.False);
                Time.timeScale = 1;
                Assert.That(window.ReceiveImpact(float.NaN), Is.False);
                Assert.That(window.ReceiveImpact(float.PositiveInfinity), Is.False);
                Assert.That(window.ReceiveImpact(6), Is.False);
                Assert.That(window.ReceiveImpact(12), Is.True);
                Assert.That(window.IsBroken, Is.True);
                Assert.That(obj.GetComponent<Collider>().enabled, Is.False);
                Assert.That(obj.GetComponent<Renderer>().enabled, Is.False);
                Assert.That(window.ReceiveImpact(12), Is.False);
            }
            finally { Time.timeScale = previous; Object.DestroyImmediate(obj); }
        }
    }
}
