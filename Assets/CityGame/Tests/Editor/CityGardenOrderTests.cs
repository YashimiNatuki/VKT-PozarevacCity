using System;
using System.IO;
using NUnit.Framework;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityGardenOrderTests
    {
        private static CityProgression Campaign(int stock = 4, int version = 7)
        {
            var garden = new CityGarden(new CitySession(1000)).Snapshot(); garden.stock[0] = stock;
            var profile = new CityProfileData { version=version, callsign="Rodja", cash=1000, reputation=50, completedContracts=3, garden=garden };
            var progress = new CityProgression(CitySession.Restore(profile), profile);
            progress.BindStreetJobs(new CityStreetJobs(progress.Session));
            return progress;
        }

        [Test]
        public void AcceptanceConsumesRealStockOnceAndRejectsInsufficientStock()
        {
            var empty = Campaign(1); Assert.IsFalse(empty.TryAcceptGardenOrder(0));
            Assert.AreEqual(1, empty.Garden.Stock(0)); Assert.IsFalse(empty.GardenOrder.IsActive);
            var p = Campaign(); Assert.IsTrue(p.TryAcceptGardenOrder(0));
            Assert.AreEqual(2, p.Garden.Stock(0)); Assert.IsFalse(p.TryAcceptGardenOrder(0));
            Assert.AreEqual(2, p.Garden.Stock(0)); Assert.IsTrue(p.GardenOrder.HasCargo);
            Assert.IsFalse(p.GardenOrder.TrySettle()); Assert.AreEqual(1000, p.Session.Cash);
        }

        [Test]
        public void CampaignAndMutualDeliveryGatesAreAuthoritative()
        {
            var early = new CityProgression(new CitySession(1000)); early.CreateCharacter("Early");
            Assert.IsFalse(early.TryAcceptGardenOrder(0));
            var p = Campaign(); var jobs = new CityStreetJobs(p.Session);
            // The already bound instance may not be replaced.
            Assert.Throws<InvalidOperationException>(() => p.BindStreetJobs(jobs));
            var profile=p.Snapshot(); var restored=new CityProgression(CitySession.Restore(profile),profile);
            var restoredJobs=new CityStreetJobs(restored.Session);restored.BindStreetJobs(restoredJobs); Assert.IsTrue(restoredJobs.TryAccept());
            Assert.IsFalse(restored.TryAcceptGardenOrder(0)); restoredJobs.Cancel();
            Assert.IsTrue(restored.TryAcceptGardenOrder(0)); restoredJobs.Tick(31);
            Assert.IsFalse(restoredJobs.TryAccept());
            Assert.AreEqual(2, restored.Garden.Stock(0));
        }

        [Test]
        public void DiskReloadResumesCargoAndReceiptAndSettlementCannotReplay()
        {
            string folder=Path.Combine(Path.GetTempPath(),"VKT-order-test-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder); string path=Path.Combine(folder,"profile.json");
            try
            {
                var p=Campaign(); Assert.IsTrue(p.TryAcceptGardenOrder(0)); p.GardenOrder.Tick(7.5);
                var store=new CitySaveStore(path);store.Save(p.Snapshot());
                var loader=new CitySaveStore(path);Assert.IsTrue(loader.TryLoad(out var saved,out _));
                var resumed=new CityProgression(CitySession.Restore(saved),saved);
                Assert.IsTrue(resumed.GardenOrder.HasCargo);Assert.AreEqual(172.5,resumed.GardenOrder.RemainingSeconds);
                Assert.AreEqual(2,resumed.Garden.Stock(0));Assert.IsTrue(resumed.GardenOrder.TryDeliver());
                loader.Save(resumed.Snapshot());
                var receiptStore=new CitySaveStore(path);Assert.IsTrue(receiptStore.TryLoad(out var receipt,out _));
                var final=new CityProgression(CitySession.Restore(receipt),receipt);
                Assert.AreEqual(CityGardenOrderPhase.Receipt,final.GardenOrder.Phase);
                Assert.IsFalse(final.GardenOrder.TryDeliver());int reward=final.GardenOrder.CashReward;
                final.Session.Changed+=()=>receiptStore.Save(final.Snapshot());
                Assert.IsTrue(final.GardenOrder.TrySettle());Assert.AreEqual(1000+reward,final.Session.Cash);
                Assert.IsFalse(final.GardenOrder.TrySettle());Assert.AreEqual(1,final.GardenOrder.Completed);
                var reloadedStore=new CitySaveStore(path);Assert.IsTrue(reloadedStore.TryLoad(out var paid,out _));
                var paidProgress=new CityProgression(CitySession.Restore(paid),paid);
                Assert.AreEqual(1000+reward,paidProgress.Session.Cash);Assert.IsFalse(paidProgress.GardenOrder.TrySettle());
                Assert.AreEqual(2,paidProgress.Garden.Stock(0));
            }
            finally { foreach(string file in Directory.EnumerateFiles(folder))File.Delete(file);Directory.Delete(folder); }
        }

        [Test]
        public void CancellationAndTimeoutNeverRefundOrPay()
        {
            var p=Campaign();Assert.IsTrue(p.TryAcceptGardenOrder(0));Assert.IsTrue(p.GardenOrder.Cancel());
            Assert.AreEqual(2,p.Garden.Stock(0));Assert.AreEqual(1000,p.Session.Cash);
            Assert.IsFalse(p.GardenOrder.Cancel());Assert.IsFalse(p.TryAcceptGardenOrder(0));
            p.GardenOrder.Tick(15);Assert.IsTrue(p.TryAcceptGardenOrder(0));Assert.IsTrue(p.GardenOrder.TryDeliver());
            p.GardenOrder.Tick(CityGardenOrder.TimeLimit);
            Assert.IsFalse(p.GardenOrder.IsActive);Assert.IsFalse(p.GardenOrder.TrySettle());
            Assert.AreEqual(0,p.Garden.Stock(0));Assert.AreEqual(1000,p.Session.Cash);Assert.AreEqual(0,p.GardenOrder.Completed);
        }

        [Test]
        public void PursuitAndHeatBlockContactsWithoutConsumingMoreStock()
        {
            var p=Campaign();p.Session.AddHeat(46);Assert.IsFalse(p.TryAcceptGardenOrder(0));
            p.Session.CoolDown(46);Assert.IsTrue(p.TryAcceptGardenOrder(0));p.Lifestyle.ReportCrime();
            Assert.IsFalse(p.GardenOrder.TryDeliver());Assert.AreEqual(2,p.Garden.Stock(0));
            p.Lifestyle.TickPolice(19,false,false);p.Session.CoolDown(p.Session.Heat);
            Assert.IsTrue(p.GardenOrder.TryDeliver());
        }

        [Test]
        public void FullWalletRetainsReceiptUntilRewardFits()
        {
            var p=Campaign();Assert.IsTrue(p.TryAcceptGardenOrder(0));Assert.IsTrue(p.GardenOrder.TryDeliver());
            int reward=p.GardenOrder.CashReward;p.Session.Earn(int.MaxValue);
            Assert.IsFalse(p.GardenOrder.TrySettle());Assert.AreEqual(CityGardenOrderPhase.Receipt,p.GardenOrder.Phase);
            Assert.IsTrue(p.Session.TrySpend(reward));Assert.IsTrue(p.GardenOrder.TrySettle());
            Assert.AreEqual(int.MaxValue,p.Session.Cash);Assert.AreEqual(1,p.GardenOrder.Completed);
        }

        [Test]
        public void LegacyV7MigratesWithoutChangingBalancesInventoryOrRoutes()
        {
            var p=Campaign();
            Assert.AreEqual(8,p.Snapshot().version);Assert.AreEqual(1000,p.Session.Cash);Assert.AreEqual(4,p.Garden.Stock(0));
            var profile=p.Snapshot();profile.version=7;profile.gardenOrder=null;
            var restored=new CityProgression(CitySession.Restore(profile),profile);
            var restoredJobs=new CityStreetJobs(restored.Session);restored.BindStreetJobs(restoredJobs);
            Assert.AreEqual(profile.streetJob.offerSeed,restored.Snapshot().streetJob.offerSeed);
            Assert.AreEqual(p.Snapshot().streetJob.objectiveIndex,restoredJobs.ObjectiveIndex);
            Assert.IsFalse(restored.GardenOrder.IsActive);Assert.AreEqual(4,restored.Garden.Stock(0));
        }

        [Test]
        public void InvalidStatesAndOverlappingSavedDeliveriesAreRejected()
        {
            Assert.Throws<ArgumentException>(()=>CityGardenOrder.Validate(new CityGardenOrderData{quantity=2}));
            Assert.Throws<ArgumentException>(()=>CityGardenOrder.Validate(new CityGardenOrderData{phase=CityGardenOrderPhase.Receipt,remainingSeconds=10}));
            var p=Campaign();Assert.Throws<ArgumentOutOfRangeException>(()=>p.GardenOrder.Tick(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(()=>p.GardenOrder.Tick(-1));
            Assert.IsTrue(p.TryAcceptGardenOrder(0));var snapshot=p.Snapshot();snapshot.version=7;
            Assert.Throws<ArgumentException>(()=>CityProgression.Validate(snapshot));snapshot.version=8;
            snapshot.streetJob.active=true;snapshot.streetJob.remainingSeconds=10;
            Assert.Throws<ArgumentException>(()=>CityProgression.Validate(snapshot));
        }
    }
}
