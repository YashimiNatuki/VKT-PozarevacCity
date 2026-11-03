using System;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // Independent presentation clone. Training time, repetitions, input and payouts remain external.
    internal sealed class CityHumanBenchPose : IDisposable
    {
        private readonly Transform stage;
        private readonly Transform[] bones;
        private readonly Quaternion[] rest;
        private readonly CityHumanHandPose[] grips;
        private readonly Quaternion[] feet=new Quaternion[2];
        private readonly float[] soles=new float[2];
        private readonly float side,barZ,lower,upper;
        public GameObject Visual {get;}
        public float HandError {get;private set;}
        public float FootError {get;private set;}
        public float WristClearance {get;private set;}
        public float HandOrientationError {get;private set;}
        public bool FeetReady {get;private set;}
        public static CityHumanBenchPose Create(Transform stage)
        {
            if(!CityCharacterModel.TryPrepareHuman(Resources.Load<GameObject>("Models/RodjaHuman/RodjaHuman"),stage,0,out GameObject visual))return null;
            bool ready=false;
            try
            {
                if(!CityHumanPose.TryCreate(visual,visual.transform,out CityHumanPose pose))return null;
                CityHumanBenchPose result=pose.CreateBench(stage,visual);ready=result!=null;return result;
            }
            finally {if(!ready)Release(visual);}
        }
        internal CityHumanBenchPose(Transform bench,GameObject visual,Transform[] selected,CityHumanHandPose left,CityHumanHandPose right)
        {
            stage=bench;Visual=visual;Visual.name="VKT_HumanBenchAthlete";bones=selected;grips=new[]{left,right};
            rest=new Quaternion[bones.Length];for(int i=0;i<bones.Length;i++)rest[i]=bones[i].localRotation;
            for(int i=0;i<2;i++)
            {
                Transform foot=bones[i==0?11:14];feet[i]=Quaternion.Inverse(stage.rotation)*foot.rotation;
                soles[i]=stage.InverseTransformPoint(foot.position).y;
            }
            visual.SetActive(true);
            visual.transform.localRotation=Quaternion.Euler(-90,0,0);
            float pelvisZ=stage.InverseTransformPoint(bones[0].position).z;
            visual.transform.position+=stage.forward*(.35f-pelvisZ);
            Bounds bounds=CityCharacterModel.Measure(visual);
            visual.transform.position+=stage.up*(.495f-(bounds.min.y-stage.position.y));
            foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
            Vector3 shoulder=stage.InverseTransformPoint(bones[3].position),other=stage.InverseTransformPoint(bones[6].position);
            side=Mathf.Sign(shoulder.x);barZ=(shoulder.z+other.z)*.5f+.04f;
            upper=Mathf.Min(Maximum(0),Maximum(1));
            lower=Mathf.Min(Mathf.Max(shoulder.y,other.y)+.1f,upper-.08f);
            visual.SetActive(false);
        }
        private float Maximum(int index)
        {
            int start=index==0?3:6;
            Quaternion orientation=grips[index].PhoneOrientation(stage.up,stage.forward);
            Vector3 target=stage.InverseTransformPoint(grips[index].BenchWrist(stage.TransformPoint(new Vector3((index==0?side:-side)*.30f,0,barZ)),orientation));
            Vector3 shoulder=stage.InverseTransformPoint(bones[start].position);
            float reach=(Vector3.Distance(bones[start].position,bones[start+1].position)+Vector3.Distance(bones[start+1].position,bones[start+2].position))*.96f;
            float flat=(target.x-shoulder.x)*(target.x-shoulder.x)+(target.z-shoulder.z)*(target.z-shoulder.z);
            return shoulder.y+Mathf.Sqrt(Mathf.Max(.001f,reach*reach-flat))-target.y;
        }
        public void Pose(bool active,float extension,Transform bar)
        {
            Visual.SetActive(active);bar.localPosition=new Vector3(0,Mathf.Lerp(lower,upper,extension),barZ);
            if(!active)return;
            for(int i=0;i<bones.Length;i++)bones[i].localRotation=rest[i];
            HandError=FootError=HandOrientationError=0;WristClearance=float.PositiveInfinity;FeetReady=true;
            for(int i=0;i<2;i++)
            {
                int start=i==0?3:6;float outward=i==0?side:-side;
                Quaternion rotation=grips[i].PhoneOrientation(stage.up,stage.forward);
                Vector3 contact=bar.TransformPoint(new Vector3(outward*.30f,0,0));
                CityTwoBonePose.Solve(bones[start],bones[start+1],bones[start+2],grips[i].BenchWrist(contact,rotation),stage.right*outward+stage.forward*.4f);
                grips[i].ApplyBench(rotation);
                HandError=Mathf.Max(HandError,Vector3.Distance(grips[i].BenchContact,contact));
                HandOrientationError=Mathf.Max(HandOrientationError,Quaternion.Angle(bones[start+2].rotation,rotation));
                WristClearance=Mathf.Min(WristClearance,Vector3.ProjectOnPlane(bones[start+2].position-contact,stage.right).magnitude-.016f);
                int leg=i==0?9:12;Vector3 hip=stage.InverseTransformPoint(bones[leg].position);
                float reach=(Vector3.Distance(bones[leg].position,bones[leg+1].position)+Vector3.Distance(bones[leg+1].position,bones[leg+2].position))*.98f;
                float across=hip.x-(i==0?-.30f:.30f),height=hip.y-soles[i];
                float forward=Mathf.Min(.10f,Mathf.Sqrt(Mathf.Max(0,reach*reach-across*across-height*height)));
                Vector3 target=stage.TransformPoint(new Vector3(i==0?-.30f:.30f,soles[i],hip.z+forward));
                CityTwoBonePose.Solve(bones[leg],bones[leg+1],bones[leg+2],target,stage.up);
                bones[leg+2].rotation=stage.rotation*feet[i];
                FootError=Mathf.Max(FootError,Vector3.Distance(bones[leg+2].position,target));
                FeetReady&=FootError<.03f;
            }
        }
        public void Dispose()=>Release(Visual);
        private static void Release(GameObject value){if(value!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}}
    }
}
