using NUnit.Framework;
using UnityEngine;
namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityVisualMeshesTests
    {
        [Test] public void RoundedBoxCutsCornersAndRetainsFiniteUnitNormals()
        {
            Mesh mesh=CityVisualMeshes.RoundedBox();
            try
            {
                Vector3[] vertices=mesh.vertices,normals=mesh.normals;int cutCorners=0;
                for(int i=0;i<vertices.Length;i++)
                {
                    Assert.That(vertices[i].sqrMagnitude,Is.LessThan(.751f));Assert.That(normals[i].sqrMagnitude,Is.EqualTo(1).Within(.001f));
                    if(Mathf.Abs(vertices[i].x)>.4f&&Mathf.Abs(vertices[i].y)>.4f&&Mathf.Abs(vertices[i].z)>.4f)cutCorners++;
                }
                Assert.That(cutCorners,Is.GreaterThan(0));Assert.That(mesh.bounds.size.x,Is.EqualTo(1).Within(.001f));
                int[] indices=mesh.triangles;for(int i=0;i<indices.Length;i+=3){Vector3 normal=Vector3.Cross(vertices[indices[i+1]]-vertices[indices[i]],vertices[indices[i+2]]-vertices[indices[i]]);Assert.That(Vector3.Dot(normal,normals[indices[i]]),Is.GreaterThan(0));}
            }
            finally{Object.DestroyImmediate(mesh);}
        }
        [Test] public void RevolvedPlanterHasAnActualCircularProfileAndInvalidTessellationFails()
        {
            Mesh mesh=CityVisualMeshes.Lathe(new[]{new Vector2(.3f,-.5f),new Vector2(.5f,.5f)},32);
            try{foreach(Vector3 vertex in mesh.vertices)Assert.That(new Vector2(vertex.x,vertex.z).magnitude,Is.InRange(.299f,.501f));Assert.That(mesh.bounds.size.z,Is.EqualTo(1).Within(.001f));}
            finally{Object.DestroyImmediate(mesh);}
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>CityVisualMeshes.RoundedBox(.6f));
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>CityVisualMeshes.RoundedBox(float.NaN));
        }
    }
}
