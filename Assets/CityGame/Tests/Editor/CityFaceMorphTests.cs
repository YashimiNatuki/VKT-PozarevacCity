using NUnit.Framework;
using UnityEngine;
using System.Reflection;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityFaceMorphTests
    {
        [Test] public void JawMovesOnlyLowerHeadAndLeavesImportedMeshUntouched()
        {
            GameObject model=Object.Instantiate(CityCharacterModel.LoadSource());
            CityCharacterPhysique body=null;
            try
            {
                var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
                Mesh imported=skin.sharedMesh;Vector3[] source=imported.vertices;
                int oldCount=imported.blendShapeCount;
                body=new CityCharacterPhysique(model);body.Apply(0);
                Assert.AreNotSame(imported,skin.sharedMesh);
                int shape=skin.sharedMesh.GetBlendShapeIndex(CityFaceMorph.JawShape);
                Assert.That(shape,Is.GreaterThanOrEqualTo(0));
                var delta=new Vector3[source.Length];skin.sharedMesh.GetBlendShapeFrameVertices(shape,0,delta,null,null);
                var weights=imported.boneWeights;int changed=0;
                for(int i=0;i<delta.Length;i++)
                {
                    Assert.IsFalse(float.IsNaN(delta[i].z));
                    if(delta[i].sqrMagnitude>0)
                    {
                        changed++;
                        Assert.AreEqual("Head",skin.bones[weights[i].boneIndex0].name);
                        Assert.That(weights[i].weight0,Is.GreaterThanOrEqualTo(.99f));
                        Assert.That(source[i].z,Is.LessThan(.032f));
                        Assert.That(delta[i].z,Is.InRange(-.0007f,0));
                    }
                    if(source[i].z>.033f)Assert.That(delta[i],Is.EqualTo(Vector3.zero),"Skull and eye region must stay fixed.");
                }
                Assert.That(changed,Is.GreaterThan(8).And.LessThan(100));
                body.SetJaw(.4f);body.Apply(5);body.Apply(0);
                Assert.That(skin.GetBlendShapeWeight(shape),Is.EqualTo(40).Within(.001));
                Assert.That(imported.vertices,Is.EqualTo(source));Assert.AreEqual(oldCount,imported.blendShapeCount);
                body.SetJaw(float.NaN);Assert.That(skin.GetBlendShapeWeight(shape),Is.Zero);
                body.SetJaw(9);Assert.That(skin.GetBlendShapeWeight(shape),Is.EqualTo(100));
            }
            finally{body?.Dispose();Object.DestroyImmediate(model);}
        }

        [Test] public void JawFeedbackIsBoundedAndRespondsToEffortWithoutClaimingSpeech()
        {
            foreach(CityFaceMood mood in System.Enum.GetValues(typeof(CityFaceMood)))
                foreach(float value in new[]{-1f,0,.5f,1,100,float.NaN,float.PositiveInfinity})
                    Assert.That(CityFaceMorph.JawWeight(mood,value,float.NaN),Is.InRange(0,1));
            Assert.That(CityFaceMorph.JawWeight(CityFaceMood.Effort,1,0),Is.GreaterThan(CityFaceMorph.JawWeight(CityFaceMood.Calm,0,0)));
        }

        [Test] public void DisablingFaceReleasesItsJawPose()
        {
            GameObject model=Object.Instantiate(CityCharacterModel.LoadSource());CityCharacterPhysique body=null;
            Material material=new Material(Resources.Load<Shader>("StreetCharacter"));
            try
            {
                body=new CityCharacterPhysique(model);
                var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();int jaw=skin.sharedMesh.GetBlendShapeIndex(CityFaceMorph.JawShape);
                var face=model.AddComponent<CityFaceExpression>();face.Initialize(material,body);body.SetJaw(.4f);
                typeof(CityFaceExpression).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(face,null);
                Assert.That(skin.GetBlendShapeWeight(jaw),Is.Zero);
            }
            finally{body?.Dispose();Object.DestroyImmediate(model);Object.DestroyImmediate(material);}
        }
    }
}
