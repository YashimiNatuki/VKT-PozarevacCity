using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityPhoneScreenTests
    {
        private GameObject root;
        private CityPhonePanel phone;
        [TearDown] public void TearDown()
        {
            if(phone!=null)
            {
                var events=(GameObject)typeof(CityPhonePanel).GetField("ownedEventSystem",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(phone);
                if(events!=null)UnityEngine.Object.DestroyImmediate(events);
            }
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);
        }
        [TestCase(true)]
        [TestCase(false)]
        public void CatalogRequestFromMapHonorsUnlockAndKeepsExactlyOneDestination(bool unlocked)
        {
            // Keep the world bootstrap inactive: this fixture exercises real uGUI and progression,
            // without terrain startup, personal saves or a substitute phone implementation.
            root=new GameObject("PhoneScreenFixture");root.SetActive(false);
            var game=root.AddComponent<CityGameBootstrap>();
            var session=new CitySession(2500);var state=new CityProgression(session);
            Assert.That(state.CreateCharacter("Phone Test"),Is.True);
            if(unlocked)Assert.That(state.CompleteContract(1),Is.True);
            Set(game,"<Session>k__BackingField",session);Set(game,"<Progression>k__BackingField",state);
            var owner=root.AddComponent<CityProgressionController>();Set(owner,"game",game);
            var display=new GameObject("Phone",typeof(RectTransform));display.transform.SetParent(root.transform,false);
            phone=display.AddComponent<CityPhonePanel>();phone.Initialize(owner);
            phone.Open();phone.OpenMap();
            GameObject map=display.transform.Find("MapPanel").gameObject;
            GameObject catalog=display.transform.Find("CatalogPanel").gameObject;
            Assert.That(map.activeSelf,Is.True);Assert.That(phone.IsOpen,Is.True);
            string before=JsonUtility.ToJson(state.Snapshot());
            phone.OpenCatalog();
            Assert.That(catalog.activeSelf,Is.EqualTo(unlocked));
            Assert.That(map.activeSelf,Is.EqualTo(!unlocked),"Catalog and map must never cover each other.");
            Assert.That(display.transform.Find("PhonePanel").gameObject.activeSelf,Is.False);
            Assert.That(phone.IsOpen,Is.True);
            Assert.That(JsonUtility.ToJson(state.Snapshot()),Is.EqualTo(before),"A screen transition changed progression.");
            phone.Close();Assert.That(phone.IsOpen,Is.False);
        }
        private static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        [TestCase(0)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void StarterJournalShowsFiveRealGoalsAndNavigationDoesNotMutateProgress(int completedGoals)
        {
            CityProgression state=StarterJournalFixture(completedGoals, false);
            string before=JsonUtility.ToJson(state.Snapshot());
            phone.Open();
            phone.transform.Find("PhonePanel/StreetJob").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            var text=(UnityEngine.UI.Text)typeof(CityPhonePanel).GetField("jobDetails",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(phone);
            Assert.That(text.text,Does.Contain("MOJ PUT / "+completedGoals+"/5"));
            Assert.That(text.text,Does.Contain("Prva noć / Mila"));
            Assert.That(text.text,Does.Contain("Roba za blok / Kure"));
            Assert.That(text.text,Does.Contain("Noćni ekspres / Luka"));
            Assert.That(text.text,Does.Contain("Pronađi štek"));
            Assert.That(text.text,Does.Contain("Posadi i zalij"));
            Assert.That(text.supportRichText,Is.False);
            if(completedGoals==0)Assert.That(text.text,Does.Contain("prvog ugovora"));
            if(completedGoals==3)Assert.That(text.text,Does.Contain("vrata šteka"));
            if(completedGoals==4)
            {
                Assert.That(text.text,Does.Contain("kupi seme"));
                Assert.That(text.text,Does.Contain("ponovo E"));
            }
            if(completedGoals==5)Assert.That(text.text,Does.Contain("ne moraš da čekaš berbu"));
            var scroll=(UnityEngine.UI.ScrollRect)typeof(CityPhonePanel).GetField("jobScroll",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(phone);
            Assert.That(scroll.vertical,Is.True);
            Assert.That(scroll.content,Is.EqualTo(text.rectTransform));
            Assert.That(phone.transform.Find("JobsPanel").gameObject.activeSelf,Is.True);
            phone.transform.Find("JobsPanel/BackFromJobs").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(phone.transform.Find("PhonePanel").gameObject.activeSelf,Is.True);
            Assert.That(phone.transform.Find("JobsPanel").gameObject.activeSelf,Is.False);
            phone.Close();
            Assert.That(JsonUtility.ToJson(state.Snapshot()),Is.EqualTo(before),"Reading and navigating the guide must not complete goals or grant rewards.");
        }

        [Test]
        public void ActiveGardenOrderKeepsItsPhaseAndDeadlineAheadOfStarterGuide()
        {
            CityProgression state=StarterJournalFixture(3, true);
            string before=JsonUtility.ToJson(state.Snapshot());
            phone.Open();phone.OpenJobs();
            var text=(UnityEngine.UI.Text)typeof(CityPhonePanel).GetField("jobDetails",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(phone);
            int order=text.text.IndexOf("PORUDŽBINA IZ ŠTEKA",System.StringComparison.Ordinal);
            int deadline=text.text.IndexOf("Rok: 180 s igre",System.StringComparison.Ordinal);
            int guide=text.text.IndexOf("MOJ PUT / 3/5",System.StringComparison.Ordinal);
            Assert.That(order,Is.GreaterThanOrEqualTo(0));
            Assert.That(deadline,Is.GreaterThan(order));
            Assert.That(guide,Is.GreaterThan(deadline),"The current parcel and clock must stay ahead of optional starter guidance.");
            Assert.That(text.text,Does.Contain("PREDAJA"));
            Assert.That(JsonUtility.ToJson(state.Snapshot()),Is.EqualTo(before));
        }

        [Test]
        public void ActiveStreetJobKeepsItsObjectiveAndDeadlineAheadOfStarterGuide()
        {
            CityProgression state=StarterJournalFixture(3, false);
            CityStreetJobs jobs=root.GetComponent<CityMissionDirector>().Jobs;
            Assert.That(jobs.TryAccept(),Is.True);
            string before=JsonUtility.ToJson(state.Snapshot());
            phone.Open();phone.OpenJobs();
            var text=(UnityEngine.UI.Text)typeof(CityPhonePanel).GetField("jobDetails",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(phone);
            int objective=text.text.IndexOf(jobs.Offer.ObjectiveTitle(0),System.StringComparison.Ordinal);
            string remaining="Aktivno / preostalo "+System.Math.Ceiling(jobs.RemainingSeconds)+" sekundi igre";
            int deadline=text.text.IndexOf(remaining,System.StringComparison.Ordinal);
            int guide=text.text.IndexOf("MOJ PUT / 3/5",System.StringComparison.Ordinal);
            Assert.That(objective,Is.GreaterThanOrEqualTo(0));
            Assert.That(deadline,Is.GreaterThan(objective));
            Assert.That(guide,Is.GreaterThan(deadline),"The current street job must not be buried under completed tutorial guidance.");
            Assert.That(JsonUtility.ToJson(state.Snapshot()),Is.EqualTo(before));
        }

        private CityProgression StarterJournalFixture(int completedGoals,bool activeOrder)
        {
            root=new GameObject("StarterJournalFixture");root.SetActive(false);
            var game=root.AddComponent<CityGameBootstrap>();var wallet=new CitySession(5000);
            var state=new CityProgression(wallet);state.CreateCharacter("Starter Guide");
            int contracts=System.Math.Min(3,completedGoals);
            for(int i=1;i<=contracts;i++)Assert.That(state.CompleteContract(i),Is.True);
            if(completedGoals>=4)Assert.That(state.RecordHideoutVisit(),Is.True);
            if(completedGoals>=5)
            {
                Assert.That(state.Garden.BuySeed(0),Is.True);
                Assert.That(state.Garden.PlantFromInventory(0,0,1000),Is.True);
                Assert.That(state.WaterGardenPlot(0,1000),Is.True);
            }
            if(activeOrder)
            {
                var saved=state.Snapshot();
                saved.gardenOrder=new CityGardenOrderData{phase=CityGardenOrderPhase.Deliver,variety=0,quantity=2,
                    cashReward=CityGardenOrder.Quote(0),remainingSeconds=180};
                state=new CityProgression(wallet,saved);
            }
            var director=root.AddComponent<CityMissionDirector>();
            CityMissionContract[] catalog=CityMissionDirector.CreateContracts();
            int index=System.Math.Min(contracts,catalog.Length-1);
            Set(director,"contracts",catalog);Set(director,"contractIndex",index);
            Set(director,"run",contracts==3?CityMissionRun.RestoreCompleted(catalog[index],wallet):new CityMissionRun(catalog[index],wallet));
            Set(director,"<Session>k__BackingField",wallet);
            var jobs=new CityStreetJobs(wallet);state.BindStreetJobs(jobs);Set(director,"<Jobs>k__BackingField",jobs);
            Set(game,"<Session>k__BackingField",wallet);Set(game,"<Progression>k__BackingField",state);
            Set(game,"<Director>k__BackingField",director);
            var owner=root.AddComponent<CityProgressionController>();Set(owner,"game",game);Set(owner,"director",director);
            var display=new GameObject("Phone",typeof(RectTransform));display.transform.SetParent(root.transform,false);
            phone=display.AddComponent<CityPhonePanel>();phone.Initialize(owner);Set(owner,"panel",phone);
            return state;
        }
        [Test] public void RealNavigationButtonsPublishExactlyOnePresentationTapAndPreserveProfile()
        {
            root=new GameObject("PhoneInteractionFixture");root.SetActive(false);
            var game=root.AddComponent<CityGameBootstrap>();var session=new CitySession(2500);var state=new CityProgression(session);
            Assert.That(state.CreateCharacter("Phone Tap Test"),Is.True);
            Set(game,"<Session>k__BackingField",session);Set(game,"<Progression>k__BackingField",state);
            var owner=root.AddComponent<CityProgressionController>();Set(owner,"game",game);
            var display=new GameObject("Phone",typeof(RectTransform));display.transform.SetParent(root.transform,false);
            phone=display.AddComponent<CityPhonePanel>();phone.Initialize(owner);phone.Open();
            PropertyInfo signal=typeof(CityPhonePanel).GetProperty("InteractionVersion");
            Assert.That(signal,Is.Not.Null,"Real phone buttons must publish their own presentation signal.");
            int before=(int)signal.GetValue(phone);string profile=JsonUtility.ToJson(state.Snapshot());
            display.transform.Find("PhonePanel/Map").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That((int)signal.GetValue(phone),Is.EqualTo(unchecked(before+1)));
            Assert.That(display.transform.Find("MapPanel").gameObject.activeSelf,Is.True);
            display.transform.Find("MapPanel/BackFromMap").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That((int)signal.GetValue(phone),Is.EqualTo(unchecked(before+2)));
            Assert.That(display.transform.Find("PhonePanel").gameObject.activeSelf,Is.True);
            Assert.That(JsonUtility.ToJson(state.Snapshot()),Is.EqualTo(profile));
        }
        [Test] public void DestroyingPhoneBeforeItsOwnerAndRigLeavesTeardownSafe()
        {
            root=new GameObject("PhoneTeardownFixture");root.SetActive(false);
            var game=root.AddComponent<CityGameBootstrap>();var session=new CitySession(2500);var state=new CityProgression(session);
            Assert.That(state.CreateCharacter("Phone Teardown"),Is.True);
            Set(game,"<Session>k__BackingField",session);Set(game,"<Progression>k__BackingField",state);
            var owner=root.AddComponent<CityProgressionController>();Set(owner,"game",game);
            var display=new GameObject("ActivePhoneCanvas",typeof(RectTransform));
            var rigObject=new GameObject("PhoneTeardownRig");GameObject events=null;
            try
            {
                phone=display.AddComponent<CityPhonePanel>();phone.Initialize(owner);Set(owner,"panel",phone);
                events=(GameObject)typeof(CityPhonePanel).GetField("ownedEventSystem",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(phone);
                display.GetComponent<Canvas>().scaleFactor=2;phone.SetWorldPreview(false);
                var rig=rigObject.AddComponent<CityCameraRig>();Set(rig,"phone",owner);
                UnityEngine.Object.DestroyImmediate(display);display=null;
                Assert.DoesNotThrow(()=>owner.SetPhoneWorldPreview(false));
                Assert.DoesNotThrow(()=>UnityEngine.Object.DestroyImmediate(rigObject));rigObject=null;
            }
            finally
            {
                if(events!=null)UnityEngine.Object.DestroyImmediate(events);
                if(display!=null)UnityEngine.Object.DestroyImmediate(display);
                if(rigObject!=null)UnityEngine.Object.DestroyImmediate(rigObject);
            }
        }
    }
}
