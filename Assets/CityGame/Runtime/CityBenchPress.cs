using System;

namespace Pozarevac.CityGame
{
    public enum CityBenchPhase { Ready, Lowering, PressWindow, Pressing, Recovery }

    // Simulation time only. A complete eccentric/concentric cycle earns one repetition.
    public sealed class CityBenchPress
    {
        public const double LowerSeconds = .9, WindowSeconds = .75, PressSeconds = .8, RecoverySeconds = .25;
        public CityBenchPhase Phase { get; private set; }
        public int Misses { get; private set; }
        public int Completed { get; private set; }
        private double elapsed;
        private bool successful;
        public float Extension => Phase == CityBenchPhase.Lowering ? 1 - Smooth(elapsed / LowerSeconds) :
            Phase == CityBenchPhase.PressWindow ? 0 : Phase == CityBenchPhase.Pressing ? Smooth(elapsed / PressSeconds) : 1;
        public bool Press()
        {
            if (Phase == CityBenchPhase.Ready) { Phase = CityBenchPhase.Lowering; elapsed = 0; return true; }
            if (Phase != CityBenchPhase.PressWindow) return false;
            Phase = CityBenchPhase.Pressing; elapsed = 0; successful = true; return true;
        }
        public bool Tick(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (seconds == 0 || Phase == CityBenchPhase.Ready) return false;
            elapsed += seconds;
            // Discard surplus frame time at phase boundaries: stalls cannot skip an input window.
            if (Phase == CityBenchPhase.Lowering && elapsed >= LowerSeconds) { Phase = CityBenchPhase.PressWindow; elapsed = 0; }
            else if (Phase == CityBenchPhase.PressWindow && elapsed >= WindowSeconds) { Misses++; successful = false; Phase = CityBenchPhase.Pressing; elapsed = 0; }
            else if (Phase == CityBenchPhase.Pressing && elapsed >= PressSeconds)
            {
                Phase = CityBenchPhase.Recovery; elapsed = 0;
                if (successful) { successful = false; Completed++; return true; }
            }
            else if (Phase == CityBenchPhase.Recovery && elapsed >= RecoverySeconds) { Phase = CityBenchPhase.Ready; elapsed = 0; }
            return false;
        }
        private static float Smooth(double n) { float t = (float)Math.Clamp(n, 0, 1); return t * t * (3 - 2 * t); }
    }
}
