using UnityEditor;
using UnityEngine;
namespace Pozarevac.CityGame.Editor
{
    // Scoped only to reviewed CC0 model files; never changes vendor or user model imports.
    public sealed class CityCharacterAssetImporter : AssetPostprocessor
    {
        public override uint GetVersion()=>2;
        private bool IsReviewed=>assetPath.StartsWith("Assets/CityGame/Resources/Models/KenneyRetro/",System.StringComparison.Ordinal);
        private void OnPreprocessModel()
        {
            if(!IsReviewed)return;ModelImporter model=(ModelImporter)assetImporter;
            model.animationType=ModelImporterAnimationType.Legacy;model.importAnimation=true;model.materialImportMode=ModelImporterMaterialImportMode.None;
        }
        private void OnPostprocessAnimation(GameObject root,AnimationClip clip)
        {
            if(!IsReviewed)return;clip.legacy=true;clip.wrapMode=WrapMode.Loop;
            // Imported motion files carry an authoring-unit root transform (100x scale).
            // Preserve the model's root TRS; animate its bones only.
            foreach(EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                if(binding.path.Length==0&&binding.type==typeof(Transform))AnimationUtility.SetEditorCurve(clip,binding,null);
        }
    }
}
