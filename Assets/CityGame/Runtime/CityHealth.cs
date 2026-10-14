using System;

namespace Pozarevac.CityGame
{
    public sealed class CityHealth
    {
        public float Maximum { get; }
        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;
        public float InvulnerabilityRemaining { get; private set; }
        public float DamageCooldownSeconds { get; }
        public CityHealth(float maximum = 100f, float damageCooldownSeconds = 0.75f)
        {
            if (!Finite(maximum) || maximum <= 0f) throw new ArgumentOutOfRangeException(nameof(maximum));
            if (!Finite(damageCooldownSeconds) || damageCooldownSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(damageCooldownSeconds));
            Maximum = maximum; Current = maximum; DamageCooldownSeconds = damageCooldownSeconds;
        }
        public void Tick(float seconds)
        {
            if (!Finite(seconds) || seconds < 0f) return;
            InvulnerabilityRemaining = Math.Max(0f, InvulnerabilityRemaining - seconds);
        }
        public bool TryDamage(float amount)
        {
            if (!Finite(amount) || amount <= 0f || !IsAlive || InvulnerabilityRemaining > 0f) return false;
            Current = Math.Max(0f, Current - Math.Min(amount, Maximum));
            InvulnerabilityRemaining = DamageCooldownSeconds;
            return true;
        }
        public void Restore(float value)
        {
            if (!Finite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Current = Math.Max(0f, Math.Min(Maximum, value));
            InvulnerabilityRemaining = 0f;
        }
        public void Respawn() { Current = Maximum; InvulnerabilityRemaining = 3f; }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public sealed class CityWorldClock
    {
        public float Hour { get; private set; }
        public double DaySeconds { get; }
        public double NightSeconds { get; }
        public bool IsDay => Hour >= 6f && Hour < 18f;
        private double cycleSeconds;
        public CityWorldClock(float daySeconds = 1080f, float nightSeconds = 720f, float startingHour = 8f)
        {
            if (float.IsNaN(daySeconds) || float.IsInfinity(daySeconds) || daySeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(daySeconds));
            if (float.IsNaN(nightSeconds) || float.IsInfinity(nightSeconds) || nightSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(nightSeconds));
            if (float.IsNaN(startingHour) || float.IsInfinity(startingHour)) throw new ArgumentOutOfRangeException(nameof(startingHour));
            DaySeconds = daySeconds; NightSeconds = nightSeconds;
            double offset = ((startingHour - 6d) % 24d + 24d) % 24d;
            cycleSeconds = offset < 12d ? offset / 12d * DaySeconds : DaySeconds + (offset - 12d) / 12d * NightSeconds;
            UpdateHour();
        }
        public void Tick(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f) return;
            cycleSeconds = (cycleSeconds + seconds) % (DaySeconds + NightSeconds);
            UpdateHour();
        }
        private void UpdateHour()
        {
            double offset = cycleSeconds < DaySeconds ? cycleSeconds / DaySeconds * 12d : 12d + (cycleSeconds - DaySeconds) / NightSeconds * 12d;
            Hour = (float)((6d + offset) % 24d);
        }
    }
}
