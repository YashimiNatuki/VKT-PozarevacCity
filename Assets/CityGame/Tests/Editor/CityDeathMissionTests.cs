using NUnit.Framework;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityDeathMissionTests
    {
        [Test]
        public void DeathAbortsCargoWithoutRewardAndAllowsCleanRetry()
        {
            CitySession session = new CitySession(100);
            CityMissionRun run = new CityMissionRun(CityMissionDirector.CreateContracts()[0], session);
            Assert.That(run.TryVisit(0), Is.True);
            run.Abort("Player died");
            Assert.That(run.Cargo, Is.Zero);
            Assert.That(run.State, Is.EqualTo(CityMissionState.Failed));
            Assert.That(run.TryVisit(1), Is.False);
            Assert.That(session.Cash, Is.EqualTo(100));
            Assert.That(run.Retry(), Is.True);
            Assert.That(run.ObjectiveIndex, Is.Zero);
        }
    }
}
