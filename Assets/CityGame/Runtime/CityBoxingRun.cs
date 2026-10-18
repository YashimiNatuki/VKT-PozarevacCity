using System;

namespace Pozarevac.CityGame
{
    public enum CityBoxingAction { Jab, Guard }
    public enum CityBoxingState { Idle, Active, Completed, Failed, Cancelled }

    // Fixed rounds use game time. Holding/spamming one input cannot complete a round.
    public sealed class CityBoxingRun
    {
        public const int Rounds=8, RequiredSuccesses=6;
        public const double RoundDuration=1.8, Windup=.7, ReactionWindow=.65;
        public CityBoxingState State { get; private set; }
        public int Round { get; private set; }
        public int Successes { get; private set; }
        public double RoundTime { get; private set; }
        public double LastReaction { get; private set; }
        public bool Responded { get; private set; }
        private bool claimed;
        public CityBoxingAction Expected => Round%2==0?CityBoxingAction.Jab:CityBoxingAction.Guard;
        public bool WindowOpen => State==CityBoxingState.Active && !Responded && RoundTime>=Windup && RoundTime<=Windup+ReactionWindow;
        public float Punch => Expected==CityBoxingAction.Jab && Responded ? (float)Math.Max(0,1-Math.Abs(RoundTime-1.05)*4) : 0;
        public void Start()
        {
            if(State==CityBoxingState.Active)throw new InvalidOperationException("Boxing already active.");
            State=CityBoxingState.Active;Round=Successes=0;RoundTime=LastReaction=0;Responded=claimed=false;
        }
        public bool Respond(CityBoxingAction action)
        {
            if(!Enum.IsDefined(typeof(CityBoxingAction),action))throw new ArgumentOutOfRangeException(nameof(action));
            if(State!=CityBoxingState.Active || Responded)return false;
            // An early or wrong press consumes the round: wait for the cue.
            bool correct=WindowOpen && action==Expected;Responded=true;
            if(correct){Successes++;LastReaction=RoundTime-Windup;}
            return correct;
        }
        public void Tick(double seconds)
        {
            if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
            if(State!=CityBoxingState.Active || seconds==0)return;
            RoundTime+=seconds;
            while(State==CityBoxingState.Active && RoundTime>=RoundDuration)
            {
                RoundTime-=RoundDuration;Round++;Responded=false;
                if(Round>=Rounds){RoundTime=0;State=Successes>=RequiredSuccesses?CityBoxingState.Completed:CityBoxingState.Failed;}
            }
        }
        public bool TryClaim(CityLifestyle life)
        {
            if(life==null)throw new ArgumentNullException(nameof(life));
            if(State!=CityBoxingState.Completed||claimed)return false;
            claimed=true;return life.CompleteBoxing();
        }
        public void Cancel(){if(State==CityBoxingState.Active)State=CityBoxingState.Cancelled;}
    }
}
