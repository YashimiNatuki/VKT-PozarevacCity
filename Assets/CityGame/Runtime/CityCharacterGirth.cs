using System;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // Deform the skin in bind space, keeping joint lengths and IK transforms intact.
    public sealed class CityCharacterGirth : IDisposable
    {
        private readonly Mesh mesh;
        private readonly Vector3[] original,modified;
        private readonly BoneWeight[] weights;
        private readonly Matrix4x4[] bind,inverse,changes;
        private readonly string[] names;
        private readonly SkinnedMeshRenderer renderer;
        private readonly int jaw;
        private int last=-1;
        public CityCharacterGirth(SkinnedMeshRenderer renderer)
        {
            this.renderer=renderer;
            Mesh source=renderer.sharedMesh;
            if(source==null||!source.isReadable)throw new InvalidOperationException("Player skin must be readable for bind-space body morphs.");
            original=source.vertices;modified=new Vector3[original.Length];weights=source.boneWeights;bind=source.bindposes;
            if(weights.Length!=original.Length)throw new InvalidOperationException("Player skin requires four-weight bone data.");
            if(renderer.bones.Length!=bind.Length)throw new InvalidOperationException("Player bone bindings are inconsistent.");
            mesh=UnityEngine.Object.Instantiate(source);mesh.name="VKT_OwnedBodyMorph";renderer.sharedMesh=mesh;
            inverse=new Matrix4x4[bind.Length];changes=new Matrix4x4[bind.Length];names=new string[bind.Length];
            for(int i=0;i<bind.Length;i++){inverse[i]=bind[i].inverse;names[i]=renderer.bones[i].name;}
            jaw=source.name=="characterMedium"?CityFaceMorph.AddJaw(mesh,original,weights,Array.IndexOf(names,"Head")):-1;
            renderer.updateWhenOffscreen=true;
        }
        public void Apply(int strength)
        {
            int level=Mathf.Clamp(strength,0,5);if(last==level)return;last=level;
            for(int i=0;i<bind.Length;i++)
            {
                Vector3 scale=Vector3.one;string n=names[i];
                if(n=="UpperChest"||n=="Chest")scale=new Vector3(1.04f+level*.014f,1,1.02f+level*.012f);
                else if(n.EndsWith("Arm",StringComparison.Ordinal)){float w=1.03f+level*.025f;scale=new Vector3(w,1,w);}
                else if(n.EndsWith("Leg",StringComparison.Ordinal)){float w=1.02f+level*.02f;scale=new Vector3(w,1,w);}
                changes[i]=inverse[i]*Matrix4x4.Scale(scale)*bind[i];
            }
            for(int i=0;i<original.Length;i++)
            {
                BoneWeight w=weights[i];Vector3 v=original[i];
                modified[i]=Point(w.boneIndex0,w.weight0,v)+Point(w.boneIndex1,w.weight1,v)+Point(w.boneIndex2,w.weight2,v)+Point(w.boneIndex3,w.weight3,v);
            }
            mesh.vertices=modified;mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
        private Vector3 Point(int bone,float weight,Vector3 point)=>weight<=0?Vector3.zero:changes[bone].MultiplyPoint3x4(point)*weight;
        public void SetJaw(float value)
        {
            if(jaw>=0&&renderer!=null)renderer.SetBlendShapeWeight(jaw,Mathf.Clamp01(float.IsNaN(value)?0:value)*100);
        }
        public void Dispose(){if(mesh!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(mesh);else UnityEngine.Object.DestroyImmediate(mesh);}}
    }
}
