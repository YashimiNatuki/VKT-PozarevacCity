using System;

namespace Pozarevac.CityGame
{
    public enum CityMissionState { Ready, Active, Completed, Failed }
    public enum CityMissionChallenge { Standard, Express, Ghost }

    public sealed class CityMissionContract
    {
        private readonly string[] objectives;
        public string Title { get; }
        public string Briefing { get; }
        public double TimeLimit { get; }
        public int CashReward { get; }
        public int ReputationReward { get; }
        public float HeatGain { get; }
        public float HeatLimit { get; }
        public int ObjectiveCount => objectives.Length;

        public CityMissionContract(string title, string briefing, double timeLimit,
            int cashReward, int reputationReward, float heatGain, float heatLimit,
            params string[] objectiveTitles)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(briefing))
                throw new ArgumentException("Mission title and briefing are required.");
            if (double.IsNaN(timeLimit) || double.IsInfinity(timeLimit) || timeLimit <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeLimit));
            if (cashReward <= 0 || reputationReward < 0)
                throw new ArgumentOutOfRangeException(nameof(cashReward));
            if (float.IsNaN(heatGain) || float.IsInfinity(heatGain) || heatGain < 0 || heatGain > 100 ||
                float.IsNaN(heatLimit) || float.IsInfinity(heatLimit) || heatLimit <= 0 || heatLimit > 100)
                throw new ArgumentOutOfRangeException(nameof(heatLimit));
            if (objectiveTitles == null || objectiveTitles.Length != 3)
                throw new ArgumentException("Each route must have pickup, delivery and contact objectives.");
            foreach (string objective in objectiveTitles)
                if (string.IsNullOrWhiteSpace(objective))
                    throw new ArgumentException("Every objective needs a title.");

            Title = title;
            Briefing = briefing;
            TimeLimit = timeLimit;
            CashReward = cashReward;
            ReputationReward = reputationReward;
            HeatGain = heatGain;
            HeatLimit = heatLimit;
            objectives = (string[])objectiveTitles.Clone();
        }

        public string ObjectiveTitle(int index) => objectives[index];
    }

    // Main-thread domain state; UI, raycasts and terrain streaming are adapters.
    public sealed class CityMissionRun
    {
        private readonly CitySession session;
        public CityMissionContract Contract { get; }
        public CityMissionState State { get; private set; } = CityMissionState.Ready;
        public int ObjectiveIndex { get; private set; }
        public int Cargo { get; private set; }
        public int Attempt { get; private set; } = 1;
        public int Revision { get; private set; }
        public double RemainingSeconds { get; private set; }
        public string Message { get; private set; }
        public CityMissionChallenge Challenge { get; private set; }
        public double EffectiveTimeLimit => Challenge == CityMissionChallenge.Express ? Contract.TimeLimit * 0.6 : Contract.TimeLimit;
        public float EffectiveHeatLimit => Challenge == CityMissionChallenge.Ghost ? Math.Min(20f, Contract.HeatLimit) : Contract.HeatLimit;
        public int EffectiveCashReward => (int)Math.Min(int.MaxValue,
            (long)Contract.CashReward + (Challenge == CityMissionChallenge.Express ? Contract.CashReward / 2 :
                Challenge == CityMissionChallenge.Ghost ? Contract.CashReward / 4 : 0));
        public bool HasTarget => State == CityMissionState.Ready || State == CityMissionState.Active;

        public CityMissionRun(CityMissionContract contract, CitySession citySession)
        {
            Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            session = citySession ?? throw new ArgumentNullException(nameof(citySession));
            RemainingSeconds = contract.TimeLimit;
            Message = contract.Briefing;
        }

        public bool CanVisit(int station) => HasTarget && station == ObjectiveIndex && session.Heat < EffectiveHeatLimit;

        public bool TrySelectChallenge(CityMissionChallenge challenge)
        {
            if (!Enum.IsDefined(typeof(CityMissionChallenge), challenge))
                throw new ArgumentOutOfRangeException(nameof(challenge));
            if (State != CityMissionState.Ready || Challenge == challenge) return false;
            Challenge = challenge;
            RemainingSeconds = EffectiveTimeLimit;
            Message = $"{Challenge}: {EffectiveTimeLimit:0}s, heat below {EffectiveHeatLimit:0}, {EffectiveCashReward} cash. Accept at pickup.";
            Revision++;
            return true;
        }

        public static CityMissionRun RestoreCompleted(CityMissionContract contract, CitySession session)
        {
            return new CityMissionRun(contract, session)
            {
                State = CityMissionState.Completed, ObjectiveIndex = contract.ObjectiveCount,
                RemainingSeconds = 0, Message = "Completed route restored. Reward was already paid."
            };
        }

        public bool TryVisit(int station)
        {
            Tick(0);
            if (!CanVisit(station))
                return false;
            if (State == CityMissionState.Ready)
            {
                State = CityMissionState.Active;
                RemainingSeconds = EffectiveTimeLimit;
            }

            if (ObjectiveIndex == 0)
                Cargo = 1;
            else if (ObjectiveIndex == 1)
            {
                if (Cargo != 1)
                    return false;
                Cargo = 0;
            }

            ObjectiveIndex++;
            Revision++;
            if (ObjectiveIndex == Contract.ObjectiveCount)
            {
                // Commit completion before observers receive the reward event.
                State = CityMissionState.Completed;
                Message = $"{Challenge} complete: +{EffectiveCashReward} cash, +{Contract.ReputationReward} reputation.";
                session.ApplyMissionReward(EffectiveCashReward, Contract.ReputationReward, Contract.HeatGain);
            }
            else
                Message = Contract.ObjectiveTitle(ObjectiveIndex);
            return true;
        }

        public void Tick(double elapsedSeconds)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (State != CityMissionState.Active)
                return;
            RemainingSeconds = Math.Max(0, RemainingSeconds - elapsedSeconds);
            if (session.Heat >= EffectiveHeatLimit)
                Fail("Too much heat. Cool down, then press R to retry.");
            else if (RemainingSeconds <= 0)
                Fail("Deadline missed. Press R to retry from the pickup.");
        }

        public bool Retry()
        {
            if (State != CityMissionState.Failed || session.Heat >= EffectiveHeatLimit)
                return false;
            State = CityMissionState.Ready;
            ObjectiveIndex = 0;
            Cargo = 0;
            RemainingSeconds = EffectiveTimeLimit;
            Attempt++;
            Revision++;
            Message = Contract.Briefing;
            return true;
        }

        private void Fail(string reason)
        {
            State = CityMissionState.Failed;
            Cargo = 0;
            Message = reason;
            Revision++;
        }

        public void Abort(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A failure reason is required.", nameof(reason));
            if (HasTarget) Fail(reason);
        }
    }
}
