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
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath); UpgradeOfflineGeometry();
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath); return;
            }
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
            UpgradeOfflineGeometry();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
        }

        private static void UpgradeOfflineGeometry()
        {
            CityOsmStreets.Data source = CityOsmStreets.Load();
            CesiumGeoreference origin = UnityEngine.Object.FindAnyObjectByType<CesiumGeoreference>();
            if (source != null && origin != null) origin.SetOriginLongitudeLatitudeHeight(source.anchorLongitude, source.anchorLatitude, 0);
            GameObject floor = GameObject.Find("VKT_Offline_Practice_Surface");
            if (floor != null)
            {
                float diameter = source != null ? source.radiusMeters * 2 + 20 : 640;
                floor.transform.localPosition = new Vector3(0, -.52f, 0); floor.transform.localScale = new Vector3(diameter, 1, diameter);
                string path = SettingsRoot + "/PortableGround.mat";
                Material ground = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (ground == null)
                {
                    Material template = Resources.Load<Material>("CityMissionMarker");
                    if (template == null) throw new InvalidOperationException("Portable ground shader template is missing.");
                    ground = new Material(template); ground.color = new Color(.28f, .30f, .25f);
                    ground.DisableKeyword("_EMISSION"); if (ground.HasProperty("_EmissionColor")) ground.SetColor("_EmissionColor", Color.black);
                    if (ground.HasProperty("_Smoothness")) ground.SetFloat("_Smoothness", .1f);
                    AssetDatabase.CreateAsset(ground, path);
                }
                floor.GetComponent<Renderer>().sharedMaterial = ground;
            }
            Camera camera = Camera.main; if (camera != null) camera.farClipPlane = 800;
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
        public static void ExportAndroidProject() => Build(true, true);
        private static void Build(bool android, bool exportAndroid = false)
        {
            string previousBuildThreads = Environment.GetEnvironmentVariable("BEE_BUILD_THREADS");
            if (string.IsNullOrWhiteSpace(previousBuildThreads)) Environment.SetEnvironmentVariable("BEE_BUILD_THREADS", "4");
            bool windowsRootTrust = android && Array.IndexOf(Environment.GetCommandLineArgs(), "-vktWindowsRootTrust") >= 0;
            string previousJavaOptions = Environment.GetEnvironmentVariable("JAVA_TOOL_OPTIONS");
            RenderPipelineAsset previousDefault = GraphicsSettings.defaultRenderPipeline;
            RenderPipelineAsset previousQuality = QualitySettings.renderPipeline;
            AndroidSdkVersions previousMin = PlayerSettings.Android.minSdkVersion;
            UIOrientation previousOrientation = PlayerSettings.defaultInterfaceOrientation;
            AndroidArchitecture previousArchitectures = PlayerSettings.Android.targetArchitectures;
            ScriptingImplementation previousBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
            string previousId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            bool previousExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            bool previousDefaultGraphics = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);
            GraphicsDeviceType[] previousGraphics = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
            try
            {
                if (windowsRootTrust)
                {
                    if (Application.platform != RuntimePlatform.WindowsEditor) throw new InvalidOperationException("Windows root trust requires Windows.");
                    Environment.SetEnvironmentVariable("JAVA_TOOL_OPTIONS", "-Djavax.net.ssl.trustStoreType=Windows-ROOT -Djavax.net.ssl.trustStore=NONE");
                }
                CreatePlayground();
                UniversalRenderPipelineAsset pipeline = EnsurePipeline();
                GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
                if (!android)
                {
                    PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
                    PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
                }
                if (android)
                {
                    EditorUserBuildSettings.exportAsGoogleAndroidProject = exportAndroid;
                    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel28;
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                    PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.yashimi.vkt");
                    PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
                }
                string output = Argument("-vktOutput", Path.Combine(Path.GetTempPath(), "VKT-Portable", exportAndroid ? "AndroidGradle" : android ? "VKT.apk" : "VKT.exe"));
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
                Environment.SetEnvironmentVariable("BEE_BUILD_THREADS", previousBuildThreads);
                if (windowsRootTrust) Environment.SetEnvironmentVariable("JAVA_TOOL_OPTIONS", previousJavaOptions);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, previousDefaultGraphics);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, previousGraphics);
                GraphicsSettings.defaultRenderPipeline = previousDefault; QualitySettings.renderPipeline = previousQuality;
                PlayerSettings.Android.minSdkVersion = previousMin; PlayerSettings.Android.targetArchitectures = previousArchitectures;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, previousBackend);
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, previousId);
                PlayerSettings.defaultInterfaceOrientation = previousOrientation;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = previousExport;
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
