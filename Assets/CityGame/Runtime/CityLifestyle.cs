using System;

namespace Pozarevac.CityGame
{
    [Serializable]
    public sealed class CityLifestyleData
    {
        public float nutrition = 80, stamina = 100;
        public int experience, workouts, meals, stolenMeals, escapes, arrests;
        public double workoutCooldown, mealCooldown;
        public bool wanted, challengeClaimed;
    }

    // Game-time character development; no offline hunger or automatic offline escape.
    public sealed class CityLifestyle
    {
        public const int MealPrice = 35, RequiredReps = 24, MaximumCount = 1000000;
        public const double WorkoutDuration = 15, EscapeDuration = 18, ArrestDuration = 2.5;
        private readonly CityLifestyleData data;
        private readonly CitySession session;
        private double tapCooldown, dirtyClock;
        private bool sprintExhausted;
        public event Action Changed;
        public float Nutrition => data.nutrition;
        public float Stamina => data.stamina;
        public int Experience => data.experience;
        public int Level => 1 + Math.Min(19, Experience / 100);
        public int Strength => Math.Min(5, data.workouts / 3);
        public int Workouts => data.workouts;
        public int Meals => data.meals;
        public int Escapes => data.escapes;
        public int Arrests => data.arrests;
        public bool Wanted => data.wanted;
        public bool ChallengeClaimed => data.challengeClaimed;
        public bool IsTraining { get; private set; }
        public int Reps { get; private set; }
        public double TrainingRemaining { get; private set; }
        public double WorkoutCooldown => data.workoutCooldown;
        public double EscapeSeconds { get; private set; }
        public double CaptureSeconds { get; private set; }
        public bool CanSprint => !IsTraining && !sprintExhausted && Stamina > 5 && Nutrition > 5;
        public float SprintMultiplier => Nutrition < 20 ? .8f : 1f + Strength * .025f;

        public CityLifestyle(CitySession owner, CityLifestyleData saved = null)
        {
            session = owner ?? throw new ArgumentNullException(nameof(owner));
            Validate(saved); data = Copy(saved) ?? new CityLifestyleData();
            sprintExhausted = Stamina <= 5;
        }
        public static void Validate(CityLifestyleData value)
        {
            if (value == null) return; // v1-v4 migration.
            if (!Finite(value.nutrition) || !Finite(value.stamina) || value.nutrition < 0 || value.nutrition > 100 || value.stamina < 0 || value.stamina > 100 ||
                !ValidCount(value.experience) || !ValidCount(value.workouts) || !ValidCount(value.meals) || !ValidCount(value.stolenMeals) ||
                !ValidCount(value.escapes) || !ValidCount(value.arrests) || value.stolenMeals > value.meals ||
                !Finite(value.workoutCooldown) || value.workoutCooldown < 0 || value.workoutCooldown > 90 ||
                !Finite(value.mealCooldown) || value.mealCooldown < 0 || value.mealCooldown > 20 ||
                (value.challengeClaimed && (value.workouts < 1 || value.meals < 1 || value.escapes < 1)))
                throw new ArgumentException("Invalid lifestyle state.");
        }
        public static CityLifestyleData Copy(CityLifestyleData value) => value == null ? null : new CityLifestyleData
        {
            nutrition=value.nutrition,stamina=value.stamina,experience=value.experience,workouts=value.workouts,meals=value.meals,
            stolenMeals=value.stolenMeals,escapes=value.escapes,arrests=value.arrests,workoutCooldown=value.workoutCooldown,
            mealCooldown=value.mealCooldown,wanted=value.wanted,challengeClaimed=value.challengeClaimed
        };
        public CityLifestyleData Snapshot() => Copy(data);
        public bool TryStartWorkout()
        {
            if (IsTraining || Wanted || Nutrition < 15 || Stamina < 30 || data.workoutCooldown > 0) return false;
            IsTraining=true; Reps=0; TrainingRemaining=WorkoutDuration; tapCooldown=0; Notify(); return true;
        }
        public bool TryRep()
        {
            if (!IsTraining || tapCooldown > 0 || Stamina < 1) return false;
            Reps++; data.stamina=Math.Max(0,Stamina-1); tapCooldown=.18;
            if (Reps >= RequiredReps)
            {
                IsTraining=false;TrainingRemaining=0;data.workouts=Increment(data.workouts);data.workoutCooldown=90;
                data.nutrition=Math.Max(0,Nutrition-5); GainExperience(40); TryChallenge();
            }
            Notify(); return true;
        }
        public void CancelWorkout()
        {
            if (!IsTraining) return;
            IsTraining=false;TrainingRemaining=0;data.workoutCooldown=Math.Max(10,data.workoutCooldown);Notify();
        }
        public bool TryEat(bool steal)
        {
            if (IsTraining || Wanted || Nutrition > 90 || data.mealCooldown > 0 || (!steal && session.Cash < MealPrice)) return false;
            // Commit the meal before cash/heat observers can snapshot the transaction.
            data.nutrition=Math.Min(100,Nutrition+35);data.stamina=Math.Min(100,Stamina+20);
            if(Stamina>=20)sprintExhausted=false;
            data.meals=Increment(data.meals);data.mealCooldown=20;
            if (steal) { data.stolenMeals=Increment(data.stolenMeals); BeginPursuit(); session.AddHeat(30); }
            else session.TrySpend(MealPrice);
            TryChallenge();Notify();return true;
        }
        public void ReportCrime()
        {
            BeginPursuit();Notify();
        }
        private void BeginPursuit()
        {
            data.wanted=true;EscapeSeconds=0;CaptureSeconds=0;CancelWorkout();
        }
        public void Tick(double seconds, bool sprinting)
        {
            CheckSeconds(seconds);if(seconds==0)return;
            data.nutrition=(float)Math.Max(0,Nutrition-seconds/20);
            data.stamina=(float)Math.Clamp(Stamina+(sprinting?-(10-Strength):IsTraining?0:8)*seconds,0,100);
            if(Stamina<=5)sprintExhausted=true;else if(Stamina>=20)sprintExhausted=false;
            data.workoutCooldown=Math.Max(0,data.workoutCooldown-seconds);data.mealCooldown=Math.Max(0,data.mealCooldown-seconds);
            tapCooldown=Math.Max(0,tapCooldown-seconds);
            if(IsTraining){TrainingRemaining=Math.Max(0,TrainingRemaining-seconds);if(TrainingRemaining<=0)CancelWorkout();}
            dirtyClock+=seconds;if(dirtyClock>=1){dirtyClock=0;Notify();}
        }
        public void TickPolice(double seconds, bool visible, bool inReach)
        {
            CheckSeconds(seconds);if(!Wanted||seconds==0)return;
            EscapeSeconds=visible?0:Math.Min(EscapeDuration,EscapeSeconds+seconds);
            CaptureSeconds=visible&&inReach?Math.Min(ArrestDuration,CaptureSeconds+seconds):0;
            if(CaptureSeconds>=ArrestDuration)
            {
                data.wanted=false;data.arrests=Increment(data.arrests);EscapeSeconds=CaptureSeconds=0;
                int fine=Math.Min(120,session.Cash);session.CoolDown(session.Heat);if(fine>0)session.TrySpend(fine);Notify();
            }
            else if(EscapeSeconds>=EscapeDuration)
            {
                data.wanted=false;data.escapes=Increment(data.escapes);EscapeSeconds=CaptureSeconds=0;
                GainExperience(30);session.CoolDown(Math.Max(0,session.Heat-15));TryChallenge();Notify();
            }
        }
        public void OnDeath(){CancelWorkout();CaptureSeconds=EscapeSeconds=0;}
        private void TryChallenge()
        {
            if(data.challengeClaimed||Workouts<1||Meals<1||Escapes<1)return;
            data.challengeClaimed=true;GainExperience(60);session.Earn(250,10);
        }
        private void GainExperience(int amount) => data.experience=Math.Min(MaximumCount,data.experience+amount);
        private void Notify()=>Changed?.Invoke();
        private static bool ValidCount(int n)=>n>=0&&n<=MaximumCount;
        private static int Increment(int n)=>Math.Min(MaximumCount,n+1);
        private static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
        private static void CheckSeconds(double n){if(!Finite(n)||n<0)throw new ArgumentOutOfRangeException(nameof(n));}
    }
}
