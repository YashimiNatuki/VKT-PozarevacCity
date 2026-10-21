using System;
using System.IO;
using NUnit.Framework;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityPlayerStatsTests
    {
        [TestCase(1)] [TestCase(7)] [TestCase(8)]
        public void OldProfileMigratesWithEmptyStatsAndPreservesBalances(int version)
        {
            var old = new CityProfileData {version=version,callsign="Rodja",cash=123};
            var progression = new CityProgression(CitySession.Restore(old),old);
            var snapshot = progression.Snapshot();
            Assert.That(snapshot.version,Is.EqualTo(CityProgression.CurrentProfileVersion));
            Assert.That(snapshot.cash,Is.EqualTo(123));
            Assert.That(progression.Stats.TotalDistance,Is.Zero);
            Assert.That(progression.Stats.TotalPlayTimeSeconds,Is.Zero);
            Assert.DoesNotThrow(()=>CityProgression.Validate(snapshot));
        }

        [Test]
        public void ProfileRoundTripPersistsIndependentStatsAndRejectsInvalidData()
        {
            string folder=Path.Combine(Path.GetTempPath(),"vkt-stats-"+Guid.NewGuid().ToString("N"));
            string path=Path.Combine(folder,"profile.json");
            try
            {
                var progression=new CityProgression(new CitySession(321));
                progression.CreateCharacter("Rodja");
                progression.Stats.RecordMovement(12.5f,false,false);
                progression.Stats.RecordMovement(7.5f,true,false);
                progression.Stats.Tick(60);
                var snapshot=progression.Snapshot();
                progression.Stats.RecordMovement(100,false,false);
                Assert.That(snapshot.stats.totalDistanceWalked,Is.EqualTo(12.5f));
                var store=new CitySaveStore(path);store.Save(snapshot);
                Assert.That(new CitySaveStore(path).TryLoad(out var loaded,out _),Is.True);
                var restored=new CityProgression(CitySession.Restore(loaded),loaded);
                Assert.That(restored.Stats.TotalDistance,Is.EqualTo(20));
                Assert.That(restored.Stats.TotalPlayTimeSeconds,Is.EqualTo(60));
                Assert.That(restored.Session.Cash,Is.EqualTo(321));
                loaded.stats.totalDistanceWalked=float.NaN;
                Assert.Throws<ArgumentException>(()=>CityProgression.Validate(loaded));
            }
            finally { if(Directory.Exists(folder))Directory.Delete(folder,true); }
        }
        [Test]
        public void ExtremeMovementAndTimeRemainFiniteAndValid()
        {
            var stats = new CityPlayerStats();
            stats.RecordMovement(float.MaxValue, false, false);
            stats.RecordMovement(float.MaxValue, true, false);
            stats.RecordMovement(float.MaxValue, false, true);
            stats.Tick(float.MaxValue);
            stats.Tick(float.MaxValue);
            Assert.That(float.IsInfinity(stats.TotalDistance), Is.False);
            Assert.DoesNotThrow(() => CityPlayerStats.Validate(stats.Snapshot()));
        }

        [Test]
        public void InvalidMissionDurationCannotPoisonBestTime()
        {
            var stats = new CityPlayerStats();
            stats.RecordMissionOutcome(true, float.PositiveInfinity);
            Assert.That(stats.FastestMissionSeconds, Is.Zero);
            Assert.DoesNotThrow(() => CityPlayerStats.Validate(stats.Snapshot()));
        }

        [Test]
        public void EventCountersSaturateInsteadOfWrappingNegative()
        {
            var stats = new CityPlayerStats(new CityStatsData {totalDeaths=int.MaxValue,totalArrests=int.MaxValue,totalEscapes=int.MaxValue,totalMissionsCompleted=int.MaxValue,totalMissionsFailed=int.MaxValue});
            stats.RecordDeath(); stats.RecordArrest(); stats.RecordEscape();
            stats.RecordMissionOutcome(true, 1); stats.RecordMissionOutcome(false, 1);
            Assert.DoesNotThrow(() => CityPlayerStats.Validate(stats.Snapshot()));
        }
        [Test]
        public void DefaultStatsAreZeroAndValid()
        {
            var stats = new CityPlayerStats();
            Assert.That(stats.TotalDistance, Is.Zero);
            Assert.That(stats.TotalMissionsCompleted, Is.Zero);
            Assert.That(stats.TotalDeaths, Is.Zero);
            Assert.That(stats.FormatSummary(), Does.Contain("POŽAREVCA"));
        }

        [Test]
        public void RecordingMovementAccumulatesCategories()
        {
            var stats = new CityPlayerStats();
            stats.RecordMovement(100f, false, false);
            stats.RecordMovement(50f, true, false);
            stats.RecordMovement(200f, false, true);

            Assert.That(stats.TotalDistanceWalked, Is.EqualTo(100f));
            Assert.That(stats.TotalDistanceSprinted, Is.EqualTo(50f));
            Assert.That(stats.TotalDistanceRidden, Is.EqualTo(200f));
            Assert.That(stats.TotalDistance, Is.EqualTo(350f));
        }

        [Test]
        public void SurvivalStreakResetsOnDeathOrArrest()
        {
            var stats = new CityPlayerStats();
            stats.Tick(120f);
            Assert.That(stats.CurrentSurvivalStreak, Is.EqualTo(120f));
            Assert.That(stats.LongestSurvivalStreak, Is.EqualTo(120f));

            stats.RecordDeath();
            Assert.That(stats.CurrentSurvivalStreak, Is.Zero);
            Assert.That(stats.LongestSurvivalStreak, Is.EqualTo(120f));
            Assert.That(stats.TotalDeaths, Is.EqualTo(1));

            stats.Tick(60f);
            Assert.That(stats.CurrentSurvivalStreak, Is.EqualTo(60f));
            Assert.That(stats.LongestSurvivalStreak, Is.EqualTo(120f));

            stats.RecordArrest();
            Assert.That(stats.CurrentSurvivalStreak, Is.Zero);
            Assert.That(stats.TotalArrests, Is.EqualTo(1));
        }

        [Test]
        public void FastestMissionUpdatesOnlyWhenBetter()
        {
            var stats = new CityPlayerStats();
            stats.RecordMissionOutcome(true, 45f);
            Assert.That(stats.FastestMissionSeconds, Is.EqualTo(45f));

            stats.RecordMissionOutcome(true, 60f);
            Assert.That(stats.FastestMissionSeconds, Is.EqualTo(45f));

            stats.RecordMissionOutcome(true, 30f);
            Assert.That(stats.FastestMissionSeconds, Is.EqualTo(30f));
            Assert.That(stats.TotalMissionsCompleted, Is.EqualTo(3));
        }

        [Test]
        public void SnapshotPreservesIndependentData()
        {
            var stats = new CityPlayerStats();
            stats.RecordCashEarned(500);
            stats.RecordCashSpent(200);

            var snap = stats.Snapshot();
            Assert.That(snap.totalDinarsEarned, Is.EqualTo(500));
            Assert.That(snap.totalDinarsSpent, Is.EqualTo(200));

            stats.RecordCashEarned(1000);
            Assert.That(snap.totalDinarsEarned, Is.EqualTo(500));
        }

        [Test]
        public void ValidationRejectsNegativeValues()
        {
            var bad = new CityStatsData { totalDeaths = -1 };
            Assert.Throws<ArgumentException>(() => CityPlayerStats.Validate(bad));
        }
    }
}
