using System;

namespace Pozarevac.CityGame
{
    public sealed class CitySession
    {
        public event Action Changed;

        public int Cash { get; private set; }
        public int Reputation { get; private set; }
        public float Heat { get; private set; }

        public CitySession(int startingCash)
        {
            Cash = Math.Max(0, startingCash);
        }

        public static CitySession Restore(CityProfileData profile)
        {
            CityProgression.Validate(profile);
            return new CitySession(profile.cash) { Reputation = profile.reputation, Heat = profile.heat };
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || amount > Cash)
                return false;

            Cash -= amount;
            NotifyChanged();
            return true;
        }

        public void Earn(int amount, int reputationGain = 0)
        {
            if (amount <= 0)
                return;

            Cash = (int)Math.Min(int.MaxValue, (long)Cash + amount);
            Reputation = (int)Math.Clamp((long)Reputation + reputationGain, 0L, int.MaxValue);
            NotifyChanged();
        }

        public void ApplyMissionReward(int amount, int reputationGain, float heatGain)
        {
            if (amount <= 0 || reputationGain < 0 || float.IsNaN(heatGain) || float.IsInfinity(heatGain))
                throw new ArgumentOutOfRangeException(nameof(amount));
            Cash = (int)Math.Min(int.MaxValue, (long)Cash + amount);
            Reputation = (int)Math.Min(int.MaxValue, (long)Reputation + reputationGain);
            Heat = Math.Clamp(Heat + Math.Max(0f, heatGain), 0f, 100f);
            NotifyChanged();
        }

        public void AddHeat(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount))
                throw new ArgumentOutOfRangeException(nameof(amount));
            Heat = Math.Clamp(Heat + amount, 0f, 100f);
            NotifyChanged();
        }

        public void CoolDown(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount))
                throw new ArgumentOutOfRangeException(nameof(amount));
            Heat = Math.Max(0f, Heat - Math.Max(0f, amount));
            NotifyChanged();
        }

        private void NotifyChanged() => Changed?.Invoke();
    }
}
