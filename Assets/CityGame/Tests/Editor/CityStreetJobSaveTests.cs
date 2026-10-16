using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityStreetJobSaveTests
    {
        [Serializable] private sealed class TestEnvelope { public int version = 1; public string payload; public string sha256; }
        private string folder;
        [SetUp] public void SetUp() => folder = Path.Combine(Path.GetTempPath(), "VKT-street-save-" + Guid.NewGuid().ToString("N"));
        [TearDown] public void TearDown() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        private static CityProgression Campaign()
        {
            CityProgression p = new CityProgression(new CitySession(1000)); p.CreateCharacter("Yashimi");
            p.CompleteContract(1); p.CompleteContract(2); p.CompleteContract(3);
            return p;
        }
        private static void Finish(CityStreetJobs jobs)
        {
            Assert.That(jobs.TryAccept(), Is.True);
            for (int step = 0; step < 3; step++) Assert.That(jobs.TryVisit(jobs.Offer.StationAt(step)), Is.True);
            jobs.Tick(30);
        }
        private static void WriteEnvelope(string path, string payload)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string hash;
            using (SHA256 sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", "").ToLowerInvariant();
            File.WriteAllText(path, JsonUtility.ToJson(new TestEnvelope { payload = payload, sha256 = hash }));
        }
        [Test]
        public void RealFileRoundTripPreservesChapterInventoryAndDoesNotReplayReward()
        {
            CityProgression p = Campaign(); CityStreetJobs jobs = new CityStreetJobs(p.Session); p.BindStreetJobs(jobs);
            p.Garden.BuySeed(0);
            for (int i = 0; i < 3; i++) Finish(jobs);
            Assert.That(jobs.StoryChapter, Is.EqualTo(2));
            jobs.TryAccept(); jobs.TryVisit(jobs.Offer.StationAt(0));
            int cash = p.Session.Cash, reputation = p.Session.Reputation;
            string path = Path.Combine(folder, "profile.json"); new CitySaveStore(path).Save(p.Snapshot());
            Assert.That(new CitySaveStore(path).TryLoad(out CityProfileData loaded, out _), Is.True);
            Assert.That(loaded.version, Is.EqualTo(CityProgression.CurrentProfileVersion));
            CityProgression restored = new CityProgression(CitySession.Restore(loaded), loaded);
            CityStreetJobs restarted = new CityStreetJobs(restored.Session); restored.BindStreetJobs(restarted);
            Assert.That(restarted.CompletedJobs, Is.EqualTo(3)); Assert.That(restarted.StoryChapter, Is.EqualTo(2));
            Assert.That(restarted.IsActive, Is.False); Assert.That(restarted.ObjectiveIndex, Is.Zero);
            Assert.That(restarted.TryVisit(0), Is.False);
            Assert.That(restored.Session.Cash, Is.EqualTo(cash)); Assert.That(restored.Session.Reputation, Is.EqualTo(reputation));
            Assert.That(restored.Garden.Seeds(0), Is.EqualTo(1));
        }
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void OlderProfilesDefaultToZeroAndUpgradeWithoutLosingGarden(int version)
        {
            CityProfileData old = Campaign().Snapshot(); old.version = version;
            string path = Path.Combine(folder, "old.json");
            WriteEnvelope(path, JsonUtility.ToJson(old).Replace("\"completedStreetJobs\":0,", ""));
            Assert.That(new CitySaveStore(path).TryLoad(out CityProfileData loaded, out _), Is.True);
            CityProgression restored = new CityProgression(CitySession.Restore(loaded), loaded);
            CityStreetJobs jobs = new CityStreetJobs(restored.Session); restored.BindStreetJobs(jobs);
            Assert.That(jobs.CompletedJobs, Is.Zero); Assert.That(jobs.StoryChapter, Is.EqualTo(1));
            Assert.That(restored.Snapshot().version, Is.EqualTo(CityProgression.CurrentProfileVersion));
        }
        [Test]
        public void CashObserverSnapshotsAlreadyContainCompletedCountAndCompletionEventCanSaveImmediately()
        {
            CityProgression p = Campaign(); CityStreetJobs jobs = new CityStreetJobs(p.Session); p.BindStreetJobs(jobs);
            CityProfileData observed = null;
            p.Session.Changed += () => observed = p.Snapshot();
            string path = Path.Combine(folder, "immediate.json"); CitySaveStore writer = new CitySaveStore(path);
            jobs.Completed += count => writer.Save(p.Snapshot());
            Finish(jobs);
            Assert.That(observed.completedStreetJobs, Is.EqualTo(1));
            Assert.That(new CitySaveStore(path).TryLoad(out CityProfileData loaded, out _), Is.True);
            Assert.That(loaded.completedStreetJobs, Is.EqualTo(1)); Assert.That(loaded.cash, Is.EqualTo(p.Session.Cash));
        }
        [TestCase(-1)] [TestCase(1000001)]
        public void RejectsMalformedBoundedCompletedCounts(int count)
        {
            CityProfileData corrupt = Campaign().Snapshot(); corrupt.completedStreetJobs = count;
            Assert.Throws<ArgumentOutOfRangeException>(() => CityProgression.Validate(corrupt));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CityStreetJobs(new CitySession(1000), completedJobs: count));
        }
        [Test]
        public void RejectsProgressBeforeCampaignAndPreservesFutureProfileOnDisk()
        {
            CityProfileData invalid = Campaign().Snapshot(); invalid.completedContracts = 2; invalid.completedStreetJobs = 1;
            Assert.Throws<ArgumentException>(() => CityProgression.Validate(invalid));
            Directory.CreateDirectory(folder);
            CityProfileData future = Campaign().Snapshot(); future.version = CityProgression.CurrentProfileVersion + 1;
            string payload = JsonUtility.ToJson(future);
            string hash;
            using (SHA256 sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", "").ToLowerInvariant();
            string text = JsonUtility.ToJson(new TestEnvelope { payload = payload, sha256 = hash });
            string path = Path.Combine(folder, "future.json"); File.WriteAllText(path, text);
            CitySaveStore store = new CitySaveStore(path);
            Assert.That(store.TryLoad(out _, out _), Is.False); Assert.That(store.CanWrite, Is.False);
            Assert.Throws<InvalidOperationException>(() => store.Save(Campaign().Snapshot()));
            Assert.That(File.ReadAllText(path), Is.EqualTo(text));
        }
        [Test]
        public void UpperBoundSaturatesWithoutInvalidatingFutureSaves()
        {
            CityProfileData saved = Campaign().Snapshot(); saved.completedStreetJobs = CityStreetJobs.MaximumCompletedJobs;
            CityProgression restored = new CityProgression(CitySession.Restore(saved), saved);
            CityStreetJobs jobs = new CityStreetJobs(restored.Session); restored.BindStreetJobs(jobs); Finish(jobs);
            Assert.That(jobs.CompletedJobs, Is.EqualTo(CityStreetJobs.MaximumCompletedJobs));
            CityProgression.Validate(restored.Snapshot());
        }
    }
}
