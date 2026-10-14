using NUnit.Framework;
using System;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityHealthTests
    {
        [Test] public void DamageCooldownBlocksRepeatedHitsAndDeathCannotRepeat()
        {
            CityHealth health = new CityHealth();
            Assert.That(health.TryDamage(40f), Is.True);
            Assert.That(health.Current, Is.EqualTo(60f));
            Assert.That(health.TryDamage(40f), Is.False);
            health.Tick(0.75f);
            Assert.That(health.TryDamage(1000f), Is.True);
            Assert.That(health.Current, Is.Zero);
            health.Tick(10f);
            Assert.That(health.TryDamage(1f), Is.False);
        }
        [Test] public void RespawnRestoresHealthAndProvidesEscapeWindow()
        {
            CityHealth health = new CityHealth(); health.TryDamage(100f); health.Respawn();
            Assert.That(health.Current, Is.EqualTo(100f));
            Assert.That(health.TryDamage(20f), Is.False);
            health.Tick(3f); Assert.That(health.TryDamage(20f), Is.True);
        }
        [Test] public void InvalidDamageDoesNotCorruptHealth()
        {
            CityHealth health = new CityHealth();
            Assert.That(health.TryDamage(float.NaN), Is.False);
            Assert.That(health.TryDamage(float.PositiveInfinity), Is.False);
            Assert.That(health.TryDamage(-5f), Is.False);
            Assert.That(health.Current, Is.EqualTo(100f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CityHealth(0f));
        }
        [Test] public void RestoreClampsAndPausedTickDoesNotConsumeProtection()
        {
            CityHealth health = new CityHealth(); health.Restore(200f);
            Assert.That(health.Current, Is.EqualTo(100f)); health.TryDamage(10f);
            health.Tick(0f); health.Tick(float.NaN);
            Assert.That(health.TryDamage(10f), Is.False);
        }
    }
}
