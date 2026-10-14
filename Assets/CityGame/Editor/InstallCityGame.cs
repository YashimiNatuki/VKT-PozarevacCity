using CesiumForUnity;
using Pozarevac.CityGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Pozarevac.CityGame.Editor
{
    internal static class InstallCityGame
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Pozarevac/City Game/Install First Playable")]
        public static void Install()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (Object.FindAnyObjectByType<CesiumGeoreference>() == null)
                throw new System.InvalidOperationException("CesiumGeoreference must exist before City Game is installed.");

            if (Object.FindAnyObjectByType<CityGameBootstrap>() == null)
            {
                var root = new GameObject("CityGame");
                root.AddComponent<CityGameBootstrap>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.InvalidOperationException("Unity could not save the City Game scene.");
        }
    }
}
