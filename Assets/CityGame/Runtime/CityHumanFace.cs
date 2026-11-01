using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // Applies reviewed CC0 morph targets. Never mutates imported meshes or materials.
    public sealed class CityHumanFace
    {
        private static readonly string[] Channels = { "eyeBlinkLeft", "eyeBlinkRight", "jawOpen", "mouthSmileLeft", "mouthSmileRight", "browDownLeft", "browDownRight",
            "eyeLookUpLeft", "eyeLookDownLeft", "eyeLookInLeft", "eyeLookOutLeft", "eyeLookUpRight", "eyeLookDownRight", "eyeLookInRight", "eyeLookOutRight" };
        private readonly struct Slot
        {
            public readonly SkinnedMeshRenderer Renderer;
            public readonly int Index;
            public readonly float Rest;
            public Slot(SkinnedMeshRenderer renderer, int index) { Renderer = renderer; Index = index; Rest = renderer.GetBlendShapeWeight(index); }
        }
        private readonly Transform frame, head, neck;
        private readonly Quaternion headRest, neckRest;
        private readonly Slot[][] slots;
        private readonly float horizontalSign,verticalSign;
        private readonly bool eyeGazeReady;
        private float elapsed, jaw, smile, brow;
        private Vector2 eyes;
        public Vector2 GazeAngles { get; private set; }
        public CityFaceMood Mood { get; private set; }
        public Transform Head => head;
        public bool HasEyeGaze => eyeGazeReady;

        private CityHumanFace(Transform reference, Transform headBone, Transform neckBone, Slot[][] channels)
        {
            frame = reference; head = headBone; neck = neckBone; slots = channels;
            headRest = head.localRotation; neckRest = neck.localRotation;
            bool complete=true;
            for(int i=7;i<slots.Length;i++)complete&=slots[i].Length>0;
            if(complete)
            {
                horizontalSign=DirectionSign(slots[10],reference.right);
                verticalSign=DirectionSign(slots[7],reference.up);
                eyeGazeReady=horizontalSign!=0&&verticalSign!=0;
            }
        }
        // Calibrate from the smallest complete ocular target set, not assumed FBX handedness.
        private static float DirectionSign(Slot[] candidates,Vector3 worldAxis)
        {
            Slot selected=default;int smallest=int.MaxValue;
            foreach(Slot slot in candidates)
            {
                Mesh mesh=slot.Renderer.sharedMesh;
                if(!mesh.isReadable||mesh.vertexCount>=smallest)continue;
                bool complete=true;
                for(int c=7;c<Channels.Length;c++)
                {
                    bool found=false;
                    for(int i=0;i<mesh.blendShapeCount;i++)
                    {
                        string name=mesh.GetBlendShapeName(i);
                        if(name==Channels[c]||name.EndsWith("."+Channels[c],StringComparison.Ordinal)){found=true;break;}
                    }
                    complete&=found;
                }
                if(complete){selected=slot;smallest=mesh.vertexCount;}
            }
            if(smallest==int.MaxValue)return 0;
            Mesh source=selected.Renderer.sharedMesh;
            var delta=new Vector3[source.vertexCount];
            source.GetBlendShapeFrameVertices(selected.Index,source.GetBlendShapeFrameCount(selected.Index)-1,delta,null,null);
            Vector3 sum=Vector3.zero;int count=0;
            foreach(Vector3 vertex in delta)
            {
                if(!Finite(vertex))return 0;
                if(vertex.sqrMagnitude<=1e-18f)continue;
                sum+=selected.Renderer.transform.TransformVector(vertex);count++;
            }
            float projection=count>0?Vector3.Dot(sum/count,worldAxis):0;
            return float.IsFinite(projection)&&Mathf.Abs(projection)>1e-6f?Mathf.Sign(projection):0;
        }
        public static bool TryCreate(GameObject model, Transform reference, out CityHumanFace face)
        {
            face = null;
            if (model == null || reference == null || !model.transform.IsChildOf(reference)) return false;
            Transform head = null, neck = null;
            foreach (Transform bone in model.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "head") { if (head != null) return false; head = bone; }
                if (bone.name == "neck_01") { if (neck != null) return false; neck = bone; }
            }
            if (head == null || neck == null || !head.IsChildOf(neck)) return false;
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var slots = new Slot[Channels.Length][];
            for (int channel = 0; channel < Channels.Length; channel++)
            {
                var found = new List<Slot>();
                foreach (var renderer in renderers)
                {
                    Mesh mesh = renderer.sharedMesh;
                    if (mesh == null) continue;
                    int selected=-1;
                    for (int i = 0; i < mesh.blendShapeCount; i++)
                    {
                        string name=mesh.GetBlendShapeName(i);
                        if(name!=Channels[channel]&&!name.EndsWith("."+Channels[channel],StringComparison.Ordinal))continue;
                        if(selected>=0)return false;
                        selected=i;
                    }
                    if(selected>=0)found.Add(new Slot(renderer,selected));
                }
                if (channel<7&&found.Count == 0) return false;
                slots[channel] = found.ToArray();
            }
            face = new CityHumanFace(reference, head, neck, slots); return true;
        }
        public void Step(float seconds, Vector3 worldViewDirection, CityFaceMood mood, float effort)
        {
            if (!float.IsFinite(seconds) || seconds <= 0 || frame == null || head == null || neck == null) return;
            float dt = Mathf.Min(seconds, .1f), smooth = 1 - Mathf.Exp(-9 * dt);
            elapsed = Mathf.Repeat(elapsed + dt, 410);
            effort = float.IsFinite(effort) ? Mathf.Clamp01(effort) : 0;
            Mood = (uint)mood <= (uint)CityFaceMood.Hurt ? mood : CityFaceMood.Calm;
            Vector3 direction = Finite(worldViewDirection) && worldViewDirection.sqrMagnitude > .00001f ? frame.InverseTransformDirection(worldViewDirection.normalized) : Vector3.forward;
            float desiredYaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
            float desiredPitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
            Vector2 target = new Vector2(Mathf.Clamp(desiredYaw,-35,35),Mathf.Clamp(desiredPitch,-20,20));
            GazeAngles = Vector2.Lerp(GazeAngles, target, smooth);
            Vector2 eyeTarget=Vector2.ClampMagnitude(new Vector2((desiredYaw-GazeAngles.x)/20,(GazeAngles.y-desiredPitch)/20),.65f);
            eyes=eyeGazeReady?Vector2.Lerp(eyes,eyeTarget,1-Mathf.Exp(-14*dt)):Vector2.zero;
            neck.localRotation = neckRest; head.localRotation = headRest;
            Quaternion baseHead = head.rotation;
            Quaternion aim = Quaternion.AngleAxis(GazeAngles.x, frame.up) * Quaternion.AngleAxis(GazeAngles.y, frame.right);
            neck.rotation = Quaternion.Slerp(Quaternion.identity, aim, .35f) * neck.rotation;
            head.rotation = aim * baseHead;
            float blink = CityFaceExpression.Blink(elapsed + 1.7f);
            Vector4 expression = CityFaceState.Pose(Mood, blink, effort, 0);
            jaw = Mathf.Lerp(jaw, CityFaceMorph.JawWeight(Mood, effort, elapsed) * .65f, smooth);
            smile = Mathf.Lerp(smile, Mathf.Max(0, expression.z) * .65f, smooth);
            brow = Mathf.Lerp(brow, expression.y * .55f, smooth);
            Set(0, expression.x); Set(1, expression.x); Set(2, jaw); Set(3, smile); Set(4, smile); Set(5, brow); Set(6, brow);
            float eyeX=eyes.x*horizontalSign,eyeY=eyes.y*verticalSign;
            Set(7,Mathf.Max(0,eyeY));Set(8,Mathf.Max(0,-eyeY));Set(9,Mathf.Max(0,-eyeX));Set(10,Mathf.Max(0,eyeX));
            Set(11,Mathf.Max(0,eyeY));Set(12,Mathf.Max(0,-eyeY));Set(13,Mathf.Max(0,eyeX));Set(14,Mathf.Max(0,-eyeX));
        }
        private void Set(int channel, float weight)
        {
            foreach (Slot slot in slots[channel]) if (slot.Renderer != null) slot.Renderer.SetBlendShapeWeight(slot.Index, Mathf.Clamp01(weight) * 100);
        }
        public void Reset()
        {
            if (neck != null) neck.localRotation = neckRest;
            if (head != null) head.localRotation = headRest;
            foreach (Slot[] channel in slots) foreach (Slot slot in channel) if (slot.Renderer != null) slot.Renderer.SetBlendShapeWeight(slot.Index, slot.Rest);
            elapsed = jaw = smile = brow = 0; GazeAngles = eyes = Vector2.zero; Mood = CityFaceMood.Calm;
        }
        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
