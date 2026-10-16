using UnityEngine;
namespace Pozarevac.CityGame
{
    public static class CityCharacterModel
    {
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
        public static bool Normalize(Transform wrapper,GameObject instance,AnimationClip referencePose)
        {
            foreach(Animation player in instance.GetComponentsInChildren<Animation>()){player.Stop();player.enabled=false;}
            if(referencePose!=null)referencePose.SampleAnimation(AnimationRoot(instance),referencePose.length*.1f);
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
