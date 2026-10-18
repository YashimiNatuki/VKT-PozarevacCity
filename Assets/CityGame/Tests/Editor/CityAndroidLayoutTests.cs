using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityAndroidLayoutTests
    {
        [Test] public void MusicLauncherClearsTouchActionRowsAndVitals()
        {
            Vector2 origin=CityMusicPanel.LauncherPosition(true);
            // Common 1280x720 reference: music is top-left pivoted; touch buttons are centered.
            Rect music=new Rect(1280+origin.x,origin.y-42,120,42);
            foreach(Vector2 at in new[]{new Vector2(-145,70),new Vector2(-60,160),new Vector2(-60,250),new Vector2(-170,250)})
            {
                Rect action=new Rect(1280+at.x-43,at.y-36,86,72);
                Assert.IsFalse(music.Overlaps(action),"Music blocks a touch action.");
            }
            Rect vitals=new Rect(1004,550,260,154);
            Assert.IsFalse(music.Overlaps(vitals));
            Assert.That(music.xMin,Is.GreaterThan(0));Assert.That(music.xMax,Is.LessThan(1280));
            Assert.That(music.yMin,Is.GreaterThan(0));Assert.That(music.yMax,Is.LessThan(720));
        }
    }
}
