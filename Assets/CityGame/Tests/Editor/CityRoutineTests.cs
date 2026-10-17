using System;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityRoutineTests
    {
        private static void CompleteCourse(CityConditioningRun run)
        {
            for(int stop=0;stop<3;stop++)for(int frame=0;frame<12;frame++)run.Tick(.1,true);
        }
        [Test] public void WantedAtZeroHeatBlocksAcceptanceAndCurrentHandover()
        {
            var session=new CitySession(1000);var life=new CityLifestyle(session);var jobs=new CityStreetJobs(session,7);jobs.BindLifestyle(life);
            life.ReportCrime();Assert.That(session.Heat,Is.Zero);Assert.That(jobs.TryAccept(),Is.False);
            Assert.That(jobs.BlockReason,Does.Contain("Potera"));
            life.TickPolice(CityLifestyle.EscapeDuration,false,false);Assert.That(jobs.TryAccept(),Is.True);
            jobs.TryVisit(jobs.Offer.StationAt(0));Assert.That(jobs.HasCargo,Is.True);
            life.ReportCrime();double remaining=jobs.RemainingSeconds;
            Assert.That(jobs.TryVisit(jobs.Offer.StationAt(1)),Is.False);jobs.Tick(2);
            Assert.That(jobs.RemainingSeconds,Is.EqualTo(remaining-2));Assert.That(jobs.ObjectiveIndex,Is.EqualTo(1));Assert.That(session.Cash,Is.EqualTo(1000));
            life.TickPolice(CityLifestyle.EscapeDuration,false,false);
            int reward=jobs.Offer.CashReward;jobs.TryVisit(jobs.Offer.StationAt(1));
            Assert.That(jobs.HasCargo,Is.False);Assert.That(jobs.HasReceipt,Is.True);
            jobs.TryVisit(jobs.Offer.StationAt(2));Assert.That(jobs.HasReceipt,Is.False);
            Assert.That(session.Cash,Is.EqualTo(1000+reward));Assert.That(jobs.TryVisit(0),Is.False);
        }
        [Test] public void TrainingBlocksContactsAndCannotBypassItsDurableRest()
        {
            var session=new CitySession(1000);var life=new CityLifestyle(session);var jobs=new CityStreetJobs(session);jobs.BindLifestyle(life);
            Assert.That(life.TryStartConditioning(),Is.True);Assert.That(life.CanSprint,Is.True);
            Assert.That(life.TryStartWorkout(),Is.False);Assert.That(life.TryEat(false),Is.False);Assert.That(jobs.TryAccept(),Is.False);
            life.CancelConditioning();Assert.That(life.Experience,Is.Zero);
            var restored=new CityLifestyle(session,life.Snapshot());Assert.That(restored.IsConditioning,Is.False);
            Assert.That(restored.TryStartConditioning(),Is.False);Assert.That(restored.TryStartWorkout(),Is.False);
            restored.Tick(90,false);Assert.That(restored.TryStartWorkout(),Is.True);
        }
        [Test] public void CourseRequiresContinuousDwellAndNeverSkipsAStopOnAStall()
        {
            var course=new CityConditioningRun();course.Start();course.Tick(10,true);
            Assert.That(course.Checkpoint,Is.Zero);Assert.That(course.Hold,Is.EqualTo(.1).Within(.0001));
            course.Tick(.1,false);Assert.That(course.Hold,Is.Zero);
            for(int i=0;i<12;i++)course.Tick(.1,true);
            Assert.That(course.Checkpoint,Is.EqualTo(1));Assert.That(course.State,Is.EqualTo(CityConditioningState.Active));
        }
        [Test] public void CompletedConditioningAwardsOnlyOnceAndPreservesBenchStrength()
        {
            var life=new CityLifestyle(new CitySession(1000));var course=new CityConditioningRun();
            life.TryStartConditioning();course.Start();CompleteCourse(course);
            Assert.That(course.State,Is.EqualTo(CityConditioningState.Completed));Assert.That(course.TryClaim(life),Is.True);
            Assert.That(course.TryClaim(life),Is.False);Assert.That(life.Experience,Is.EqualTo(20));
            Assert.That(life.Workouts,Is.Zero);Assert.That(life.Strength,Is.Zero);Assert.That(life.IsConditioning,Is.False);
            Assert.That(new CityLifestyle(new CitySession(1000),life.Snapshot()).Experience,Is.EqualTo(20));
        }
        [Test] public void ExpiryCrimeAndDeathCannotPayConditioningRewards()
        {
            var life=new CityLifestyle(new CitySession(1000));var course=new CityConditioningRun();
            life.TryStartConditioning();course.Start();course.Tick(CityConditioningRun.Duration,true);
            Assert.That(course.State,Is.EqualTo(CityConditioningState.Expired));Assert.That(course.TryClaim(life),Is.False);
            life.ReportCrime();Assert.That(life.IsConditioning,Is.False);Assert.That(life.Experience,Is.Zero);
            life.OnDeath();Assert.That(life.Experience,Is.Zero);
        }
        [Test] public void InvalidOrZeroTimeCannotAdvanceCourse()
        {
            var run=new CityConditioningRun();run.Start();double remaining=run.Remaining;
            Assert.Throws<ArgumentOutOfRangeException>(()=>run.Tick(double.NaN,true));
            Assert.Throws<ArgumentOutOfRangeException>(()=>run.Tick(-1,true));
            run.Tick(0,true);Assert.That(run.Remaining,Is.EqualTo(remaining));Assert.That(run.Hold,Is.Zero);
        }
        [Test] public void DeliveredButUnconfirmedJobExpiresWithoutInventingAPackageReturn()
        {
            var session=new CitySession(1000);var jobs=new CityStreetJobs(session);jobs.TryAccept();
            jobs.TryVisit(jobs.Offer.StationAt(0));jobs.TryVisit(jobs.Offer.StationAt(1));jobs.Tick(jobs.RemainingSeconds);
            Assert.That(jobs.Message,Does.Contain("potvrde"));Assert.That(jobs.HasCargo||jobs.HasReceipt,Is.False);
            Assert.That(session.Cash,Is.EqualTo(1000));
        }
        [TestCase(CityAudioCue.Checkpoint)] [TestCase(CityAudioCue.Success)] [TestCase(CityAudioCue.Failure)]
        public void FeedbackClipsAreMonoBoundedAndHaveQuietLoopEdges(CityAudioCue cue)
        {
            AudioClip clip=CityGameAudio.CreateFeedbackClip(cue);
            try
            {
                Assert.That(clip.channels,Is.EqualTo(1));Assert.That(clip.frequency,Is.EqualTo(22050));
                float[] data=new float[clip.samples];Assert.That(clip.GetData(data,0),Is.True);
                double square=0;float peak=0;foreach(float value in data){Assert.That(float.IsNaN(value)||float.IsInfinity(value),Is.False);square+=value*value;peak=Math.Max(peak,Math.Abs(value));}
                Assert.That(peak,Is.LessThanOrEqualTo(.201f));Assert.That(Math.Sqrt(square/data.Length),Is.GreaterThan(.03));
                Assert.That(Math.Abs(data[0])+Math.Abs(data[data.Length-1]),Is.LessThan(.0001));
            }
            finally{UnityEngine.Object.DestroyImmediate(clip);}
        }
        [Test] public void RealWantedStateOverridesLowHeatContactAdvice()
        {
            Assert.That(CityContactStory.Action(new CityContactFacts{wanted=true,heat=0,nutrition=100,stamina=100,campaign=3}),Is.EqualTo("evade"));
        }
    }
}
