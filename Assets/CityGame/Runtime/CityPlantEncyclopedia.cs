using System;

namespace Pozarevac.CityGame
{
    public sealed class CityPlantCard
    {
        public string Name { get; }
        public string CommonName { get; }
        public string ScientificName { get; }
        public string Region { get; }
        public string Story { get; }
        public string SourceUrl { get; }
        public string ImageResourceId { get; }
        public int AtlasIndex { get; }
        public int CatalogIndex { get; }
        public bool IsTradable => CatalogIndex >= 0;
        internal CityPlantCard(int catalogIndex, string commonName, string scientificName, string region, string story, string sourceSlug)
        {
            CatalogIndex = catalogIndex;
            Name = catalogIndex >= 0 ? CityPlantCatalog.Get(catalogIndex).Name : commonName;
            CommonName = commonName; ScientificName = scientificName; Region = region;
            Story = (catalogIndex >= 0 ? "Ime u VKT je izmišljeno; ovo je stvarna botanička inspiracija. " : "Informativna karta; još nije predmet trgovine u VKT. ") + story;
            SourceUrl = "https://plants.ces.ncsu.edu/plants/" + sourceSlug + "/";
            ImageResourceId = catalogIndex >= 0 ? "BotanicalAtlas" : string.Empty;
            AtlasIndex = catalogIndex;
        }
    }

    // Facts paraphrased from NC State Extension Plant Toolbox, verified 2026-10-14.
    // Fictional economy IDs 0..7 remain stable. Entries 8..15 do not create new save IDs.
    public static class CityPlantEncyclopedia
    {
        private static readonly CityPlantCard[] Cards = {
            new CityPlantCard(0, "Lavanda", "Lavandula angustifolia", "Mediteran: Francuska, Italija i Španija", "Mirisni žbun sa ljubičastim cvastima koristi se u mirisnim jastučićima i suvim aranžmanima.", "lavandula-angustifolia"),
            new CityPlantCard(1, "Bosiljak", "Ocimum basilicum", "Tropska Afrika i Azija", "Aromatična vrsta iz porodice usnatica ima i ukrasne forme sa ljubičastim lišćem; vrhovi se koriste i kao biljna boja.", "ocimum-basilicum"),
            new CityPlantCard(2, "Klasasta nana", "Mentha spicata", "Evropa do Kine", "Mirisna višegodišnja nana poznata je po upotrebi kao aroma u čajevima i žvakaćim gumama.", "mentha-spicata"),
            new CityPlantCard(3, "Ruzmarin", "Salvia rosmarinus", "Mediteran i okolne oblasti Afrike, Evrope i zapadne Azije", "Zimzeleni aromatični žbun ranije se vodio kao Rosmarinus officinalis. Ime rosmarinus vezuje se za latinske reči za rosu i more.", "salvia-rosmarinus"),
            new CityPlantCard(4, "Žalfija", "Salvia officinalis", "Severni Mediteran", "Prepoznatljiva je po sivkastom, mirisnom lišću i ljubičastim cvetovima koji privlače oprašivače.", "salvia-officinalis"),
            new CityPlantCard(5, "Kadifica", "Tagetes erecta", "Meksiko do Gvatemale", "Uprkos engleskom nazivu African marigold, vrsta potiče iz Amerike. Asteci su je koristili u svečanim i ukrasnim aranžmanima.", "tagetes-erecta"),
            new CityPlantCard(6, "Purpurna ehinacea", "Echinacea purpurea", "Istočni i centralni deo SAD", "Prerijska višegodišnja biljka ima izdignut središnji disk cveta. Cvetove posećuju oprašivači, a seme privlači ptice.", "echinacea-purpurea"),
            new CityPlantCard(7, "Matičnjak", "Melissa officinalis", "Mediteran do centralne Azije", "Lišće ima miris limuna. Ime roda Melissa potiče od grčke reči za medonosnu pčelu, koja posećuje njene cvetove.", "melissa-officinalis"),
            new CityPlantCard(-1, "Suncokret", "Helianthus annuus", "Zapad SAD i delovi Meksika", "Visoka jednogodišnja biljka iz porodice glavočika ima mnoštvo ukrasnih sorti, od žutih do crvenkastih cvetova.", "helianthus-annuus"),
            new CityPlantCard(-1, "Divlja dan i noć", "Viola tricolor", "Evropa i Azija", "Predak je savremenih hibridnih maćuhica. Latinsko tricolor upućuje na trobojni cvet.", "viola-tricolor"),
            new CityPlantCard(-1, "Neven", "Calendula officinalis", "Mediteran", "Narandžasti i žuti cvetovi poznati su kao ukras bašte, a koristili su se i za žutu boju.", "calendula-officinalis"),
            new CityPlantCard(-1, "Kosmos", "Cosmos bipinnatus", "Meksiko i jugozapad Severne Amerike", "Jednogodišnja ukrasna vrsta sa fino podeljenim listovima privlači ptice, leptire i druge oprašivače.", "cosmos-bipinnatus"),
            new CityPlantCard(-1, "Cinija", "Zinnia elegans", "Meksiko", "Šareni cvetovi česti su u rezanim aranžmanima, a dokumentovana je i upotreba cvetova za žutu boju.", "zinnia-elegans"),
            new CityPlantCard(-1, "Bela rada", "Bellis perennis", "Evropa", "Niska cvetna biljka iz porodice glavočika poznata je sa travnjaka; njeni cvetovi pružaju nektar oprašivačima.", "bellis-perennis"),
            new CityPlantCard(-1, "Noćurak četiri sata", "Mirabilis jalapa", "Meksiko do Centralne Amerike", "Cvetovi se otvaraju kasno popodne ili pri veoma oblačnom vremenu. Mogu biti beli, crveni, žuti ili prugasti.", "mirabilis-jalapa"),
            new CityPlantCard(-1, "Različak", "Centaurea cyanus", "Zapadna Azija i Evropa", "Jednogodišnja vrsta poznata po plavim cvetovima; cvetovi i plodovi služe kao hrana pticama i leptirima.", "centaurea-cyanus")
        };
        public static int Count => Cards.Length;
        public static CityPlantCard Get(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            return Cards[index];
        }
    }
}
