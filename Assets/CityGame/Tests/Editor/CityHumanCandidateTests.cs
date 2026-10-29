using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityHumanCandidateTests
    {
        [Test]
        public void ReviewedHumanCandidateIsAvailableWithoutReplacingThePlayableSource()
        {
            GameObject candidate = Resources.Load<GameObject>("Models/RodjaHuman/RodjaHuman");
            Assert.That(candidate, Is.Not.Null, "A detailed human asset must exist before the preview can be delivered.");
            Assert.That(candidate, Is.Not.SameAs(CityCharacterModel.LoadSource()));
            Assert.That(CityCharacterModel.LoadSource().name, Does.Not.Contain("RodjaHuman"));
        }

        [Test]
        public void ImportedCandidateHasSkinningTexturesAndBoundedGeometry()
        {
            GameObject source = Resources.Load<GameObject>("Models/RodjaHuman/RodjaHuman");
            Assert.That(source, Is.Not.Null);
            Assert.That(source.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
            Assert.That(source.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(source.GetComponentsInChildren<Camera>(true), Is.Empty);
            Assert.That(source.GetComponentsInChildren<AudioSource>(true), Is.Empty);
            int triangles = 0;
            var skins = source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Assert.That(skins.Length, Is.GreaterThanOrEqualTo(5), "Body, clothing, eyes, hair and eyebrows must be real geometry.");
            foreach (var skin in skins)
            {
                Assert.That(skin.bones, Is.Not.Empty);
                Assert.That(skin.bones, Has.None.Null);
                Assert.That(skin.sharedMesh.boneWeights.Length, Is.EqualTo(skin.sharedMesh.vertexCount));
                triangles += skin.sharedMesh.triangles.Length / 3;
                foreach (Material material in skin.sharedMaterials)
                {
                    Assert.That(material, Is.Not.Null);
                    Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
                    Assert.That(material.GetTexture("_BaseMap"), Is.Not.Null);
                }
            }
            Assert.That(triangles, Is.InRange(10000, 80000));
        }

        [Test]
        public void PreviewNormalizesAtTheRemoteStageWithoutChangingSourceOrLeakingChildren()
        {
            var parent = new GameObject("TestPreviewStage");
            parent.transform.position = new Vector3(10000, -1000, 10000);
            var source = Resources.Load<GameObject>("Models/RodjaHuman/RodjaHuman");
            Vector3 scale = source.transform.localScale;
            try
            {
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    Assert.That(Prepare(source, parent.transform, out GameObject model), Is.True);
                    Assert.That(model.activeSelf, Is.False);
                    model.SetActive(true);
                    Bounds bounds = CityCharacterModel.Measure(model);
                    Assert.That(bounds.size.y, Is.EqualTo(1.8f).Within(.01f));
                    Assert.That(bounds.min.y, Is.EqualTo(parent.transform.position.y).Within(.01f));
                    foreach (var item in model.GetComponentsInChildren<Transform>(true)) Assert.That(item.gameObject.layer, Is.EqualTo(30));
                    Object.DestroyImmediate(model);
                    Assert.That(parent.transform.childCount, Is.Zero);
                }
                Assert.That(source.transform.localScale, Is.EqualTo(scale));
            }
            finally { Object.DestroyImmediate(parent); }
        }

        [Test]
        public void MissingOrInvalidArtLeavesThePreviewStageUntouched()
        {
            var parent = new GameObject("TestPreviewStage");
            var source = new GameObject("InvalidCandidate");
            try
            {
                Assert.That(Prepare(null, parent.transform, out _), Is.False);
                Assert.That(Prepare(source, parent.transform, out _), Is.False);
                source.AddComponent<BoxCollider>();
                Assert.That(Prepare(source, parent.transform, out _), Is.False);
                Assert.That(parent.transform.childCount, Is.Zero);
                Assert.That(source.GetComponent<BoxCollider>(), Is.Not.Null);
            }
            finally { Object.DestroyImmediate(source); Object.DestroyImmediate(parent); }
        }

        private static bool Prepare(GameObject source, Transform parent, out GameObject prepared)
        {
            var method = typeof(CityWardrobePanel).GetMethod("TryPrepareCandidate", BindingFlags.Static | BindingFlags.NonPublic);
            object[] args = { source, parent, null };
            bool result = (bool)method.Invoke(null, args);
            prepared = (GameObject)args[2];
            return result;
        }
    }
}
