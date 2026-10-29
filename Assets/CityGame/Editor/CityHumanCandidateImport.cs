using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pozarevac.CityGame.Editor
{
    /// <summary>Explicit, folder-scoped import. Never changes the playable character or project render settings.</summary>
    public static class CityHumanCandidateImport
    {
        private const string Folder = "Assets/CityGame/Resources/Models/RodjaHuman";
        [Serializable] private sealed class Manifest { public Entry[] materials; }
        [Serializable] private sealed class Entry
        {
            public string name, diffuse, normal;
            public bool alphaClip;
        }

        public static void Import()
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder + "/manifest.json"));
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (manifest?.materials == null || shader == null)
                throw new InvalidOperationException("Human manifest or URP Lit shader missing.");
            if (!AssetDatabase.IsValidFolder(Folder + "/Materials")) AssetDatabase.CreateFolder(Folder, "Materials");
            var materials = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (Entry entry in manifest.materials)
            {
                string path = Folder + "/Materials/" + entry.name + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
                material.shader = shader;
                material.name = entry.name;
                material.SetTexture("_BaseMap", Texture(entry.diffuse, false));
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Metallic", 0);
                material.SetFloat("_Smoothness", .22f);
                material.SetFloat("_Cull", entry.alphaClip ? 0 : 2);
                material.SetFloat("_AlphaClip", entry.alphaClip ? 1 : 0);
                material.SetFloat("_Cutoff", .35f);
                material.SetFloat("_Surface", 0);
                material.renderQueue = entry.alphaClip ? 2450 : 2000;
                material.SetOverrideTag("RenderType", entry.alphaClip ? "TransparentCutout" : "Opaque");
                if (entry.alphaClip) material.EnableKeyword("_ALPHATEST_ON"); else material.DisableKeyword("_ALPHATEST_ON");
                Texture2D normal = Texture(entry.normal, true);
                material.SetTexture("_BumpMap", normal);
                if (normal != null) material.EnableKeyword("_NORMALMAP"); else material.DisableKeyword("_NORMALMAP");
                EditorUtility.SetDirty(material);
                materials.Add(entry.name, material);
            }
            string modelPath = Folder + "/RodjaSource.fbx";
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Human FBX missing.");
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = false;
            importer.isReadable = true;
            importer.addCollider = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (var pair in materials)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key), pair.Value);
            importer.SaveAndReimport();
            var root = new GameObject("RodjaHuman");
            try
            {
                var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath), root.transform);
                model.name = "RodjaSource";
                foreach (Animator animator in model.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(animator);
                int triangles = 0;
                foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    triangles += renderer.sharedMesh.triangles.Length / 3;
                    renderer.updateWhenOffscreen = false;
                    foreach (Material material in renderer.sharedMaterials)
                        if (material == null || !materials.ContainsValue(material))
                            throw new InvalidOperationException("Unmapped human material on " + renderer.name);
                }
                if (triangles < 10000 || triangles > 80000) throw new InvalidOperationException("Human triangle budget exceeded: " + triangles);
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "/RodjaHuman.prefab");
                AssetDatabase.SaveAssets();
                Debug.Log("VKT_HUMAN_IMPORT triangles=" + triangles + " previewOnly=true");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static Texture2D Texture(string file, bool normal)
        {
            if (string.IsNullOrEmpty(file)) return null;
            if (Path.GetFileName(file) != file) throw new InvalidOperationException("Texture must be in the candidate folder.");
            string path = Folder + "/" + file;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing candidate texture: " + file);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = !normal;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
