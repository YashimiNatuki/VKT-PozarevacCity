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

    // Offer seed and phase persist with balances in the same atomic profile.
    // No claims of geographically accurate addresses: routes reuse three authored local stations.
    public sealed class CityStreetJobs
    {
        public const int MaximumCompletedJobs = 1000000;
        public event Action<int> Completed;
        public event Action Changed;
        private readonly CitySession session;
        private CityLifestyle lifestyle;
        private CityGardenOrder gardenOrder;
        public void BindGardenOrder(CityGardenOrder value)
        {
            if(value==null)throw new ArgumentNullException(nameof(value));
            if(gardenOrder!=null&&gardenOrder!=value)throw new InvalidOperationException("Garden order already bound.");
            gardenOrder=value;
        }
        public void BindLifestyle(CityLifestyle value)
        {
            if(value==null)throw new ArgumentNullException(nameof(value));
            if(lifestyle!=null&&lifestyle!=value)throw new InvalidOperationException("Lifestyle already bound.");
            lifestyle=value;
        }
        public string BlockReason=>gardenOrder!=null&&gardenOrder.IsActive?"Završi ili otkaži aktivnu porudžbinu iz šteka.":
            lifestyle!=null&&lifestyle.Wanted?"Potera je aktivna. Izgubi policiju pre kontakta.":
            lifestyle!=null&&(lifestyle.IsTraining||lifestyle.IsConditioning||lifestyle.IsBoxing)?"Završi ili prekini trening pre kontakta.":
            session.Heat>Offer.MaximumHeat?"Smanji HEAT na "+Offer.MaximumHeat+" za ovaj kontakt.":"";
        private uint random;
        private uint offerSeed;
        public CityStreetJobOffer Offer { get; private set; }
        public bool IsActive { get; private set; }
        public int ObjectiveIndex { get; private set; }
        public int CompletedJobs { get; private set; }
        public double RemainingSeconds { get; private set; }
        public double CooldownSeconds { get; private set; }
        public string Message { get; private set; }
        public bool HasCargo=>IsActive&&ObjectiveIndex==1;
        public bool HasReceipt=>IsActive&&ObjectiveIndex==2;
        public string PhaseSummary=>!IsActive?(CooldownSeconds>0?"ODMOR KONTAKTA":"PONUDA / bez preuzetog tereta"):
            ObjectiveIndex==0?"PREUZIMANJE / priđi prvom kontaktu":ObjectiveIndex==1?"ISPORUKA / paket je kod tebe":"POTVRDA / isporučeno, vrati potvrdu za isplatu";
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
        public static void ValidateCheckpoint(CityStreetJobSaveData saved,int count)
        {
            ValidateCompletedCount(count);CityStreetJobSaveData.Validate(saved);
            if(saved==null||saved.offerSeed==0)return;
            var expected=new CityStreetJobs(new CitySession(0),saved.offerSeed,count);
            if(saved.remainingSeconds>expected.Offer.TimeLimit)throw new ArgumentException("Street deadline exceeds its deterministic offer.");
        }
        internal void RestoreCompletedCount(int count)
        {
            ValidateCompletedCount(count);
            if (IsActive || CompletedJobs != 0) throw new InvalidOperationException("Street progress must be restored before play.");
            CompletedJobs = count;
            GenerateOffer();
        }
        public CityStreetJobSaveData Snapshot() => new CityStreetJobSaveData
        {
            offerSeed=offerSeed, active=IsActive, objectiveIndex=IsActive?ObjectiveIndex:0,
            remainingSeconds=RemainingSeconds, cooldownSeconds=CooldownSeconds
        };
        internal void Restore(int count, CityStreetJobSaveData saved)
        {
            CityStreetJobSaveData.Validate(saved);
            RestoreCompletedCount(count);
            if (saved == null || saved.offerSeed==0) return;
            random=saved.offerSeed; GenerateOffer();
            if (saved.remainingSeconds > Offer.TimeLimit) throw new ArgumentException("Street deadline exceeds its offer.");
            IsActive=saved.active; ObjectiveIndex=saved.objectiveIndex;
            RemainingSeconds=saved.remainingSeconds; CooldownSeconds=saved.cooldownSeconds;
            Message=IsActive ? "Posao nastavljen: " + Offer.ObjectiveTitle(ObjectiveIndex) : "Ponuda učitana; prethodna isplata je zabeležena.";
        }
        public bool TryAccept()
        {
            if (IsActive || CooldownSeconds > 0) return false;
            if(BlockReason.Length>0){Message=BlockReason;return false;}
            IsActive = true; ObjectiveIndex = 0; RemainingSeconds = Offer.TimeLimit;
            Message = Offer.Briefing;
            Changed?.Invoke();
            return true;
        }
        public bool TryVisit(int station)
        {
            if (station < 0 || station > 2) throw new ArgumentOutOfRangeException(nameof(station));
            if (!IsActive || station != Offer.StationAt(ObjectiveIndex)) return false;
            if(BlockReason.Length>0){Message=BlockReason+" Rok i dalje teče.";return false;}
            ObjectiveIndex++;
            if (ObjectiveIndex < 3)
            {
                Message = Offer.ObjectiveTitle(ObjectiveIndex) + " / marker " + (Offer.StationAt(ObjectiveIndex) + 1);
                Changed?.Invoke();
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
            Changed?.Invoke();
            return true;
        }
        public void Tick(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!IsActive) { CooldownSeconds = Math.Max(0, CooldownSeconds - seconds); return; }
            RemainingSeconds = Math.Max(0, RemainingSeconds - seconds);
            if (RemainingSeconds <= 0) Cancel(HasReceipt?"Rok je istekao bez završne potvrde. Posao zatvoren; nema isplate.":"Istekao je rok. Prevoz paketa je prekinut; nema isplate.");
        }
        public bool Cancel(string reason = "Posao otkazan; nema isplate.")
        {
            if (!IsActive) return false;
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A cancellation reason is required.", nameof(reason));
            IsActive = false; ObjectiveIndex = 0; RemainingSeconds = 0; CooldownSeconds = 15;
            GenerateOffer(); Message = reason;
            Changed?.Invoke();
            return true;
        }
        private uint Next()
        {
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            return random;
        }
        private void GenerateOffer()
        {
            offerSeed=random;
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
