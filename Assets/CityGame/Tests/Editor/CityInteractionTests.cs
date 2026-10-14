using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityInteractionTests
    {
        [TestCase(false, 3f, true)]
        [TestCase(true, 3f, false)]
        [TestCase(false, 8f, false)]
        public void InteractionIgnoresPlayerButRespectsWallsAndRange(bool obstructed, float distance, bool expected)
        {
            GameObject player = new GameObject("InteractionPlayerTest");
            GameObject cameraObject = new GameObject("InteractionCameraTest");
            GameObject station = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            float previousScale = Time.timeScale;
            try
            {
                Time.timeScale = 1;
                Vector3 origin = new Vector3(30000f, 0f, 30000f);
                player.transform.position = origin;
                CharacterController body = player.AddComponent<CharacterController>();
                body.height = 1.8f;
                body.radius = 0.32f;
                body.center = Vector3.up * 0.9f;
                cameraObject.transform.SetParent(player.transform, false);
                cameraObject.transform.localPosition = Vector3.up * 1.62f;
                Camera view = cameraObject.AddComponent<Camera>();
                station.transform.position = origin + new Vector3(0, 0.9f, distance);
                CityMission mission = station.AddComponent<CityMission>();
                mission.Configure("Interaction test", 500, 10, 0);
                wall.transform.position = origin + new Vector3(0, 1, 1.5f);
                wall.transform.localScale = new Vector3(2, 3, 0.2f);
                wall.SetActive(obstructed);
                view.transform.LookAt(station.transform.position);
                CitySession session = new CitySession(100);
                CityPlayerInteraction interaction = player.AddComponent<CityPlayerInteraction>();
                interaction.Configure(view, session, null);
                Physics.SyncTransforms();
                Assert.That(interaction.TryInteract(), Is.EqualTo(expected));
                Assert.That(session.Cash, Is.EqualTo(expected ? 600 : 100));
                Assert.That(interaction.TryInteract(), Is.False);
            }
            finally
            {
                Time.timeScale = previousScale;
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(station);
                Object.DestroyImmediate(wall);
            }
        }
    }
}
