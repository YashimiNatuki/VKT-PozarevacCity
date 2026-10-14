using System;
using NUnit.Framework;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityMissionChallengeTests
    {
        private static CityMissionRun Create(CitySession session, int reward = 600) => new CityMissionRun(
            new CityMissionContract("Route", "Complete the route", 100, reward, 12, 4, 80,
                "Pickup", "Delivery", "Contact"), session);

        [Test]
        public void ExpressPaysPremiumOnlyAfterFullRouteAndCannotPayTwice()
        {
            CitySession session = new CitySession(100);
            CityMissionRun run = Create(session);
            Assert.That(run.TrySelectChallenge(CityMissionChallenge.Express), Is.True);
            Assert.That(run.RemainingSeconds, Is.EqualTo(60));
            Assert.That(run.EffectiveCashReward, Is.EqualTo(900));
            run.TryVisit(0); run.Tick(59); run.TryVisit(1);
            Assert.That(session.Cash, Is.EqualTo(100));
            run.TryVisit(2);
            Assert.That(session.Cash, Is.EqualTo(1000));
            Assert.That(session.Reputation, Is.EqualTo(12));
            Assert.That(run.TryVisit(2), Is.False);
            Assert.That(run.TrySelectChallenge(CityMissionChallenge.Standard), Is.False);
            Assert.That(session.Cash, Is.EqualTo(1000));
        }

        [Test]
        public void ExactExpressDeadlineFailsAndRetryRetainsChallenge()
        {
            CitySession session = new CitySession(0);
            CityMissionRun run = Create(session);
            run.TrySelectChallenge(CityMissionChallenge.Express);
            run.TryVisit(0); run.Tick(60);
            Assert.That(run.State, Is.EqualTo(CityMissionState.Failed));
            Assert.That(session.Cash, Is.Zero);
            Assert.That(run.Retry(), Is.True);
            Assert.That(run.Challenge, Is.EqualTo(CityMissionChallenge.Express));
            Assert.That(run.RemainingSeconds, Is.EqualTo(60));
            Assert.That(run.Cargo, Is.Zero);
        }

        [Test]
        public void GhostBlocksHotPickupAndFailsAtExactHeatLimit()
        {
            CitySession session = new CitySession(0);
            CityMissionRun run = Create(session);
            run.TrySelectChallenge(CityMissionChallenge.Ghost);
            session.AddHeat(20);
            Assert.That(run.TryVisit(0), Is.False);
            Assert.That(run.State, Is.EqualTo(CityMissionState.Ready));
            session.CoolDown(1); run.TryVisit(0); session.AddHeat(1);
            Assert.That(run.TryVisit(1), Is.False);
            Assert.That(run.State, Is.EqualTo(CityMissionState.Failed));
            Assert.That(run.Retry(), Is.False);
            session.CoolDown(1);
            Assert.That(run.Retry(), Is.True);
            run.TryVisit(0); run.TryVisit(1); run.TryVisit(2);
            Assert.That(session.Cash, Is.EqualTo(750));
        }

        [Test]
        public void ActiveChallengeCannotBeChangedToEvadeDeadlineOrHeat()
        {
            CityMissionRun run = Create(new CitySession(0));
            run.TrySelectChallenge(CityMissionChallenge.Express); run.TryVisit(0); run.Tick(10);
            Assert.That(run.TrySelectChallenge(CityMissionChallenge.Standard), Is.False);
            Assert.That(run.RemainingSeconds, Is.EqualTo(50));
            Assert.That(run.Challenge, Is.EqualTo(CityMissionChallenge.Express));
        }

        [Test]
        public void ReadySelectionRestoresStandardConditionsAndRejectsInvalidValues()
        {
            CityMissionRun run = Create(new CitySession(0));
            run.TrySelectChallenge(CityMissionChallenge.Express);
            Assert.Throws<ArgumentOutOfRangeException>(() => run.TrySelectChallenge((CityMissionChallenge)99));
            Assert.That(run.Challenge, Is.EqualTo(CityMissionChallenge.Express));
            Assert.That(run.TrySelectChallenge(CityMissionChallenge.Standard), Is.True);
            Assert.That(run.EffectiveTimeLimit, Is.EqualTo(100));
            Assert.That(run.EffectiveCashReward, Is.EqualTo(600));
            Assert.That(run.EffectiveHeatLimit, Is.EqualTo(80));
        }

        [Test]
        public void PremiumSaturatesWithoutOverflowAndCompletionCommitsBeforeObservers()
        {
            CitySession session = new CitySession(100);
            CityMissionRun run = Create(session, int.MaxValue);
            run.TrySelectChallenge(CityMissionChallenge.Express);
            session.Changed += () => Assert.That(run.TryVisit(2), Is.False);
            run.TryVisit(0); run.TryVisit(1); run.TryVisit(2);
            Assert.That(session.Cash, Is.EqualTo(int.MaxValue));
        }
    }
}
