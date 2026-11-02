using System;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // Presentation owns its meshes/materials. It never owns input, colliders, gameplay clocks or saves.
    internal sealed class CityHumanPhonePose : IDisposable
    {
        private readonly Transform frame;
        private readonly Transform[] arms;
        private readonly Quaternion[] from = new Quaternion[6];
        private readonly CityHumanHandPose left, right;
        private readonly float leftSide, rightSide;
        private readonly Material[] materials = new Material[4];
        private Mesh mesh;
        public GameObject Visual { get; private set; }
        public bool Active { get; private set; }
        public float Weight { get; private set; }
        public float TapDistance { get; private set; } = float.PositiveInfinity;
        private float tap = -1;
        private bool queued;

        public static CityHumanPhonePose Create(Transform owner, Transform reference, Transform[] limbs, CityHumanHandPose left, CityHumanHandPose right)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return shader == null ? null : new CityHumanPhonePose(owner, reference, limbs, left, right, shader);
        }
        private CityHumanPhonePose(Transform owner, Transform reference, Transform[] limbs, CityHumanHandPose left, CityHumanHandPose right, Shader shader)
        {
            frame = reference; arms = limbs; this.left = left; this.right = right;
            leftSide = Mathf.Sign(Vector3.Dot(arms[0].position - frame.position, frame.right));
            rightSide = Mathf.Sign(Vector3.Dot(arms[3].position - frame.position, frame.right));
            try
            {
                mesh = CityVisualMeshes.RoundedBox(.12f, 8);
                materials[0] = Material(shader, new Color(.065f, .075f, .085f), .55f, .68f);
                materials[1] = Material(shader, new Color(.025f, .14f, .18f), .15f, .9f);
                materials[2] = Material(shader, new Color(.008f, .012f, .016f), .2f, .7f);
                materials[3] = Material(shader, new Color(.25f, .8f, .82f), .05f, .5f);
                materials[1].EnableKeyword("_EMISSION"); materials[1].SetColor("_EmissionColor", new Color(.025f, .11f, .14f));
                Visual = new GameObject("VKT_HeldPhone3D"); Visual.transform.SetParent(owner, false);
                Vector3 scale = owner.lossyScale;
                Visual.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
                Part("Body", Vector3.zero, new Vector3(.075f, .151f, .009f), 0);
                Part("Glass", new Vector3(0, 0, .0049f), new Vector3(.066f, .133f, .0006f), 1);
                Part("Speaker", new Vector3(0, .063f, .0055f), new Vector3(.018f, .0018f, .0006f), 2);
                Part("HomeBar", new Vector3(0, -.058f, .0055f), new Vector3(.017f, .0013f, .0006f), 3);
                Part("ScreenLine", new Vector3(-.009f, .029f, .0055f), new Vector3(.033f, .003f, .0006f), 3);
                Part("CameraIsland", new Vector3(-.018f, .053f, -.005f), new Vector3(.025f, .025f, .002f), 0);
                Part("LensA", new Vector3(-.022f, .058f, -.0062f), new Vector3(.008f, .008f, .001f), 2);
                Part("LensB", new Vector3(-.013f, .049f, -.0062f), new Vector3(.008f, .008f, .001f), 2);
                Part("Power", new Vector3(.0378f, .025f, 0), new Vector3(.0015f, .019f, .003f), 0);
                Part("Volume", new Vector3(-.0378f, .018f, 0), new Vector3(.0015f, .023f, .003f), 0);
                Visual.SetActive(false);
            }
            catch { Dispose(); throw; }
        }

        public void SetActive(bool active)
        {
            if (Active == active) return;
            if (active)
            {
                for (int i = 0; i < 6; i++) from[i] = arms[i].localRotation;
                left.BeginPhone(); right.BeginPhone(); Active = true;
            }
            else
            {
                Restore(); left.EndPhone(); right.EndPhone(); Active = false;
                Weight = 0; tap = -1; queued = false; TapDistance = float.PositiveInfinity;
                if (Visual != null) Visual.SetActive(false);
            }
        }
        public void NotifyTap() { if (Active) queued = true; }
        public void Step(float seconds)
        {
            if (!Active || Visual == null || !float.IsFinite(seconds) || seconds <= 0) return;
            float dt = Mathf.Min(seconds, .05f);
            Restore(); Weight = Mathf.MoveTowards(Weight, 1, dt * 4);
            Vector3 shoulder = (arms[0].position + arms[3].position) * .5f;
            Vector3 normal = (-frame.forward + frame.up * .32f).normalized;
            Vector3 up = Vector3.ProjectOnPlane(frame.up, normal).normalized;
            Vector3 target = shoulder - frame.up * .14f + frame.forward * .30f + frame.right * leftSide * .08f;
            PoseArm(0, target, left.PhoneOrientation(up, normal), leftSide, Weight);
            left.ApplyPhone(false, Weight);
            Visual.transform.SetPositionAndRotation(left.PhoneAnchor, left.PhoneFrame);
            Visual.SetActive(true);
            TapDistance = float.PositiveInfinity;
            if (Weight >= .999f && queued && tap < 0) { queued = false; tap = 0; }
            if (tap >= 0)
            {
                tap += dt;
                if (tap >= .5f) tap = -1;
                else
                {
                    float reach = Mathf.Sin(tap * Mathf.PI / .5f); reach *= reach;
                    Vector3 point = Visual.transform.TransformPoint(new Vector3(0, .015f, .0055f));
                    Vector3 fingertip = point + Visual.transform.forward * (.022f * (1 - reach) + .003f);
                    PosePointer(fingertip, reach);
                    right.ApplyPhone(true, reach);
                    TapDistance = Vector3.Distance(right.Tip(0), point);
                }
            }
        }
        private void PosePointer(Vector3 fingertip, float weight)
        {
            // Fit the calibrated index direction to the forearm, retaining fingertip contact.
            // Bounded fixed-point steps avoid solving a fingertip target by overbending the wrist.
            Vector3 normal = -Visual.transform.up;
            Quaternion orientation = right.PointerOrientation((fingertip - arms[4].position).normalized, normal);
            Vector3 hint = frame.right * rightSide * .65f - frame.up - frame.forward * .15f;
            for (int i = 0; i < 6; i++)
            {
                CityTwoBonePose.Solve(arms[3], arms[4], arms[5], right.PointerWrist(fingertip, orientation), hint);
                orientation = right.PointerOrientation((arms[5].position - arms[4].position).normalized, normal);
                arms[5].rotation = orientation;
            }
            for (int i = 3; i < 6; i++) arms[i].localRotation = Quaternion.Slerp(from[i], arms[i].localRotation, weight);
        }
        private void PoseArm(int start, Vector3 target, Quaternion orientation, float side, float weight)
        {
            CityTwoBonePose.Solve(arms[start], arms[start + 1], arms[start + 2], target, frame.right * side * .65f - frame.up - frame.forward * .15f);
            arms[start + 2].rotation = orientation;
            for (int i = start; i < start + 3; i++) arms[i].localRotation = Quaternion.Slerp(from[i], arms[i].localRotation, weight);
        }
        private void Restore()
        {
            for (int i = 0; i < 6; i++) if (arms[i] != null) arms[i].localRotation = from[i];
            left.RestorePhone(); right.RestorePhone();
        }
        public float GripDistance
        {
            get
            {
                if (!Active || Visual == null) return float.PositiveInfinity;
                float first = float.PositiveInfinity, second = first;
                for (int i = 0; i < 4; i++)
                {
                    Vector3 p = Visual.transform.InverseTransformPoint(left.Tip(i));
                    float x = Mathf.Max(0, Mathf.Abs(p.x) - .0375f), y = Mathf.Max(0, Mathf.Abs(p.y) - .0755f);
                    float distance = new Vector3(x, y, p.z + .0045f).magnitude;
                    if (distance < first) { second = first; first = distance; } else second = Mathf.Min(second, distance);
                }
                return second;
            }
        }
        private static Material Material(Shader shader, Color color, float metal, float smooth)
        {
            var material = new Material(shader) { name = "VKT owned phone material" };
            material.SetColor(material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color", color);
            material.SetFloat("_Metallic", metal); material.SetFloat("_Smoothness", smooth); return material;
        }
        private void Part(string name, Vector3 position, Vector3 scale, int material)
        {
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); part.layer = 2;
            part.transform.SetParent(Visual.transform, false); part.transform.localPosition = position; part.transform.localScale = scale;
            part.GetComponent<MeshFilter>().sharedMesh = mesh; part.GetComponent<MeshRenderer>().sharedMaterial = materials[material];
        }
        public void Dispose()
        {
            SetActive(false); Release(Visual); Visual = null; Release(mesh); mesh = null;
            foreach (Material material in materials) Release(material);
        }
        private static void Release(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
