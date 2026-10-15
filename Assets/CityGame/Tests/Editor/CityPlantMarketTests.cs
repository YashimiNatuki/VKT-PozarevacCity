using System;
using NUnit.Framework;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityPlantMarketTests
    {
        [Test]
        public void CatalogHasEightCannabisStrainsWithFiveInitiallyAvailable()
        {
            CityGarden garden = new CityGarden(new CitySession(1000));
            Assert.That(CityPlantCatalog.Count, Is.EqualTo(8));
            for (int i = 0; i < 8; i++)
            {
                Assert.That(garden.IsUnlocked(i), Is.EqualTo(i < 5));
                if (i > 0) Assert.That(CityPlantCatalog.Get(i).SeedPrice, Is.GreaterThan(CityPlantCatalog.Get(i - 1).SeedPrice));
            }
            Assert.That(garden.BuySeed(5), Is.False);
            Assert.That(garden.PlantFromInventory(0, 0, 1000), Is.False);
        }

        [Test]
        public void StrainRenameKeepsAllEightSaveIdsAndEconomyValues()
        {
            string[] names = { "Amnesia Haze", "OG Kush", "Wedding Cake", "Purple Haze", "AK-47", "White Widow", "Sour Diesel", "Green Gelato" };
            int[] prices = { 60, 85, 110, 140, 175, 230, 310, 420 };
            int[] seconds = { 21600, 25200, 28800, 32400, 36000, 39600, 43200, 50400 };
            int[] yields = { 3, 3, 3, 4, 4, 5, 5, 6 };
            int[] values = { 60, 75, 90, 95, 115, 125, 160, 190 };
            CityGardenData saved = new CityGarden(new CitySession(1000)).Snapshot();
            for (int i = 0; i < 8; i++)
            {
                CityPlantDefinition plant = CityPlantCatalog.Get(i);
                Assert.That(plant.Name, Is.EqualTo(names[i]));
                Assert.That(plant.SeedPrice, Is.EqualTo(prices[i])); Assert.That(plant.GrowSeconds, Is.EqualTo(seconds[i]));
                Assert.That(plant.Yield, Is.EqualTo(yields[i])); Assert.That(plant.BaseUnitPrice, Is.EqualTo(values[i]));
                saved.seeds[i] = i + 1; saved.stock[i] = 8 - i;
            }
            CityGarden restored = new CityGarden(new CitySession(1000), saved);
            for (int i = 0; i < 8; i++) { Assert.That(restored.Seeds(i), Is.EqualTo(i + 1)); Assert.That(restored.Stock(i), Is.EqualTo(8 - i)); }
        }

        [Test]
        public void SeedPurchaseAndPlantingAreSeparateAndDoNotDoubleCharge()
        {
            CitySession session = new CitySession(1000);
            CityGarden garden = new CityGarden(session);
            Assert.That(garden.BuySeed(1), Is.True);
            Assert.That(session.Cash, Is.EqualTo(915));
            Assert.That(garden.Seeds(1), Is.EqualTo(1));
            Assert.That(garden.PlantFromInventory(0, 1, 1000), Is.True);
            Assert.That(garden.Seeds(1), Is.Zero);
            Assert.That(session.Cash, Is.EqualTo(915));
            garden.BuySeed(1);
            Assert.That(garden.PlantFromInventory(0, 1, 1000), Is.False);
            Assert.That(garden.Seeds(1), Is.EqualTo(1));
        }

        [Test]
        public void HarvestStoresInventoryAndBuyerDistrictChangePayout()
        {
            CitySession session = new CitySession(1000);
            CityGarden garden = new CityGarden(session);
            garden.BuySeed(0); garden.PlantFromInventory(0, 0, 1000);
            for (int step = 0; step < 3; step++) { garden.Water(0, 1000 + step * 7200); garden.Advance(1000 + (step + 1) * 7200); }
            Assert.That(garden.Harvest(0, 22600), Is.True);
            Assert.That(garden.Stock(0), Is.EqualTo(3));
            Assert.That(session.Cash, Is.EqualTo(940));
            Assert.That(garden.Sell(0, 1, 1, 3), Is.False, "Locked buyer cannot purchase.");
            Assert.That(garden.Quote(0, 0, 1), Is.EqualTo(69));
            Assert.That(garden.Quote(0, 0, 2), Is.EqualTo(54));
            Assert.That(garden.Sell(0, 0, 1, 3), Is.True);
            Assert.That(session.Cash, Is.EqualTo(1147));
            Assert.That(garden.Stock(0), Is.Zero);
            Assert.That(garden.Sell(0, 0, 1, 3), Is.False);
            Assert.That(garden.Harvest(0, 22600), Is.False);
        }

        [Test]
        public void SalesUnlockPremiumAndSplitOrdersGrantSameReputationWithoutDuplicateRewards()
        {
            CityGardenData saved = new CityGarden(new CitySession(1000)).Snapshot();
            saved.stock[0] = 10;
            CitySession wholeSession = new CitySession(1000), splitSession = new CitySession(1000);
            wholeSession.Earn(1, 55); splitSession.Earn(1, 55);
            CityGarden whole = new CityGarden(wholeSession, saved), split = new CityGarden(splitSession, saved);
            Assert.That(whole.IsUnlocked(6), Is.False);
            Assert.That(whole.Sell(0, 0, 0, 10), Is.True);
            Assert.That(split.Sell(0, 0, 0, 4), Is.True);
            Assert.That(split.Sell(0, 0, 0, 6), Is.True);
            Assert.That(wholeSession.Reputation, Is.EqualTo(65));
            Assert.That(splitSession.Reputation, Is.EqualTo(wholeSession.Reputation));
            Assert.That(splitSession.Cash, Is.EqualTo(wholeSession.Cash));
            Assert.That(whole.IsUnlocked(6), Is.True);
            Assert.That(whole.BuySeed(6), Is.True);
            Assert.That(whole.Sell(0, 0, 0, 10), Is.False);
            Assert.That(wholeSession.Reputation, Is.EqualTo(65));
        }
        [Test]
        public void ReputationUnlocksPremiumSeedsAndCustomers()
        {
            CitySession session = new CitySession(10000);
            CityGarden garden = new CityGarden(session);
            session.Earn(1, 30);
            Assert.That(garden.BuySeed(5), Is.True);
            Assert.That(garden.BuySeed(6), Is.False);
            session.Earn(1, 30);
            Assert.That(garden.BuySeed(6), Is.True);
            Assert.That(garden.BuySeed(7), Is.False);
            session.Earn(1, 40);
            Assert.That(garden.BuySeed(7), Is.True);
            Assert.That(garden.Quote(7, 1, 1), Is.GreaterThan(garden.Quote(7, 0, 1)));
        }

        [Test]
        public void LegacySaveMigratesInventoryAndSnapshotsAreIndependent()
        {
            CityGarden legacy = new CityGarden(new CitySession(1000));
            CityGardenData saved = legacy.Snapshot(); saved.seeds = null; saved.stock = null;
            CityGarden restored = new CityGarden(new CitySession(1000), saved);
            restored.BuySeed(4);
            CityGardenData copy = restored.Snapshot();
            copy.seeds[4] = 999;
            Assert.That(restored.Seeds(4), Is.EqualTo(1));
            CityGarden roundtrip = new CityGarden(new CitySession(1000), restored.Snapshot());
            Assert.That(roundtrip.Seeds(4), Is.EqualTo(1));
            roundtrip.PlantFromInventory(1, 4, 1000);
            Assert.That(roundtrip.Plot(1).catalogPlant, Is.True);
            Assert.That(new CityGarden(new CitySession(1000), roundtrip.Snapshot()).Plot(1).catalogPlant, Is.True);
        }

        [Test]
        public void FullWarehousePreservesReadyPlantAndSeedCapacityPreservesCash()
        {
            CityGardenData saved = new CityGarden(new CitySession(1000)).Snapshot();
            saved.seeds[0] = CityGarden.InventoryCapacity;
            saved.stock[0] = CityGarden.InventoryCapacity;
            saved.plots[0].planted = true; saved.plots[0].catalogPlant = true;
            saved.plots[0].grownSeconds = 21600;
            CitySession session = new CitySession(1000);
            CityGarden garden = new CityGarden(session, saved);
            Assert.That(garden.BuySeed(0), Is.False);
            Assert.That(session.Cash, Is.EqualTo(1000));
            Assert.That(garden.Harvest(0, 1000), Is.False);
            Assert.That(garden.IsReady(0), Is.True);
            Assert.That(garden.Sell(0, 0, 0, 3), Is.True);
            Assert.That(garden.Harvest(0, 1000), Is.True);
            Assert.That(garden.Stock(0), Is.EqualTo(CityGarden.InventoryCapacity));
        }
        [Test]
        public void InvalidInventoryAndOverflowSalesCannotChangeBalances()
        {
            CityGarden garden = new CityGarden(new CitySession(1000));
            CityGardenData saved = garden.Snapshot(); saved.stock[0] = -1;
            Assert.Throws<ArgumentException>(() => CityGarden.Validate(saved));
            saved.stock[0] = 1; saved.seeds = new int[1];
            Assert.Throws<ArgumentException>(() => CityGarden.Validate(saved));
            saved.seeds = new int[8];
            CitySession rich = new CitySession(int.MaxValue);
            CityGarden capped = new CityGarden(rich, saved);
            Assert.That(capped.Sell(0, 0, 0, 1), Is.False);
            Assert.That(capped.Stock(0), Is.EqualTo(1));
            Assert.That(rich.Cash, Is.EqualTo(int.MaxValue));
        }
    }
}
