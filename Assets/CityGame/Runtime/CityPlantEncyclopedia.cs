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
        internal CityPlantCard(int catalogIndex, string strainName, string provenance, string history, string sourceUrl)
        {
            if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out Uri source) || source.Scheme != "https")
                throw new ArgumentException("A public HTTPS primary source is required.", nameof(sourceUrl));
            CatalogIndex = catalogIndex;
            Name = catalogIndex >= 0 ? CityPlantCatalog.Get(catalogIndex).Name : strainName;
            CommonName = strainName;
            ScientificName = "Cannabis sativa L. / naziv sorte: " + strainName;
            Region = provenance;
            Story = history + (catalogIndex >= 0 ? " VKT cene, sati i jedinice prinosa su isključivo pravila igre." : " Informativna sorta; još nema kupovine ni prodaje u VKT.");
            SourceUrl = source.AbsoluteUri;
            ImageResourceId = catalogIndex >= 0 ? "BotanicalAtlas" : string.Empty;
            AtlasIndex = catalogIndex;
        }
    }

    // General strain history attributed to each breeder's public account, verified 2026-10-15.
    // Strain names are not separate botanical species or guarantees of a universal pedigree.
    // Stable economy/save IDs 0..7 are retained; 8..15 remain informational, with no save IDs.
    public static class CityPlantEncyclopedia
    {
        private static readonly CityPlantCard[] Cards = {
            new CityPlantCard(0, "Amnesia Haze", "RQS linija; istorija šire Haze porodice nije jedinstven geografski rodoslov.", "Royal Queen Seeds navodi Original Amnesia Haze kao genetsku osnovu svoje Amnesia Haze linije. Ovaj zapis predstavlja njihov katalog, ne potvrdu identiteta svih biljaka pod istim imenom.", "https://www.royalqueenseeds.com/feminized-cannabis-seeds/115-amnesia-haze.html"),
            new CityPlantCard(1, "OG Kush", "SAD / različita tumačenja istorijskog porekla.", "RQS za svoju verziju navodi Chemdawg, Lemon Thai i Pakistani Kush, ali na istoj stranici naglašava da je izvorna genetika OG Kush sporna. Naziv ne označava jedinstven dokazani rodoslov.", "https://www.royalqueenseeds.com/feminized-cannabis-seeds/149-og-kush.html"),
            new CityPlantCard(2, "Wedding Cake", "Kalifornijska priča o izvornoj liniji; verzija Barney's Farm.", "Barney's Farm povezuje originalni Pink Cookies sa Seed Junky Genetics. Njihov proizvod navodi Cherry Pie, Girl Scout Cookies i OG Kush, dok njihov istorijski tekst opisuje izvornu Triangle Mints liniju. Te tvrdnje nisu predstavljene kao ista, nesporna genealogija.", "https://www.barneysfarm.com/wedding-cake-511"),
            new CityPlantCard(3, "Purple Haze", "White Label / Sensi Seeds katalog; geografski nastanak nije preciziran.", "White Label preko Sensi Seeds opisuje svoju Purple Haze kao ukrštanje Purple Thai i Haze. Stranica povezuje ime sa pesmom Džimija Hendriksa; to je priča tog izvora, ne dokaz da su sve istoimene linije identične.", "https://sensiseeds.com/en/feminized-seeds/whitelabel/purple-haze-feminised"),
            new CityPlantCard(4, "AK-47", "Serious Seeds / holandska oplemenjivačka linija.", "Serious Seeds pripisuje stvaranje AK-47 svom oplemenjivaču Simonu i datira je u 1992. Naziv se ovde koristi kao ime poznate sorte, uz jasno navedenog izvora istorije.", "https://www.seriousseeds.com/cannabis-seeds/ak-47"),
            new CityPlantCard(5, "White Widow", "Dutch Passion / holandski katalog; poreklo naziva ima više tržišnih verzija.", "Dutch Passion opisuje sopstvenu White Widow kao očuvanu ranu F1 liniju. Karta ne proglašava jednog uzgajivača univerzalno potvrđenim autorom svih White Widow linija.", "https://dutch-passion.com/en/cannabis-seeds/white-widow"),
            new CityPlantCard(6, "Sour Diesel", "RQS verzija američke Diesel porodice.", "Royal Queen Seeds za svoju Sour Diesel navodi Original Diesel ukršten sa Northern Light, Shiva i Hawaiian osnovom. To je izričito njihov opis verzije, a ne univerzalni rodoslov svega što nosi ime Sour Diesel.", "https://www.royalqueenseeds.com/us/feminized-cannabis-seeds/185-sour-diesel.html"),
            new CityPlantCard(7, "Green Gelato", "Royal Queen Seeds / Green Gelato linija.", "Royal Queen Seeds povezuje Green Gelato sa Sunset Sherbet i Thin Mint Girl Scout Cookies. Koristimo puni naziv Green Gelato da ga ne poistovećujemo sa svakim Gelato fenotipom ili drugim proizvođačem.", "https://www.royalqueenseeds.com/feminized-cannabis-seeds/416-green-gelato.html"),
            new CityPlantCard(-1, "Chronic", "Serious Seeds / holandska kataloška linija.", "Serious Seeds datira predstavljanje Chronic u 1994. Karta beleži istorijski naziv i poreklo navoda, bez prenošenja praktičnih uputstava iz izvora.", "https://www.seriousseeds.com/cannabis-seeds/chronic"),
            new CityPlantCard(-1, "White Russian", "Serious Seeds / AK-47 i White Widow linije.", "Serious Seeds opisuje White Russian kao spoj svoje AK-47 i klona White Widow iz devedesetih. Roditeljska imena predstavljaju tvrdnju uzgajivača.", "https://www.seriousseeds.com/cannabis-seeds/white-russian"),
            new CityPlantCard(-1, "Bubble Gum", "Indiana, zatim Nova Engleska i Holandija, prema Serious Seeds.", "Istorijski prikaz Serious Seeds prati put Bubble Gum materijala iz SAD u Holandiju. Izvor izričito govori o više verzija oplemenjivačke istorije.", "https://www.seriousseeds.com/cannabis-seeds/bubble-gum"),
            new CityPlantCard(-1, "Kali Mist", "Serious Seeds / holandska oplemenjivačka linija.", "Serious Seeds pripisuje Kali Mist Simonu i navodi 1993. kao godinu njenog nastanka. Ovaj datum je prenet kao tvrdnja proizvođača.", "https://www.seriousseeds.com/cannabis-seeds/kali-mist"),
            new CityPlantCard(-1, "Warlock", "Magus Genetics / kasnije Serious Seeds katalog.", "Serious Seeds opisuje Warlock kao nekadašnju vodeću liniju Magus Genetics, sa Afghani i Skunk osnovom, i vezuje njeno predstavljanje za 1997.", "https://www.seriousseeds.com/cannabis-seeds/warlock"),
            new CityPlantCard(-1, "Serious Kush", "Serious Seeds; OG Kush roditeljski klon iz SAD prema izvoru.", "Serious Seeds za Serious Kush navodi svoju White Russian i OG Kush klon američkog porekla. To je opis njihove konkretne linije.", "https://www.seriousseeds.com/cannabis-seeds/serious-kush"),
            new CityPlantCard(-1, "Blueberry", "DJ Short / saradnja sa Dutch Passion.", "Dutch Passion pripisuje izvornu Blueberry DJ Shortu i opisuje saradnju kroz koju je 1998. uvedena na holandsko tržište sa drugim članovima Blue porodice.", "https://dutch-passion.com/en/cannabis-seeds/blueberry"),
            new CityPlantCard(-1, "Orange Bud", "Dutch Passion / selekcija Skunk linija iz ranih osamdesetih.", "Dutch Passion opisuje Orange Bud kao rezultat selekcije Skunk materijala početkom osamdesetih i deo istorije holandskih coffeeshop kataloga.", "https://dutch-passion.com/en/cannabis-seeds/orange-bud")
        };
        public static int Count => Cards.Length;
        public static CityPlantCard Get(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            return Cards[index];
        }
    }
}
