using NUnit.Framework;
using UnityEngine;
namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityGroundProbeTests
    {
        [Test]
        public void GroundSelectionUsesAuthoredElevationInsteadOfAlwaysChoosingRoof()
        {
            GameObject ground=GameObject.CreatePrimitive(PrimitiveType.Cube),roof=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                ground.transform.position=new Vector3(1200,-.5f,1200);ground.transform.localScale=new Vector3(10,1,10);
                roof.transform.position=new Vector3(1200,9.5f,1200);roof.transform.localScale=new Vector3(10,1,10);Physics.SyncTransforms();
                CityGroundProbe probe=new CityGroundProbe();
                Assert.That(probe.TryFindSurface(new Vector3(1200,3,1200),out Vector3 floorPoint),Is.True);
                Assert.That(floorPoint.y,Is.EqualTo(0).Within(.01f));
                Assert.That(probe.TryFindSurface(new Vector3(1200,11,1200),out Vector3 roofPoint),Is.True);
                Assert.That(roofPoint.y,Is.EqualTo(10).Within(.01f));
                Assert.That(probe.TryFindSurface(new Vector3(float.NaN,3,1200),out _),Is.False);
            }
            finally{Object.DestroyImmediate(ground);Object.DestroyImmediate(roof);}
        }
        [Test]
        public void MissionMarkerTopDoesNotBecomeSpawnGround()
        {
            GameObject ground=GameObject.CreatePrimitive(PrimitiveType.Cube),marker=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                ground.transform.position=new Vector3(1220,-.5f,1220);ground.transform.localScale=new Vector3(10,1,10);
                marker.transform.position=new Vector3(1220,2,1220);marker.AddComponent<CityMission>();Physics.SyncTransforms();
                Assert.That(new CityGroundProbe().TryFindSurface(new Vector3(1220,3,1220),out Vector3 point),Is.True);
                Assert.That(point.y,Is.EqualTo(0).Within(.01f));
            }
            finally{Object.DestroyImmediate(ground);Object.DestroyImmediate(marker);}
        }
    }
}
