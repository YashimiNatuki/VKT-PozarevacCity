using System;
using System.IO;
using NUnit.Framework;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityMissionPersistenceTests
    {
        private static CityMissionContract Contract() => new CityMissionContract("Courier","Route",120,600,12,4,85,"Pickup","Deliver","Confirm");
        private static CityProgression Campaign(CitySession session)
        {
            var profile=new CityProgression(session);profile.CreateCharacter("Mini Yash");
            for(int i=1;i<=3;i++)profile.CompleteContract(i);
            return profile;
        }
        [TestCase(1)] [TestCase(2)]
        public void ActiveMainRouteRestoresCargoReceiptClockAndChallenge(int stage)
        {
            var session=new CitySession(1000);var profile=new CityProgression(session);profile.CreateCharacter("Mini Yash");
            var run=new CityMissionRun(Contract(),session);run.TrySelectChallenge(CityMissionChallenge.Express);
            for(int i=0;i<stage;i++)run.TryVisit(i);run.Tick(7.25);
            profile.BindMissionSnapshot(()=>run.Snapshot(1));var saved=profile.Snapshot();CityProgression.Validate(saved);
            var restored=CityMissionRun.Restore(Contract(),session,saved.mission);
            Assert.That(restored.ObjectiveIndex,Is.EqualTo(stage));Assert.That(restored.Cargo,Is.EqualTo(stage==1?1:0));
            Assert.That(restored.RemainingSeconds,Is.EqualTo(64.75).Within(.00001));
            Assert.That(restored.Challenge,Is.EqualTo(CityMissionChallenge.Express));
            for(int i=stage;i<3;i++)Assert.That(restored.TryVisit(i),Is.True);
            Assert.That(session.Cash,Is.EqualTo(1900));Assert.That(restored.TryVisit(2),Is.False);
        }
        [Test] public void RewardObserverCanAtomicallySaveTerminalMainStateBeforeDirectorCallback()
        {
            var session=new CitySession(1000);var profile=new CityProgression(session);profile.CreateCharacter("Mini Yash");
            var run=new CityMissionRun(Contract(),session);profile.BindMissionSnapshot(()=>run.Snapshot(1));
            CityProfileData observed=null;session.Changed+=()=>observed=profile.Snapshot();
            for(int i=0;i<3;i++)run.TryVisit(i);
            Assert.That(observed.completedContracts,Is.EqualTo(1));Assert.That(observed.cash,Is.EqualTo(1600));CityProgression.Validate(observed);
            var restored=CityMissionRun.Restore(Contract(),session,observed.mission);
            Assert.That(restored.TryVisit(2),Is.False);Assert.That(session.Cash,Is.EqualTo(1600));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void StreetRouteSurvivesDiskReloadAndPaysOnce(int stage)
        {
            string folder=Path.Combine(Path.GetTempPath(),"VKT-checkpoint-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            try
            {
                var session=new CitySession(1000);var profile=Campaign(session);var jobs=new CityStreetJobs(session,123);
                profile.BindStreetJobs(jobs);jobs.TryAccept();
                for(int i=0;i<stage;i++)jobs.TryVisit(jobs.Offer.StationAt(i));jobs.Tick(5.75);
                int reward=jobs.Offer.CashReward;string title=jobs.Offer.Title;double remaining=jobs.RemainingSeconds;
                var store=new CitySaveStore(Path.Combine(folder,"profile.json"));store.Save(profile.Snapshot());
                var reader=new CitySaveStore(store.PathName);Assert.That(reader.TryLoad(out var saved,out _),Is.True);
                var restoredSession=CitySession.Restore(saved);
                var restoredProfile=new CityProgression(restoredSession,saved);var restored=new CityStreetJobs(restoredSession);
                restoredProfile.BindStreetJobs(restored);
                Assert.That(restored.Offer.Title,Is.EqualTo(title));Assert.That(restored.ObjectiveIndex,Is.EqualTo(stage));
                Assert.That(restored.RemainingSeconds,Is.EqualTo(remaining));Assert.That(restored.IsActive,Is.True);
                for(int i=stage;i<3;i++)restored.TryVisit(restored.Offer.StationAt(i));
                reader.Save(restoredProfile.Snapshot());var terminalReader=new CitySaveStore(store.PathName);
                Assert.That(terminalReader.TryLoad(out var terminal,out _),Is.True);
                Assert.That(terminal.cash,Is.EqualTo(1000+reward));Assert.That(terminal.completedStreetJobs,Is.EqualTo(1));
                var paidProfile=new CityProgression(restoredSession,terminal);var paid=new CityStreetJobs(restoredSession);paidProfile.BindStreetJobs(paid);
                Assert.That(paid.TryVisit(0),Is.False);Assert.That(paid.TryAccept(),Is.False);Assert.That(paid.CooldownSeconds,Is.EqualTo(30));
            }
            finally { Directory.Delete(folder,true); }
        }
        [Test] public void OldProfilesMigrateAndImpossibleCheckpointsFailClosed()
        {
            var old=new CityProfileData{version=6,callsign="Mini Yash"};var profile=new CityProgression(new CitySession(0),old);
            Assert.That(profile.Snapshot().version,Is.EqualTo(7));Assert.That(profile.SavedMission,Is.Null);
            old.mission=new CityMissionSaveData{contractNumber=1,state=CityMissionState.Active,objectiveIndex=3,attempt=1,remainingSeconds=4};
            Assert.Throws<ArgumentException>(()=>CityProgression.Validate(old));old.mission=null;
            old.streetJob=new CityStreetJobSaveData{offerSeed=1,active=true,remainingSeconds=60};
            Assert.Throws<ArgumentException>(()=>CityProgression.Validate(old));
            old.completedContracts=3;old.streetJob.remainingSeconds=double.NaN;
            Assert.Throws<ArgumentException>(()=>CityProgression.Validate(old));
        }
        [Test] public void FailedRouteReloadDoesNotRestoreCargoOrPay()
        {
            var session=new CitySession(1000);var run=new CityMissionRun(Contract(),session);run.TryVisit(0);run.Tick(120);
            var restored=CityMissionRun.Restore(Contract(),session,run.Snapshot(1));
            Assert.That(restored.State,Is.EqualTo(CityMissionState.Failed));Assert.That(restored.Cargo,Is.Zero);
            Assert.That(restored.TryVisit(1),Is.False);Assert.That(session.Cash,Is.EqualTo(1000));
        }
        [Test] public void OfferSpecificDeadlineIsRejectedDuringProfileValidationBeforeBinding()
        {
            var profile=Campaign(new CitySession(1000)).Snapshot();
            for(uint seed=1;seed<100;seed++)
            {
                var offer=new CityStreetJobs(new CitySession(0),seed);
                if(offer.Offer.TimeLimit>=165)continue;
                profile.streetJob=new CityStreetJobSaveData{offerSeed=seed,active=true,objectiveIndex=1,remainingSeconds=165};
                Assert.Throws<ArgumentException>(()=>CityProgression.Validate(profile));return;
            }
            Assert.Fail("Fixture did not find a shorter offer.");
        }
    }
}
