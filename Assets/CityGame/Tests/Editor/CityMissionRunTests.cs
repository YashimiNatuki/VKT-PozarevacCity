using System;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityMissionRunTests
    {
        private static CityMissionContract Contract(double seconds = 60) =>
            new CityMissionContract("Test route", "Pickup, delivery, contact", seconds, 600, 12, 4f, 80f,
                "Collect parcel", "Deliver parcel", "Report back");

        [Test]
        public void FullRouteTracksCargoAndPaysOnlyAtFinalConfirmation()
        {
            CitySession session = new CitySession(100);
            CityMissionRun run = new CityMissionRun(Contract(), session);
            Assert.That(run.TryVisit(0), Is.True);
            Assert.That(run.Cargo, Is.EqualTo(1));
            Assert.That(session.Cash, Is.EqualTo(100));
            Assert.That(run.TryVisit(1), Is.True);
            Assert.That(run.Cargo, Is.Zero);
            Assert.That(session.Cash, Is.EqualTo(100));
            Assert.That(run.TryVisit(2), Is.True);
            Assert.That(run.State, Is.EqualTo(CityMissionState.Completed));
            Assert.That(session.Cash, Is.EqualTo(700));
            Assert.That(session.Reputation, Is.EqualTo(12));
            Assert.That(session.Heat, Is.EqualTo(4f));
            Assert.That(run.TryVisit(2), Is.False);
            Assert.That(run.TryVisit(0), Is.False);
            Assert.That(run.Retry(), Is.False);
            Assert.That(session.Cash, Is.EqualTo(700));
        }

        [Test]
        public void OutOfOrderAndRepeatedStationsCannotAdvanceOrCreateCargo()
        {
            CityMissionRun run = new CityMissionRun(Contract(), new CitySession(100));
            Assert.That(run.TryVisit(1), Is.False);
            Assert.That(run.TryVisit(2), Is.False);
            Assert.That(run.State, Is.EqualTo(CityMissionState.Ready));
            Assert.That(run.Cargo, Is.Zero);
            run.TryVisit(0);
            Assert.That(run.TryVisit(0), Is.False);
            Assert.That(run.TryVisit(2), Is.False);
            Assert.That(run.ObjectiveIndex, Is.EqualTo(1));
            Assert.That(run.Cargo, Is.EqualTo(1));
        }

        [Test]
        public void ClockStartsAtPickupAndExactDeadlineFailsWithoutPayment()
        {
            CitySession session = new CitySession(100);
            CityMissionRun run = new CityMissionRun(Contract(10), session);
            run.Tick(100);
            Assert.That(run.RemainingSeconds, Is.EqualTo(10));
            run.TryVisit(0);
            run.Tick(10);
            Assert.That(run.State, Is.EqualTo(CityMissionState.Failed));
            Assert.That(run.RemainingSeconds, Is.Zero);
            Assert.That(run.Cargo, Is.Zero);
            Assert.That(run.TryVisit(1), Is.False);
            Assert.That(session.Cash, Is.EqualTo(100));
        }

        [Test]
        public void RetryStartsCleanAndStillPaysOnlyOnce()
        {
            CitySession session = new CitySession(100);
            CityMissionRun run = new CityMissionRun(Contract(10), session);
            run.TryVisit(0);
            run.Tick(11);
            Assert.That(run.Retry(), Is.True);
            Assert.That(run.Attempt, Is.EqualTo(2));
            Assert.That(run.ObjectiveIndex, Is.Zero);
            Assert.That(run.Cargo, Is.Zero);
            Assert.That(run.RemainingSeconds, Is.EqualTo(10));
            Assert.That(run.TryVisit(1), Is.False);
            run.TryVisit(0); run.TryVisit(1); run.TryVisit(2);
            Assert.That(session.Cash, Is.EqualTo(700));
        }

        [Test]
        public void ExcessHeatFailsActiveRunAndRequiresCooldownBeforeRetry()
        {
            CitySession session = new CitySession(100);
            CityMissionRun run = new CityMissionRun(Contract(), session);
            run.TryVisit(0);
            session.AddHeat(80);
            Assert.That(run.TryVisit(1), Is.False);
            Assert.That(run.State, Is.EqualTo(CityMissionState.Failed));
            Assert.That(run.Retry(), Is.False);
            session.CoolDown(5);
            Assert.That(run.Retry(), Is.True);
            Assert.That(run.Cargo, Is.Zero);
            Assert.That(session.Cash, Is.EqualTo(100));
        }

        [Test]
        public void HotSessionCannotAcceptContract()
        {
            CitySession session = new CitySession(100);
            session.AddHeat(80);
            CityMissionRun run = new CityMissionRun(Contract(), session);
            Assert.That(run.TryVisit(0), Is.False);
            Assert.That(run.State, Is.EqualTo(CityMissionState.Ready));
            Assert.That(run.Cargo, Is.Zero);
        }

        [Test]
        public void RewardIsAtomicAndObserversCannotClaimItAgain()
        {
            CitySession session = new CitySession(100);
            CityMissionRun run = new CityMissionRun(Contract(), session);
            int notifications = 0;
            session.Changed += () =>
            {
                notifications++;
                Assert.That(run.State, Is.EqualTo(CityMissionState.Completed));
                Assert.That(session.Cash, Is.EqualTo(700));
                Assert.That(session.Reputation, Is.EqualTo(12));
                Assert.That(session.Heat, Is.EqualTo(4f));
                Assert.That(run.TryVisit(2), Is.False);
            };
            run.TryVisit(0); run.TryVisit(1); run.TryVisit(2);
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void AllThreeContractsHaveValidRoutesAndIndependentRewards()
        {
            CitySession session = new CitySession(2500);
            foreach (CityMissionContract contract in CityMissionDirector.CreateContracts())
            {
                CityMissionRun run = new CityMissionRun(contract, session);
                Assert.That(run.TryVisit(0), Is.True);
                Assert.That(run.TryVisit(1), Is.True);
                Assert.That(run.TryVisit(2), Is.True);
                Assert.That(run.State, Is.EqualTo(CityMissionState.Completed));
                Assert.That(run.Cargo, Is.Zero);
            }
            Assert.That(session.Cash, Is.EqualTo(5600));
            Assert.That(session.Reputation, Is.EqualTo(55));
            Assert.That(session.Heat, Is.EqualTo(21));
        }

        [Test]
        public void ElapsedTimeDoesNotDependOnFrameCount()
        {
            CityMissionRun slow = new CityMissionRun(Contract(), new CitySession(0));
            CityMissionRun fast = new CityMissionRun(Contract(), new CitySession(0));
            slow.TryVisit(0); fast.TryVisit(0);
            slow.Tick(10);
            for (int i = 0; i < 100; i++) fast.Tick(0.1);
            Assert.That(fast.RemainingSeconds, Is.EqualTo(slow.RemainingSeconds).Within(0.00001));
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidElapsedTimeIsRejectedWithoutChangingProgress(double elapsed)
        {
            CityMissionRun run = new CityMissionRun(Contract(), new CitySession(0));
            run.TryVisit(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => run.Tick(elapsed));
            Assert.That(run.ObjectiveIndex, Is.EqualTo(1));
            Assert.That(run.RemainingSeconds, Is.EqualTo(60));
        }

        [Test]
        public void ContractCopiesAuthoringDataAndRejectsInvalidConfiguration()
        {
            string[] titles = { "Pickup", "Delivery", "Contact" };
            CityMissionContract contract = new CityMissionContract("Route", "Brief", 60, 100, 1, 0, 80, titles);
            titles[0] = "Mutated externally";
            Assert.That(contract.ObjectiveTitle(0), Is.EqualTo("Pickup"));
            Assert.Throws<ArgumentOutOfRangeException>(() => Contract(0));
            Assert.Throws<ArgumentException>(() => new CityMissionContract("Route", "Brief", 60, 100, 1, 0, 80, "Only one"));
        }

        [Test]
        public void RewardsCannotOverflowSessionBalances()
        {
            CitySession session = new CitySession(int.MaxValue - 1);
            session.ApplyMissionReward(600, 12, 4);
            Assert.That(session.Cash, Is.EqualTo(int.MaxValue));
            Assert.That(session.Cash, Is.GreaterThanOrEqualTo(0));
            Assert.That(session.Reputation, Is.EqualTo(12));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.AddHeat(float.NaN));
            Assert.That(session.Heat, Is.EqualTo(4));
        }

        [Test]
        public void GroundProbeUsesWalkableFloorAndIgnoresMissionObjectsAndTriggers()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject station = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                floor.transform.position = new Vector3(10000, -0.5f, 10000);
                floor.transform.localScale = new Vector3(10, 1, 10);
                station.transform.position = new Vector3(10000, 8, 10000);
                station.AddComponent<CityMission>();
                trigger.transform.position = new Vector3(10000, 12, 10000);
                trigger.GetComponent<Collider>().isTrigger = true;
                Physics.SyncTransforms();
                CityGroundProbe probe = new CityGroundProbe();
                Assert.That(probe.TryFindSurface(new Vector3(10000, 3, 10000), out Vector3 point), Is.True);
                Assert.That(point.y, Is.EqualTo(0).Within(0.001));
                Assert.That(probe.TryFindSurface(new Vector3(20000, 3, 20000), out _), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(floor);
                UnityEngine.Object.DestroyImmediate(station);
                UnityEngine.Object.DestroyImmediate(trigger);
            }
        }
    }
}
