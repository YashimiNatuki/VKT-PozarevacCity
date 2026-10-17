using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityMapUpgradeTests
    {
        [Test]
        public void SharedCornerUsesSameBoundedEdgesFromEitherTravelDirection()
        {
            Vector3 a=Vector3.back,b=Vector3.right;
            Vector3 one=CityRoadGeometry.NodeOffset(a,b,Vector3.Cross(Vector3.forward,Vector3.up));
            Vector3 two=CityRoadGeometry.NodeOffset(a,b,Vector3.Cross(Vector3.left,Vector3.up));
            Assert.That(one.magnitude,Is.LessThanOrEqualTo(1.251f));
            Assert.That(Vector3.Distance(one,-two),Is.LessThan(.0001f));
            Vector3 reverse=CityRoadGeometry.NodeOffset(Vector3.forward,Vector3.forward,Vector3.right);
            Assert.That(float.IsFinite(reverse.x)&&float.IsFinite(reverse.z),Is.True);
        }
        [Test]
        public void JunctionMeshIsUpwardAndBoundedAroundItsRealNode()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();Vector3 node=new Vector3(4,0,-7);
            CityRoadGeometry.Junction(node,4.5f,vertices,triangles);
            Assert.That(triangles.Count,Is.EqualTo(72));
            foreach(Vector3 point in vertices)Assert.That(Vector2.Distance(new Vector2(point.x,point.z),new Vector2(4,-7)),Is.LessThanOrEqualTo(4.501f));
            for(int i=0;i<triangles.Count;i+=3)
                Assert.That(Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).y,Is.GreaterThan(0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CityRoadGeometry.Junction(node,float.NaN,vertices,triangles));
        }
        [Test]
        public void RenderBudgetHasHysteresisAndNeverDisablesCollision()
        {
            GameObject root=new GameObject("VisibilityTest"),building=GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.transform.SetParent(root.transform,false);
            try
            {
                Renderer renderer=building.GetComponent<Renderer>();Collider collider=building.GetComponent<Collider>();
                var visibility=root.AddComponent<CityMapVisibility>();visibility.Register(renderer,false);
                visibility.ApplyAt(new Vector3(410,0,0));Assert.That(renderer.enabled,Is.False);
                Assert.That(collider.enabled&&building.activeInHierarchy,Is.True);
                visibility.ApplyAt(new Vector3(380,0,0));Assert.That(renderer.enabled,Is.False,"Hidden geometry should not oscillate in the hysteresis band.");
                visibility.ApplyAt(new Vector3(350,0,0));Assert.That(renderer.enabled,Is.True);
                visibility.ApplyAt(new Vector3(380,0,0));Assert.That(renderer.enabled,Is.True);
                Assert.Throws<ArgumentOutOfRangeException>(()=>visibility.ApplyAt(new Vector3(float.NaN,0,0)));
                visibility.enabled=false;Assert.That(renderer.enabled,Is.True);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test]
        public void BundledLayoutComposesJunctionsAndReviewedStreetSigns()
        {
            GameObject root=new GameObject("MapUpgradeTest");
            try
            {
                var streets=root.AddComponent<CityOsmStreets>();streets.Initialize(root.transform,Vector3.zero);
                Assert.That(streets.JunctionCount,Is.GreaterThan(10));
                Assert.That(streets.Visibility.Registered,Is.GreaterThan(streets.Source.buildings.Length));
                int walls=streets.WallColliderCount;streets.Visibility.ApplyAt(Vector3.up*1000);
                Assert.That(streets.Visibility.VisibleCount,Is.Zero);
                Assert.That(streets.WallColliderCount,Is.EqualTo(walls));
                foreach(Collider c in root.GetComponentsInChildren<Collider>())Assert.That(c.enabled,Is.True);
                streets.Visibility.ApplyAt(Vector3.zero);Assert.That(streets.Visibility.VisibleCount,Is.GreaterThan(0));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
