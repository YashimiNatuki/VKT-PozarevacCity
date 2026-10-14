using System;
using System.IO;
using NUnit.Framework;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityProgressionTests
    {
        private string directory;
        [SetUp] public void SetUp() => directory = Path.Combine(Path.GetTempPath(), "vkt-test-" + Guid.NewGuid().ToString("N"));
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        private static CityProgression Player(int cash = 2500)
        {
            CityProgression p = new CityProgression(new CitySession(cash));
            Assert.That(p.CreateCharacter("Yashimi"), Is.True);
            return p;
        }
        private static void Campaign(CityProgression p)
        {
            CityMissionContract[] contracts = CityMissionDirector.CreateContracts();
            for (int i = 0; i < contracts.Length; i++)
            {
                CityMissionRun run = new CityMissionRun(contracts[i], p.Session);
                for (int j = 0; j < 3; j++) Assert.That(run.TryVisit(j), Is.True);
                Assert.That(p.CompleteContract(i + 1), Is.True);
            }
        }

        [Test] public void UnlocksPhoneOnlyAfterCharacterAndOrderedContract()
        {
            CityProgression p = new CityProgression(new CitySession(100));
            Assert.That(p.CompleteContract(1), Is.False);
            Assert.That(p.BuySeed(), Is.False);
            Assert.That(p.CreateCharacter("Yashimi"), Is.True);
            Assert.That(p.CreateCharacter("Replacement"), Is.False);
            Assert.That(p.CompleteContract(2), Is.False);
            Assert.That(p.CompleteContract(1), Is.True);
            Assert.That(p.PhoneUnlocked, Is.True);
            Assert.That(p.CompleteContract(1), Is.False);
            Assert.That(p.BuySeed(), Is.True);
        }

        [Test] public void CompletesGrowthTradeCrewAndDistrictCampaignWithoutDuplicateRewards()
        {
            CityProgression p = Player(); Campaign(p);
            Assert.That(p.Session.Cash, Is.EqualTo(5600));
            Assert.That(p.Session.Reputation, Is.EqualTo(55));
            Assert.That(p.BuySeed(), Is.True); Assert.That(p.Plant(), Is.True);
            Assert.That(p.Plant(), Is.False); Assert.That(p.Harvest(), Is.False);
            Assert.That(p.Tick(29), Is.False); Assert.That(p.GrowSeconds, Is.EqualTo(1));
            Assert.That(p.Tick(1), Is.True); Assert.That(p.Harvest(), Is.True);
            Assert.That(p.Harvest(), Is.False); Assert.That(p.Sell(), Is.True); Assert.That(p.Sell(), Is.False);
            Assert.That(p.Session.Cash, Is.EqualTo(5665));
            Assert.That(p.ClaimTerritory(), Is.False);
            Assert.That(p.Recruit(), Is.True); Assert.That(p.ClaimTerritory(), Is.False); Assert.That(p.Recruit(), Is.True);
            for (int i = 0; i < 3; i++) Assert.That(p.ClaimTerritory(), Is.True);
            Assert.That(p.ClaimTerritory(), Is.False);
            Assert.That(p.Session.Cash, Is.EqualTo(2915));
            Assert.That(p.Territories, Is.EqualTo(3));
            CityProgression.Validate(p.Snapshot());
        }

        [Test] public void RespectsInventoryAndAffordabilityLimits()
        {
            CityProgression poor = Player(39); poor.CompleteContract(1);
            Assert.That(poor.BuySeed(), Is.False); Assert.That(poor.Session.Cash, Is.EqualTo(39));
            CityProgression p = Player(10000); p.CompleteContract(1);
            for (int i = 0; i < 30; i++) Assert.That(p.BuySeed(), Is.True);
            Assert.That(p.BuySeed(), Is.False); Assert.That(p.Seeds, Is.EqualTo(30));
            Assert.That(p.Recruit(), Is.False);
        }

        [Test] public void GrowthUsesElapsedGameTimeAndDoesNotAdvanceOnZeroTick()
        {
            CityProgression p = Player(); p.CompleteContract(1); p.BuySeed(); p.Plant();
            p.Tick(0); Assert.That(p.GrowSeconds, Is.EqualTo(30));
            for (int i = 0; i < 29; i++) p.Tick(1);
            Assert.That(p.HarvestReady, Is.False);
            p.Tick(1); Assert.That(p.HarvestReady, Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => p.Tick(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => p.Tick(-1));
        }

        [TestCase("x")] [TestCase("<b>name</b>")] [TestCase("bad/name")] [TestCase("")]
        public void RejectsInvalidCharacterNames(string name) => Assert.That(new CityProgression(new CitySession(100)).CreateCharacter(name), Is.False);

        [Test] public void SnapshotsDoNotExposeMutableProgression()
        {
            CityProgression p = Player(); p.CompleteContract(1);
            CityProfileData copy = p.Snapshot(); copy.callsign = "Changed"; copy.completedContracts = 3;
            Assert.That(p.Callsign, Is.EqualTo("Yashimi")); Assert.That(p.CompletedContracts, Is.EqualTo(1));
        }

        [Test] public void PurchaseStateIsCommittedBeforeReentrantObservers()
        {
            CityProgression p = Player(10000); Campaign(p);
            p.Session.Changed += () => p.Recruit();
            p.Recruit();
            Assert.That(p.Crew, Is.EqualTo(3));
            Assert.That(p.Session.Cash, Is.EqualTo(12350));
            CityProgression.Validate(p.Snapshot());
        }

        [Test] public void RestoresCompletedRouteWithoutPayingAgain()
        {
            CityProgression p = Player(); Campaign(p);
            CityProfileData saved = p.Snapshot();
            CitySession restored = CitySession.Restore(saved);
            CityMissionRun run = CityMissionRun.RestoreCompleted(CityMissionDirector.CreateContracts()[2], restored);
            Assert.That(run.TryVisit(2), Is.False);
            Assert.That(run.State, Is.EqualTo(CityMissionState.Completed));
            Assert.That(restored.Cash, Is.EqualTo(5600));
        }

        [Test] public void SavesAndRestoresCharacterEconomyAndGrowingPlot()
        {
            CityProgression p = Player(); Campaign(p); p.BuySeed(); p.Plant(); p.Tick(12);
            string file = Path.Combine(directory, "profile.json");
            CitySaveStore writer = new CitySaveStore(file); writer.Save(p.Snapshot());
            CitySaveStore reader = new CitySaveStore(file);
            Assert.That(reader.TryLoad(out CityProfileData saved, out _), Is.True);
            CityProgression restored = new CityProgression(CitySession.Restore(saved), saved);
            Assert.That(restored.GrowSeconds, Is.EqualTo(18));
            Assert.That(restored.Callsign, Is.EqualTo("Yashimi"));
            Assert.That(restored.CompletedContracts, Is.EqualTo(3));
            Assert.That(restored.Session.Cash, Is.EqualTo(5560));
        }

        [Test] public void RecoversBackupAndPreservesDamagedPrimary()
        {
            string file = Path.Combine(directory, "profile.json");
            CityProgression p = Player(); p.CompleteContract(1);
            CitySaveStore writer = new CitySaveStore(file); writer.Save(p.Snapshot());
            p.BuySeed(); writer.Save(p.Snapshot()); File.WriteAllText(file, "damaged-original");
            CitySaveStore reader = new CitySaveStore(file);
            Assert.That(reader.TryLoad(out CityProfileData recovered, out _), Is.True);
            Assert.That(recovered.seeds, Is.EqualTo(0));
            reader.Save(recovered);
            Assert.That(Directory.GetFiles(directory, "*.recovery-*").Length, Is.EqualTo(2));
            Assert.That(reader.TryLoad(out _, out _), Is.True);
        }

        [Test] public void RefusesToOverwriteUnreadableAndFutureSaves()
        {
            Directory.CreateDirectory(directory); string file = Path.Combine(directory, "profile.json");
            foreach (string contents in new[] { "broken", "{\"version\":2,\"payload\":\"future\"}" })
            {
                File.WriteAllText(file, contents);
                CitySaveStore reader = new CitySaveStore(file);
                Assert.That(reader.TryLoad(out _, out _), Is.False);
                Assert.That(reader.CanWrite, Is.False);
                Assert.Throws<InvalidOperationException>(() => reader.Save(Player().Snapshot()));
                Assert.That(File.ReadAllText(file), Is.EqualTo(contents));
            }
        }

        [Test] public void RejectsInvalidProfileBalancesAndUnlockState()
        {
            CityProfileData data = Player().Snapshot(); data.cash = -1;
            Assert.Throws<ArgumentException>(() => CityProgression.Validate(data));
            data = Player().Snapshot(); data.growSeconds = double.PositiveInfinity;
            Assert.Throws<ArgumentException>(() => CityProgression.Validate(data));
            data = Player().Snapshot(); data.territories = 1;
            Assert.Throws<ArgumentException>(() => CityProgression.Validate(data));
        }

        [Test] public void RejectsStaleWritesFromAnotherGameInstance()
        {
            string file = Path.Combine(directory, "profile.json");
            CityProgression original = Player(); new CitySaveStore(file).Save(original.Snapshot());
            CitySaveStore first = new CitySaveStore(file), second = new CitySaveStore(file);
            Assert.That(first.TryLoad(out CityProfileData a, out _), Is.True);
            Assert.That(second.TryLoad(out CityProfileData b, out _), Is.True);
            CityProgression updated = new CityProgression(CitySession.Restore(a), a);
            updated.Session.Earn(100); first.Save(updated.Snapshot());
            Assert.Throws<InvalidOperationException>(() => second.Save(b));
            Assert.That(new CitySaveStore(file).TryLoad(out CityProfileData latest, out _), Is.True);
            Assert.That(latest.cash, Is.EqualTo(2600));
        }
    }
}
