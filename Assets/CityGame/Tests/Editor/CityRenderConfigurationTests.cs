using NUnit.Framework;
using UnityEngine.Rendering;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityRenderConfigurationTests
    {
        [Test]
        public void InstalledMapRenderCallbacksHaveADefaultPipeline()
        {
            if (!System.IO.Directory.Exists("Packages/com.esri.arcgis-maps-sdk"))
                Assert.Ignore("Optional ArcGIS integration is absent from the portable source export.");
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.Not.Null,
                "ArcGIS terrain occlusion reads defaultRenderPipeline; a Quality-only pipeline causes a render-time null reference.");
        }
    }
}
