using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityReliefMapTests
    {
        [Test] public void BundledReliefMatchesTheStreetAnchorAndContainsSurfaceVariation()
        {
            CityOsmStreets.Data streets = CityOsmStreets.Load();
            CityReliefMap relief = CityReliefMap.Load(streets);
            Assert.That(relief, Is.Not.Null);
            Assert.That(relief.Radius, Is.EqualTo(2000));
            Assert.That(relief.Maximum - relief.Minimum, Is.GreaterThan(100));
            Assert.That(relief.TrySample(0, 0, out float height), Is.True);
            Assert.That(height, Is.InRange(70, 110));
            streets.anchorLongitude += .01;
            Assert.That(CityReliefMap.Load(streets), Is.Null, "Mismatched georeferencing must not silently move hills.");
        }

        [Test] public void SamplingPreservesNorthAndRejectsOutsideOrInvalidData()
        {
            var dto = new CityReliefMap.Data { schema=1, usage="map-only", surfaceModel="DSM", verticalDatum="EGM2008", order="south-to-north-row-major",
                radiusMeters=100, gridSize=3, heightsMeters=new float[] { 80,80,80,90,90,90,100,100,100 }, minimumMeters=80, maximumMeters=100,
                sourceSha256=new string('a',64), attribution="Copernicus", disclaimer="No liability" };
            Assert.That(CityReliefMap.TryCreate(dto, new CityOsmStreets.Data(), out CityReliefMap relief), Is.True);
            Assert.That(relief.TrySample(25, 50, out float north), Is.True);
            Assert.That(north, Is.EqualTo(95).Within(.001f));
            Assert.That(relief.TrySample(0, -100, out float south), Is.True); Assert.That(south, Is.EqualTo(80));
            Assert.That(relief.TrySample(101, 0, out _), Is.False);
            Assert.That(relief.TrySample(float.NaN, 0, out _), Is.False);
            dto.heightsMeters[0] = float.PositiveInfinity;
            Assert.That(CityReliefMap.TryCreate(dto, new CityOsmStreets.Data(), out _), Is.False);
            Assert.That(relief.TrySample(0, -100, out _), Is.True, "Validated map must own its data snapshot.");
        }

        [Test] public void RealMapToggleReusesItsTextureAndPreservesGameplayClockAndOwners()
        {
            GameObject root = new GameObject("ReliefMapFixture", typeof(RectTransform));
            float scale = Time.timeScale;
            try
            {
                CityMapView view = root.AddComponent<CityMapView>(); view.Initialize();
                Button toggle = root.transform.Find("ReliefToggle").GetComponent<Button>();
                Texture original = root.GetComponentInChildren<RawImage>().texture;
                int colliders = root.GetComponentsInChildren<Collider>(true).Length;
                toggle.onClick.Invoke(); Assert.That(view.ReliefActive, Is.True);
                Assert.That(root.transform.Find("MapStatus").GetComponent<Text>().text, Does.Contain("© OpenStreetMap contributors / ODbL"), "Relief must preserve visible street attribution.");
                Assert.That(root.GetComponentInChildren<RawImage>().texture, Is.SameAs(original));
                Assert.That(root.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(colliders));
                Assert.That(root.GetComponentsInChildren<Camera>(true), Is.Empty);
                Assert.That(Time.timeScale, Is.EqualTo(scale));
                toggle.onClick.Invoke(); Assert.That(view.ReliefActive, Is.False);
                Assert.That(root.transform.Find("MapStatus").GetComponent<Text>().text, Does.Contain("© OpenStreetMap contributors / ODbL"));
            }
            finally { Object.DestroyImmediate(root); Time.timeScale=scale; }
        }
    }
}
