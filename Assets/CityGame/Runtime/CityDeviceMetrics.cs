#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

namespace Pozarevac.CityGame
{
    // Local development evidence only. No identifiers, analytics service or network traffic.
    public sealed class CityDeviceMetrics : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public string platform, utc;
            public string cpu,gpu,scope;
            public int width,height,frames,touchPresses,audioSampleRate;
            public double observedFps,p95FrameMilliseconds;
            public long totalEligibleFrames,allocatedBytes,baselineAllocatedBytes;
            public double eligibleSeconds,windowSeconds,medianFrameMilliseconds;
            public bool physicalTouchReviewed,humanAudioReviewed;
        }
        private readonly CityFrameWindow frames=new CityFrameWindow();
        private CityGameBootstrap game;
        private int touches;
        private long baseline;
        private double sinceWrite;
        private bool suspended,wasEligible;
        public string OutputPath=>Path.Combine(Application.persistentDataPath,"vkt-device-metrics.json");
        public void Initialize(CityGameBootstrap owner){game=owner;}
        private void Update()
        {
            bool eligible=game!=null&&game.WorldReady&&!suspended&&!game.Director.IsPaused&&game.Vitals.IsAlive&&(game.Intro==null||!game.Intro.IsActive);
            if(!eligible){wasEligible=false;return;}
            if(!wasEligible){wasEligible=true;if(baseline==0)baseline=Profiler.GetTotalAllocatedMemoryLong();return;} // The first resume frame may include suspended wall time.
            float seconds=Time.unscaledDeltaTime;if(!float.IsFinite(seconds)||seconds<=0)return;
            frames.Add(seconds);sinceWrite+=seconds;
            if(Touchscreen.current!=null)foreach(var touch in Touchscreen.current.touches)if(touch.press.wasPressedThisFrame)touches++;
            if(sinceWrite>=30){Write();sinceWrite=0;}
        }
        private void Write()
        {
            var window=frames.Read();if(window.Frames==0)return;
            var report=new Report{platform=Application.platform.ToString(),utc=DateTime.UtcNow.ToString("O"),width=Screen.width,height=Screen.height,
                frames=window.Frames,touchPresses=touches,audioSampleRate=AudioSettings.outputSampleRate,
                observedFps=window.Fps,p95FrameMilliseconds=window.P95Milliseconds,medianFrameMilliseconds=window.MedianMilliseconds,
                totalEligibleFrames=frames.TotalFrames,eligibleSeconds=frames.EligibleSeconds,windowSeconds=window.Seconds,
                cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,allocatedBytes=Profiler.GetTotalAllocatedMemoryLong(),baselineAllocatedBytes=baseline,
                scope="Latest 1800 eligible frame deltas; lifetime excludes intro, owned pause, suspension and death. Unity allocated memory is not process RSS or a leak diagnosis."};
            try
            {
                File.WriteAllText(OutputPath,JsonUtility.ToJson(report,true));
                Debug.Log("VKT_DEVICE_METRICS frames="+window.Frames+" fps="+report.observedFps.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+" touchPresses="+touches+" eligibleSeconds="+report.eligibleSeconds.ToString("F1",System.Globalization.CultureInfo.InvariantCulture));
            }
            catch(Exception error)when(error is IOException||error is UnauthorizedAccessException){Debug.LogWarning("VKT device metrics unavailable: "+error.GetType().Name);}
        }
        private void OnApplicationPause(bool paused){suspended=paused;if(paused)Write();}
        private void OnApplicationQuit(){Write();}
    }
}
#endif
