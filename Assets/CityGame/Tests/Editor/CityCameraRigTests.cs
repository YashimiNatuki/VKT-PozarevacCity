using NUnit.Framework;
using UnityEngine;
namespace Pozarevac.CityGame.Tests
{
    public sealed class CityCameraRigTests
    {
        [Test] public void PerspectivesKeepTheSameHumanEyeAndBoundedDistances()
        {
            Assert.That(CityCameraRig.Offset(CityCameraMode.FirstPerson), Is.EqualTo(Vector3.zero));
            Assert.That(CityCameraRig.Offset(CityCameraMode.Shoulder).z, Is.InRange(-3f, -2f));
            Assert.That(CityCameraRig.Offset(CityCameraMode.Chase).z, Is.InRange(-6f, -4f));
        }
        [Test] public void InvalidModeIsRejectedWithoutMutatingCameraState()
        {
            GameObject camera = new GameObject("CameraModeTest");
            try
            {
                CityCameraRig rig = camera.AddComponent<CityCameraRig>();
                rig.SetMode(CityCameraMode.Chase);
                Assert.Throws<System.ArgumentOutOfRangeException>(() => rig.SetMode((CityCameraMode)5));
                Assert.That(rig.Mode, Is.EqualTo(CityCameraMode.Chase));
            }
            finally { Object.DestroyImmediate(camera); }
        }
    }
}
