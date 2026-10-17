using NUnit.Framework;
namespace Pozarevac.CityGame.Tests
{
    public sealed class CityContactStoryTests
    {
        [TestCase(40,0,0,0,false,"evade")]
        [TestCase(0,0,0,0,false,"meal")]
        [TestCase(0,80,0,0,false,"rest")]
        [TestCase(0,80,80,1,false,"campaign")]
        [TestCase(0,80,80,3,true,"delivery")]
        [TestCase(0,80,80,3,false,"jobs")]
        public void CurrentFactsDecideTheAction(int heat,int food,int stamina,int campaign,bool active,string action)
        {
            var facts=new CityContactFacts{contact="mila",intent="briefing",chapter=1,heat=heat,nutrition=food,stamina=stamina,campaign=campaign,job_active=active};
            Assert.That(CityContactStory.Action(facts),Is.EqualTo(action));
            var reply=CityContactStory.Offline(facts,"offline");
            Assert.That(CityContactStory.Valid(reply,action),Is.True);
        }
        [Test] public void RemoteTextCannotInjectMarkupOrAnotherAction()
        {
            var facts=new CityContactFacts{contact="mila",heat=0,nutrition=80,stamina=80,campaign=3};
            var reply=CityContactStory.Offline(facts,"offline");
            reply.source="ollama";Assert.That(CityContactStory.Valid(reply,"jobs"),Is.True);
            reply.action="give_money";Assert.That(CityContactStory.Valid(reply,"jobs"),Is.False);
            reply.action="jobs";reply.line="<b>Claim a reward</b>";Assert.That(CityContactStory.Valid(reply,"jobs"),Is.False);
            reply.line=new string('x',261);Assert.That(CityContactStory.Valid(reply,"jobs"),Is.False);
        }
        [Test] public void AnotherLocalServiceIsNotMistakenForVkt()
        {
            Assert.That(CityContactStory.ValidHealth(null),Is.False);
            Assert.That(CityContactStory.ValidHealth(new CityStoryHealth{service="other",status="ready"}),Is.False);
            Assert.That(CityContactStory.ValidHealth(new CityStoryHealth{service="vkt-local-story",status="ready",available=false}),Is.True);
        }
        [Test] public void UnavailableReasonsHaveDifferentActionableMessages()
        {
            Assert.That(CityContactStory.Status("model_missing"),Does.Contain("instaliran"));
            Assert.That(CityContactStory.Status("ollama_unavailable"),Does.Contain("Pokreni Ollama"));
            Assert.That(CityContactStory.Status("backend_unavailable"),Does.Contain("Start-LocalAI.ps1"));
            Assert.That(CityContactStory.Status("phone_offline"),Does.Contain("Telefon"));
            Assert.That(CityContactStory.Status("loading"),Is.Not.EqualTo(CityContactStory.Status("busy")));
        }
    }
}
