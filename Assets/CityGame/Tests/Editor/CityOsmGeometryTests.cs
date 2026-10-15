using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityOsmGeometryTests
    {
        [Test]
        public void RoadRibbonsFaceUpAndStayWithinTheDeclaredOfflineBounds()
        {
            GameObject owner=new GameObject("OsmRoadTest");
            try
            {
                CityOsmStreets streets=owner.AddComponent<CityOsmStreets>();streets.Initialize(owner.transform,Vector3.zero);
                Mesh road=owner.transform.Find("VKT_OSM_RealRoadLayout").GetComponent<MeshFilter>().sharedMesh;
                Assert.That(road.vertexCount,Is.GreaterThan(0));
                foreach(Vector3 normal in road.normals)Assert.That(normal.y,Is.GreaterThan(.99f),"Road triangle winding points away from the sky.");
                foreach(Vector3 vertex in road.vertices)
                {
                    Assert.That(float.IsNaN(vertex.x)||float.IsInfinity(vertex.x)||float.IsNaN(vertex.z)||float.IsInfinity(vertex.z),Is.False);
                    Assert.That(Mathf.Abs(vertex.x),Is.LessThanOrEqualTo(streets.Source.radiusMeters+5));
                    Assert.That(Mathf.Abs(vertex.z),Is.LessThanOrEqualTo(streets.Source.radiusMeters+5));
                }
            }
            finally{Object.DestroyImmediate(owner);}
        }
        [Test]
        public void FacadesHaveUsableNormalsAndRoofsFaceUp()
        {
            GameObject owner=new GameObject("OsmFacadeTest");
            try
            {
                owner.AddComponent<CityOsmStreets>().Initialize(owner.transform,Vector3.zero);
                int facades=0,roofs=0;
                foreach(MeshFilter filter in owner.GetComponentsInChildren<MeshFilter>())
                {
                    if(!filter.name.StartsWith("OSM_Building_"))continue;
                    facades++;
                    Mesh mesh=filter.sharedMesh;Vector3[] vertices=mesh.vertices,normals=mesh.normals;
                    for(int i=0;i<vertices.Length;i++)
                    {
                        Assert.That(normals[i].sqrMagnitude,Is.GreaterThan(.9f),"Opposing duplicate wall triangles cancelled the facade normals.");
                        Assert.That(float.IsNaN(vertices[i].y)||float.IsInfinity(vertices[i].y),Is.False);
                        if(normals[i].y>.99f)roofs++;
                    }
                }
                Assert.That(facades,Is.GreaterThan(0));Assert.That(roofs,Is.GreaterThan(0),"Valid bundled footprints should produce upward-facing roofs.");
            }
            finally{Object.DestroyImmediate(owner);}
        }
        [Test]
        public void BundledSourceHasFiniteGeographicAnchorAndBoundedGeometry()
        {
            CityOsmStreets.Data source=CityOsmStreets.Load();Assert.That(source,Is.Not.Null);
            Assert.That(double.IsNaN(source.anchorLatitude)||double.IsInfinity(source.anchorLatitude),Is.False);
            Assert.That(double.IsNaN(source.anchorLongitude)||double.IsInfinity(source.anchorLongitude),Is.False);
            Assert.That(source.anchorLatitude,Is.InRange(-90d,90d));Assert.That(source.anchorLongitude,Is.InRange(-180d,180d));
            Assert.That(source.roads.Length,Is.InRange(1,200));Assert.That(source.buildings.Length,Is.InRange(1,40));
            Assert.That(source.attribution,Does.Contain("OpenStreetMap"));Assert.That(source.license,Is.EqualTo("ODbL-1.0"));
        }
    }
}
