#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Pozarevac.CityGame
{
    public sealed partial class CityPlayerValidation
    {
        private bool humanHandsVerified;
        [Serializable] private sealed class HumanHandsReport
        {
            public bool passed, previewFists, previewDeformation, worldAttack, cooldown, pauseStable,
                pausedAttackBlocked, recovery, profileUnchanged, ownersUnchanged, controlRestored;
            public float minimumFingerDegrees, previewVertexMeters, maxRightCurlDegrees, maxLeftCurlDegrees,
                rightForwardChange, pauseFingerDelta, recoveredFingerDelta;
            public int fingerJoints;
        }
        private IEnumerator VerifyHumanHands(CityGameBootstrap game)
        {
            var result=new HumanHandsReport();var life=game.GetComponent<CityLifestyleRuntime>();
            var progress=game.GetComponent<CityProgressionController>();
            var rig=game.Player.GetComponentInChildren<CityCameraRig>();
            var mover=game.Player.GetComponent<CityPlayerController>();bool moverBefore=mover.enabled;
            string folder=Argument("-vktHumanHandsCaptures","");
            if(folder.Length==0){Finish(false,"Human hand captures missing.");yield break;}
            Directory.CreateDirectory(folder);
            try
            {
                progress.ClosePanel();game.GetComponent<CityMusicPanel>()?.Close();life.CloseMenu();
                if(game.Hideout.IsInside)game.Hideout.Travel(false);
                mover.enabled=false;
                int cameras=FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
                int listeners=FindObjectsByType<AudioListener>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
                int controllers=game.Player.GetComponentsInChildren<CharacterController>(true).Length;
                int cash=game.Session.Cash,xp=life.Life.Experience;
                if(!life.Wardrobe.Open()||!life.Wardrobe.ToggleCandidate())
                {Finish(false,"Human hand wardrobe unavailable.");yield break;}
                life.Wardrobe.Rotate(15);yield return new WaitForSecondsRealtime(.25f);yield return new WaitForEndOfFrame();
                GameObject preview=GameObject.Find("ReviewedHumanPreview");
                Transform[] fingers=HumanFingerJoints(preview);
                if(fingers==null){Finish(false,"Human must have thirty unique finger joints.");yield break;}
                result.fingerJoints=fingers.Length;
                Quaternion[] relaxed=Array.ConvertAll(fingers,b=>b.localRotation);
                for(int i=0;i<3;i++)life.Wardrobe.CycleCandidateMotion();
                yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();
                result.minimumFingerDegrees=180;
                for(int i=0;i<fingers.Length;i++)result.minimumFingerDegrees=Mathf.Min(result.minimumFingerDegrees,Quaternion.Angle(relaxed[i],fingers[i].localRotation));
                result.previewFists=result.minimumFingerDegrees>5&&game.Director.IsPaused&&life.Wardrobe.CandidateMotion==3;
                result.previewVertexMeters=HumanHandsVertexDelta(HumanFaceBody(preview),fingers,relaxed);
                result.previewDeformation=result.previewVertexMeters>.003f;
                if(!result.previewFists||!result.previewDeformation)
                {Finish(false,"Human guard did not close/deform all five digits.");yield break;}
                Camera studio=GameObject.Find("WardrobeCamera").GetComponent<Camera>();
                Transform[] all=preview.GetComponentsInChildren<Transform>(true);
                Transform left=Array.Find(all,b=>b.name=="hand_l"),right=Array.Find(all,b=>b.name=="hand_r");
                studio.transform.position=preview.transform.position+Vector3.up*1.4f+preview.transform.forward*1.6f;
                studio.transform.LookAt(preview.transform.position+Vector3.up*1.4f);
                yield return new WaitForEndOfFrame();
                if(!CaptureRenderedFrame(game,Path.Combine(folder,"hands-guard-front.png"),out string error))
                {Finish(false,error);yield break;}
                foreach(Transform hand in new[]{left,right})
                {
                    Vector3 target=hand.position+(hand.position-hand.parent.position).normalized*.035f;
                    studio.transform.position=target+preview.transform.forward*.34f+Vector3.up*.035f;
                    studio.transform.LookAt(target,Vector3.up);yield return new WaitForEndOfFrame();
                    if(!CaptureRenderedFrame(game,Path.Combine(folder,hand==left?"hands-left.png":"hands-right.png"),out error))
                    {Finish(false,error);yield break;}
                }
                progress.SaveNow();string profile=PreviewProfileSignature(game.Progression.Snapshot());
                string disk=File.ReadAllText(progress.SavePath);
                HumanButton("IGRAJ U GRADU").onClick.Invoke();
                result.profileUnchanged=profile==PreviewProfileSignature(game.Progression.Snapshot())&&disk==File.ReadAllText(progress.SavePath);
                yield return null;yield return new WaitForEndOfFrame();
                var actor=rig.HumanActor;
                if(actor==null||!actor.IsVisible||game.Director.IsPaused||!result.profileUnchanged)
                {Finish(false,"Human hand selection did not preserve control/profile.");yield break;}
                fingers=HumanFingerJoints(actor.Visual);relaxed=Array.ConvertAll(fingers,b=>b.localRotation);
                all=actor.Visual.GetComponentsInChildren<Transform>(true);
                Transform indexL=Array.Find(all,b=>b.name=="index_02_l"),indexR=Array.Find(all,b=>b.name=="index_02_r");
                right=Array.Find(all,b=>b.name=="hand_r");Transform elbow=right.parent;
                Quaternion relaxedL=indexL.localRotation,relaxedR=indexR.localRotation;
                float forward=Vector3.Dot((right.position-elbow.position).normalized,actor.Visual.transform.forward);
                game.Combat.Inventory.SelectWeapon(CityWeapon.Unarmed);game.Combat.Inventory.Tick(2);
                if(!game.Combat.TryAttack()){Finish(false,"Actual unarmed attack was rejected.");yield break;}
                result.cooldown=!game.Combat.TryAttack();
                bool captured=false;float end=Time.realtimeSinceStartup+.35f;
                while(Time.realtimeSinceStartup<end)
                {
                    yield return new WaitForEndOfFrame();
                    result.maxRightCurlDegrees=Mathf.Max(result.maxRightCurlDegrees,Quaternion.Angle(relaxedR,indexR.localRotation));
                    result.maxLeftCurlDegrees=Mathf.Max(result.maxLeftCurlDegrees,Quaternion.Angle(relaxedL,indexL.localRotation));
                    result.rightForwardChange=Mathf.Max(result.rightForwardChange,Vector3.Dot((right.position-elbow.position).normalized,actor.Visual.transform.forward)-forward);
                    if(captured||result.maxRightCurlDegrees<55||result.rightForwardChange<.5f)continue;
                    Camera view=game.Player.GetComponentInChildren<Camera>();Vector3 position=view.transform.position;Quaternion rotation=view.transform.rotation;
                    view.transform.position=actor.Visual.transform.position+Vector3.up*1.35f+actor.Visual.transform.forward*1.25f;
                    view.transform.LookAt(actor.Visual.transform.position+Vector3.up*1.35f);
                    bool saved=CaptureRenderedFrame(game,Path.Combine(folder,"hands-world-attack.png"),out error);
                    view.transform.SetPositionAndRotation(position,rotation);
                    if(!saved){Finish(false,error);yield break;}captured=true;
                    progress.OpenPanel();yield return new WaitForEndOfFrame();
                    Quaternion[] frozen=Array.ConvertAll(fingers,b=>b.localRotation);
                    result.pausedAttackBlocked=!game.Combat.TryAttack();
                    yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
                    for(int i=0;i<fingers.Length;i++)result.pauseFingerDelta=Mathf.Max(result.pauseFingerDelta,Quaternion.Angle(frozen[i],fingers[i].localRotation));
                    result.pauseStable=game.Director.IsPaused&&result.pauseFingerDelta<.02f;
                    progress.ClosePanel();end=Time.realtimeSinceStartup+.22f;
                }
                result.worldAttack=captured&&result.maxRightCurlDegrees>35&&result.maxLeftCurlDegrees>20&&result.rightForwardChange>.25f;
                if(!result.worldAttack){Finish(false,"Actual unarmed attack did not animate human fists/right arm.");yield break;}
                yield return new WaitForSecondsRealtime(.65f);yield return new WaitForEndOfFrame();
                for(int i=0;i<fingers.Length;i++)result.recoveredFingerDelta=Mathf.Max(result.recoveredFingerDelta,Quaternion.Angle(relaxed[i],fingers[i].localRotation));
                result.recovery=result.recoveredFingerDelta<1;
                result.ownersUnchanged=cameras==FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length&&
                    listeners==FindObjectsByType<AudioListener>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length&&controllers==game.Player.GetComponentsInChildren<CharacterController>(true).Length;
                result.controlRestored=!game.Director.IsPaused&&!life.Wardrobe.IsOpen&&life.CanSimulate;
                result.passed=result.worldAttack&&result.previewFists&&result.previewDeformation&&result.cooldown&&result.pauseStable&&result.pausedAttackBlocked&&
                    result.recovery&&result.profileUnchanged&&result.ownersUnchanged&&result.controlRestored&&game.Session.Cash==cash&&life.Life.Experience==xp;
                if(!result.passed){Finish(false,"Human hand pause/recovery/owner invariants failed.");yield break;}
                humanHandsVerified=true;Debug.Log("VKT_HUMAN_HANDS_VERIFIED "+JsonUtility.ToJson(result));
            }
            finally
            {
                life.Wardrobe.Close();progress.ClosePanel();mover.ResetView();mover.enabled=moverBefore;
                File.WriteAllText(Path.Combine(folder,"human-hands.json"),JsonUtility.ToJson(result,true));
            }
        }
        private static Transform[] HumanFingerJoints(GameObject root)
        {
            if(root==null)return null;Transform[] all=root.GetComponentsInChildren<Transform>(true);var result=new Transform[30];int i=0;
            foreach(string side in new[]{"l","r"})foreach(string digit in new[]{"index","middle","ring","pinky","thumb"})for(int joint=1;joint<=3;joint++)
            {
                string name=digit+"_0"+joint+"_"+side;Transform found=null;
                foreach(Transform bone in all)if(bone.name==name){if(found!=null)return null;found=bone;}
                if(found==null)return null;result[i++]=found;
            }
            return result;
        }
        private static float HumanHandsVertexDelta(SkinnedMeshRenderer skin,Transform[] joints,Quaternion[] neutral)
        {
            if(skin==null)return 0;var posed=new Mesh();var baseline=new Mesh();Quaternion[] saved=Array.ConvertAll(joints,b=>b.localRotation);
            try
            {
                skin.BakeMesh(posed,true);for(int i=0;i<joints.Length;i++)joints[i].localRotation=neutral[i];skin.BakeMesh(baseline,true);
                Vector3[] a=posed.vertices,b=baseline.vertices;float max=0;
                for(int i=0;i<a.Length;i++)max=Mathf.Max(max,skin.transform.TransformVector(a[i]-b[i]).magnitude);
                return max;
            }
            finally{for(int i=0;i<joints.Length;i++)joints[i].localRotation=saved[i];Destroy(posed);Destroy(baseline);}
        }
    }
}
#endif
