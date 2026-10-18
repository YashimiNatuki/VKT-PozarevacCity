using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityFaceStateTests
    {
        [Test]
        public void DamageOverridesEffortAndAlertThenExpiresWithoutChangingSuccess()
        {
            var state=new CityFaceState();state.Celebrate();state.Damage(20);
            Assert.AreEqual(CityFaceMood.Hurt,state.Resolve(1,true));
            state.Tick(1.4f);
            Assert.AreEqual(CityFaceMood.Effort,state.Resolve(1,true));
            Assert.AreEqual(CityFaceMood.Focus,state.Resolve(0,true));
            Assert.AreEqual(CityFaceMood.Success,state.Resolve(0,false));
            state.Tick(1);Assert.AreEqual(CityFaceMood.Calm,state.Resolve(0,false));
        }
        [Test]
        public void PausedAndInvalidTimeCannotConsumeExpressions()
        {
            var state=new CityFaceState();state.Focus();
            foreach(float dt in new[]{0f,-1,float.NaN,float.PositiveInfinity})state.Tick(dt);
            Assert.AreEqual(CityFaceMood.Focus,state.Resolve(0,false));
            state.Clear();state.Damage(float.NaN);state.Damage(-1);state.Damage(float.PositiveInfinity);
            Assert.AreEqual(CityFaceMood.Calm,state.Resolve(float.NaN,false));
        }
        [Test]
        public void EveryPoseSanitizesInputAndDistinguishesSuccessFromPain()
        {
            foreach(CityFaceMood mood in System.Enum.GetValues(typeof(CityFaceMood)))
            {
                Vector4 pose=CityFaceState.Pose(mood,float.NaN,float.PositiveInfinity,float.NegativeInfinity);
                for(int i=0;i<4;i++)Assert.IsTrue(float.IsFinite(pose[i]));
                Assert.That(pose.x,Is.InRange(0f,1f));
            }
            Assert.Greater(CityFaceState.Pose(CityFaceMood.Success,0,0,0).z,0);
            Assert.Less(CityFaceState.Pose(CityFaceMood.Hurt,0,0,0).z,0);
        }
        [Test]
        public void RebindingAndDisablingReleaseOldVitalsSubscriptions()
        {
            var owner=new GameObject("FaceOwnershipTest");var other=new GameObject("OtherVitals");
            try
            {
                var a=owner.AddComponent<CityPlayerVitals>();var b=other.AddComponent<CityPlayerVitals>();
                var face=owner.AddComponent<CityFaceExpression>();face.BindFeedback(a,null);
                Assert.IsTrue(a.ApplyDamage(10));Assert.AreEqual(CityFaceMood.Hurt,face.Mood);
                face.BindFeedback(b,null);a.Health.Tick(2);a.ApplyDamage(10);
                Assert.AreEqual(CityFaceMood.Calm,face.Mood);
                Assert.IsTrue(b.ApplyDamage(10));Assert.AreEqual(CityFaceMood.Hurt,face.Mood);
                typeof(CityFaceExpression).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(face,null);
                b.Health.Tick(2);b.ApplyDamage(10);Assert.AreEqual(CityFaceMood.Calm,face.Mood);
            }
            finally {Object.DestroyImmediate(owner);Object.DestroyImmediate(other);}
        }
        [Test]
        public void AudioProbeRefusesUninitializedOwnersAndInvalidCues()
        {
            var owner=new GameObject("AudioProbeTest");
            try
            {
                var audio=owner.AddComponent<CityGameAudio>();
                Assert.IsFalse(audio.TryPlayDiagnostic());audio.StopDiagnostic();
                Assert.Throws<System.ArgumentOutOfRangeException>(()=>audio.PlayCue((CityAudioCue)99));
                Assert.IsFalse(audio.DiagnosticPlaying);
            }
            finally {Object.DestroyImmediate(owner);}
        }
    }
}
