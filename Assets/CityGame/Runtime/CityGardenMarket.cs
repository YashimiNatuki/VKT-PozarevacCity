using System;

namespace Pozarevac.CityGame
{
    public static class CityGardenMarket
    {
        public const int BuyerCount = 3, DistrictCount = 3;
        public static string BuyerName(int buyer) => buyer switch {
            0 => "Cvećara Mila", 1 => "Kolekcionar Luka", 2 => "Hotel Vuk", _ => throw new ArgumentOutOfRangeException(nameof(buyer)) };
        public static string DistrictName(int district) => district switch {
            0 => "Pijaca", 1 => "Centar", 2 => "Obod grada", _ => throw new ArgumentOutOfRangeException(nameof(district)) };
        public static int RequiredReputation(int buyer) { BuyerName(buyer); return buyer == 0 ? 0 : buyer == 1 ? 30 : 60; }
        public static int UnitPrice(int variety, int buyer, int district)
        {
            CityPlantDefinition plant = CityPlantCatalog.Get(variety);
            BuyerName(buyer); DistrictName(district);
            int buyerPercent = buyer == 0 ? 100 : buyer == 1 ? (variety >= 5 ? 145 : 110) : (variety >= 3 ? 125 : 115);
            int districtPercent = district == 0 ? 100 : district == 1 ? 115 : 90;
            return Math.Max(1, plant.BaseUnitPrice * buyerPercent * districtPercent / 10000);
        }
    }
}
