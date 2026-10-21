using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityGaitStateTests
    {
        [Test]
        public void ArmsCounterTheirOwnLegAndMatchTheOppositeLeg()
        {
            var gait=new CityGaitState();gait.Step(Vector3.forward*.6f,.1f);
            Assert.That(gait.LeftStride,Is.GreaterThan(0));
            Assert.That(gait.RightStride,Is.LessThan(0));
            Assert.That(gait.LeftArmSwing*gait.LeftStride,Is.LessThan(0));
            Assert.That(gait.RightArmSwing*gait.RightStride,Is.LessThan(0));
            Assert.That(gait.LeftArmSwing*gait.RightStride,Is.GreaterThan(0));
            Assert.That(gait.LeftKneeFlex,Is.InRange(0,48));
            Assert.That(gait.RightKneeFlex,Is.InRange(0,48));
        }

        [Test]
        public void EqualTravelAtThirtyAndSixtyFramesHasTheSamePhaseAndSpeed()
        {
            var slow=new CityGaitState();var fast=new CityGaitState();
            for(int i=0;i<30;i++)slow.Step(Vector3.forward*.08f,1f/30);
            for(int i=0;i<60;i++)fast.Step(Vector3.forward*.04f,1f/60);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(slow.Phase*Mathf.Rad2Deg,fast.Phase*Mathf.Rad2Deg)),Is.LessThan(.01f));
            Assert.That(slow.Speed,Is.EqualTo(fast.Speed).Within(.001f));
            Assert.That(slow.Weight,Is.EqualTo(fast.Weight).Within(.001f));
        }

        [Test]
        public void StopFadesContinuouslyWithoutInventingMoreTravel()
        {
            var gait=new CityGaitState();
            for(int i=0;i<30;i++)gait.Step(Vector3.forward*.1f,1f/30);
            float phase=gait.Phase,weight=gait.Weight;
            for(int i=0;i<30;i++)
            {
                gait.Step(Vector3.zero,1f/60);
                Assert.That(gait.Weight,Is.LessThan(weight));weight=gait.Weight;
                Assert.That(gait.Phase,Is.EqualTo(phase));
            }
            Assert.That(gait.Weight,Is.LessThan(.005f));
            Assert.That(gait.Speed,Is.LessThan(.03f));
            Assert.That(Mathf.Abs(gait.LeftStride),Is.LessThan(.1f));
        }

        [Test]
        public void PauseFreezesTheEntireStateEvenWhenTheOwnerIsRepositioned()
        {
            var gait=new CityGaitState();gait.Step(Vector3.forward*.3f,.1f);
            float speed=gait.Speed,phase=gait.Phase,weight=gait.Weight;
            Assert.IsFalse(gait.Step(Vector3.right*40,0));
            Assert.That(gait.Speed,Is.EqualTo(speed));Assert.That(gait.Phase,Is.EqualTo(phase));Assert.That(gait.Weight,Is.EqualTo(weight));
        }

        [Test]
        public void TeleportClearsSpeedPhaseAndPoseInsteadOfTakingAStride()
        {
            var gait=new CityGaitState();gait.Step(Vector3.forward*.3f,.1f);
            gait.Step(Vector3.right*40,.016f);
            Assert.IsTrue(gait.WasTeleported);
            Assert.That(gait.Speed,Is.Zero);Assert.That(gait.Phase,Is.Zero);Assert.That(gait.Weight,Is.Zero);
            Assert.That(gait.LeftArmSwing,Is.Zero);Assert.That(gait.LeftStride,Is.Zero);
            gait.Step(Vector3.forward*.04f,.016f);Assert.IsFalse(gait.WasTeleported);
            Assert.That(gait.Phase,Is.LessThan(1));
        }

        [Test]
        public void VerticalMotionDoesNotGenerateWalkingButVerticalTeleportResets()
        {
            var gait=new CityGaitState();gait.Step(Vector3.up*.2f,.016f);
            Assert.That(gait.Speed,Is.Zero);Assert.That(gait.Phase,Is.Zero);Assert.That(gait.Weight,Is.Zero);
            gait.Step(Vector3.up*6,.016f);Assert.IsTrue(gait.WasTeleported);
        }

        [Test]
        public void InvalidSamplesCannotPoisonLaterMovement()
        {
            var gait=new CityGaitState();gait.Step(Vector3.forward*.3f,.1f);
            float phase=gait.Phase;
            Assert.IsFalse(gait.Step(Vector3.forward,float.NaN));Assert.That(gait.Phase,Is.EqualTo(phase));
            Assert.IsFalse(gait.Step(Vector3.forward,-1));Assert.That(gait.Phase,Is.EqualTo(phase));
            Assert.IsFalse(gait.Step(new Vector3(float.NaN,0,0),.1f));
            Assert.That(gait.Speed,Is.Zero);Assert.That(gait.Weight,Is.Zero);
            Assert.IsTrue(gait.Step(Vector3.forward*.1f,.1f));
            Assert.IsTrue(float.IsFinite(gait.LeftStride));Assert.IsTrue(float.IsFinite(gait.LeftArmSwing));
        }

        [Test]
        public void ResetIsIdempotentAndClearsTheTeleportMarker()
        {
            var gait=new CityGaitState();gait.Step(Vector3.forward*10,.1f);
            gait.Reset();gait.Reset();
            Assert.IsFalse(gait.WasTeleported);Assert.That(gait.Speed,Is.Zero);Assert.That(gait.Weight,Is.Zero);Assert.That(gait.Phase,Is.Zero);
        }
    }
}
