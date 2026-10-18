using System;

namespace Pozarevac.CityGame
{
    [Serializable]
    public sealed class CityMissionSaveData
    {
        public int contractNumber;
        public CityMissionState state;
        public CityMissionChallenge challenge;
        public int objectiveIndex;
        public int attempt;
        public double remainingSeconds;

        public static CityMissionSaveData Copy(CityMissionSaveData value) => value == null || value.contractNumber==0 ? null :
            new CityMissionSaveData { contractNumber=value.contractNumber, state=value.state,
                challenge=value.challenge, objectiveIndex=value.objectiveIndex, attempt=value.attempt,
                remainingSeconds=value.remainingSeconds };

        public static void Validate(CityMissionSaveData value, int completed)
        {
            if (value == null) return; // Profiles from versions 1-6 have no checkpoint.
            // JsonUtility materializes null inline classes as an all-zero object.
            if(value.contractNumber==0 && value.state==0 && value.challenge==0 && value.objectiveIndex==0 && value.attempt==0 && value.remainingSeconds==0)return;
            if (value.contractNumber < 1 || value.contractNumber > 3 || value.attempt < 1 ||
                !Enum.IsDefined(typeof(CityMissionState), value.state) ||
                !Enum.IsDefined(typeof(CityMissionChallenge), value.challenge) ||
                double.IsNaN(value.remainingSeconds) || double.IsInfinity(value.remainingSeconds) ||
                value.remainingSeconds < 0 || value.remainingSeconds > 120 ||
                value.objectiveIndex < 0 || value.objectiveIndex > 3)
                throw new ArgumentException("Invalid mission checkpoint.");
            bool terminal = value.state == CityMissionState.Completed;
            double limit=(value.contractNumber==1?120:value.contractNumber==2?90:60)*(value.challenge==CityMissionChallenge.Express?.6:1);
            if ((terminal && (value.objectiveIndex != 3 || completed != value.contractNumber)) ||
                value.remainingSeconds > limit || (!terminal && value.objectiveIndex==3) ||
                (!terminal && completed != value.contractNumber - 1) ||
                (value.state == CityMissionState.Ready && value.objectiveIndex != 0) ||
                (value.state == CityMissionState.Active && (value.objectiveIndex < 1 || value.objectiveIndex > 2 || value.remainingSeconds <= 0)))
                throw new ArgumentException("Mission checkpoint disagrees with campaign progress.");
        }
    }

    [Serializable]
    public sealed class CityStreetJobSaveData
    {
        public uint offerSeed;
        public bool active;
        public int objectiveIndex;
        public double remainingSeconds;
        public double cooldownSeconds;

        public static CityStreetJobSaveData Copy(CityStreetJobSaveData value) => value == null || value.offerSeed==0 ? null :
            new CityStreetJobSaveData { offerSeed=value.offerSeed, active=value.active,
                objectiveIndex=value.objectiveIndex, remainingSeconds=value.remainingSeconds,
                cooldownSeconds=value.cooldownSeconds };

        public static void Validate(CityStreetJobSaveData value)
        {
            if (value == null) return;
            if(value.offerSeed==0 && !value.active && value.objectiveIndex==0 && value.remainingSeconds==0 && value.cooldownSeconds==0)return;
            if (value.offerSeed == 0 || double.IsNaN(value.remainingSeconds) || double.IsInfinity(value.remainingSeconds) ||
                double.IsNaN(value.cooldownSeconds) || double.IsInfinity(value.cooldownSeconds) ||
                value.remainingSeconds < 0 || value.remainingSeconds > 165 || value.cooldownSeconds < 0 || value.cooldownSeconds > 30 ||
                value.objectiveIndex < 0 || value.objectiveIndex > 2 ||
                (value.active && (value.remainingSeconds <= 0 || value.cooldownSeconds != 0)) ||
                (!value.active && (value.objectiveIndex != 0 || value.remainingSeconds != 0)))
                throw new ArgumentException("Invalid street job checkpoint.");
        }
    }
}
