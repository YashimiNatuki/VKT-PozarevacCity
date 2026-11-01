using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityHumanPlayableTests
    {
        private GameObject owner;
        private object actor;
        private Type type;
        [SetUp] public void SetUp()
        {
            type=typeof(CityWardrobePanel).Assembly.GetType("Pozarevac.CityGame.CityHumanActor");
            Assert.That(type,Is.Not.Null,"The human needs a world movement adapter.");
            owner=new GameObject("PhysicalPlayer");owner.AddComponent<CharacterController>().enabled=false;
            object[] args={owner.transform,Resources.Load<GameObject>("Models/RodjaHuman/RodjaHuman"),null};
            Assert.That((bool)type.GetMethod("TryCreate").Invoke(null,args),Is.True);
            actor=args[2];Call("SetVisible",true);
        }
        [TearDown] public void TearDown(){if(actor is IDisposable disposable)disposable.Dispose();if(owner!=null)UnityEngine.Object.DestroyImmediate(owner);}
        [Test] public void MovementUsesActualDisplacementAndStopsAdvancingAgainstAWall()
        {
            for(int i=0;i<60;i++){owner.transform.position+=Vector3.forward*.04f;Call("Advance",.02f,false);}
            Assert.That(Value<float>("ObservedSpeed"),Is.GreaterThan(1));
            float phase=Value<float>("Phase");
            for(int i=0;i<60;i++)Call("Advance",.02f,false);
            Assert.That(Value<float>("Phase"),Is.EqualTo(phase).Within(.0001f));
            Assert.That(Value<float>("ObservedSpeed"),Is.LessThan(.01f));
            Assert.That(owner.GetComponentsInChildren<CharacterController>(true).Length,Is.EqualTo(1));
            Assert.That(owner.GetComponentsInChildren<AudioListener>(true),Is.Empty);
            Assert.That(owner.GetComponentsInChildren<Camera>(true),Is.Empty);
        }
        [Test] public void PausePreservesPoseAndDoesNotReplayMovementOnResume()
        {
            owner.transform.position+=Vector3.forward*.1f;Call("Advance",.02f,false);
            var visual=Value<GameObject>("Visual");
            var knee=Array.Find(visual.GetComponentsInChildren<Transform>(),b=>b.name=="calf_l");
            Quaternion before=knee.localRotation;float phase=Value<float>("Phase");
            owner.transform.position+=Vector3.forward;Call("Advance",.3f,true);
            Assert.That(Quaternion.Angle(before,knee.localRotation),Is.LessThan(.01f));
            Call("Advance",.02f,false);
            Assert.That(Value<float>("Phase"),Is.EqualTo(phase).Within(.0001f));
        }
        [Test] public void TeleportAndVisibilityResetTheGaitWithoutMovingTheController()
        {
            owner.transform.position+=Vector3.forward*.1f;Call("Advance",.02f,false);
            owner.transform.position+=Vector3.right*40;Vector3 position=owner.transform.position;Call("Advance",.02f,false);
            Assert.That(Value<float>("ObservedSpeed"),Is.Zero);Assert.That(Value<float>("Phase"),Is.Zero);
            Call("SetVisible",false);Call("SetVisible",true);
            Assert.That(Value<float>("Phase"),Is.Zero);Assert.That(owner.transform.position,Is.EqualTo(position));
            foreach(var item in Value<GameObject>("Visual").GetComponentsInChildren<Transform>(true))Assert.That(item.gameObject.layer,Is.Zero);
        }
        [Test] public void TeleportDuringPauseResetsGaitOnlyAfterTheDisplayedPoseIsReleased()
        {
            owner.transform.position+=Vector3.forward*.1f;Call("Advance",.02f,false);
            var knee=Array.Find(Value<GameObject>("Visual").GetComponentsInChildren<Transform>(),b=>b.name=="calf_l");
            Quaternion before=knee.localRotation;
            owner.transform.position+=Vector3.right*40;Call("Advance",.3f,true);
            Assert.That(Quaternion.Angle(before,knee.localRotation),Is.LessThan(.01f));
            Call("Advance",.02f,false);
            Assert.That(Value<float>("Phase"),Is.Zero);Assert.That(Value<float>("ObservedSpeed"),Is.Zero);
        }
        [Test] public void IncompatibleAssetFailsWithoutLeavingASecondVisual()
        {
            var empty=new GameObject("InvalidArt");int children=owner.transform.childCount;
            try{object[] args={owner.transform,empty,null};Assert.That((bool)type.GetMethod("TryCreate").Invoke(null,args),Is.False);Assert.That(args[2],Is.Null);Assert.That(owner.transform.childCount,Is.EqualTo(children));}
            finally{UnityEngine.Object.DestroyImmediate(empty);}
        }
        private void Call(string name,params object[] args)=>type.GetMethod(name).Invoke(actor,args);
        private T Value<T>(string name)=>(T)type.GetProperty(name).GetValue(actor);
    }
}
