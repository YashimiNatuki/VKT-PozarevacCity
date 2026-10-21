using System;

namespace Pozarevac.CityGame
{
    [Serializable]
    public sealed class CityGardenPlotData
    {
        public int variety;
        public bool catalogPlant;
        public bool planted;
        public double grownSeconds;
        public double moistureSeconds;
    }

    [Serializable]
    public sealed class CityGardenData
    {
        public long lastUtcSeconds;
        public int harvests;
        public int[] seeds;
        public int[] stock;
        public CityGardenPlotData[] plots;
    }

    // Fictional game-balanced cannabis plants; timings and varieties are game design values.
    public sealed class CityGarden
    {
        public const int PlotCount = 4, SeedCost = 60, InventoryCapacity = 999;
        public const double WaterDuration = 7200;
        private readonly CityGardenData data;
        private readonly CitySession session;
        public int Harvests => data.harvests;
        public CityGarden(CitySession citySession, CityGardenData saved = null)
        {
            session = citySession ?? throw new ArgumentNullException(nameof(citySession));
            Validate(saved);
            data = saved == null ? new CityGardenData { plots = new CityGardenPlotData[PlotCount] } : Copy(saved);
            data.seeds ??= new int[CityPlantCatalog.Count];
            data.stock ??= new int[CityPlantCatalog.Count];
            for (int i = 0; i < PlotCount; i++) if (data.plots[i] == null) data.plots[i] = new CityGardenPlotData();
        }
        public static string VarietyName(int variety) => CityPlantCatalog.Get(variety).Name;
        public static double GrowDuration(int variety) => CityPlantCatalog.Get(variety).GrowSeconds;
        public static int HarvestValue(int variety) => CityPlantCatalog.Get(variety).Yield * CityPlantCatalog.Get(variety).BaseUnitPrice;
        public bool IsUnlocked(int variety) => session.Reputation >= CityPlantCatalog.Get(variety).RequiredReputation;
        public int Seeds(int variety) { CityPlantCatalog.Get(variety); return data.seeds[variety]; }
        public int Stock(int variety) { CityPlantCatalog.Get(variety); return data.stock[variety]; }
        public int Quote(int variety, int buyerIndex, int districtIndex) => CityGardenMarket.UnitPrice(variety, buyerIndex, districtIndex);
        public bool BuySeed(int variety)
        {
            CityPlantDefinition plant = CityPlantCatalog.Get(variety);
            if (!IsUnlocked(variety) || Seeds(variety) >= InventoryCapacity || session.Cash < plant.SeedPrice) return false;
            data.seeds[variety]++;
            return session.TrySpend(plant.SeedPrice);
        }
        public bool PlantFromInventory(int index, int variety, long utcSeconds)
        {
            CityPlantCatalog.Get(variety); CityGardenPlotData plot = At(index); Advance(utcSeconds);
            if (plot.planted || !IsUnlocked(variety) || Seeds(variety) == 0) return false;
            data.seeds[variety]--;
            plot.variety = variety; plot.planted = true; plot.catalogPlant = true; plot.grownSeconds = 0; plot.moistureSeconds = 0;
            return true;
        }
        public bool Sell(int variety, int buyerIndex, int districtIndex, int quantity)
        {
            int price = Quote(variety, buyerIndex, districtIndex);
            if (quantity <= 0 || quantity > Stock(variety) || session.Reputation < CityGardenMarket.RequiredReputation(buyerIndex)) return false;
            long reward = (long)price * quantity;
            if (reward > int.MaxValue - session.Cash) return false;
            data.stock[variety] -= quantity;
            session.Earn((int)reward, quantity);
            return true;
        }
        public CityGardenPlotData Plot(int index) => CopyPlot(At(index));
        internal bool TryTakeStock(int variety, int quantity)
        {
            CityPlantCatalog.Get(variety);
            if (quantity <= 0 || quantity > Stock(variety)) return false;
            data.stock[variety] -= quantity;
            return true;
        }
        public bool IsReady(int index) { CityGardenPlotData p = At(index); return p.planted && p.grownSeconds >= GrowDuration(p.variety); }
        public bool NeedsWater(int index) => At(index).planted && !IsReady(index) && At(index).moistureSeconds <= 0;
        public bool Plant(int index, int variety, long utcSeconds)
        {
            if (variety < 0 || variety > 2) throw new ArgumentOutOfRangeException(nameof(variety));
            CityGardenPlotData p = At(index); Advance(utcSeconds);
            if (p.planted || session.Cash < SeedCost) return false;
            p.variety = variety; p.catalogPlant = false; p.planted = true; p.grownSeconds = 0; p.moistureSeconds = 0;
            return session.TrySpend(SeedCost);
        }
        public bool Water(int index, long utcSeconds)
        {
            CityGardenPlotData p = At(index); Advance(utcSeconds);
            if (!p.planted || IsReady(index) || p.moistureSeconds > 0) return false;
            p.moistureSeconds = WaterDuration;
            return true;
        }
        public bool Harvest(int index, long utcSeconds)
        {
            CityGardenPlotData p = At(index); Advance(utcSeconds);
            if (!IsReady(index)) return false;
            int reward = HarvestValue(p.variety);
            if (p.catalogPlant && Stock(p.variety) > InventoryCapacity - CityPlantCatalog.Get(p.variety).Yield) return false;
            bool catalogHarvest = p.catalogPlant;
            if (catalogHarvest) data.stock[p.variety] += CityPlantCatalog.Get(p.variety).Yield;
            p.planted = false; p.grownSeconds = 0; p.moistureSeconds = 0;
            data.harvests = Math.Min(int.MaxValue - 1, data.harvests) + 1;
            if (!catalogHarvest) session.Earn(reward);
            return true;
        }
        public bool Advance(long utcSeconds)
        {
            if (utcSeconds < 0 || utcSeconds > 253402300799L) throw new ArgumentOutOfRangeException(nameof(utcSeconds));
            if (utcSeconds <= data.lastUtcSeconds) return false;
            double elapsed = data.lastUtcSeconds == 0 ? 0 : utcSeconds - data.lastUtcSeconds;
            data.lastUtcSeconds = utcSeconds;
            bool changed = false;
            foreach (CityGardenPlotData p in data.plots)
            {
                if (!p.planted) continue;
                double wateredTime = Math.Min(elapsed, p.moistureSeconds);
                if (wateredTime > 0) changed = true;
                p.grownSeconds = Math.Min(GrowDuration(p.variety), p.grownSeconds + wateredTime);
                p.moistureSeconds = Math.Max(0, p.moistureSeconds - elapsed);
            }
            return changed;
        }
        public CityGardenData Snapshot() => Copy(data);
        private CityGardenPlotData At(int index)
        {
            if (index < 0 || index >= PlotCount) throw new ArgumentOutOfRangeException(nameof(index));
            return data.plots[index];
        }
        public static void Validate(CityGardenData saved)
        {
            if (saved == null) return;
            if (saved.lastUtcSeconds < 0 || saved.lastUtcSeconds > 253402300799L || saved.harvests < 0 || saved.plots == null || saved.plots.Length != PlotCount)
                throw new ArgumentException("Invalid garden state.");
            ValidateInventory(saved.seeds);
            ValidateInventory(saved.stock);
            foreach (CityGardenPlotData p in saved.plots)
            {
                if (p == null || p.variety < 0 || p.variety >= CityPlantCatalog.Count || double.IsNaN(p.grownSeconds) || double.IsInfinity(p.grownSeconds) ||
                    p.grownSeconds < 0 || p.grownSeconds > GrowDuration(p.variety) || double.IsNaN(p.moistureSeconds) ||
                    double.IsInfinity(p.moistureSeconds) || p.moistureSeconds < 0 || p.moistureSeconds > WaterDuration ||
                    (!p.planted && (p.grownSeconds != 0 || p.moistureSeconds != 0))) throw new ArgumentException("Invalid garden plot.");
            }
        }
        private static void ValidateInventory(int[] inventory)
        {
            if (inventory == null) return;
            if (inventory.Length != CityPlantCatalog.Count) throw new ArgumentException("Invalid garden inventory length.");
            foreach (int count in inventory) if (count < 0 || count > InventoryCapacity) throw new ArgumentException("Invalid garden inventory count.");
        }
        private static CityGardenPlotData CopyPlot(CityGardenPlotData p) => new CityGardenPlotData
            { variety = p.variety, catalogPlant = p.catalogPlant, planted = p.planted, grownSeconds = p.grownSeconds, moistureSeconds = p.moistureSeconds };
        public static CityGardenData Copy(CityGardenData source)
        {
            if (source == null) return null;
            CityGardenPlotData[] plots = new CityGardenPlotData[PlotCount];
            for (int i = 0; i < PlotCount; i++) plots[i] = CopyPlot(source.plots[i]);
            return new CityGardenData { lastUtcSeconds = source.lastUtcSeconds, harvests = source.harvests, plots = plots, seeds = source.seeds == null ? null : (int[])source.seeds.Clone(), stock = source.stock == null ? null : (int[])source.stock.Clone() };
        }
    }
}
