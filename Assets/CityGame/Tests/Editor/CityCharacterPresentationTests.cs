using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityCharacterPresentationTests
    {
        [Test] public void BlinkClosesAndReopensWithoutInvalidShaderValues()
        {
            Assert.That(CityFaceExpression.Blink(.065f), Is.EqualTo(1).Within(.001));
            Assert.That(CityFaceExpression.Blink(.3f), Is.Zero);
            Assert.That(CityFaceExpression.Blink(float.NaN), Is.Zero);
            Assert.That(CityFaceExpression.Blink(float.PositiveInfinity), Is.Zero);
            for(int i=0;i<10000;i++)Assert.That(CityFaceExpression.Blink(i*.017f),Is.InRange(0,1));
        }
        [Test] public void StrengthAndRepeatedAppearanceChangesPreserveJointLengthsAndHeadScale()
        {
            GameObject root=new GameObject("ProportionTest");CityCharacterPhysique body=null;
            try
            {
                GameObject model=Object.Instantiate(CityCharacterModel.LoadSource(),root.transform);
                body=new CityCharacterPhysique(model);
                AnimationClip pose=System.Array.Find(Resources.LoadAll<AnimationClip>("Models/KenneyRetro/idle"),c=>!c.name.StartsWith("__preview__"));
                Assert.IsTrue(CityCharacterModel.Normalize(root.transform,model,pose,body));
                Transform arm=System.Array.Find(model.GetComponentsInChildren<Transform>(),t=>t.name=="LeftArm");
                Transform elbow=System.Array.Find(model.GetComponentsInChildren<Transform>(),t=>t.name=="LeftForeArm");
                float length=Vector3.Distance(arm.position,elbow.position);
                Bounds neutral=CityCharacterModel.Measure(model);Vector3 headScale=body.Head.localScale;
                for(int i=0;i<20;i++){body.Apply(5);body.Apply(0);}
                Assert.That(Vector3.Distance(arm.position,elbow.position),Is.EqualTo(length).Within(.0001));
                Assert.That(body.Head.localScale,Is.EqualTo(headScale));
                Assert.That(CityCharacterModel.Measure(model).size.y,Is.EqualTo(1.8f).Within(.015));
                Assert.That(CityCharacterModel.Measure(model).size,Is.EqualTo(neutral.size));
            }
            finally{body?.Dispose();Object.DestroyImmediate(root);}
        }
        [Test] public void FaceDisableResetsOnlyItsOwnedMaterial()
        {
            GameObject root=new GameObject("FaceTest");Material material=new Material(Resources.Load<Shader>("StreetCharacter"));
            try
            {
                Assert.IsTrue(material.HasProperty("_Face"));
                CityFaceExpression face=root.AddComponent<CityFaceExpression>();face.Initialize(material);
                material.SetVector("_Face",Vector4.one);
                // EditMode does not dispatch runtime MonoBehaviour lifecycle callbacks.
                typeof(CityFaceExpression).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(face,null);
                Assert.That(material.GetVector("_Face"),Is.EqualTo(Vector4.zero));
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(material);}
        }
        [Test] public void HudCardsDoNotOverlapAndEmptyNotificationsStayHidden()
        {
            GameObject root=new GameObject("HudLayoutTest");
            try
            {
                CityHud hud=root.AddComponent<CityHud>();
                typeof(CityHud).GetMethod("BuildCanvas",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hud,null);
                var profile=(RectTransform)root.transform.Find("ProfileCard");
                var mission=(RectTransform)root.transform.Find("MissionCard");
                Assert.That(-profile.anchoredPosition.y+profile.sizeDelta.y,Is.LessThan(-mission.anchoredPosition.y));
                hud.ShowNotification("",2);Assert.IsFalse(root.transform.Find("NotificationCard").gameObject.activeSelf);
                hud.SetPrompt("Test");Assert.IsTrue(root.transform.Find("PromptCard").gameObject.activeSelf);
                hud.SetTrainingPresentation(true);Assert.IsFalse(root.transform.Find("PromptCard").gameObject.activeSelf);
                Assert.IsFalse(root.transform.Find("MissionCard").gameObject.activeSelf);
                hud.SetTrainingPresentation(false);Assert.IsTrue(root.transform.Find("PromptCard").gameObject.activeSelf);
                hud.SetPrompt("");Assert.IsFalse(root.transform.Find("PromptCard").gameObject.activeSelf);
                foreach(Graphic graphic in root.GetComponentsInChildren<Graphic>(true))Assert.IsFalse(graphic.raycastTarget,"HUD must not consume gameplay touches.");
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
