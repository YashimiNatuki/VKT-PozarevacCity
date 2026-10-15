using System;
using System.IO;
using NUnit.Framework;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityGardenTests
    {
        [Test]
        public void DryPlantsPauseAndOfflineGrowthIsBoundedByWater()
        {
            CityGarden garden = new CityGarden(new CitySession(1000));
            Assert.That(garden.Plant(0, 0, 1000), Is.True);
            garden.Advance(10000);
            Assert.That(garden.Plot(0).grownSeconds, Is.Zero);
            Assert.That(garden.Water(0, 10000), Is.True);
            Assert.That(garden.Water(0, 10000), Is.False);
            garden.Advance(1000000);
            Assert.That(garden.Plot(0).grownSeconds, Is.EqualTo(7200));
            Assert.That(garden.NeedsWater(0), Is.True);
            Assert.That(garden.IsReady(0), Is.False);
        }

        [Test]
        public void FullCareLoopPaysOnceAndSupportsFourIndependentPlots()
        {
            CitySession session = new CitySession(1000);
            CityGarden garden = new CityGarden(session);
            for (int i = 0; i < 4; i++) Assert.That(garden.Plant(i, 0, 1000), Is.True);
            Assert.That(session.Cash, Is.EqualTo(760));
            Assert.That(garden.Plant(0, 1, 1000), Is.False);
            for (int watering = 0; watering < 3; watering++)
            {
                long now = 1000 + watering * 7200;
                Assert.That(garden.Water(0, now), Is.True);
                garden.Advance(now + 7200);
            }
            Assert.That(garden.IsReady(0), Is.True);
            Assert.That(garden.Plot(1).grownSeconds, Is.Zero);
            Assert.That(garden.Harvest(0, 22600), Is.True);
            Assert.That(session.Cash, Is.EqualTo(940));
            Assert.That(garden.Harvest(0, 22600), Is.False);
            Assert.That(garden.Harvests, Is.EqualTo(1));
        }

        [Test]
        public void BackwardClockCannotCreateWaterOrGrowthAndSnapshotIsIsolated()
        {
            CityGarden garden = new CityGarden(new CitySession(100));
            garden.Plant(0, 1, 1000); garden.Water(0, 1000); garden.Advance(1100);
            Assert.That(garden.Advance(1050), Is.False);
            garden.Advance(1200);
            Assert.That(garden.Plot(0).grownSeconds, Is.EqualTo(200));
            CityGardenData copy = garden.Snapshot(); copy.plots[0].grownSeconds = 10000;
            Assert.That(garden.Plot(0).grownSeconds, Is.EqualTo(200));
        }

        [Test]
        public void RejectsCorruptStateAndInsufficientFunds()
        {
            CityGarden garden = new CityGarden(new CitySession(59));
            Assert.That(garden.Plant(0, 0, 1000), Is.False);
            Assert.That(garden.Plot(0).planted, Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => garden.Plant(4, 0, 1000));
            Assert.Throws<ArgumentOutOfRangeException>(() => garden.Plant(0, 99, 1000));
            CityGardenData saved = garden.Snapshot(); saved.plots[0].moistureSeconds = double.NaN;
            Assert.Throws<ArgumentException>(() => new CityGarden(new CitySession(100), saved));
        }

        [Test]
        public void VersionOneProfileMigratesAndGardenSurvivesRealFileRestore()
        {
            string folder = Path.Combine(Path.GetTempPath(), "VKT-garden-tests", Guid.NewGuid().ToString("N"));
            try
            {
                CityProfileData old = new CityProfileData { version = 1, cash = 1000, callsign = "Garden", completedContracts = 1 };
                CityProgression p = new CityProgression(CitySession.Restore(old), old);
                p.Garden.Plant(2, 2, 1000); p.Garden.Water(2, 1000); p.Garden.Advance(1300);
                CitySaveStore store = new CitySaveStore(Path.Combine(folder, "profile.json"));
                store.Save(p.Snapshot());
                Assert.That(store.TryLoad(out CityProfileData loaded, out _), Is.True);
                Assert.That(loaded.version, Is.EqualTo(CityProgression.CurrentProfileVersion));
                CityProgression restored = new CityProgression(CitySession.Restore(loaded), loaded);
                Assert.That(restored.Garden.Plot(2).variety, Is.EqualTo(2));
                Assert.That(restored.Garden.Plot(2).grownSeconds, Is.EqualTo(300));
                Assert.That(restored.Session.Cash, Is.EqualTo(940));
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
    }
}
