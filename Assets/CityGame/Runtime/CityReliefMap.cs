using System;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // Validated, immutable map-only surface samples. This type never changes world geometry.
    public sealed class CityReliefMap
    {
        [Serializable] public sealed class Data
        {
            public int schema, gridSize;
            public string usage, surfaceModel, verticalDatum, order, sourceSha256, attribution, disclaimer;
            public bool streetHeightsVerified;
            public double anchorLatitude, anchorLongitude;
            public float radiusMeters, minimumMeters, maximumMeters;
            public float[] heightsMeters;
        }
        private readonly float[] heights;
        private readonly int size;
        private static readonly Vector3 Light = new Vector3(-.6f, 1, .7f).normalized;
        public float Radius { get; }
        public float Minimum { get; }
        public float Maximum { get; }
        private CityReliefMap(Data data)
        {
            heights = (float[])data.heightsMeters.Clone(); size = data.gridSize;
            Radius = data.radiusMeters; Minimum = data.minimumMeters; Maximum = data.maximumMeters;
        }
        public static CityReliefMap Load(CityOsmStreets.Data streets)
        {
            TextAsset asset = Resources.Load<TextAsset>("Maps/PozarevacRelief");
            if (asset == null) return null;
            try { return TryCreate(JsonUtility.FromJson<Data>(asset.text), streets, out CityReliefMap map) ? map : null; }
            catch (ArgumentException) { return null; }
        }
        public static bool TryCreate(Data data, CityOsmStreets.Data streets, out CityReliefMap map)
        {
            map = null;
            if (data == null || streets == null || data.schema != 1 || data.usage != "map-only" || data.surfaceModel != "DSM" ||
                data.verticalDatum != "EGM2008" || data.order != "south-to-north-row-major" || data.streetHeightsVerified ||
                data.sourceSha256 == null || data.sourceSha256.Length != 64 || string.IsNullOrEmpty(data.attribution) || string.IsNullOrEmpty(data.disclaimer) ||
                !double.IsFinite(data.anchorLatitude) || !double.IsFinite(data.anchorLongitude) ||
                !double.IsFinite(streets.anchorLatitude) || !double.IsFinite(streets.anchorLongitude) ||
                Math.Abs(data.anchorLatitude - streets.anchorLatitude) > .000001 || Math.Abs(data.anchorLongitude - streets.anchorLongitude) > .000001 ||
                data.gridSize < 3 || data.gridSize > 257 || !float.IsFinite(data.radiusMeters) || data.radiusMeters < 100 || data.radiusMeters > 5000 ||
                data.heightsMeters == null || data.heightsMeters.Length != data.gridSize * data.gridSize ||
                !float.IsFinite(data.minimumMeters) || !float.IsFinite(data.maximumMeters) || data.minimumMeters > data.maximumMeters) return false;
            foreach (float value in data.heightsMeters)
                if (!float.IsFinite(value) || value < -500 || value > 9000 || value < data.minimumMeters - .011f || value > data.maximumMeters + .011f) return false;
            map = new CityReliefMap(data); return true;
        }
        public bool TrySample(float x, float z, out float height)
        {
            height = 0;
            if (!float.IsFinite(x) || !float.IsFinite(z) || Mathf.Abs(x) > Radius || Mathf.Abs(z) > Radius) return false;
            float u = (x / Radius * .5f + .5f) * (size - 1), v = (z / Radius * .5f + .5f) * (size - 1);
            int ix = Mathf.Min(Mathf.FloorToInt(u), size - 2), iz = Mathf.Min(Mathf.FloorToInt(v), size - 2);
            height = Mathf.Lerp(Mathf.Lerp(heights[iz * size + ix], heights[iz * size + ix + 1], u - ix),
                Mathf.Lerp(heights[(iz + 1) * size + ix], heights[(iz + 1) * size + ix + 1], u - ix), v - iz);
            return true;
        }
        public Color32 Shade(float x, float z)
        {
            TrySample(x, z, out float height);
            float step = Radius * 2 / (size - 1);
            TrySample(Mathf.Clamp(x - step, -Radius, Radius), z, out float west);
            TrySample(Mathf.Clamp(x + step, -Radius, Radius), z, out float east);
            TrySample(x, Mathf.Clamp(z - step, -Radius, Radius), out float south);
            TrySample(x, Mathf.Clamp(z + step, -Radius, Radius), out float north);
            Vector3 normal = new Vector3(-(east - west) / (2 * step), 1, -(north - south) / (2 * step)).normalized;
            float light = .65f + Mathf.Max(0, Vector3.Dot(normal, Light)) * .4f;
            Color color = Color.Lerp(new Color(.17f,.29f,.28f), new Color(.62f,.49f,.31f), Mathf.InverseLerp(Minimum, Maximum, height));
            if (Mathf.Repeat(height, 10) < .65f) light *= .8f;
            color *= light; color.a = 1; return color;
        }
    }
}
