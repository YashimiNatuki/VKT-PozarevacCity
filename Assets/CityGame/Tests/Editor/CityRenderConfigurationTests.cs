using NUnit.Framework;
using UnityEngine.Rendering;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityRenderConfigurationTests
    {
        [Test]
        public void InstalledMapRenderCallbacksHaveADefaultPipeline()
        {
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.Not.Null,
                "ArcGIS terrain occlusion reads defaultRenderPipeline; a Quality-only pipeline causes a render-time null reference.");
        }
    }
}
