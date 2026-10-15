using NUnit.Framework;
namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityGeoRegistryTests
    {
        [Test] public void ProjectionUsesMetersEastAndNorthInsteadOfDegreesAsUnityCoordinates()
        {
            var center=CityGeoRegistry.Project(44.62,21.18,44.62,21.18);
            Assert.That(center.sqrMagnitude,Is.EqualTo(0));
            var north=CityGeoRegistry.Project(44.621,21.18,44.62,21.18);
            Assert.That(north.z,Is.EqualTo(111.32f).Within(.01f));Assert.That(north.x,Is.EqualTo(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>CityGeoRegistry.Project(double.NaN,21,44,21));
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>CityGeoRegistry.Project(45.62,21.18,44.62,21.18));
        }
        [Test] public void CandidateIdsAreUniqueAndConflictIsNotPresentedAsVerified()
        {
            var ids=new System.Collections.Generic.HashSet<string>();int conflicts=0;
            for(int i=0;i<CityGeoRegistry.Count;i++){var pin=CityGeoRegistry.At(i);Assert.IsTrue(ids.Add(pin.Id));CityGeoRegistry.Project(pin.Latitude,pin.Longitude,44.6199926,21.1854);if(pin.ConflictingPin)conflicts++;}
            Assert.That(conflicts,Is.EqualTo(1));
        }
    }
}
