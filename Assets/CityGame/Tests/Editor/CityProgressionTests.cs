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

    public sealed class CityStarterGoalsTests
    {
        private static CityProgression Player(bool unlocked = true)
        {
            var player = new CityProgression(new CitySession(2500));
            player.CreateCharacter("Starter Test");
            if (unlocked) player.CompleteContract(1);
            return player;
        }
        private static System.Reflection.MethodInfo Method(string name)
        {
            var method = typeof(CityProgression).GetMethod(name);
            Assert.That(method, Is.Not.Null, name + " must use the existing progression owner.");
            return method;
        }
        private static bool Visit(CityProgression player) => (bool)Method("RecordHideoutVisit").Invoke(player, null);
        private static bool Water(CityProgression player, int plot, long utc) =>
            (bool)Method("WaterGardenPlot").Invoke(player, new object[] { plot, utc });
        private static System.Reflection.FieldInfo GoalsField()
        {
            var field = typeof(CityProfileData).GetField("starterGoals");
            Assert.That(field, Is.Not.Null, "Persistent starter milestones need a versioned profile field.");
            return field;
        }
        private static object Goals(CityProgression player)
        {
            var property = typeof(CityProgression).GetProperty("StarterGoals");
            Assert.That(property, Is.Not.Null);
            object result = property.GetValue(player);
            Assert.That(result, Is.Not.Null);
            return result;
        }
        private static bool Flag(object goals, string name) => (bool)goals.GetType().GetField(name).GetValue(goals);
        private static void SetFlag(object goals, string name, bool value) => goals.GetType().GetField(name).SetValue(goals, value);

        [Test]
        public void VisitRequiresTheExistingUnlockAndNeverChangesEconomyOrTraining()
        {
            var player = Player(false);
            Assert.That(Visit(player), Is.False);
            Assert.That(Flag(Goals(player), "hideoutVisited"), Is.False);
            player.CompleteContract(1);
            int cash = player.Session.Cash, reputation = player.Session.Reputation, xp = player.Lifestyle.Experience;
            Assert.That(Visit(player), Is.True);
            Assert.That(Visit(player), Is.False);
            Assert.That(Flag(Goals(player), "hideoutVisited"), Is.True);
            Assert.That(Flag(Goals(player), "firstPlantWatered"), Is.False);
            Assert.That(player.Session.Cash, Is.EqualTo(cash));
            Assert.That(player.Session.Reputation, Is.EqualTo(reputation));
            Assert.That(player.Lifestyle.Experience, Is.EqualTo(xp));
            Assert.That(player.CompletedContracts, Is.EqualTo(1));
        }

        [Test]
        public void ExplicitSuccessfulCatalogWateringLatchesCareWithoutAnotherReward()
        {
            var player = Player(); Visit(player);
            Assert.That(player.Garden.BuySeed(0), Is.True);
            Assert.That(player.Garden.PlantFromInventory(0, 0, 1000), Is.True);
            Assert.That(Flag(Goals(player), "firstPlantWatered"), Is.False, "Planting is not explicit care.");
            int cash = player.Session.Cash, reputation = player.Session.Reputation, xp = player.Lifestyle.Experience;
            Assert.That(Water(player, 0, 1000), Is.True);
            Assert.That(Flag(Goals(player), "firstPlantWatered"), Is.True);
            Assert.That(player.Garden.Plot(0).moistureSeconds, Is.EqualTo(CityGarden.WaterDuration));
            Assert.That(Water(player, 0, 1000), Is.False);
            Assert.That(Water(player, 1, 1000), Is.False);
            Assert.That(player.Session.Cash, Is.EqualTo(cash));
            Assert.That(player.Session.Reputation, Is.EqualTo(reputation));
            Assert.That(player.Lifestyle.Experience, Is.EqualTo(xp));
        }

        [Test]
        public void CareDoesNotInferAVisitOrCountTheLegacyTrialPlant()
        {
            var unvisited = Player();
            unvisited.Garden.BuySeed(0); unvisited.Garden.PlantFromInventory(0, 0, 1000);
            Assert.That(Water(unvisited, 0, 1000), Is.True);
            Assert.That(Flag(Goals(unvisited), "firstPlantWatered"), Is.False);
            Assert.That(Flag(Goals(unvisited), "hideoutVisited"), Is.False);
            var legacy = Player(); Visit(legacy); legacy.Garden.Plant(0, 0, 1000);
            Assert.That(Water(legacy, 0, 1000), Is.True);
            Assert.That(Flag(Goals(legacy), "firstPlantWatered"), Is.False);
        }

        [Test]
        public void StarterSnapshotsAndPublicViewAreDeepCopies()
        {
            var player = Player(); Visit(player);
            object exposed = Goals(player); SetFlag(exposed, "hideoutVisited", false);
            Assert.That(Flag(Goals(player), "hideoutVisited"), Is.True);
            object saved = GoalsField().GetValue(player.Snapshot()); SetFlag(saved, "hideoutVisited", false);
            Assert.That(Flag(Goals(player), "hideoutVisited"), Is.True);
        }

        [TestCase(1)] [TestCase(8)] [TestCase(9)]
        public void LegacyProfilesUpgradeWithoutInventingHistoricGoals(int version)
        {
            Assert.That(CityProgression.CurrentProfileVersion, Is.EqualTo(10));
            var garden = new CityGarden(new CitySession(2500)); garden.BuySeed(0);
            var old = new CityProfileData { version=version, callsign="Legacy", cash=2440, completedContracts=1, garden=garden.Snapshot() };
            var player = new CityProgression(CitySession.Restore(old), old);
            Assert.That(Flag(Goals(player), "hideoutVisited"), Is.False);
            Assert.That(Flag(Goals(player), "firstPlantWatered"), Is.False);
            Assert.That(player.Session.Cash, Is.EqualTo(2440));
            Assert.That(player.Garden.Seeds(0), Is.EqualTo(1));
            Assert.That(player.Snapshot().version, Is.EqualTo(10));
            CityProgression.Validate(player.Snapshot());
        }

        [TestCase(10, true, false, 1)]
        [TestCase(9, false, true, 1)]
        [TestCase(10, false, true, 0)]
        public void RejectsImpossibleOrBackdatedStarterHistory(int version, bool care, bool visit, int contracts)
        {
            var saved = Player().Snapshot(); saved.version=version; saved.completedContracts=contracts;
            object goals = Activator.CreateInstance(GoalsField().FieldType);
            SetFlag(goals, "hideoutVisited", visit); SetFlag(goals, "firstPlantWatered", care);
            GoalsField().SetValue(saved, goals);
            Assert.Throws<ArgumentException>(() => CityProgression.Validate(saved));
        }

        [Test]
        public void DiskReloadPreservesVisitAndCareAfterThePlantIsHarvested()
        {
            string directory=Path.Combine(Path.GetTempPath(),"vkt-starter-"+Guid.NewGuid().ToString("N"));
            string path=Path.Combine(directory,"profile.json");
            try
            {
                var player=Player(); Visit(player);
                var first=new CitySaveStore(path); first.Save(player.Snapshot());
                var disk=new CitySaveStore(path); Assert.That(disk.TryLoad(out var saved,out _),Is.True);
                player=new CityProgression(CitySession.Restore(saved),saved);
                Assert.That(Flag(Goals(player),"hideoutVisited"),Is.True);
                Assert.That(Flag(Goals(player),"firstPlantWatered"),Is.False);
                player.Garden.BuySeed(0); player.Garden.PlantFromInventory(0,0,1000); Water(player,0,1000);
                long now=1000;
                while(!player.Garden.IsReady(0))
                {
                    now+=(long)CityGarden.WaterDuration;player.Garden.Advance(now);
                    if(!player.Garden.IsReady(0))Assert.That(Water(player,0,now),Is.True);
                }
                Assert.That(player.Garden.Harvest(0,now),Is.True);
                disk.Save(player.Snapshot());
                var reload=new CitySaveStore(path);Assert.That(reload.TryLoad(out saved,out _),Is.True);
                player=new CityProgression(CitySession.Restore(saved),saved);
                Assert.That(player.Garden.Plot(0).planted,Is.False);
                Assert.That(Flag(Goals(player),"hideoutVisited"),Is.True);
                Assert.That(Flag(Goals(player),"firstPlantWatered"),Is.True);
                Assert.That(Visit(player),Is.False);
                CityProgression.Validate(player.Snapshot());
            }
            finally { if(Directory.Exists(directory))Directory.Delete(directory,true); }
        }
    }
}
