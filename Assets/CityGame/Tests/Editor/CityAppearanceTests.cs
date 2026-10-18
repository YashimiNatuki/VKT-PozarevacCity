using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityAppearanceTests
    {
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void OldProfileKeepsBalancesAndGetsDefaultClothes(int version)
        {
            var data=new CityProfileData{version=version,callsign="Yashimi",cash=340};
            var progression=new CityProgression(CitySession.Restore(data),data);
            Assert.That(progression.Appearance.Outfit,Is.EqualTo(CityOutfit.Korzo));
            Assert.That(progression.Session.Cash,Is.EqualTo(340));
            Assert.That(progression.Snapshot().version,Is.EqualTo(CityProgression.CurrentProfileVersion));
        }
        [Test] public void ChoiceNotifiesOnceAndSurvivesIndependentSnapshots()
        {
            var data=new CityProfileData{version=5,callsign="Yashimi",cash=340};
            var p=new CityProgression(CitySession.Restore(data),data);int events=0;p.Appearance.Changed+=()=>events++;
            Assert.That(p.Appearance.Select(CityOutfit.Busije),Is.True);
            Assert.That(p.Appearance.Select(CityOutfit.Busije),Is.False);
            var saved=p.Snapshot();saved.appearance.outfit=1;
            Assert.That(p.Appearance.Outfit,Is.EqualTo(CityOutfit.Busije));
            var restored=new CityProgression(CitySession.Restore(saved),saved);
            Assert.That(restored.Appearance.Outfit,Is.EqualTo(CityOutfit.Midnight));Assert.That(events,Is.EqualTo(1));
            Assert.Throws<ArgumentException>(()=>CityAppearance.Validate(new CityAppearanceData{outfit=3}));
            Assert.Throws<ArgumentOutOfRangeException>(()=>p.Appearance.Select((CityOutfit)(-1)));
        }
        [Test] public void AdultBuildRetainsHeightAndRepeatedApplicationDoesNotAccumulate()
        {
            var root=new GameObject("AdultBuild");
            CityCharacterPhysique body=null;
            try
            {
                var model=UnityEngine.Object.Instantiate(CityCharacterModel.LoadSource(),root.transform);
                body=new CityCharacterPhysique(model);Assert.That(body.HasFullRig,Is.True);
                AnimationClip clip=null;
                foreach(var c in Resources.LoadAll<AnimationClip>("Models/KenneyRetro/idle"))if(!c.name.StartsWith("__preview__")){clip=c;break;}
                Assert.That(CityCharacterModel.Normalize(root.transform,model,clip,body),Is.True);
                float height=CityCharacterModel.Measure(model).size.y;
                Assert.That(height,Is.EqualTo(1.8f).Within(.015f));
                Mesh mesh=model.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
                Vector3[] untrained=mesh.vertices;
                body.Apply(5);Bounds trained=CityCharacterModel.Measure(model);
                Assert.That(mesh.vertices,Is.Not.EqualTo(untrained),"Training must change the actual skin.");
                for(int i=0;i<20;i++)body.Apply(5);
                Assert.That(CityCharacterModel.Measure(model).size,Is.EqualTo(trained.size));
                Assert.That(trained.size.y,Is.InRange(1.7f,1.9f));
                body.Apply(0);Assert.That(mesh.vertices,Is.EqualTo(untrained),"Rebuilding must start from original vertices.");
            }
            finally{body?.Dispose();UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void CharacterShaderImportsWithoutCompilationErrors()
        {
            var shader=Resources.Load<Shader>("StreetCharacter");Assert.That(shader,Is.Not.Null);
            foreach(var message in ShaderUtil.GetShaderMessages(shader))Assert.That(message.severity,Is.Not.EqualTo(ShaderCompilerMessageSeverity.Error),message.message);
        }
    }
}
