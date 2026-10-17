using System;

namespace Pozarevac.CityGame
{
    public enum CityConditioningState { Idle, Active, Completed, Cancelled, Expired }

    // Only simulation time advances the course. A stop must be held continuously.
    public sealed class CityConditioningRun
    {
        public const int Checkpoints = 3;
        public const double Duration = 75, HoldSeconds = 1.2;
        public CityConditioningState State { get; private set; }
        public int Checkpoint { get; private set; }
        public double Remaining { get; private set; }
        public double Hold { get; private set; }
        private bool claimed;
        public bool Start()
        {
            if(State==CityConditioningState.Active)return false;
            State=CityConditioningState.Active;Checkpoint=0;Remaining=Duration;Hold=0;claimed=false;return true;
        }
        public bool Tick(double seconds,bool atTarget)
        {
            if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
            if(State!=CityConditioningState.Active||seconds==0)return false;
            Remaining=Math.Max(0,Remaining-seconds);
            if(Remaining<=0){State=CityConditioningState.Expired;Hold=0;return false;}
            // A stalled frame cannot satisfy a checkpoint in a single update.
            Hold=atTarget?Math.Min(HoldSeconds,Hold+Math.Min(seconds,.1)):0;
            if(Hold+1e-9<HoldSeconds)return false;
            Hold=0;Checkpoint++;
            if(Checkpoint==Checkpoints)State=CityConditioningState.Completed;
            return true;
        }
        public void Cancel(){if(State==CityConditioningState.Active){State=CityConditioningState.Cancelled;Hold=0;}}
        public bool TryClaim(CityLifestyle lifestyle)
        {
            if(lifestyle==null)throw new ArgumentNullException(nameof(lifestyle));
            if(State!=CityConditioningState.Completed||claimed)return false;
            claimed=true;return lifestyle.CompleteConditioning();
        }
    }
}
