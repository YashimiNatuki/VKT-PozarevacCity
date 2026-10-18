using UnityEngine;

namespace Pozarevac.CityGame
{
    // Bounded foot pursuit with last-seen search and wall-aware local steering.
    // This is deliberately not a substitute for a baked city navigation graph.
    [RequireComponent(typeof(CharacterController))]
    public sealed class CityPoliceOfficer : MonoBehaviour
    {
        private CityGameBootstrap game;
        private CharacterController body;
        private Vector3 home;
        private readonly CityPoliceInvestigation investigation=new CityPoliceInvestigation();
        private int patrolStop;
        private static readonly Vector3[] PatrolOffsets={Vector3.forward*4,Vector3.right*4,Vector3.back*4,Vector3.left*4};
        private float patrolWait;
        public CityPolicePhase Phase=>investigation.Phase;
        public Vector3 LastKnown=>investigation.LastKnown;
        public void ReportIncident(Vector3 at){investigation.ReportIncident(at);}
        private readonly RaycastHit[] hits=new RaycastHit[24];
        private Material skin;
        private float vertical,stun;
        public bool SeesPlayer {get;private set;}
        public bool CanCapture {get;private set;}

        public void Initialize(CityGameBootstrap owner,Material uniform,Material dark)
        {
            game=owner;home=transform.position;body=GetComponent<CharacterController>();
            body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.stepOffset=.3f;
            GameObject asset=CityCharacterModel.LoadSource();
            if(asset!=null)
            {
                Transform wrapper=new GameObject("PatrolVisual").transform;wrapper.SetParent(transform,false);
                GameObject model=Instantiate(asset,wrapper);
                AnimationClip idle=Clip("idle"),run=Clip("run");
                CityCharacterModel.Normalize(wrapper,model,idle);
                skin=new Material(dark);skin.color=new Color(.75f,.85f,1);Texture2D texture=Resources.Load<Texture2D>("Models/KenneyRetro/humanMaleA");skin.mainTexture=texture;if(skin.HasProperty("_BaseMap"))skin.SetTexture("_BaseMap",texture);
                foreach(Renderer renderer in model.GetComponentsInChildren<Renderer>()){Material[] slots=new Material[Mathf.Max(1,renderer.sharedMaterials.Length)];for(int i=0;i<slots.Length;i++)slots[i]=skin;renderer.sharedMaterials=slots;}
                if(idle!=null&&run!=null)
                {
                    GameObject rig=CityCharacterModel.AnimationRoot(model);Animation motion=rig.GetComponent<Animation>();if(motion==null)motion=rig.AddComponent<Animation>();motion.enabled=true;motion.cullingType=AnimationCullingType.AlwaysAnimate;
                    motion.AddClip(idle,"VKT_Idle");motion.AddClip(run,"VKT_Run");motion.wrapMode=WrapMode.Loop;motion.Play("VKT_Idle");wrapper.gameObject.AddComponent<CityActorMotion>().Initialize(transform,motion);
                }
            }
            Hat(uniform);
            GameObject label=new GameObject("PatrolSign",typeof(TextMesh));label.transform.SetParent(transform,false);label.transform.localPosition=Vector3.up*2.15f;
            TextMesh text=label.GetComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=32;text.characterSize=.018f;text.anchor=TextAnchor.MiddleCenter;text.text="POLICIJA";
            label.GetComponent<Renderer>().sharedMaterial=text.font.material;label.AddComponent<CityWorldLabel>().Initialize(game.Player.GetComponentInChildren<Camera>().transform);
        }
        private void Hat(Material uniform)
        {
            GameObject hat=GameObject.CreatePrimitive(PrimitiveType.Sphere);hat.name="PoliceCap";hat.transform.SetParent(transform,false);hat.transform.localPosition=new Vector3(0,1.79f,0);hat.transform.localScale=new Vector3(.31f,.12f,.33f);hat.GetComponent<Renderer>().sharedMaterial=uniform;Destroy(hat.GetComponent<Collider>());
        }
        private static AnimationClip Clip(string name){foreach(AnimationClip clip in Resources.LoadAll<AnimationClip>("Models/KenneyRetro/"+name))if(!clip.name.StartsWith("__preview__",System.StringComparison.Ordinal))return clip;return null;}
        public bool ReceiveImpact(Vector3 direction)
        {
            if(game==null||!game.Vitals.IsAlive||game.Director.IsPaused)return false;
            stun=.7f;SeesPlayer=CanCapture=false;game.Progression.Lifestyle.ReportCrime();game.Session.AddHeat(12);
            if(direction.sqrMagnitude>.001f){direction.y=0;body.Move(direction.normalized*.25f);}return true;
        }
        public void Step(float seconds,bool wanted)
        {
            SeesPlayer=CanCapture=false;
            if(game==null||seconds<=0||!body.enabled)return;
            if(stun>0){stun=Mathf.Max(0,stun-seconds);return;}
            Vector3 eye=transform.position+Vector3.up*1.5f,target=game.Player.position+Vector3.up*1.3f;
            float distance=Vector3.Distance(eye,target);
            if(wanted&&distance<=32)
            {
                Vector3 direction=(target-eye).normalized;
                int count=Physics.RaycastNonAlloc(eye,direction,hits,distance+.2f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
                float nearest=float.PositiveInfinity;Collider collider=null;
                if(count<hits.Length)for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].transform.IsChildOf(transform)&&hits[i].distance<nearest){nearest=hits[i].distance;collider=hits[i].collider;}
                SeesPlayer=collider!=null&&collider.transform.IsChildOf(game.Player);
            }
            CanCapture=SeesPlayer&&distance<2.2f;
            Vector3 patrolGoal=home+PatrolOffsets[patrolStop];
            if(!wanted&&(patrolGoal-transform.position).sqrMagnitude<1.5f)
            {
                patrolWait+=seconds;if(patrolWait>=2.5f){patrolStop=(patrolStop+1)%4;patrolWait=0;}
            }
            Vector3 goal=investigation.Step(seconds,wanted,SeesPlayer,game.Player.position,transform.position,patrolGoal);
            Vector3 travel=goal-transform.position;travel.y=0;
            if(travel.magnitude>.8f&&!CanCapture)
            {
                travel.Normalize();int count=Physics.SphereCastNonAlloc(transform.position+Vector3.up*.8f,.34f,travel,hits,1f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
                float nearest=float.PositiveInfinity;Vector3 normal=Vector3.zero;
                if(count==hits.Length)travel=Vector3.zero;
                else for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].transform.IsChildOf(transform)&&!hits[i].transform.IsChildOf(game.Player)&&hits[i].distance<nearest){nearest=hits[i].distance;normal=hits[i].normal;}
                if(normal.sqrMagnitude>.01f){travel=Vector3.ProjectOnPlane(travel,normal);travel.y=0;if(travel.sqrMagnitude>.001f)travel.Normalize();}
                if(travel.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(travel),1-Mathf.Exp(-6*seconds));
            }
            else travel=Vector3.zero;
            vertical=body.isGrounded?-1:Mathf.Max(-20,vertical-20*seconds);
            body.Move((travel*(wanted?5.8f:1.7f)+Vector3.up*vertical)*seconds);
        }
        private void OnDestroy(){if(skin!=null)Destroy(skin);}
    }
}
