using UnityEngine;

namespace Pozarevac.CityGame
{
    public enum CityFaceMood { Calm, Focus, Effort, Success, Hurt }

    // Cosmetic state only. Never awards progression or changes health/combat authority.
    public sealed class CityFaceState
    {
        private float hurt, success, focus;
        public void Damage(float amount) { if(float.IsFinite(amount)&&amount>0)hurt=Mathf.Max(hurt,.6f+Mathf.Clamp01(amount/40)*.7f); }
        public void Celebrate() { success=1.8f; }
        public void Focus() { focus=.9f; }
        public void Clear() { hurt=success=focus=0; }
        public void Tick(float seconds)
        {
            if(!float.IsFinite(seconds)||seconds<=0)return;
            hurt=Mathf.Max(0,hurt-seconds);success=Mathf.Max(0,success-seconds);focus=Mathf.Max(0,focus-seconds);
        }
        public CityFaceMood Resolve(float exertion,bool alert)
        {
            if(hurt>0)return CityFaceMood.Hurt;
            if(float.IsFinite(exertion)&&exertion>.65f)return CityFaceMood.Effort;
            if(alert||focus>0)return CityFaceMood.Focus;
            return success>0?CityFaceMood.Success:CityFaceMood.Calm;
        }
        public static string Name(CityFaceMood mood) => mood switch
        {
            CityFaceMood.Focus=>"FOKUS",CityFaceMood.Effort=>"NAPOR",CityFaceMood.Success=>"USPEH",CityFaceMood.Hurt=>"POVREDA",_=>"MIRAN"
        };
        public static Vector4 Pose(CityFaceMood mood,float blink,float exertion,float gaze)
        {
            blink=float.IsFinite(blink)?Mathf.Clamp01(blink):0;
            exertion=float.IsFinite(exertion)?Mathf.Clamp01(exertion):0;
            gaze=float.IsFinite(gaze)?Mathf.Clamp(gaze,-.25f,.25f):0;
            return mood switch
            {
                CityFaceMood.Focus=>new Vector4(blink,.65f,0,gaze*.15f),
                CityFaceMood.Effort=>new Vector4(blink,.25f+.6f*exertion,-.15f,gaze*.2f),
                CityFaceMood.Success=>new Vector4(blink,.05f,.85f,gaze),
                CityFaceMood.Hurt=>new Vector4(Mathf.Max(blink,.45f),1,-.5f,gaze*.1f),
                _=>new Vector4(blink,.05f,.08f,gaze)
            };
        }
    }
}
