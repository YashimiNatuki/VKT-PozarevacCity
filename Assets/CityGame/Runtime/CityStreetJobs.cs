using System;

namespace Pozarevac.CityGame
{
    public sealed class CityStreetJobOffer
    {
        public string Title { get; }
        public string Briefing { get; }
        public int CashReward { get; }
        public int ReputationReward { get; }
        public double TimeLimit { get; }
        public int MaximumHeat { get; }
        private readonly int[] stations;
        private readonly string[] objectives;
        internal CityStreetJobOffer(string title, string briefing, int cash, int reputation, double seconds, int[] route,int maximumHeat=100,string[] objectiveNames=null)
        { Title = title; Briefing = briefing; CashReward = cash; ReputationReward = reputation; TimeLimit = seconds; stations = (int[])route.Clone(); MaximumHeat=maximumHeat;objectives=objectiveNames!=null?(string[])objectiveNames.Clone():new[]{"Preuzmi paket","Predaj porudžbinu","Vrati potvrdu kontaktu"}; }
        public int StationAt(int objective)
        {
            if (objective < 0 || objective >= 3) throw new ArgumentOutOfRangeException(nameof(objective));
            return stations[objective];
        }
        public string ObjectiveTitle(int objective)
        {
            StationAt(objective);
            return objectives[objective];
        }
    }

    // Completed counts persist; active routes are deliberately abandoned on reload.
    // No claims of geographically accurate addresses: routes reuse three authored local stations.
    public sealed class CityStreetJobs
    {
        public const int MaximumCompletedJobs = 1000000;
        public event Action<int> Completed;
        private readonly CitySession session;
        private uint random;
        public CityStreetJobOffer Offer { get; private set; }
        public bool IsActive { get; private set; }
        public int ObjectiveIndex { get; private set; }
        public int CompletedJobs { get; private set; }
        public double RemainingSeconds { get; private set; }
        public double CooldownSeconds { get; private set; }
        public string Message { get; private set; }
        public int StoryChapter => Math.Min(3, CompletedJobs / 3 + 1);

        public CityStreetJobs(CitySession citySession, uint seed = 0x56b23d91u, int completedJobs = 0)
        {
            session = citySession ?? throw new ArgumentNullException(nameof(citySession));
            random = seed == 0 ? 0x56b23d91u : seed;
            ValidateCompletedCount(completedJobs);
            CompletedJobs = completedJobs;
            GenerateOffer();
        }
        public static void ValidateCompletedCount(int count)
        {
            if (count < 0 || count > MaximumCompletedJobs) throw new ArgumentOutOfRangeException(nameof(count));
        }
        internal void RestoreCompletedCount(int count)
        {
            ValidateCompletedCount(count);
            if (IsActive || CompletedJobs != 0) throw new InvalidOperationException("Street progress must be restored before play.");
            CompletedJobs = count;
            GenerateOffer();
        }
        public bool TryAccept()
        {
            if (IsActive || CooldownSeconds > 0) return false;
            if(session.Heat>Offer.MaximumHeat){Message="Kontakt čeka da se smiri blok. HEAT mora biti najviše "+Offer.MaximumHeat+".";return false;}
            IsActive = true; ObjectiveIndex = 0; RemainingSeconds = Offer.TimeLimit;
            Message = Offer.Briefing;
            return true;
        }
        public bool TryVisit(int station)
        {
            if (station < 0 || station > 2) throw new ArgumentOutOfRangeException(nameof(station));
            if (!IsActive || station != Offer.StationAt(ObjectiveIndex)) return false;
            if(session.Heat>Offer.MaximumHeat){Message="Primopredaja čeka: smanji HEAT na "+Offer.MaximumHeat+". Rok i dalje teče.";return false;}
            ObjectiveIndex++;
            if (ObjectiveIndex < 3)
            {
                Message = Offer.ObjectiveTitle(ObjectiveIndex) + " / marker " + (Offer.StationAt(ObjectiveIndex) + 1);
                return true;
            }
            int cash = Offer.CashReward, reputation = Offer.ReputationReward;
            IsActive = false; RemainingSeconds = 0; CooldownSeconds = 30;
            CompletedJobs = Math.Min(MaximumCompletedJobs - 1, CompletedJobs) + 1;
            // Commit terminal state and next offer before cash observers may reenter this module.
            GenerateOffer();
            Message = "Sporedni posao završen: +" + cash + " cash / +" + reputation + " REP. Novi poziv za 30 sekundi igre.";
            session.Earn(cash, reputation);
            Completed?.Invoke(CompletedJobs);
            return true;
        }
        public void Tick(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!IsActive) { CooldownSeconds = Math.Max(0, CooldownSeconds - seconds); return; }
            RemainingSeconds = Math.Max(0, RemainingSeconds - seconds);
            if (RemainingSeconds <= 0) Cancel("Istekao je rok. Paket je vraćen; nema isplate.");
        }
        public bool Cancel(string reason = "Posao otkazan; nema isplate.")
        {
            if (!IsActive) return false;
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A cancellation reason is required.", nameof(reason));
            IsActive = false; ObjectiveIndex = 0; RemainingSeconds = 0; CooldownSeconds = 15;
            GenerateOffer(); Message = reason;
            return true;
        }
        private uint Next()
        {
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            return random;
        }
        private void GenerateOffer()
        {
            int[] route = { 0, 1, 2 };
            for (int i = 2; i > 0; i--) { int selected = (int)(Next() % (uint)(i + 1)); int swap = route[i]; route[i] = route[selected]; route[selected] = swap; }
            int type = (int)(Next() % 6u), chapter = StoryChapter;
            string title = type switch { 0 => "Porudžbina iz šteka", 1 => "Izgubljena torba", 2 => "Delovi za bicikl", 3 => "Kasna dostava",4=>"Tihi dogovor",_=>"Inventar ekipe" };
            string story = chapter == 1 ? "PRVI BLOK / Danju sređuješ bicikle i kvariš san bagovima u kodu. Mila traži pouzdanog čoveka: preuzmi paket, završi dogovor, vrati potvrdu. Reč vredi više od prečice."
                : chapter == 2 ? "REPUTACIJA / Vuk te uvodi u novu ekipu. Posao raste, pritisak takođe. Poveži kontakte i vrati potvrdu; brzina bez pouzdanosti ne gradi ime."
                : "SVOJA MREŽA / Luka ti poverava raspored gradske ekipe. Više nisi samo trkač: održavaš dogovore i brineš da se svako vrati. Tvoj blok pamti rezultate.";
            string[] neighborhoods = { "Centar", "Burjan", "Radna mala", "Tulba", "Busije", "Meminac" };
            string neighborhood = neighborhoods[(CompletedJobs + type) % neighborhoods.Length];
            story += " Kontakt dolazi iz dela grada: " + neighborhood + ". Primopredaja je u trenutnoj centralnoj igrivoj zoni, ne u još neizgrađenom naselju.";
            int maximumHeat=type==4?30:type==5?45:100;
            string[] objectives=type==4?new[]{"Preuzmi poruku ekipe","Dogovori tihu primopredaju","Potvrdi dogovor kontaktu"}:
                type==5?new[]{"Preuzmi spisak zaliha","Proveri inventar kod kontakta","Vrati potpisani spisak"}:null;
            if(maximumHeat<100)story+=" Kontakt odbija preuzimanje i predaju dok je HEAT iznad "+maximumHeat+". Skloni se iz potere; rok teče tokom aktivne igre.";
            Offer = new CityStreetJobOffer("Poglavlje " + chapter + " / " + title,
                story + " Obiđi markere " + (route[0] + 1) + " → " + (route[1] + 1) + " → " + (route[2] + 1) + ".",
                120 + chapter * 40 + type * 15, 2 + chapter, 90 + type * 15, route,maximumHeat,objectives);
            Message = "Novi poziv: " + Offer.Title;
        }
    }
}
