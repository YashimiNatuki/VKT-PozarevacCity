#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pozarevac.CityGame
{
    // Local development evidence only. No identifiers, analytics service or network traffic.
    public sealed class CityDeviceMetrics : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public string platform, utc;
            public int width,height,frames,touchPresses,audioSampleRate;
            public double observedFps,p95FrameMilliseconds;
            public bool physicalTouchReviewed,humanAudioReviewed;
        }
        private readonly float[] frames=new float[1800];
        private CityGameBootstrap game;
        private int count,index,touches;
        private double sinceWrite;
        private bool suspended,wasEligible;
        public string OutputPath=>Path.Combine(Application.persistentDataPath,"vkt-device-metrics.json");
        public void Initialize(CityGameBootstrap owner){game=owner;}
        private void Update()
        {
            bool eligible=game!=null&&game.WorldReady&&!suspended&&!game.Director.IsPaused&&game.Vitals.IsAlive&&(game.Intro==null||!game.Intro.IsActive);
            if(!eligible){wasEligible=false;return;}
            if(!wasEligible){wasEligible=true;return;} // The first resume frame may include suspended wall time.
            float seconds=Time.unscaledDeltaTime;if(!float.IsFinite(seconds)||seconds<=0)return;
            frames[index]=seconds;index=(index+1)%frames.Length;count=Math.Min(count+1,frames.Length);sinceWrite+=seconds;
            if(Touchscreen.current!=null)foreach(var touch in Touchscreen.current.touches)if(touch.press.wasPressedThisFrame)touches++;
            if(sinceWrite>=30){Write();sinceWrite=0;}
        }
        private void Write()
        {
            if(count==0)return;
            var sorted=new float[count];Array.Copy(frames,sorted,count);Array.Sort(sorted);double total=0;
            foreach(float frame in sorted)total+=frame;
            var report=new Report{platform=Application.platform.ToString(),utc=DateTime.UtcNow.ToString("O"),width=Screen.width,height=Screen.height,
                frames=count,touchPresses=touches,audioSampleRate=AudioSettings.outputSampleRate,
                observedFps=count/total,p95FrameMilliseconds=sorted[Math.Max(0,(int)Math.Ceiling(count*.95)-1)]*1000};
            try
            {
                File.WriteAllText(OutputPath,JsonUtility.ToJson(report,true));
                Debug.Log("VKT_DEVICE_METRICS frames="+count+" fps="+report.observedFps.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+" touchPresses="+touches);
            }
            catch(Exception error)when(error is IOException||error is UnauthorizedAccessException){Debug.LogWarning("VKT device metrics unavailable: "+error.GetType().Name);}
        }
        private void OnApplicationPause(bool paused){suspended=paused;if(paused)Write();}
        private void OnApplicationQuit(){Write();}
    }
}
#endif
