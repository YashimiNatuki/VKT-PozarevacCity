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
        private readonly int[] stations;
        internal CityStreetJobOffer(string title, string briefing, int cash, int reputation, double seconds, int[] route)
        { Title = title; Briefing = briefing; CashReward = cash; ReputationReward = reputation; TimeLimit = seconds; stations = (int[])route.Clone(); }
        public int StationAt(int objective)
        {
            if (objective < 0 || objective >= 3) throw new ArgumentOutOfRangeException(nameof(objective));
            return stations[objective];
        }
        public string ObjectiveTitle(int objective)
        {
            StationAt(objective);
            return objective == 0 ? "Preuzmi paket" : objective == 1 ? "Predaj porudžbinu" : "Vrati potvrdu kontaktu";
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
            IsActive = true; ObjectiveIndex = 0; RemainingSeconds = Offer.TimeLimit;
            Message = Offer.Briefing;
            return true;
        }
        public bool TryVisit(int station)
        {
            if (station < 0 || station > 2) throw new ArgumentOutOfRangeException(nameof(station));
            if (!IsActive || station != Offer.StationAt(ObjectiveIndex)) return false;
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
            int type = (int)(Next() % 4u), chapter = StoryChapter;
            string title = type switch { 0 => "Cveće za komšiluk", 1 => "Izgubljena torba", 2 => "Delovi za bicikl", _ => "Kasna dostava" };
            string story = chapter == 1 ? "Mila proverava može li da ti poveri prve porudžbine." : chapter == 2 ? "Vuk te preporučuje novim kontaktima iz kraja." : "Luka organizuje gradsku mrežu pouzdanih kurira.";
            Offer = new CityStreetJobOffer("Poglavlje " + chapter + " / " + title,
                story + " Obiđi markere " + (route[0] + 1) + " → " + (route[1] + 1) + " → " + (route[2] + 1) + ".",
                120 + chapter * 40 + type * 15, 2 + chapter, 90 + type * 15, route);
            Message = "Novi poziv: " + Offer.Title;
        }
    }
}
