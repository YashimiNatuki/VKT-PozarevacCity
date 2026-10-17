using UnityEngine;

namespace Pozarevac.CityGame
{
    // Bounded point-source acoustics. Native Unity panning, not an HRTF or a field recording.
    public sealed class CitySpatialSoundscape : MonoBehaviour
    {
        private sealed class Emitter
        {
            public AudioSource source;
            public AudioLowPassFilter filter;
            public bool interior, night;
            public float obstruction;
        }
        private CityGameBootstrap game;
        private Transform listener;
        private GameObject root;
        private readonly Emitter[] emitters = new Emitter[6];
        private readonly RaycastHit[] hits = new RaycastHit[16];
        private AudioClip air, birds, fan;
        private AudioReverbFilter interiorReverb;
        private float nextProbe;
        private int cursor;
        private bool suspended;
        public int EmitterCount => game != null ? emitters.Length : 0;
        public int ProbeCount { get; private set; }
        public Vector3 TestSourcePosition => emitters[0].source.transform.position;
        public float FirstObstruction => emitters[0].obstruction;
        public int PlayingSources
        {
            get{int count=0;foreach(Emitter e in emitters)if(e!=null&&e.source!=null&&e.source.isPlaying)count++;return count;}
        }

        public void Initialize(CityGameBootstrap owner, Vector3 spawn)
        {
            game=owner;listener=game.Player.GetComponentInChildren<Camera>().transform;
            root=new GameObject("VKT_SpatialSoundscape");root.transform.SetParent(game.Player.parent,false);
            air=CreateLoop(0);birds=CreateLoop(1);fan=CreateLoop(2);
            Add(0,"Ventilacija ulice",spawn+new Vector3(8,2,10),air,false,false);
            Add(1,"Park istok",spawn+new Vector3(28,3,24),birds,false,false);
            Add(2,"Park zapad",spawn+new Vector3(-24,3,30),birds,false,false);
            Add(3,"Noćni park",spawn+new Vector3(12,2,-14),birds,false,true);
            Add(4,"Daleki grad",spawn+new Vector3(-26,2,-20),air,false,false);
            Add(5,"Ventilator šteka",game.Hideout.InteriorSoundPosition,fan,true,false);
        }
        private void Add(int index,string name,Vector3 at,AudioClip clip,bool inside,bool night)
        {
            GameObject node=new GameObject(name);node.transform.SetParent(root.transform,false);node.transform.position=at;
            AudioSource source=node.AddComponent<AudioSource>();source.clip=clip;source.loop=true;source.playOnAwake=false;
            source.spatialBlend=1;source.rolloffMode=AudioRolloffMode.Logarithmic;source.minDistance=3;source.maxDistance=65;
            source.dopplerLevel=0;source.spread=0;source.priority=180;source.volume=0;
            AudioLowPassFilter filter=node.AddComponent<AudioLowPassFilter>();filter.cutoffFrequency=22000;
            emitters[index]=new Emitter{source=source,filter=filter,interior=inside,night=night};
            if(inside){interiorReverb=node.AddComponent<AudioReverbFilter>();interiorReverb.reverbPreset=AudioReverbPreset.Room;interiorReverb.enabled=false;}
        }
        public bool IsBlocked(Vector3 source,Vector3 ear)
        {
            Vector3 direction=ear-source;float distance=direction.magnitude;
            if(distance<.5f)return false;
            int count=Physics.RaycastNonAlloc(source,direction/distance,hits,distance-.25f,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
                if(hits[i].distance>.15f&&!hits[i].transform.IsChildOf(game.Player))return true;
            return count==hits.Length; // Saturated query conservatively muffles instead of leaking sound.
        }
        private void Update()
        {
            if(game==null)return;
            bool quiet=suspended||Time.timeScale<=0||game.Audio.Muted||!game.Vitals.IsAlive;
            if(!quiet&&Time.unscaledTime>=nextProbe)
            {
                nextProbe=Time.unscaledTime+.034f;
                Emitter e=emitters[cursor];cursor=(cursor+1)%emitters.Length;
                e.obstruction=IsBlocked(e.source.transform.position,listener.position)?1:0;ProbeCount++;
            }
            foreach(Emitter e in emitters)
            {
                bool active=!quiet&&e.interior==game.Hideout.IsInside&&(e.interior||e.night!=game.Atmosphere.IsDay||e.source.clip==air);
                if(active&&!e.source.isPlaying)e.source.UnPause();
                if(active&&!e.source.isPlaying)e.source.Play();
                if(!active){e.source.Pause();e.source.volume=0;}
                else e.source.volume=Mathf.Lerp(e.source.volume,(e.interior?.18f:.12f)*(e.obstruction>0?.3f:1)*(game.Audio.IsDialoguePlaying?.45f:1),1-Mathf.Exp(-5*Time.unscaledDeltaTime));
                e.filter.cutoffFrequency=Mathf.Lerp(e.filter.cutoffFrequency,e.obstruction>0?900:22000,1-Mathf.Exp(-8*Time.unscaledDeltaTime));
            }
            interiorReverb.enabled=!quiet&&game.Hideout.IsInside;
        }
        private void OnApplicationPause(bool value){suspended=value;if(value)Silence();}
        private void Silence(){foreach(Emitter e in emitters)if(e!=null&&e.source!=null){e.source.Pause();e.source.volume=0;}}
        private void OnDisable(){Silence();}
        private void OnDestroy(){if(root!=null)Destroy(root);if(air!=null)Destroy(air);if(birds!=null)Destroy(birds);if(fan!=null)Destroy(fan);}
        private static AudioClip CreateLoop(int kind)
        {
            const int rate=22050,seconds=8;float[] data=new float[rate*seconds];uint state=0x921f123u+(uint)kind;
            float smooth=0;
            for(int i=0;i<data.Length;i++)
            {
                state^=state<<13;state^=state>>17;state^=state<<5;
                float noise=(state&65535)/32767.5f-1;float t=(float)i/rate;smooth+=.07f*(noise-smooth);
                float edge=Mathf.Min(1,Mathf.Min(i,data.Length-1-i)/(rate*.15f));
                float value=kind==1?Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI*.5f)),12)*Mathf.Sin(t*2*Mathf.PI*(1600+90*Mathf.Sin(t*2*Mathf.PI)))*.13f:
                    smooth*.22f+Mathf.Sin(t*2*Mathf.PI*(kind==2?50:37.5f))*.04f;
                data[i]=value*edge;
            }
            AudioClip clip=AudioClip.Create("VKT_Authored_Spatial_"+kind,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
    }
}
