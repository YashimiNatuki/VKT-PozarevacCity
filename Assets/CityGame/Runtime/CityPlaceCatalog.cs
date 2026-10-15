using UnityEngine;

namespace Pozarevac.CityGame
{
    // Names verified against official city/tourism sources; positions are authored game offsets.
    public static class CityPlaceCatalog
    {
        public const string Attribution = "VKT / autorska interpretacija centra Požarevca";
        public const string GeometryNotice = "Raspored je prilagođen igri; nije geodetska rekonstrukcija.";
        public readonly struct Place
        {
            public readonly string Id, Name, Objective;
            public readonly Vector3 Offset;
            public Place(string id, string name, string objective, Vector3 offset)
            { Id = id; Name = name; Objective = objective; Offset = offset; }
        }
        private static readonly Place[] route =
        {
            new Place("tabacka", "Tabačka čaršija", "Preuzmi kurirski paket kod zanatske radnje.", new Vector3(-28, 0, 3)),
            new Place("korzo", "Stari korzo", "Odnesi rezervni deo majstoru na korzou.", new Vector3(28, 0, 3)),
            new Place("trg", "Trg oslobođenja", "Sastani se sa kontaktom pored trga.", new Vector3(0, 0, 24)),
            new Place("park", "Gradski park / Knjaz Miloš", "Pronađi kontakt kod stilizovanog spomenika.", new Vector3(30, 0, 34))
        };
        public static int Count => route.Length;
        public static Place At(int index) => route[index];
    }
}
