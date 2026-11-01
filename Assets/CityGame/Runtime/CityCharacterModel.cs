using UnityEngine;
namespace Pozarevac.CityGame
{
    public static class CityCharacterModel
    {
        public static bool TryPrepareHuman(GameObject source,Transform parent,int layer,out GameObject prepared)
        {
            prepared=null;
            if(source==null||parent==null||!parent.gameObject.activeInHierarchy||layer<0||layer>31)return false;
            if(source.GetComponentsInChildren<MonoBehaviour>(true).Length>0||source.GetComponentsInChildren<Collider>(true).Length>0||
               source.GetComponentsInChildren<Rigidbody>(true).Length>0||source.GetComponentsInChildren<Camera>(true).Length>0||
               source.GetComponentsInChildren<AudioSource>(true).Length>0||source.GetComponentsInChildren<AudioListener>(true).Length>0)return false;
            GameObject wrapper=new GameObject("ReviewedHumanPreview");wrapper.transform.SetParent(parent,false);bool ready=false;
            try
            {
                GameObject model=Object.Instantiate(source,wrapper.transform);
                foreach(Animator animator in model.GetComponentsInChildren<Animator>(true))animator.enabled=false;
                if(!Normalize(wrapper.transform,model,null))return false;
                foreach(Transform item in wrapper.GetComponentsInChildren<Transform>(true))item.gameObject.layer=layer;
                wrapper.SetActive(false);prepared=wrapper;ready=true;return true;
            }
            finally{if(!ready){if(Application.isPlaying)Object.Destroy(wrapper);else Object.DestroyImmediate(wrapper);}}
        }
        public static GameObject LoadSource()
        {
            // The motion FBX retains the control-rig hierarchy its clips actually address.
            GameObject motion=Resources.Load<GameObject>("Models/KenneyRetro/idle");
            return motion!=null&&motion.GetComponentInChildren<SkinnedMeshRenderer>()!=null?motion:Resources.Load<GameObject>("Models/KenneyRetro/characterMedium");
        }
        public static GameObject AnimationRoot(GameObject instance)
        {
            Transform rig=instance.transform.Find("Root");return rig!=null?rig.gameObject:instance;
        }
        // Normalize the wrapper, never an animated transform whose scale curves can overwrite it.
        public static bool Normalize(Transform wrapper,GameObject instance,AnimationClip referencePose,CityCharacterPhysique physique=null)
        {
            foreach(Animation player in instance.GetComponentsInChildren<Animation>()){player.Stop();player.enabled=false;}
            if(referencePose!=null)referencePose.SampleAnimation(AnimationRoot(instance),referencePose.length*.1f);
            physique?.Apply(0);
            Bounds bounds=Measure(instance);float height=bounds.size.y;
            if(float.IsNaN(height)||float.IsInfinity(height)||height<.001f||height>10000)return false;
            float factor=1.8f/height;float bottom=bounds.min.y-wrapper.position.y;
            wrapper.localScale*=factor;wrapper.localPosition-=Vector3.up*bottom*factor;return true;
        }
        public static Bounds Measure(GameObject instance)
        {
            Bounds bounds=default;bool any=false;
            foreach(SkinnedMeshRenderer renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Mesh baked=new Mesh();
                try
                {
                    // Compensate renderer scaling before TransformPoint applies it once.
                    renderer.BakeMesh(baked,true);
                    foreach(Vector3 vertex in baked.vertices)
                    {
                        Vector3 point=renderer.transform.TransformPoint(vertex);
                        if(!any){bounds=new Bounds(point,Vector3.zero);any=true;}else bounds.Encapsulate(point);
                    }
                }
                finally{if(Application.isPlaying)Object.Destroy(baked);else Object.DestroyImmediate(baked);}
            }
            return bounds;
        }
    }
}
