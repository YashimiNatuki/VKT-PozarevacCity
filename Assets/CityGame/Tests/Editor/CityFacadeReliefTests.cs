using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityFacadeReliefTests
    {
        [Test] public void CurvedTrimStaysFiniteAndLeavesAuthoredDoorwayClear()
        {
            var building=new CityOsmStreets.Building{height=9,points=new[]{new CityOsmStreets.Point{x=-5,z=0},new CityOsmStreets.Point{x=5,z=0},new CityOsmStreets.Point{x=5,z=8},new CityOsmStreets.Point{x=-5,z=8},new CityOsmStreets.Point{x=-5,z=0}}};
            Mesh mesh=CityFacadeRelief.Create(building,1);
            try
            {
                Assert.That(mesh.vertexCount,Is.LessThan(1000));Assert.That(mesh.triangles.Length%3,Is.Zero);
                foreach(Vector3 vertex in mesh.vertices)
                {
                    Assert.That(float.IsFinite(vertex.x)&&float.IsFinite(vertex.y)&&float.IsFinite(vertex.z),Is.True);
                    Assert.That(Mathf.Abs(vertex.x)<1.19f && vertex.y<2.4f && Mathf.Abs(vertex.z)<.2f,Is.False,"Trim must leave the doorway passage open.");
                }
            }
            finally{Object.DestroyImmediate(mesh);}
        }
    }
}
