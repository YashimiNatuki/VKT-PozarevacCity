using System;
using NUnit.Framework;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityStreetJobsTests
    {
        [Test]
        public void SeedReproducesOffersAndEveryRouteUsesThreeDifferentStations()
        {
            CityStreetJobs a = new CityStreetJobs(new CitySession(1000), 42), b = new CityStreetJobs(new CitySession(1000), 42);
            Assert.That(a.Offer.Title, Is.EqualTo(b.Offer.Title));
            for (int i = 0; i < 3; i++) Assert.That(a.Offer.StationAt(i), Is.EqualTo(b.Offer.StationAt(i)));
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, new[] { a.Offer.StationAt(0), a.Offer.StationAt(1), a.Offer.StationAt(2) });
        }
        [Test]
        public void OrderedRoutePaysOnceAndCannotRestartDuringCooldown()
        {
            CitySession session = new CitySession(1000);
            CityStreetJobs jobs = new CityStreetJobs(session, 7);
            int reward = jobs.Offer.CashReward, reputation = jobs.Offer.ReputationReward;
            Assert.That(jobs.TryVisit(0), Is.False);
            Assert.That(jobs.TryAccept(), Is.True);
            Assert.That(jobs.TryAccept(), Is.False);
            Assert.That(jobs.TryVisit(jobs.Offer.StationAt(1)), Is.False);
            for (int i = 0; i < 3; i++) Assert.That(jobs.TryVisit(jobs.Offer.StationAt(i)), Is.True);
            Assert.That(session.Cash, Is.EqualTo(1000 + reward));
            Assert.That(session.Reputation, Is.EqualTo(reputation));
            Assert.That(jobs.CompletedJobs, Is.EqualTo(1));
            Assert.That(jobs.TryVisit(0), Is.False);
            Assert.That(jobs.TryAccept(), Is.False);
            jobs.Tick(29); Assert.That(jobs.TryAccept(), Is.False);
            jobs.Tick(1); Assert.That(jobs.TryAccept(), Is.True);
            Assert.That(jobs.ObjectiveIndex, Is.Zero);
        }
        [Test]
        public void DeadlineAndCancellationDiscardCargoWithoutPayout()
        {
            CitySession session = new CitySession(1000);
            CityStreetJobs jobs = new CityStreetJobs(session);
            jobs.TryAccept(); jobs.TryVisit(jobs.Offer.StationAt(0));
            jobs.Tick(jobs.Offer.TimeLimit);
            Assert.That(jobs.IsActive, Is.False);
            Assert.That(jobs.ObjectiveIndex, Is.Zero);
            Assert.That(session.Cash, Is.EqualTo(1000));
            Assert.That(jobs.CompletedJobs, Is.Zero);
            jobs.Tick(15); jobs.TryAccept();
            Assert.That(jobs.Cancel("Smrt igrača."), Is.True);
            Assert.That(jobs.Cancel(), Is.False);
            Assert.That(session.Cash, Is.EqualTo(1000));
        }
        [Test]
        public void StreetStoryAdvancesAfterThreeSuccessfulJobs()
        {
            CityStreetJobs jobs = new CityStreetJobs(new CitySession(1000));
            for (int job = 0; job < 3; job++)
            {
                jobs.TryAccept();
                for (int step = 0; step < 3; step++) jobs.TryVisit(jobs.Offer.StationAt(step));
                jobs.Tick(30);
            }
            Assert.That(jobs.StoryChapter, Is.EqualTo(2));
            Assert.That(jobs.Offer.Title, Does.StartWith("Poglavlje 2"));
        }
        [Test]
        public void CompletionCommitsBeforeReentrantCashObservers()
        {
            CitySession session = new CitySession(1000);
            CityStreetJobs jobs = new CityStreetJobs(session);
            int reward = jobs.Offer.CashReward;
            session.Changed += () => { Assert.That(jobs.IsActive, Is.False); Assert.That(jobs.TryVisit(0), Is.False); Assert.That(jobs.TryAccept(), Is.False); };
            jobs.TryAccept();
            for (int step = 0; step < 3; step++) jobs.TryVisit(jobs.Offer.StationAt(step));
            Assert.That(session.Cash, Is.EqualTo(1000 + reward));
        }
        [Test]
        public void EncyclopediaKeepsTradeIdsAndLabelsEducationalCards()
        {
            Assert.That(CityPlantEncyclopedia.Count, Is.EqualTo(16));
            for (int i = 0; i < 16; i++)
            {
                CityPlantCard card = CityPlantEncyclopedia.Get(i);
                Assert.That(card.IsTradable, Is.EqualTo(i < 8));
                Assert.That(new Uri(card.SourceUrl).Scheme, Is.EqualTo("https"));
                CollectionAssert.Contains(new[] { "www.royalqueenseeds.com", "www.barneysfarm.com", "sensiseeds.com", "www.seriousseeds.com", "dutch-passion.com" }, new Uri(card.SourceUrl).Host);
                Assert.That(card.ScientificName, Does.StartWith("Cannabis sativa L."));
                if (i < 8) { Assert.That(card.CatalogIndex, Is.EqualTo(i)); Assert.That(card.Name, Is.EqualTo(CityPlantCatalog.Get(i).Name)); }
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => CityPlantEncyclopedia.Get(16));
        }
        [Test]
        public void InvalidTickCannotMutateActiveDeadline()
        {
            CityStreetJobs jobs = new CityStreetJobs(new CitySession(1000)); jobs.TryAccept();
            double time = jobs.RemainingSeconds;
            Assert.Throws<ArgumentOutOfRangeException>(() => jobs.Tick(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => jobs.Tick(-1));
            Assert.That(jobs.RemainingSeconds, Is.EqualTo(time));
        }
    }
}
