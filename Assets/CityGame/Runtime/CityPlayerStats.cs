using System;
using UnityEngine;

namespace Pozarevac.CityGame
{
    [Serializable]
    public sealed class CityStatsData
    {
        public float totalDistanceWalked;
        public float totalDistanceSprinted;
        public float totalDistanceRidden;
        public int totalMissionsCompleted;
        public int totalMissionsFailed;
        public int totalDinarsEarned;
        public int totalDinarsSpent;
        public int totalDeaths;
        public int totalArrests;
        public int totalEscapes;
        public float totalPlayTimeSeconds;
        public float longestSurvivalStreak;
        public float currentSurvivalStreak;
        public float fastestMissionSeconds;
    }

    public sealed class CityPlayerStats
    {
        private readonly CityStatsData data;

        public float TotalDistanceWalked => data.totalDistanceWalked;
        public float TotalDistanceSprinted => data.totalDistanceSprinted;
        public float TotalDistanceRidden => data.totalDistanceRidden;
        public float TotalDistance => Add(Add(data.totalDistanceWalked, data.totalDistanceSprinted), data.totalDistanceRidden);
        public int TotalMissionsCompleted => data.totalMissionsCompleted;
        public int TotalMissionsFailed => data.totalMissionsFailed;
        public int TotalDinarsEarned => data.totalDinarsEarned;
        public int TotalDinarsSpent => data.totalDinarsSpent;
        public int TotalDeaths => data.totalDeaths;
        public int TotalArrests => data.totalArrests;
        public int TotalEscapes => data.totalEscapes;
        public float TotalPlayTimeSeconds => data.totalPlayTimeSeconds;
        public float LongestSurvivalStreak => data.longestSurvivalStreak;
        public float CurrentSurvivalStreak => data.currentSurvivalStreak;
        public float FastestMissionSeconds => data.fastestMissionSeconds;

        public CityPlayerStats(CityStatsData saved = null)
        {
            Validate(saved);
            data = Copy(saved) ?? new CityStatsData();
        }

        public static void Validate(CityStatsData value)
        {
            if (value == null) return;
            if (!Finite(value.totalDistanceWalked) || value.totalDistanceWalked < 0 ||
                !Finite(value.totalDistanceSprinted) || value.totalDistanceSprinted < 0 ||
                !Finite(value.totalDistanceRidden) || value.totalDistanceRidden < 0 ||
                value.totalMissionsCompleted < 0 || value.totalMissionsFailed < 0 ||
                value.totalDinarsEarned < 0 || value.totalDinarsSpent < 0 ||
                value.totalDeaths < 0 || value.totalArrests < 0 || value.totalEscapes < 0 ||
                !Finite(value.totalPlayTimeSeconds) || value.totalPlayTimeSeconds < 0 ||
                !Finite(value.longestSurvivalStreak) || value.longestSurvivalStreak < 0 ||
                !Finite(value.currentSurvivalStreak) || value.currentSurvivalStreak < 0 ||
                !Finite(value.fastestMissionSeconds) || value.fastestMissionSeconds < 0)
            {
                throw new ArgumentException("Invalid player stats data.");
            }
        }

        public static CityStatsData Copy(CityStatsData source)
        {
            if (source == null) return null;
            return new CityStatsData
            {
                totalDistanceWalked = source.totalDistanceWalked,
                totalDistanceSprinted = source.totalDistanceSprinted,
                totalDistanceRidden = source.totalDistanceRidden,
                totalMissionsCompleted = source.totalMissionsCompleted,
                totalMissionsFailed = source.totalMissionsFailed,
                totalDinarsEarned = source.totalDinarsEarned,
                totalDinarsSpent = source.totalDinarsSpent,
                totalDeaths = source.totalDeaths,
                totalArrests = source.totalArrests,
                totalEscapes = source.totalEscapes,
                totalPlayTimeSeconds = source.totalPlayTimeSeconds,
                longestSurvivalStreak = source.longestSurvivalStreak,
                currentSurvivalStreak = source.currentSurvivalStreak,
                fastestMissionSeconds = source.fastestMissionSeconds
            };
        }

        public CityStatsData Snapshot() => Copy(data);

        public void RecordMovement(float meters, bool sprinting, bool riding)
        {
            if (!Finite(meters) || meters <= 0f) return;
            if (riding) data.totalDistanceRidden = Add(data.totalDistanceRidden, meters);
            else if (sprinting) data.totalDistanceSprinted = Add(data.totalDistanceSprinted, meters);
            else data.totalDistanceWalked = Add(data.totalDistanceWalked, meters);
        }

        public void RecordMissionOutcome(bool completed, float durationSeconds)
        {
            if (completed)
            {
                data.totalMissionsCompleted = Increment(data.totalMissionsCompleted);
                if (Finite(durationSeconds) && durationSeconds > 0f && (data.fastestMissionSeconds <= 0f || durationSeconds < data.fastestMissionSeconds))
                {
                    data.fastestMissionSeconds = durationSeconds;
                }
            }
            else
            {
                data.totalMissionsFailed = Increment(data.totalMissionsFailed);
            }
        }

        public void RecordCashEarned(int amount)
        {
            if (amount > 0) data.totalDinarsEarned = (int)Math.Min(int.MaxValue, (long)data.totalDinarsEarned + amount);
        }

        public void RecordCashSpent(int amount)
        {
            if (amount > 0) data.totalDinarsSpent = (int)Math.Min(int.MaxValue, (long)data.totalDinarsSpent + amount);
        }

        public void RecordDeath()
        {
            data.totalDeaths = Increment(data.totalDeaths);
            data.currentSurvivalStreak = 0f;
        }

        public void RecordArrest()
        {
            data.totalArrests = Increment(data.totalArrests);
            data.currentSurvivalStreak = 0f;
        }

        public void RecordEscape()
        {
            data.totalEscapes = Increment(data.totalEscapes);
        }

        public void Tick(float seconds)
        {
            if (!Finite(seconds) || seconds <= 0f) return;
            data.totalPlayTimeSeconds = Add(data.totalPlayTimeSeconds, seconds);
            data.currentSurvivalStreak = Add(data.currentSurvivalStreak, seconds);
            if (data.currentSurvivalStreak > data.longestSurvivalStreak)
            {
                data.longestSurvivalStreak = data.currentSurvivalStreak;
            }
        }

        public string FormatSummary()
        {
            int hours = (int)Math.Min(int.MaxValue, (double)data.totalPlayTimeSeconds / 3600);
            int minutes = (int)((data.totalPlayTimeSeconds % 3600f) / 60f);
            return $"STATISTIKA GRADA POŽAREVCA\n" +
                   $"Vreme na ulici: {hours:00}h {minutes:00}m\n" +
                   $"Hod: {data.totalDistanceWalked:N0} m\nTrčanje: {data.totalDistanceSprinted:N0} m\n" +
                   $"Padovi / bolnica: {data.totalDeaths}\nNajduže preživljavanje: {data.longestSurvivalStreak:N0} s\n\n" +
                   "Meri se stvarno horizontalno kretanje.\nTelefon, uvod i pauza ne dodaju vreme.\nTeleport i povratak iz bolnice ne dodaju metre.\n\n" +
                   "Statistika počinje od ove nadogradnje;\nstare sesije se ne rekonstruišu.";
        }

        private static float Add(float current, float amount) => (float)Math.Min(float.MaxValue, (double)current + amount);
        private static int Increment(int value) => value == int.MaxValue ? value : value + 1;
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
