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
