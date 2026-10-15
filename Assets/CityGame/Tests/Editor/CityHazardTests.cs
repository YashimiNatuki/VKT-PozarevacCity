using NUnit.Framework;
using UnityEngine;
namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityHazardTests
    {
        [TestCase(CityHazardKind.GuardDog,30f)]
        [TestCase(CityHazardKind.HostileAdult,45f)]
        public void NonlethalSuppressionClampsHealthAndRejectsDuplicateDownHits(CityHazardKind kind,float maximum)
        {
            GameObject obj=new GameObject("HazardImpactTest");float original=Time.timeScale;
            try
            {
                Time.timeScale=1;CityHazard hazard=obj.AddComponent<CityHazard>();hazard.Configure(null,kind);
                Assert.That(hazard.CurrentHitPoints,Is.EqualTo(maximum));
                Assert.That(hazard.ReceiveImpact(5,Vector3.forward),Is.True);
                Assert.That(hazard.CurrentHitPoints,Is.EqualTo(maximum-5));Assert.That(hazard.IsSuppressed,Is.False);
                Assert.That(hazard.ReceiveImpact(9999,Vector3.forward),Is.True);
                Assert.That(hazard.CurrentHitPoints,Is.Zero);Assert.That(hazard.IsSuppressed,Is.True);Assert.That(hazard.IsAttacking,Is.False);
                Assert.That(hazard.ReceiveImpact(10,Vector3.forward),Is.False);Assert.That(hazard.CurrentHitPoints,Is.Zero);
            }
            finally{Time.timeScale=original;Object.DestroyImmediate(obj);}
        }
        [Test]
        public void PausedAndMalformedHitsDoNotChangeHealth()
        {
            GameObject obj=new GameObject("HazardInvalidImpactTest");float original=Time.timeScale;
            try
            {
                Time.timeScale=1;CityHazard hazard=obj.AddComponent<CityHazard>();hazard.Configure(null,CityHazardKind.HostileAdult);
                Assert.That(hazard.ReceiveImpact(float.NaN,Vector3.forward),Is.False);
                Assert.That(hazard.ReceiveImpact(float.PositiveInfinity,Vector3.forward),Is.False);
                Assert.That(hazard.ReceiveImpact(-2,Vector3.forward),Is.False);
                Assert.That(hazard.ReceiveImpact(2,new Vector3(float.NaN,0,0)),Is.False);
                Time.timeScale=0;Assert.That(hazard.ReceiveImpact(10,Vector3.forward),Is.False);
                Assert.That(hazard.CurrentHitPoints,Is.EqualTo(hazard.MaxHitPoints));Assert.That(hazard.IsSuppressed,Is.False);
            }
            finally{Time.timeScale=original;Object.DestroyImmediate(obj);}
        }
    }
}
