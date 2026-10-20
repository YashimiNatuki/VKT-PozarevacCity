using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityGameplayControlsTests
    {
        [Test] public void BundledInterfaceFontsContainSerbianLatinCharacters()
        {
            foreach(string name in new[]{"NotoSans-Regular","NotoSans-Bold"})
            {
                var font=Resources.Load<Font>("UI/Fonts/"+name);Assert.IsNotNull(font);
                foreach(char letter in "ČĆŠŽĐčćšžđ")Assert.IsTrue(font.HasCharacter(letter),name+" missing "+letter);
            }
        }
        [Test] public void ReviewedActorMotionResolvesBothLegChains()
        {
            var owner=new GameObject("MotionBindings");
            try
            {
                var model=Object.Instantiate(CityCharacterModel.LoadSource(),owner.transform);
                var rig=CityCharacterModel.AnimationRoot(model);var animation=rig.GetComponent<Animation>();if(animation==null)animation=rig.AddComponent<Animation>();
                var motion=owner.AddComponent<CityActorMotion>();motion.Initialize(owner.transform,animation);
                Assert.IsTrue(motion.GaitReady,string.Join(",",System.Array.ConvertAll(rig.GetComponentsInChildren<Transform>(),t=>t.name)));
            }
            finally{Object.DestroyImmediate(owner);}
        }
        [Test] public void GaitBendsTheKneeAndKeepsBothBoneLengthsInARotatedModel()
        {
            var root=new GameObject("GaitTest");
            try
            {
                root.transform.rotation=Quaternion.Euler(0,75,0);
                Transform hip=new GameObject("Hip").transform;hip.SetParent(root.transform,false);
                Transform knee=new GameObject("Knee").transform;knee.SetParent(hip,false);knee.localPosition=Vector3.down*.45f;
                Transform foot=new GameObject("Foot").transform;foot.SetParent(knee,false);foot.localPosition=Vector3.down*.43f;
                CityActorMotion.PoseLeg(hip,knee,foot,root.transform,28,45);
                Assert.That(Vector3.Distance(hip.position,knee.position),Is.EqualTo(.45f).Within(.0001));
                Assert.That(Vector3.Distance(knee.position,foot.position),Is.EqualTo(.43f).Within(.0001));
                Assert.That(Vector3.Angle(knee.position-hip.position,foot.position-knee.position),Is.EqualTo(45).Within(.01));
                Assert.That(Vector3.Dot(knee.position-hip.position,root.transform.forward),Is.GreaterThan(0));
                CityActorMotion.PoseLeg(hip,knee,foot,root.transform,-28,6);
                Assert.That(Vector3.Dot(knee.position-hip.position,root.transform.forward),Is.LessThan(0));
                CityActorMotion.PoseLeg(hip,knee,foot,root.transform,float.NaN,float.PositiveInfinity);
                Assert.IsFalse(float.IsNaN(foot.position.x));
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void RealImportedLegReceivesTheSamePoseAsAUnitScaleTestRig()
        {
            var wrapper=new GameObject("ImportedLegScale");CityCharacterPhysique body=null;
            try
            {
                var model=Object.Instantiate(CityCharacterModel.LoadSource(),wrapper.transform);body=new CityCharacterPhysique(model);
                var idle=System.Array.Find(Resources.LoadAll<AnimationClip>("Models/KenneyRetro/idle"),c=>!c.name.StartsWith("__preview__"));
                Assert.IsTrue(CityCharacterModel.Normalize(wrapper.transform,model,idle,body));
                Transform[] bones=model.GetComponentsInChildren<Transform>();
                var hip=System.Array.Find(bones,b=>b.name=="LeftUpLeg");var knee=System.Array.Find(bones,b=>b.name=="LeftLeg");var foot=System.Array.Find(bones,b=>b.name=="LeftFoot");
                float length=Vector3.Distance(hip.position,knee.position);
                CityActorMotion.PoseLeg(hip,knee,foot,wrapper.transform,0,0);Quaternion straight=knee.localRotation;
                CityActorMotion.PoseLeg(hip,knee,foot,wrapper.transform,28,45);
                Assert.That(Quaternion.Angle(straight,knee.localRotation),Is.GreaterThan(35),"Imported bone world length="+length);
            }
            finally{body?.Dispose();Object.DestroyImmediate(wrapper);}
        }
        [Test] public void CompactMusicDoesNotAcquirePauseOrCursorAndClosesCleanly()
        {
            var root=new GameObject("MusicControlTest");int consumed=0;
            CursorLockMode before=Cursor.lockState;float scale=Time.timeScale;
            try
            {
                var music=root.AddComponent<CityMusicPlayer>();var panel=root.AddComponent<CityMusicPanel>();
                panel.Initialize(music,()=>false,()=>consumed++);panel.Toggle();
                Assert.IsTrue(panel.IsOpen);Assert.That(Time.timeScale,Is.EqualTo(scale));Assert.That(Cursor.lockState,Is.EqualTo(before));
                var rect=(RectTransform)root.transform.Find("VKT_Music_UI/MusicPanel");
                Assert.That(rect.sizeDelta.y,Is.LessThanOrEqualTo(110));
                panel.Close();panel.Close();Assert.That(consumed,Is.EqualTo(1));Assert.IsFalse(panel.IsOpen);
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
