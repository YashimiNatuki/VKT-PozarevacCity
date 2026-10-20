using UnityEngine;

namespace Pozarevac.CityGame
{
    // Atlas expressions for the reviewed Kenney face. No generated dialogue or phoneme claims.
    public sealed class CityFaceExpression : MonoBehaviour
    {
        private static readonly int FaceId = Shader.PropertyToID("_Face");
        private Material surface;
        private CityCharacterPhysique physique;
        private float jaw;
        private float elapsed, effort, targetEffort;
        private readonly CityFaceState expression=new CityFaceState();
        private CityPlayerVitals vitals;
        private CityGameAudio audio;
        private bool subscribed,alert;
        public bool Preview { get; set; }
        public CityFaceMood PreviewMood { get; set; }
        public CityFaceMood Mood=>Preview?PreviewMood:expression.Resolve(targetEffort,alert);
        public Vector4 State { get; private set; }
        public void Initialize(Material material, CityCharacterPhysique body = null) { surface = material; physique = body; }
        public void SetEffort(float value) { targetEffort = float.IsNaN(value) ? 0 : Mathf.Clamp01(value); }
        public void SetAlert(bool value) { alert=value; }
        public void BindFeedback(CityPlayerVitals health,CityGameAudio feedback)
        {
            Unsubscribe();expression.Clear();vitals=health;audio=feedback;
            if(isActiveAndEnabled)Subscribe();
        }
        private void Subscribe()
        {
            if(subscribed)return;
            if(vitals!=null){vitals.Damaged+=OnDamage;vitals.Respawned+=ResetExpression;}
            if(audio!=null)audio.CueRequested+=OnCue;
            subscribed=true;
        }
        private void Unsubscribe()
        {
            if(!subscribed)return;
            if(vitals!=null){vitals.Damaged-=OnDamage;vitals.Respawned-=ResetExpression;}
            if(audio!=null)audio.CueRequested-=OnCue;
            subscribed=false;
        }
        private void OnDamage(float amount)=>expression.Damage(amount);
        private void ResetExpression()=>expression.Clear();
        private void OnCue(CityAudioCue cue){if(cue==CityAudioCue.Success)expression.Celebrate();else expression.Focus();}
        private void OnEnable()=>Subscribe();
        private void OnDestroy()=>Unsubscribe();

        public static float Blink(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) return 0;
            float phase = Mathf.Repeat(seconds, 4.1f);
            // A fast closure and slower reopening, separated by a long neutral interval.
            return phase < .065f ? phase / .065f : phase < .20f ? 1 - (phase - .065f) / .135f : 0;
        }

        private void LateUpdate()
        {
            float dt = Preview ? Time.unscaledDeltaTime : Time.deltaTime;
            if (surface == null || dt <= 0) return;
            elapsed += Mathf.Min(dt, .1f);
            expression.Tick(dt);
            effort = Mathf.Lerp(effort, targetEffort, 1 - Mathf.Exp(-5 * dt));
            Vector4 pose=CityFaceState.Pose(Mood,Blink(elapsed+1.7f),Preview&&PreviewMood==CityFaceMood.Effort?1:effort,Mathf.Sin(elapsed*.63f)*.22f);
            Vector4 smooth=Vector4.Lerp(State,pose,1-Mathf.Exp(-9*dt));
            State=new Vector4(pose.x,smooth.y,smooth.z,smooth.w);
            surface.SetVector(FaceId, State);
            float targetJaw=CityFaceMorph.JawWeight(Mood,Preview&&PreviewMood==CityFaceMood.Effort?1:effort,elapsed);
            jaw=Mathf.Lerp(jaw,targetJaw,1-Mathf.Exp(-9*dt));
            physique?.SetJaw(jaw);
        }
        private void OnDisable()
        {
            Unsubscribe();expression.Clear();effort=targetEffort=0;alert=false;
            State = Vector4.zero;
            jaw=0;physique?.SetJaw(0);
            if (surface != null) surface.SetVector(FaceId, State);
        }
    }
}
