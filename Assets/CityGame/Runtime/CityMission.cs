using UnityEngine;

namespace Pozarevac.CityGame
{
    public sealed class CityMission : MonoBehaviour
    {
        [SerializeField] private string title = "Night Run";
        [SerializeField, Min(1)] private int cashReward = 500;
        [SerializeField, Min(0)] private int reputationReward = 10;
        [SerializeField, Range(0f, 100f)] private float heatGain = 5f;

        private CityMission prerequisite;
        private CityMissionDirector director;
        private int stationIndex;
        private bool legacyComplete;

        public bool IsAvailable => director != null ? director.IsStationAvailable(stationIndex) : !IsComplete && (prerequisite == null || prerequisite.IsComplete);
        public string Prompt => director != null ? director.PromptFor(stationIndex) : IsAvailable
            ? $"[E] {title}  + ₽ {cashReward:N0}"
            : "Complete the previous job first";

        public void Configure(string missionTitle, int reward, int reputation, float heat, CityMission previous = null)
        {
            title = missionTitle;
            cashReward = Mathf.Max(1, reward);
            reputationReward = Mathf.Max(0, reputation);
            heatGain = Mathf.Clamp(heat, 0f, 100f);
            prerequisite = previous;
        }
        public void ConfigureStation(CityMissionDirector missionDirector, int index)
        {
            if (missionDirector == null || index < 0 || index > 2)
                throw new System.ArgumentException("A director and valid station index are required.");
            director = missionDirector;
            stationIndex = index;
        }

        public bool IsComplete => director != null ? director.IsStationComplete(stationIndex) : legacyComplete;

        public bool TryComplete(CitySession session)
        {
            if (director != null)
                return ReferenceEquals(session, director.Session) && director.TryInteract(stationIndex);
            if (!IsAvailable || session == null)
                return false;

            legacyComplete = true;
            session.Earn(cashReward, reputationReward);
            session.AddHeat(heatGain);
            gameObject.SetActive(false);
            return true;
        }
    }
}
