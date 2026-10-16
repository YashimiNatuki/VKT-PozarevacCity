using NUnit.Framework;
using UnityEngine;
namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityCharacterAssetTests
    {
        [Test] public void RelaxedArmPoseKeepsHandsDownInTheModelsOwnOrientation()
        {
            GameObject model=new GameObject("RelaxedPoseTest");
            try
            {
                model.transform.rotation=Quaternion.Euler(0,85,0);
                Transform arm=new GameObject("UpperArm").transform;arm.SetParent(model.transform,false);
                Transform elbow=new GameObject("Elbow").transform;elbow.SetParent(arm,false);elbow.localPosition=Vector3.right*.35f;
                CityActorMotion.RelaxArm(arm,elbow,model.transform,-1,0);
                Assert.That(Vector3.Dot((elbow.position-arm.position).normalized,-model.transform.up),Is.GreaterThan(.98f));
                Assert.That(Vector3.Distance(elbow.position,arm.position),Is.EqualTo(.35f).Within(.001f));
            }
            finally{Object.DestroyImmediate(model);}
        }
        [Test] public void SampledIdleAndRunKeepTheSkinnedCharacterAtHumanScale()
        {
            GameObject wrapper=new GameObject("CharacterScaleRegression");
            try
            {
                GameObject model=Object.Instantiate(CityCharacterModel.LoadSource(),wrapper.transform);
                AnimationClip idle=System.Array.Find(Resources.LoadAll<AnimationClip>("Models/KenneyRetro/idle"),clip=>!clip.name.StartsWith("__preview__"));
                Bounds before=CityCharacterModel.Measure(model);
                var renderer=model.GetComponentInChildren<SkinnedMeshRenderer>();
                GameObject rig=CityCharacterModel.AnimationRoot(model);
                int matching=0;foreach(var binding in UnityEditor.AnimationUtility.GetCurveBindings(idle))if(binding.path.Length>0&&rig.transform.Find(binding.path)!=null)matching++;
                if(matching==0)Debug.Log("MODEL_PATHS "+string.Join(",",System.Array.ConvertAll(model.GetComponentsInChildren<Transform>(),item=>UnityEditor.AnimationUtility.CalculateTransformPath(item,model.transform))));
                Assert.That(matching,Is.GreaterThan(10),"Motion does not bind to the reviewed skeleton.");
                bool normalized=CityCharacterModel.Normalize(wrapper.transform,model,idle);
                if(!normalized)
                {
                    Debug.Log("MODEL_DIAGNOSTIC before="+before+" after="+CityCharacterModel.Measure(model)+" rootScale="+model.transform.localScale+" renderer="+renderer.bounds+" rendererScale="+renderer.transform.lossyScale);
                    foreach(var binding in UnityEditor.AnimationUtility.GetCurveBindings(idle))if(binding.path.Length<15&&(binding.propertyName.Contains("Scale")||binding.propertyName.Contains("Position")))
                        Debug.Log("CURVE "+binding.path+" / "+binding.propertyName+" = "+UnityEditor.AnimationUtility.GetEditorCurve(idle,binding).Evaluate(0));
                }
                Assert.IsTrue(normalized);
                Transform[] bones=renderer.bones;Quaternion[] idleRotations=null;
                foreach(string motion in new[]{"idle","run"})
                {
                    AnimationClip clip=System.Array.Find(Resources.LoadAll<AnimationClip>("Models/KenneyRetro/"+motion),item=>!item.name.StartsWith("__preview__"));
                    clip.SampleAnimation(rig,clip.length*.35f);Bounds bounds=CityCharacterModel.Measure(model);
                    Assert.That(bounds.size.y,Is.InRange(1.2f,2.3f),motion+" animation overwrote model scale.");
                    Assert.That(bounds.center.magnitude,Is.LessThan(3),motion+" moved skin outside its actor.");
                    if(motion=="idle")idleRotations=System.Array.ConvertAll(bones,bone=>bone.rotation);
                    else{float changed=0;for(int i=0;i<bones.Length;i++)changed=Mathf.Max(changed,Quaternion.Angle(idleRotations[i],bones[i].rotation));Assert.That(changed,Is.GreaterThan(5),"Run clip did not change the deformed skeleton pose.");}
                }
            }
            finally{Object.DestroyImmediate(wrapper);}
        }
        [Test] public void ReviewedCharacterContainsSkinnedGeometryAndPlayableLegacyMotion()
        {
            GameObject asset=Resources.Load<GameObject>("Models/KenneyRetro/characterMedium");Assert.That(asset,Is.Not.Null);
            Assert.That(asset.GetComponentsInChildren<SkinnedMeshRenderer>().Length,Is.GreaterThan(0));
            foreach(string motion in new[]{"idle","run"})
            {
                AnimationClip[] clips=Resources.LoadAll<AnimationClip>("Models/KenneyRetro/"+motion);int playable=0;
                foreach(AnimationClip clip in clips)if(!clip.name.StartsWith("__preview__")){Assert.IsTrue(clip.legacy);Assert.That(clip.length,Is.GreaterThan(0));playable++;}
                Assert.That(playable,Is.GreaterThan(0));
            }
            Assert.That(Resources.Load<Texture2D>("Models/KenneyRetro/humanMaleA"),Is.Not.Null);
        }
    }
}
