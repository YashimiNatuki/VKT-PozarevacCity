using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityOsmGeometryTests
    {
        [Test]
        public void StaticPerimetersBlockWallCrossingWhileMissionGroundRemainsReachable()
        {
            GameObject owner=new GameObject("OsmCollisionTest");
            try
            {
                CityOsmStreets streets=owner.AddComponent<CityOsmStreets>();streets.Initialize(owner.transform,Vector3.zero);Physics.SyncTransforms();
                Assert.That(streets.WallColliderCount,Is.InRange(1,512));
                BoxCollider sample=null;
                foreach(BoxCollider collider in owner.GetComponentsInChildren<BoxCollider>())
                {
                    if(collider.name!="OSM_WallCollision")continue;
                    Assert.That(collider.size.x*collider.size.y*collider.size.z,Is.GreaterThan(0));
                    Assert.That(collider.isTrigger,Is.False);sample=collider;
                    Vector3 origin=collider.transform.position+collider.transform.right*2;
                    Assert.That(collider.Raycast(new Ray(origin,-collider.transform.right),out RaycastHit wallHit,4),Is.True,"Perimeter wall did not block a crossing ray.");
                }
                Assert.That(sample,Is.Not.Null);
                int doors=0;
                foreach(Transform doorway in owner.GetComponentsInChildren<Transform>())
                {
                    if(doorway.name!="OSM_Doorway")continue;doors++;
                    Ray open=new Ray(doorway.position+Vector3.up+doorway.right*.3f,-doorway.right);
                    Ray header=new Ray(doorway.position+Vector3.up*2.75f+doorway.right*.3f,-doorway.right);
                    bool headerBlocked=false;
                    foreach(BoxCollider wall in doorway.parent.GetComponentsInChildren<BoxCollider>())
                    {
                        Assert.That(wall.Raycast(open,out RaycastHit openHit,.6f),Is.False,"Authored doorway was blocked at walking height.");
                        headerBlocked|=wall.Raycast(header,out RaycastHit headerHit,.6f);
                    }
                    Assert.That(headerBlocked,Is.True,"Doorway header did not block above the opening.");
                }
                Assert.That(doors,Is.GreaterThan(0));
                Vector3[] stations={Vector3.zero,new Vector3(2,0,8),new Vector3(-5,0,22),new Vector3(4,0,36),new Vector3(8,0,36)};
                foreach(Vector3 station in stations)
                {
                    Assert.That(Physics.Raycast(station+Vector3.up*2,Vector3.down,out RaycastHit ground,3),Is.True);
                    Assert.That(ground.collider.name,Is.EqualTo("OSM_FlatOfflineGround"),"A current mission position was enclosed by building collision.");
                }
            }
            finally{Object.DestroyImmediate(owner);}
        }        [Test]
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
