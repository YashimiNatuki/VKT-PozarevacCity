using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityAudioLifecycleTests
    {
        [Test]
        public void MusicPreferencesCanBeSetBeforeInitializationAndRejectNaN()
        {
            GameObject owner = new GameObject("MusicPreferencesTest");
            try
            {
                CityMusicPlayer music = owner.AddComponent<CityMusicPlayer>();
                music.Volume = 2f; Assert.AreEqual(1f, music.Volume);
                music.Volume = -1f; Assert.AreEqual(0f, music.Volume);
                music.Volume = float.NaN; Assert.AreEqual(0f, music.Volume);
                music.Volume = .7f; music.Muted = true;
                Assert.AreEqual(.7f, music.Volume); Assert.IsTrue(music.Muted);
                music.enabled = false;
                Assert.AreEqual(.7f, music.Volume); Assert.IsTrue(music.Muted);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void UninitializedDialogueFailsExplicitlyInsteadOfNullReference()
        {
            GameObject owner = new GameObject("DialogueInitializationTest");
            try
            {
                CityGameAudio audio = owner.AddComponent<CityGameAudio>();
                Assert.Throws<System.InvalidOperationException>(() => audio.Speak(CityVoice.Mila));
                Assert.Throws<System.ArgumentOutOfRangeException>(() => audio.Speak((CityVoice)99));
                Assert.IsFalse(audio.IsDialoguePlaying);
            }
            finally { Object.DestroyImmediate(owner); }
        }
    }
}
