using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityPoliceInvestigationTests
    {
        [Test] public void HiddenPlayerCannotTeleportPoliceKnowledge()
        {
            var search=new CityPoliceInvestigation();Vector3 home=Vector3.zero,crime=Vector3.right*20;
            Assert.That(search.Step(1,true,false,Vector3.one*500,home,home),Is.EqualTo(home));
            search.ReportIncident(crime);
            Assert.That(search.Step(1,true,false,Vector3.one*900,home,home),Is.EqualTo(crime));
            Assert.That(search.LastKnown,Is.EqualTo(crime));Assert.That(search.Phase,Is.EqualTo(CityPolicePhase.Investigate));
            Vector3 seen=new Vector3(30,0,5);search.Step(1,true,true,seen,home,home);
            Assert.That(search.Phase,Is.EqualTo(CityPolicePhase.Chase));
            Vector3 goal=search.Step(3,true,false,Vector3.one*1000,seen,home);
            Assert.That(search.Phase,Is.EqualTo(CityPolicePhase.Search));Assert.That(search.LastKnown,Is.EqualTo(seen));
            Assert.That(Vector3.Distance(goal,seen),Is.LessThanOrEqualTo(4.001f));
            search.Step(1,false,false,seen,home,home);Assert.That(search.Phase,Is.EqualTo(CityPolicePhase.Patrol));
        }
        [TestCase(CityFootstepKind.Grass)] [TestCase(CityFootstepKind.Wood)]
        public void SurfaceAudioHasFiniteMonoSamplesAndQuietEdges(CityFootstepKind surface)
        {
            AudioClip clip=CityGameAudio.CreateSurfaceContact(surface);
            try
            {
                Assert.That(clip.channels,Is.EqualTo(1));float[] samples=new float[clip.samples];clip.GetData(samples,0);
                float peak=0;foreach(float value in samples){Assert.That(float.IsFinite(value),Is.True);peak=Mathf.Max(peak,Mathf.Abs(value));}
                Assert.That(peak,Is.InRange(.01f,.5f));Assert.That(Mathf.Abs(samples[0])+Mathf.Abs(samples[samples.Length-1]),Is.LessThan(.0001f));
            }
            finally{Object.DestroyImmediate(clip);}
        }
    }
}
