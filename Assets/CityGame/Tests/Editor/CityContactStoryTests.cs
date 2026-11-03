using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
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
        [Test]
        public void AuthoredContactsHaveDistinctVoicesWithoutChangingTheirAuthoritativeAction()
        {
            var lines = new System.Collections.Generic.HashSet<string>();
            foreach (string contact in new[] { "mila", "vuk", "luka" })
            {
                var facts = new CityContactFacts { contact=contact, nutrition=100, stamina=100, campaign=0 };
                CityContactReply reply = CityContactStory.Offline(facts, "not_requested");
                Assert.That(CityContactStory.Valid(reply, "campaign"), Is.True);
                Assert.That(lines.Add(reply.line), Is.True, "Every named contact needs an authored voice.");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FollowCurrentObjectiveOpensTheRealJournalAndRecognizesGardenDelivery(bool gardenDelivery)
        {
            float scale=Time.timeScale;
            GameObject root=new GameObject("ContactObjectiveFixture");root.SetActive(false);
            CityLifestyleRuntime lifestyle=null;CityLocalStoryPanel contacts=null;GameObject events=null;
            try
            {
                var game=root.AddComponent<CityGameBootstrap>();
                var wallet=new CitySession(2500);var state=new CityProgression(wallet);state.CreateCharacter("Contact Test");
                if(gardenDelivery)
                {
                    for(int i=1;i<=3;i++)state.CompleteContract(i);
                    var saved=state.Snapshot();
                    saved.gardenOrder=new CityGardenOrderData{phase=CityGardenOrderPhase.Deliver,variety=0,quantity=2,
                        cashReward=CityGardenOrder.Quote(0),remainingSeconds=180};
                    state=new CityProgression(wallet,saved);
                }
                var vitals=root.AddComponent<CityPlayerVitals>();
                var director=root.AddComponent<CityMissionDirector>();
                var contracts=CityMissionDirector.CreateContracts();
                Set(director,"contracts",contracts);
                Set(director,"run",gardenDelivery?CityMissionRun.RestoreCompleted(contracts[2],wallet):new CityMissionRun(contracts[0],wallet));
                Set(director,"contractIndex",gardenDelivery?2:0);
                var jobs=new CityStreetJobs(wallet);state.BindStreetJobs(jobs);Set(director,"<Jobs>k__BackingField",jobs);
                Set(game,"<Session>k__BackingField",wallet);Set(game,"<Progression>k__BackingField",state);
                Set(game,"<Director>k__BackingField",director);Set(game,"<Vitals>k__BackingField",vitals);
                var owner=root.AddComponent<CityProgressionController>();Set(owner,"game",game);Set(owner,"director",director);
                var display=new GameObject("ContactPhone",typeof(RectTransform));display.transform.SetParent(root.transform,false);
                var phone=display.AddComponent<CityPhonePanel>();phone.Initialize(owner);Set(owner,"panel",phone);
                events=(GameObject)typeof(CityPhonePanel).GetField("ownedEventSystem",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(phone);
                lifestyle=root.AddComponent<CityLifestyleRuntime>();Set(lifestyle,"game",game);
                contacts=root.AddComponent<CityLocalStoryPanel>();contacts.Initialize(game,lifestyle);
                var canvas=(GameObject)typeof(CityLocalStoryPanel).GetField("canvas",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(contacts);
                canvas.transform.Find("ContactsPanel").gameObject.SetActive(true);contacts.Select(0);
                if(gardenDelivery)
                {
                    var goal=(Text)typeof(CityLocalStoryPanel).GetField("goal",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(contacts);
                    Assert.That(goal.text,Does.Contain("Aktivna ruta"),"A garden parcel must not be advertised as a new job offer.");
                }
                string before=JsonUtility.ToJson(state.Snapshot());
                canvas.transform.Find("ContactsPanel/PRATI CILJ").GetComponent<Button>().onClick.Invoke();
                Assert.That(contacts.IsOpen,Is.False);
                Assert.That(phone.IsOpen,Is.True,"Follow must open the actual objective journal.");
                Assert.That(display.transform.Find("JobsPanel").gameObject.activeSelf,Is.True);
                Assert.That(JsonUtility.ToJson(state.Snapshot()),Is.EqualTo(before),"Navigation must not change progress or rewards.");
            }
            finally
            {
                if(contacts!=null)contacts.Close();
                if(lifestyle!=null)Set(lifestyle,"game",null);
                if(events!=null)UnityEngine.Object.DestroyImmediate(events);
                UnityEngine.Object.DestroyImmediate(root);Time.timeScale=scale;
            }
        }
        private static void Set(object target,string name,object value)=>
            target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);

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
