using System;

namespace Pozarevac.CityGame
{
    [Serializable]
    public sealed class CityProfileData
    {
        public int version = 1;
        public string callsign = "";
        public int cash;
        public int reputation;
        public float heat;
        public int completedContracts;
        public int completedStreetJobs;
        public int seeds;
        public int stock;
        public double growSeconds;
        public bool harvestReady;
        public int crew;
        public int territories;
        public CityGardenData garden;
    }

    // Fictional, game-time economy. No real-world cultivation or trafficking instructions.
    public sealed class CityProgression
    {
        public const int SeedPrice = 40, SalePrice = 35, HarvestYield = 3;
        public const int CrewPrice = 250, TerritoryPrice = 750, Capacity = 30;
        public const double GrowDuration = 30;
        private readonly CityProfileData data;
        private CityStreetJobs streetJobs;
        public const int CurrentProfileVersion = 4;
        public int CompletedStreetJobs => streetJobs != null ? streetJobs.CompletedJobs : data.completedStreetJobs;
        public CitySession Session { get; }
        public CityGarden Garden { get; }
        public string Callsign => data.callsign;
        public bool IsCreated => !string.IsNullOrEmpty(Callsign);
        public int CompletedContracts => data.completedContracts;
        public bool PhoneUnlocked => IsCreated && CompletedContracts >= 1;
        public int Seeds => data.seeds;
        public int Stock => data.stock;
        public int Crew => data.crew;
        public int Territories => data.territories;
        public double GrowSeconds => data.growSeconds;
        public bool HarvestReady => data.harvestReady;
        public bool IsGrowing => GrowSeconds > 0;

        public CityProgression(CitySession session, CityProfileData profile = null)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            if (profile != null) Validate(profile);
            data = profile == null ? new CityProfileData() : Copy(profile);
            Garden = new CityGarden(session, data.garden);
        }

        public static void Validate(CityProfileData value)
        {
            if (value == null || value.version < 1 || value.version > CurrentProfileVersion) throw new ArgumentException("Unsupported profile version.");
            CityGarden.Validate(value.garden);
            CityStreetJobs.ValidateCompletedCount(value.completedStreetJobs);
            if (value.completedStreetJobs > 0 && value.completedContracts != 3) throw new ArgumentException("Street progress requires the city campaign.");
            if (value.callsign == null || (value.callsign.Length > 0 && !ValidCallsign(value.callsign)))
                throw new ArgumentException("Invalid callsign.");
            if (value.cash < 0 || value.reputation < 0 || float.IsNaN(value.heat) || float.IsInfinity(value.heat) || value.heat < 0 || value.heat > 100)
                throw new ArgumentException("Invalid session balances.");
            if (value.completedContracts < 0 || value.completedContracts > 3 || value.seeds < 0 || value.seeds > Capacity || value.stock < 0 || value.stock > Capacity ||
                value.crew < 0 || value.crew > 3 || value.territories < 0 || value.territories > 3 || double.IsNaN(value.growSeconds) || double.IsInfinity(value.growSeconds) ||
                value.growSeconds < 0 || value.growSeconds > GrowDuration || (value.harvestReady && value.growSeconds > 0))
                throw new ArgumentException("Invalid progression state.");
            if (value.completedContracts > 0 && string.IsNullOrEmpty(value.callsign)) throw new ArgumentException("Progress requires a character.");
            if ((value.seeds > 0 || value.stock > 0 || value.growSeconds > 0 || value.harvestReady) && value.completedContracts < 1)
                throw new ArgumentException("Economy requires the phone unlock.");
            if (value.crew > 0 && value.reputation < 30) throw new ArgumentException("Crew requires reputation.");
            if (value.territories > 0 && (value.crew < 2 || value.completedContracts < 3)) throw new ArgumentException("Territory requires a crew and the city route.");
        }

        private static bool ValidCallsign(string value)
        {
            if (value.Length < 2 || value.Length > 24 || value != value.Trim()) return false;
            foreach (char c in value) if (!char.IsLetterOrDigit(c) && c != ' ' && c != '-' && c != '_') return false;
            return true;
        }

        public bool CreateCharacter(string callsign)
        {
            string value = callsign?.Trim();
            if (IsCreated || value == null || !ValidCallsign(value)) return false;
            data.callsign = value;
            return true;
        }

        public bool CompleteContract(int completedCount)
        {
            if (!IsCreated || completedCount != CompletedContracts + 1 || completedCount > 3) return false;
            data.completedContracts = completedCount;
            return true;
        }

        public bool BuySeed()
        {
            if (!PhoneUnlocked || Seeds >= Capacity || Session.Cash < SeedPrice) return false;
            data.seeds++;
            return Session.TrySpend(SeedPrice);
        }

        public bool Plant()
        {
            if (!PhoneUnlocked || Seeds == 0 || IsGrowing || HarvestReady) return false;
            data.seeds--;
            data.growSeconds = GrowDuration;
            return true;
        }

        public bool Tick(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!IsGrowing) return false;
            data.growSeconds = Math.Max(0, GrowSeconds - seconds);
            if (IsGrowing) return false;
            data.harvestReady = true;
            return true;
        }

        public bool Harvest()
        {
            if (!HarvestReady || Stock > Capacity - HarvestYield) return false;
            data.harvestReady = false;
            data.stock += HarvestYield;
            return true;
        }

        public bool Sell()
        {
            if (!PhoneUnlocked || Stock == 0) return false;
            int reward = Stock * SalePrice;
            data.stock = 0; // Commit before cash observers run.
            Session.Earn(reward);
            return true;
        }

        public bool Recruit()
        {
            if (!PhoneUnlocked || Session.Reputation < 30 || Crew >= 3 || Session.Cash < CrewPrice) return false;
            data.crew++;
            return Session.TrySpend(CrewPrice);
        }

        public bool ClaimTerritory()
        {
            if (CompletedContracts != 3 || Crew < 2 || Territories >= 3 || Session.Cash < TerritoryPrice) return false;
            data.territories++;
            return Session.TrySpend(TerritoryPrice);
        }

        public void BindStreetJobs(CityStreetJobs jobs)
        {
            if (jobs == null) throw new ArgumentNullException(nameof(jobs));
            if (ReferenceEquals(streetJobs, jobs)) return;
            if (streetJobs != null) throw new InvalidOperationException("Street jobs are already bound.");
            jobs.RestoreCompletedCount(data.completedStreetJobs);
            streetJobs = jobs;
        }

        public CityProfileData Snapshot()
        {
            CityProfileData snapshot = Copy(data);
            snapshot.cash = Session.Cash;
            snapshot.reputation = Session.Reputation;
            snapshot.heat = Session.Heat;
            snapshot.version = CurrentProfileVersion;
            snapshot.completedStreetJobs = CompletedStreetJobs;
            snapshot.garden = Garden.Snapshot();
            return snapshot;
        }

        private static CityProfileData Copy(CityProfileData source) => new CityProfileData
        {
            version = source.version, callsign = source.callsign, cash = source.cash, reputation = source.reputation, heat = source.heat,
            completedContracts = source.completedContracts, completedStreetJobs = source.completedStreetJobs, seeds = source.seeds, stock = source.stock, growSeconds = source.growSeconds,
            harvestReady = source.harvestReady, crew = source.crew, territories = source.territories, garden = CityGarden.Copy(source.garden)
        };
    }
}
