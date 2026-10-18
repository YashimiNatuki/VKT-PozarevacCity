using UnityEngine;
using UnityEngine.InputSystem;

namespace Pozarevac.CityGame
{
    public enum CityVoice { Mila, Vuk, Luka }
    public enum CityAudioCue { Checkpoint, Success, Failure }
    public sealed class CityGameAudio : MonoBehaviour
    {
        private AudioSource effects, dialogue, feedback;
        private AudioClip[] cues;
        private float nextCue;
        public bool CuePlaying=>feedback!=null&&feedback.isPlaying;
        public bool PlayCue(CityAudioCue cue)
        {
            if((int)cue<0||(int)cue>2)throw new System.ArgumentOutOfRangeException(nameof(cue));
            if(!initialized||Muted||suspended||Time.timeScale<=0||Time.unscaledTime<nextCue)return false;
            nextCue=Time.unscaledTime+.15f;feedback.clip=cues[(int)cue];feedback.Play();return true;
        }
        private AudioClip footstep, landing;
        private AudioClip generatedFootstep, generatedLanding;
        private AudioClip grassStep,woodStep;
        private CityOsmStreets streets;
        private Transform playerRoot;
        private readonly RaycastHit[] groundHits=new RaycastHit[16];
        public CityFootstepKind CurrentSurface {get;private set;}
        public float DialogueDuck=>IsDialoguePlaying?.55f:1f;
        public void ConfigureSurfaces(CityOsmStreets map){streets=map;}
        private bool suspended;
        public bool HasMovementClips => footstep != null && landing != null;
        public void SetMuted(bool value)
        {
            Muted=value;if(effects!=null)effects.mute=value;if(dialogue!=null)dialogue.mute=value;if(feedback!=null)feedback.mute=value;
        }
        private CityProgressionController ui;
        private CharacterController body;
        private Vector3 lastPosition;
        private float walked;
        private bool wasGrounded;
        private bool initialized, gameplayPaused;
        public bool IsDialoguePlaying => dialogue != null && dialogue.isPlaying;
        public bool Muted { get; private set; }
        public int LoadedVoiceClips { get; private set; }
        private AudioClip[] voices;
        public void Initialize(Transform player, CityProgressionController controller)
        {
            if (initialized) throw new System.InvalidOperationException("Game audio is already initialized.");
            if (player == null) throw new System.ArgumentNullException(nameof(player));
            if (controller == null) throw new System.ArgumentNullException(nameof(controller));
            ui = controller; body = player.GetComponent<CharacterController>(); lastPosition = player.position;
            playerRoot=player;
            effects = player.gameObject.AddComponent<AudioSource>(); effects.playOnAwake = false; effects.volume = 0.25f;
            dialogue = player.gameObject.AddComponent<AudioSource>(); dialogue.playOnAwake = false; dialogue.volume = 0.85f;
            feedback=player.gameObject.AddComponent<AudioSource>();feedback.playOnAwake=false;feedback.volume=.24f;feedback.priority=70;
            cues=new[]{CreateFeedbackClip(CityAudioCue.Checkpoint),CreateFeedbackClip(CityAudioCue.Success),CreateFeedbackClip(CityAudioCue.Failure)};
            footstep = Resources.Load<AudioClip>("Audio/Footstep"); landing = Resources.Load<AudioClip>("Audio/Land");
            if(footstep==null)footstep=generatedFootstep=CreateContact(false);
            if(landing==null)landing=generatedLanding=CreateContact(true);
            grassStep=CreateSurfaceContact(CityFootstepKind.Grass);woodStep=CreateSurfaceContact(CityFootstepKind.Wood);
            voices = new[] { Resources.Load<AudioClip>("Audio/Voices/mila_enter"), Resources.Load<AudioClip>("Audio/Voices/vuk_exit"), Resources.Load<AudioClip>("Audio/Voices/luka_complete") };
            foreach (AudioClip clip in voices) if (clip != null) LoadedVoiceClips++;
            effects.priority = 100; dialogue.priority = 20;
            initialized = true;
        }
        public void Speak(CityVoice speaker)
        {
            if ((int)speaker < 0 || (int)speaker > 2) throw new System.ArgumentOutOfRangeException(nameof(speaker));
            if (!initialized) throw new System.InvalidOperationException("Initialize audio before dialogue.");
            int index = (int)speaker;
            string text = speaker switch
            {
                CityVoice.Mila => "Mila: Dobro došao u štek. Priđi saksiji. E je za negu, V menja vrstu biljke.",
                CityVoice.Vuk => "Vuk: Ponovo si na ulici. Drži se, brate.",
                _ => "Luka: Posao je završen. Proveri telefon i sledeći ugovor."
            };
            ui.Notify(text);
            if (!Muted && voices[index] != null) { dialogue.clip = voices[index]; dialogue.Play(); if (suspended || Time.timeScale <= 0f) dialogue.Pause(); }
        }
        private void OnDisable() { if (effects != null) effects.Stop(); if (dialogue != null) dialogue.Stop();if(feedback!=null)feedback.Stop(); }
        private void OnDestroy()
        {
            if (effects != null) Destroy(effects);
            if (dialogue != null) Destroy(dialogue);
            if(feedback!=null)Destroy(feedback);
            if(cues!=null)foreach(AudioClip cue in cues)if(cue!=null)Destroy(cue);
            if(generatedFootstep!=null)Destroy(generatedFootstep);
            if(generatedLanding!=null)Destroy(generatedLanding);
            if(grassStep!=null)Destroy(grassStep);if(woodStep!=null)Destroy(woodStep);
            // Resources clips are shared assets and must not be destroyed by this owner.
        }
        private void Update()
        {
            if (body == null) return;
            bool shouldPause = suspended || Time.timeScale <= 0f;
            effects.volume=Mathf.MoveTowards(effects.volume,.25f*DialogueDuck,Time.unscaledDeltaTime);
            if (shouldPause != gameplayPaused)
            {
                gameplayPaused = shouldPause;
                if (shouldPause) { effects.Pause(); dialogue.Pause();feedback.Stop(); }
                else { effects.UnPause(); dialogue.UnPause(); }
            }
            if (!gameplayPaused && Cursor.lockState == CursorLockMode.Locked && Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            { SetMuted(!Muted); ui.Notify(Muted ? "Zvuk isključen / M" : "Zvuk uključen / M"); }
            Vector3 delta = body.transform.position - lastPosition; lastPosition = body.transform.position;
            bool grounded = body.enabled && body.isGrounded;
            if (shouldPause || delta.sqrMagnitude > 9 || Time.deltaTime <= 0) { walked = 0; wasGrounded = grounded; return; }
            if (grounded)
            {
                if (!wasGrounded && landing != null && delta.y < -0.01f) effects.PlayOneShot(landing);
                walked += new Vector2(delta.x, delta.z).magnitude;
                if (walked >= 1.6f)
                {
                    walked %= 1.6f;CurrentSurface=ProbeSurface();
                    AudioClip step=CurrentSurface==CityFootstepKind.Grass?grassStep:CurrentSurface==CityFootstepKind.Wood?woodStep:footstep;
                    if(step!=null)effects.PlayOneShot(step);
                }
            }
            wasGrounded = grounded;
        }
        private void OnApplicationPause(bool value){suspended=value;if(value&&initialized){gameplayPaused=true;effects.Pause();dialogue.Pause();feedback.Stop();}}
        private CityFootstepKind ProbeSurface()
        {
            int count=Physics.RaycastNonAlloc(playerRoot.position+Vector3.up*.4f,Vector3.down,groundHits,1.2f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            Collider nearest=null;float distance=float.PositiveInfinity;
            if(count==groundHits.Length)return CityFootstepKind.Pavement;
            for(int i=0;i<count;i++)if(groundHits[i].collider!=null&&!groundHits[i].transform.IsChildOf(playerRoot)&&groundHits[i].distance<distance){nearest=groundHits[i].collider;distance=groundHits[i].distance;}
            if(nearest!=null)
            {
                CityFootstepSurface surface=nearest.GetComponentInParent<CityFootstepSurface>();
                if(surface!=null&&System.Enum.IsDefined(typeof(CityFootstepKind),surface.kind))return surface.kind;
            }
            return streets!=null?streets.SurfaceAtWorld(playerRoot.position):CityFootstepKind.Pavement;
        }
        public static AudioClip CreateSurfaceContact(CityFootstepKind kind)
        {
            if(!System.Enum.IsDefined(typeof(CityFootstepKind),kind))throw new System.ArgumentOutOfRangeException(nameof(kind));
            const int rate=22050;float[] samples=new float[4410];uint noise=0x712abdU;float low=0;
            for(int i=0;i<samples.Length;i++)
            {
                noise^=noise<<13;noise^=noise>>17;noise^=noise<<5;
                float t=(float)i/rate;low+=.25f*((noise&65535)/32767.5f-1-low);
                float envelope=Mathf.Sin(Mathf.PI*i/(samples.Length-1))*Mathf.Exp(-t*18);
                samples[i]=envelope*(kind==CityFootstepKind.Grass?low*.65f:Mathf.Sin(2*Mathf.PI*(kind==CityFootstepKind.Wood?165:100)*t)*.2f+low*.2f);
            }
            AudioClip clip=AudioClip.Create("VKT_OriginalSurface_"+kind,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        // Original synthesized cues; callers own and must destroy returned clips.
        public static AudioClip CreateFeedbackClip(CityAudioCue cue)
        {
            if((int)cue<0||(int)cue>2)throw new System.ArgumentOutOfRangeException(nameof(cue));
            const int rate=22050;int count=(int)(rate*(cue==CityAudioCue.Success?.34f:.24f));float[] samples=new float[count];
            for(int i=0;i<count;i++)
            {
                float t=(float)i/rate,p=(float)i/(count-1),envelope=Mathf.Pow(Mathf.Sin(Mathf.PI*p),2);
                // Integrate the chirp phase, rather than multiplying a changing frequency by time.
                float start=cue==CityAudioCue.Failure?360:cue==CityAudioCue.Success?520:660;
                float slope=cue==CityAudioCue.Failure?-140:cue==CityAudioCue.Success?360:0;
                float phase=2*Mathf.PI*(start*t+.5f*slope*t*p);
                samples[i]=.2f*envelope*Mathf.Sin(phase);
            }
            AudioClip clip=AudioClip.Create("VKT_Feedback_"+cue,count,1,rate,false);clip.SetData(samples,0);return clip;
        }
        private static AudioClip CreateContact(bool heavy)
        {
            const int rate=22050;int count=(int)(rate*(heavy?.24f:.13f));float[] data=new float[count];uint random=0x6172a1u;float low=0;
            for(int i=0;i<count;i++)
            {
                random^=random<<13;random^=random>>17;random^=random<<5;
                float t=(float)i/rate;low+=.18f*((random&65535)/32767.5f-1-low);
                float envelope=Mathf.Min(1,t/.004f)*Mathf.Exp(-t*(heavy?22:35));
                data[i]=envelope*(low*.5f+Mathf.Sin(2*Mathf.PI*(heavy?72:115)*t)*.22f);
            }
            AudioClip clip=AudioClip.Create(heavy?"VKT_AuthoredLanding":"VKT_AuthoredFootstep",count,1,rate,false);clip.SetData(data,0);return clip;
        }
    }
}
