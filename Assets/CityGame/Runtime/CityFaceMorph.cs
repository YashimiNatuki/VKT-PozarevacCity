using UnityEngine;

namespace Pozarevac.CityGame
{
    // Adapter for the reviewed Z-up Kenney characterMedium mesh, not a general facial rig.
    public static class CityFaceMorph
    {
        public const string JawShape = "Rodja_JawRelax";

        public static int AddJaw(Mesh ownedMesh, Vector3[] vertices, BoneWeight[] weights, int head)
        {
            if (head < 0 || vertices.Length != weights.Length || ownedMesh.vertexCount != vertices.Length) return -1;
            Bounds bounds = default;
            int count = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (weights[i].boneIndex0 != head || weights[i].weight0 < .99f) continue;
                if (count++ == 0) bounds = new Bounds(vertices[i], Vector3.zero);
                else bounds.Encapsulate(vertices[i]);
            }
            if (count < 12 || bounds.size.z < .005f || bounds.size.z > .03f || bounds.size.y < .001f) return -1;
            var delta = new Vector3[vertices.Length];
            for (int i = 0; i < delta.Length; i++)
            {
                if (weights[i].boneIndex0 != head || weights[i].weight0 < .99f) continue;
                Vector3 p = vertices[i];
                float lower = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.12f, .48f, Mathf.InverseLerp(bounds.min.z, bounds.max.z, p.z)));
                float front = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.12f, .62f, Mathf.InverseLerp(bounds.min.y, bounds.max.y, p.y)));
                // Position masks keep duplicated UV seam vertices together. The neck and skull stay fixed.
                float influence = lower * front;
                delta[i] = new Vector3(0, bounds.size.z * .018f, -bounds.size.z * .055f) * influence;
            }
            ownedMesh.AddBlendShapeFrame(JawShape, 100, delta, null, null);
            return ownedMesh.GetBlendShapeIndex(JawShape);
        }

        public static float JawWeight(CityFaceMood mood, float effort, float seconds)
        {
            if (float.IsNaN(effort) || float.IsInfinity(effort)) effort = 0;
            if (float.IsNaN(seconds) || float.IsInfinity(seconds)) seconds = 0;
            float breath = .5f + .5f * Mathf.Sin(Mathf.Repeat(seconds, 100) * 2.4f);
            switch (mood)
            {
                case CityFaceMood.Effort: return Mathf.Lerp(.12f, .42f, Mathf.Clamp01(effort)) * (.7f + breath * .3f);
                case CityFaceMood.Hurt: return .32f;
                case CityFaceMood.Success: return .10f;
                case CityFaceMood.Focus: return .025f;
                default: return .015f * breath;
            }
        }
    }
}
