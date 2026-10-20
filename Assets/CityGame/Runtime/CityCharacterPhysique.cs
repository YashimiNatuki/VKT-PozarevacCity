using UnityEngine;

namespace Pozarevac.CityGame
{
    // Reapplied after animation sampling. Limb length axes remain unchanged.
    public sealed class CityCharacterPhysique : System.IDisposable
    {
        private readonly Transform[] bones;
        private readonly Vector3[] original;
        private readonly CityCharacterGirth[] skins;
        public Transform Head { get; }
        public bool HasFullRig => Head != null && bones[1] != null && bones[4] != null && bones[6] != null && bones[10] != null;
        public CityCharacterPhysique(GameObject model)
        {
            string[] names = { "Head", "UpperChest", "Neck", "Hips", "LeftArm", "LeftForeArm", "RightArm", "RightForeArm",
                "LeftHand", "RightHand", "LeftUpLeg", "LeftLeg", "RightUpLeg", "RightLeg", "LeftFoot", "RightFoot" };
            bones = new Transform[names.Length]; original = new Vector3[names.Length];
            foreach(Transform bone in model.GetComponentsInChildren<Transform>(true))
                for(int i=0;i<names.Length;i++)if(bone.name==names[i]){bones[i]=bone;original[i]=bone.localScale;}
            Head=bones[0];
            SkinnedMeshRenderer[] renderers=model.GetComponentsInChildren<SkinnedMeshRenderer>();
            skins=new CityCharacterGirth[renderers.Length];
            for(int i=0;i<skins.Length;i++)skins[i]=new CityCharacterGirth(renderers[i]);
        }
        public void Apply(int strength)
        {
            float level=Mathf.Clamp(strength,0,5);
            for(int i=0;i<bones.Length;i++)
            {
                if(bones[i]==null)continue;
                Vector3 factor=Vector3.one;
                // Shorten the reviewed rig's long neck, compensating the child's length axis.
                if(i==0)factor=new Vector3(.48f,.48f/.78f,.48f);
                else if(i==2)factor=new Vector3(1,.78f,1);
                else if(i==8||i==9)factor=Vector3.one*.82f;
                else if(i>=14)factor=new Vector3(.88f,1,.88f);
                bones[i].localScale=Vector3.Scale(original[i],factor);
            }
            foreach(var skin in skins)skin.Apply((int)level);
        }
        public void SetJaw(float value){foreach(var skin in skins)skin.SetJaw(value);}
        public void Dispose(){foreach(var skin in skins)skin.Dispose();}
    }
}
