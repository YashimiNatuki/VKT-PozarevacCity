using System;

namespace Pozarevac.CityGame
{
    public sealed class CityPlantDefinition
    {
        public string Name { get; }
        public int SeedPrice { get; }
        public double GrowSeconds { get; }
        public int Yield { get; }
        public int BaseUnitPrice { get; }
        public int RequiredReputation { get; }
        internal CityPlantDefinition(string name, int seedPrice, double growSeconds, int yield, int unitPrice, int reputation)
        { Name = name; SeedPrice = seedPrice; GrowSeconds = growSeconds; Yield = yield; BaseUnitPrice = unitPrice; RequiredReputation = reputation; }
    }

    // Real cannabis strain names; every price, timer and inventory yield is fictional game balance.
    public static class CityPlantCatalog
    {
        private static readonly CityPlantDefinition[] Entries = {
            new CityPlantDefinition("Amnesia Haze", 60, 21600, 3, 60, 0),
            new CityPlantDefinition("OG Kush", 85, 25200, 3, 75, 0),
            new CityPlantDefinition("Wedding Cake", 110, 28800, 3, 90, 0),
            new CityPlantDefinition("Purple Haze", 140, 32400, 4, 95, 0),
            new CityPlantDefinition("AK-47", 175, 36000, 4, 115, 0),
            new CityPlantDefinition("White Widow", 230, 39600, 5, 125, 30),
            new CityPlantDefinition("Sour Diesel", 310, 43200, 5, 160, 60),
            new CityPlantDefinition("Green Gelato", 420, 50400, 6, 190, 100)
        };
        public static int Count => Entries.Length;
        public static CityPlantDefinition Get(int variety)
        {
            if (variety < 0 || variety >= Count) throw new ArgumentOutOfRangeException(nameof(variety));
            return Entries[variety];
        }
    }
}
