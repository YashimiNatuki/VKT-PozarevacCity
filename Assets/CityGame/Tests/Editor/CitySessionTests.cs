using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CitySessionTests
    {
        [Test]
        public void RouteRequiresOrderedCompletionAndPaysEachJobOnce()
        {
            GameObject firstObject = new GameObject("PickupTest");
            GameObject secondObject = new GameObject("DeliveryTest");
            GameObject thirdObject = new GameObject("ContactTest");
            try
            {
                CitySession session = new CitySession(100);
                CityMission first = firstObject.AddComponent<CityMission>();
                CityMission second = secondObject.AddComponent<CityMission>();
                CityMission third = thirdObject.AddComponent<CityMission>();
                first.Configure("Pickup", 100, 2, 1f);
                second.Configure("Delivery", 400, 8, 4f, first);
                third.Configure("Contact", 500, 10, 2f, second);

                Assert.That(second.TryComplete(session), Is.False);
                Assert.That(third.TryComplete(session), Is.False);
                Assert.That(session.Cash, Is.EqualTo(100));
                Assert.That(first.TryComplete(session), Is.True);
                Assert.That(first.TryComplete(session), Is.False);
                Assert.That(second.TryComplete(session), Is.True);
                Assert.That(third.TryComplete(session), Is.True);
                Assert.That(third.TryComplete(session), Is.False);
                Assert.That(session.Cash, Is.EqualTo(1100));
                Assert.That(session.Reputation, Is.EqualTo(20));
                Assert.That(session.Heat, Is.EqualTo(7f));
            }
            finally
            {
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
                Object.DestroyImmediate(thirdObject);
            }
        }

        [Test]
        public void MissingSessionDoesNotConsumeMission()
        {
            GameObject missionObject = new GameObject("MissionTest");
            try
            {
                CityMission mission = missionObject.AddComponent<CityMission>();
                Assert.That(mission.TryComplete(null), Is.False);
                Assert.That(mission.IsComplete, Is.False);
                Assert.That(missionObject.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(missionObject);
            }
        }
        [Test]
        public void CompletingAJobChangesCashReputationAndHeat()
        {
            CitySession session = new CitySession(100);

            session.Earn(500, 10);
            session.AddHeat(5f);

            Assert.That(session.Cash, Is.EqualTo(600));
            Assert.That(session.Reputation, Is.EqualTo(10));
            Assert.That(session.Heat, Is.EqualTo(5f));
        }

        [Test]
        public void SpendingCannotOverdrawCash()
        {
            CitySession session = new CitySession(100);

            Assert.That(session.TrySpend(101), Is.False);
            Assert.That(session.Cash, Is.EqualTo(100));
        }
    }
}
