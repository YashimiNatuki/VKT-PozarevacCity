using System;
using System.IO;
using CesiumForUnity;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pozarevac.CityGame.Editor
{
    public static class CityPortableBuild
    {
        private const string ScenePath = "Assets/Scenes/VKTOffline.unity";
        private const string SettingsRoot = "Assets/CityGame/PortableSettings";

        [MenuItem("VKT/Create portable offline playground")]
        public static void CreatePlayground()
        {
            if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene before generating the playground.");
            Directory.CreateDirectory("Assets/Scenes");
            UniversalRenderPipelineAsset pipeline = EnsurePipeline();
            GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
            if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CesiumGeoreference origin = new GameObject("VKT_Georeference").AddComponent<CesiumGeoreference>();
            origin.SetOriginLongitudeLatitudeHeight(21.1764, 44.6193, 0);
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "VKT_Offline_Practice_Surface";
            floor.transform.SetParent(origin.transform, false); floor.transform.localPosition = new Vector3(0, -0.5f, 15);
            floor.transform.localScale = new Vector3(120, 1, 120);
            Material marker = Resources.Load<Material>("CityMissionMarker");
            if (marker != null) floor.GetComponent<Renderer>().sharedMaterial = marker;
            Camera camera = new GameObject("DynamicCamera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.nearClipPlane = 0.05f; camera.farClipPlane = 500;
            new GameObject("Sun").AddComponent<Light>().type = LightType.Directional;
            new GameObject("CityGame").AddComponent<CityGameBootstrap>();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
        }

        private static UniversalRenderPipelineAsset EnsurePipeline()
        {
            Directory.CreateDirectory(SettingsRoot); AssetDatabase.Refresh();
            string rendererPath = SettingsRoot + "/PortableRenderer.asset", pipelinePath = SettingsRoot + "/PortablePipeline.asset";
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null) { renderer = ScriptableObject.CreateInstance<UniversalRendererData>(); AssetDatabase.CreateAsset(renderer, rendererPath); }
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                SerializedObject serialized = new SerializedObject(pipeline);
                SerializedProperty renderers = serialized.FindProperty("m_RendererDataList");
                if (renderers == null) throw new InvalidOperationException("Installed URP does not expose its renderer list.");
                renderers.arraySize = 1; renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                pipeline.msaaSampleCount = 2; pipeline.renderScale = 0.85f;
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            AssetDatabase.SaveAssets();
            return pipeline;
        }

        public static void BuildWindows() => Build(false);
        public static void BuildAndroid() => Build(true);
        private static void Build(bool android)
        {
            RenderPipelineAsset previousDefault = GraphicsSettings.defaultRenderPipeline;
            RenderPipelineAsset previousQuality = QualitySettings.renderPipeline;
            AndroidSdkVersions previousMin = PlayerSettings.Android.minSdkVersion;
            UIOrientation previousOrientation = PlayerSettings.defaultInterfaceOrientation;
            AndroidArchitecture previousArchitectures = PlayerSettings.Android.targetArchitectures;
            ScriptingImplementation previousBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
            string previousId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            try
            {
                CreatePlayground();
                UniversalRenderPipelineAsset pipeline = EnsurePipeline();
                GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
                if (android)
                {
                    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel28;
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                    PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.yashimi.vkt");
                    PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
                }
                string output = Argument("-vktOutput", Path.Combine(Path.GetTempPath(), "VKT-Portable", android ? "VKT.apk" : "VKT.exe"));
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath }, locationPathName = Path.GetFullPath(output),
                    target = android ? BuildTarget.Android : BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                });
                Debug.Log($"VKT_PORTABLE_BUILD {report.summary.result}; android={android}; errors={report.summary.totalErrors}; bytes={report.summary.totalSize}");
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Portable build failed; inspect its log.");
            }
            finally
            {
                GraphicsSettings.defaultRenderPipeline = previousDefault; QualitySettings.renderPipeline = previousQuality;
                PlayerSettings.Android.minSdkVersion = previousMin; PlayerSettings.Android.targetArchitectures = previousArchitectures;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, previousBackend);
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, previousId);
                PlayerSettings.defaultInterfaceOrientation = previousOrientation;
                AssetDatabase.SaveAssets();
            }
        }
        private static string Argument(string key, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1];
            return fallback;
        }
    }
}
