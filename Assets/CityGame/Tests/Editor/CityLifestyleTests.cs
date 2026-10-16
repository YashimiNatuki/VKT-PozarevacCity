using System;
using NUnit.Framework;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityLifestyleTests
    {
        private static void Workout(CityLifestyle life)
        {
            Assert.That(life.TryStartWorkout(),Is.True);
            for(int i=0;i<CityLifestyle.RequiredReps;i++){Assert.That(life.TryRep(),Is.True);life.Tick(.2,false);}
        }
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)]
        public void OlderProfilesMigrateWithoutChangingBalances(int version)
        {
            CityProfileData old=new CityProfileData{version=version,callsign="Yashimi",cash=75};
            CityProgression p=new CityProgression(CitySession.Restore(old),old);
            Assert.That(p.Lifestyle.Nutrition,Is.EqualTo(80));Assert.That(p.Session.Cash,Is.EqualTo(75));
            Assert.That(p.Snapshot().version,Is.EqualTo(5));Assert.That(p.Snapshot().lifestyle,Is.Not.Null);
            CityProgression.Validate(p.Snapshot());
        }
        [Test]public void SprintExhaustionAndRecoveryAreBoundedAndZeroTimeIsPaused()
        {
            CityLifestyle life=new CityLifestyle(new CitySession(0));
            life.Tick(12,true);Assert.That(life.Stamina,Is.Zero);Assert.That(life.CanSprint,Is.False);
            float food=life.Nutrition;life.Tick(0,false);Assert.That(life.Nutrition,Is.EqualTo(food));
            life.Tick(4,false);Assert.That(life.Stamina,Is.EqualTo(32));Assert.That(life.CanSprint,Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(()=>life.Tick(double.NaN,false));
        }
        [Test]public void WorkoutRequiresTimedRepetitionsAndCannotPayTwice()
        {
            CityLifestyle life=new CityLifestyle(new CitySession(0));Assert.That(life.TryStartWorkout(),Is.True);
            Assert.That(life.TryRep(),Is.True);Assert.That(life.TryRep(),Is.False);
            for(int i=1;i<CityLifestyle.RequiredReps;i++){life.Tick(.2,false);Assert.That(life.TryRep(),Is.True);}
            Assert.That(life.Workouts,Is.EqualTo(1));Assert.That(life.Experience,Is.EqualTo(40));Assert.That(life.TryRep(),Is.False);
            Assert.That(life.TryStartWorkout(),Is.False);Assert.That(life.WorkoutCooldown,Is.EqualTo(90));
        }
        [Test]public void ThreeWorkoutsIncreaseStrengthAndSprintEfficiency()
        {
            CityLifestyle life=new CityLifestyle(new CitySession(0));for(int i=0;i<3;i++){Workout(life);life.Tick(91,false);}
            Assert.That(life.Strength,Is.EqualTo(1));Assert.That(life.Level,Is.EqualTo(2));
            Assert.That(life.SprintMultiplier,Is.GreaterThan(1));
        }
        [Test]public void TimeoutAndReloadCannotCompleteAnUnfinishedWorkout()
        {
            CitySession session=new CitySession(0);CityLifestyle life=new CityLifestyle(session);
            life.TryStartWorkout();life.TryRep();life.Tick(16,false);
            Assert.That(life.IsTraining,Is.False);Assert.That(life.Workouts,Is.Zero);Assert.That(life.Experience,Is.Zero);
            CityLifestyle restored=new CityLifestyle(session,life.Snapshot());Assert.That(restored.IsTraining,Is.False);
        }
        [Test]public void PaidAndStolenMealsHaveDifferentAtomicConsequences()
        {
            CitySession paid=new CitySession(100);CityLifestyle life=new CityLifestyle(paid);
            Assert.That(life.TryEat(false),Is.True);Assert.That(paid.Cash,Is.EqualTo(65));Assert.That(life.Wanted,Is.False);
            Assert.That(life.TryEat(false),Is.False);Assert.That(life.Meals,Is.EqualTo(1));
            CitySession broke=new CitySession(0);CityLifestyle thief=new CityLifestyle(broke);
            Assert.That(thief.TryEat(false),Is.False);Assert.That(thief.Nutrition,Is.EqualTo(80));
            bool committed=false;broke.Changed+=()=>committed=thief.Wanted&&thief.Meals==1;
            Assert.That(thief.TryEat(true),Is.True);Assert.That(committed,Is.True);Assert.That(broke.Heat,Is.EqualTo(30));Assert.That(broke.Cash,Is.Zero);
        }
        [Test]public void PoliceNeedContinuousVisibleContactAndFineCannotOverdraw()
        {
            CitySession session=new CitySession(50);CityLifestyle life=new CityLifestyle(session);life.ReportCrime();
            life.TickPolice(2,true,true);life.TickPolice(1,false,true);Assert.That(life.Arrests,Is.Zero);
            life.TickPolice(2,true,true);Assert.That(life.Arrests,Is.Zero);
            life.TickPolice(.6,true,true);Assert.That(life.Arrests,Is.EqualTo(1));Assert.That(session.Cash,Is.Zero);Assert.That(life.Wanted,Is.False);
            life.TickPolice(10,true,true);Assert.That(life.Arrests,Is.EqualTo(1));
        }
        [Test]public void VisibilityResetsEscapeAndReloadRetainsWantedWithoutOfflineEscape()
        {
            CitySession session=new CitySession(0);CityLifestyle life=new CityLifestyle(session);life.ReportCrime();
            life.TickPolice(17,false,false);life.TickPolice(.1,true,false);Assert.That(life.EscapeSeconds,Is.Zero);
            CityLifestyle restored=new CityLifestyle(session,life.Snapshot());Assert.That(restored.Wanted,Is.True);Assert.That(restored.EscapeSeconds,Is.Zero);
            restored.TickPolice(18,false,false);Assert.That(restored.Escapes,Is.EqualTo(1));Assert.That(restored.Experience,Is.EqualTo(30));
        }
        [Test]public void ChallengeCommitsBeforeReentrantCashObserversAndRewardsOnlyOnce()
        {
            CitySession session=new CitySession(0);CityLifestyle life=new CityLifestyle(session,new CityLifestyleData{workouts=1,meals=1,wanted=true});
            session.Changed+=()=>life.TickPolice(20,false,false);
            life.TickPolice(18,false,false);
            Assert.That(life.ChallengeClaimed,Is.True);Assert.That(session.Cash,Is.EqualTo(250));Assert.That(session.Reputation,Is.EqualTo(10));
            Assert.That(life.Experience,Is.EqualTo(90));Assert.That(life.Escapes,Is.EqualTo(1));
            CityLifestyle restored=new CityLifestyle(session,life.Snapshot());restored.TickPolice(100,false,false);Assert.That(session.Cash,Is.EqualTo(250));
        }
        [Test]public void SnapshotDoesNotAliasAndInvalidPersistentStateIsRejected()
        {
            CityLifestyle life=new CityLifestyle(new CitySession(0));CityLifestyleData copy=life.Snapshot();copy.nutrition=0;Assert.That(life.Nutrition,Is.EqualTo(80));
            Assert.Throws<ArgumentException>(()=>CityLifestyle.Validate(new CityLifestyleData{nutrition=float.NaN}));
            Assert.Throws<ArgumentException>(()=>CityLifestyle.Validate(new CityLifestyleData{challengeClaimed=true}));
            Assert.Throws<ArgumentException>(()=>CityLifestyle.Validate(new CityLifestyleData{stolenMeals=2,meals=1}));
        }
    }
}
