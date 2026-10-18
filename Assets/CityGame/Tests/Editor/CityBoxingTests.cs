using System;
using NUnit.Framework;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityBoxingTests
    {
        private static void Finish(CityBoxingRun run)
        {
            for(int i=0;i<8;i++){run.Tick(.8);Assert.That(run.Respond(run.Expected),Is.True);run.Tick(1);}
        }
        [Test] public void CorrectReactionAndGuardPayOnceAndDevelopEnduranceSeparately()
        {
            var life=new CityLifestyle(new CitySession(1000));var run=new CityBoxingRun();
            for(int n=0;n<3;n++)
            {
                Assert.That(life.TryStartBoxing(),Is.True);run.Start();Finish(run);
                Assert.That(run.TryClaim(life),Is.True);Assert.That(run.TryClaim(life),Is.False);life.Tick(90,false);
            }
            Assert.That(life.BoxingSessions,Is.EqualTo(3));Assert.That(life.Endurance,Is.EqualTo(1));
            Assert.That(life.Strength,Is.Zero);Assert.That(life.Workouts,Is.Zero);Assert.That(life.Experience,Is.EqualTo(90));
            Assert.That(new CityLifestyle(new CitySession(0),life.Snapshot()).Endurance,Is.EqualTo(1));
        }
        [Test] public void EarlySpamWrongActionAndStallsCannotEarnTraining()
        {
            var life=new CityLifestyle(new CitySession(0));life.TryStartBoxing();var run=new CityBoxingRun();run.Start();
            Assert.That(run.Respond(CityBoxingAction.Jab),Is.False);run.Tick(.8);
            Assert.That(run.Respond(CityBoxingAction.Jab),Is.False);run.Tick(1);run.Tick(.8);
            Assert.That(run.Expected,Is.EqualTo(CityBoxingAction.Guard));Assert.That(run.Respond(CityBoxingAction.Jab),Is.False);
            run.Tick(100);Assert.That(run.State,Is.EqualTo(CityBoxingState.Failed));Assert.That(run.TryClaim(life),Is.False);
            Assert.That(life.Experience,Is.Zero);
        }
        [Test] public void CrimeDeathReloadAndCancelPreserveRestAndRejectReward()
        {
            var life=new CityLifestyle(new CitySession(0));var run=new CityBoxingRun();life.TryStartBoxing();run.Start();Finish(run);
            life.ReportCrime();Assert.That(run.TryClaim(life),Is.False);Assert.That(life.BoxingSessions,Is.Zero);
            life.OnDeath();var restored=new CityLifestyle(new CitySession(0),life.Snapshot());
            Assert.That(restored.IsBoxing,Is.False);Assert.That(restored.TryStartBoxing(),Is.False);
        }
        [Test] public void InvalidTimeAndPauseCannotAdvanceReactionWindow()
        {
            var run=new CityBoxingRun();run.Start();run.Tick(0);Assert.That(run.RoundTime,Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(()=>run.Tick(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(()=>run.Tick(-1));
        }
    }
}
