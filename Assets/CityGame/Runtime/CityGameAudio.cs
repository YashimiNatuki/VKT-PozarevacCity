using UnityEngine;
using UnityEngine.InputSystem;

namespace Pozarevac.CityGame
{
    public enum CityVoice { Mila, Vuk, Luka }
    public sealed class CityGameAudio : MonoBehaviour
    {
        private AudioSource effects, dialogue;
        private AudioClip footstep, landing;
        private AudioClip generatedFootstep, generatedLanding;
        private bool suspended;
        public bool HasMovementClips => footstep != null && landing != null;
        public void SetMuted(bool value)
        {
            Muted=value;if(effects!=null)effects.mute=value;if(dialogue!=null)dialogue.mute=value;
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
            effects = player.gameObject.AddComponent<AudioSource>(); effects.playOnAwake = false; effects.volume = 0.25f;
            dialogue = player.gameObject.AddComponent<AudioSource>(); dialogue.playOnAwake = false; dialogue.volume = 0.85f;
            footstep = Resources.Load<AudioClip>("Audio/Footstep"); landing = Resources.Load<AudioClip>("Audio/Land");
            if(footstep==null)footstep=generatedFootstep=CreateContact(false);
            if(landing==null)landing=generatedLanding=CreateContact(true);
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
        private void OnDisable() { if (effects != null) effects.Stop(); if (dialogue != null) dialogue.Stop(); }
        private void OnDestroy()
        {
            if (effects != null) Destroy(effects);
            if (dialogue != null) Destroy(dialogue);
            if(generatedFootstep!=null)Destroy(generatedFootstep);
            if(generatedLanding!=null)Destroy(generatedLanding);
            // Resources clips are shared assets and must not be destroyed by this owner.
        }
        private void Update()
        {
            if (body == null) return;
            bool shouldPause = suspended || Time.timeScale <= 0f;
            if (shouldPause != gameplayPaused)
            {
                gameplayPaused = shouldPause;
                if (shouldPause) { effects.Pause(); dialogue.Pause(); }
                else { effects.UnPause(); dialogue.UnPause(); }
            }
            if (!gameplayPaused && Cursor.lockState == CursorLockMode.Locked && Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            { SetMuted(!Muted); ui.Notify(Muted ? "Zvuk isključen / M" : "Zvuk uključen / M"); }
            Vector3 delta = body.transform.position - lastPosition; lastPosition = body.transform.position;
            bool grounded = body.enabled && body.isGrounded;
            if (delta.sqrMagnitude > 9 || Time.deltaTime <= 0) { walked = 0; wasGrounded = grounded; return; }
            if (grounded)
            {
                if (!wasGrounded && landing != null && delta.y < -0.01f) effects.PlayOneShot(landing);
                walked += new Vector2(delta.x, delta.z).magnitude;
                if (walked >= 1.6f) { walked %= 1.6f; if (footstep != null) effects.PlayOneShot(footstep); }
            }
            wasGrounded = grounded;
        }
        private void OnApplicationPause(bool value){suspended=value;if(value&&initialized){gameplayPaused=true;effects.Pause();dialogue.Pause();}}
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
