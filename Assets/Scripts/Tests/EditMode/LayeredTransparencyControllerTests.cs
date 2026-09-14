using HumanBodyExplorer.Core;
using NUnit.Framework;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    public class LayeredTransparencyControllerTests
    {
        private GameObject _controllerGO;
        private GameObject _rendererGO;
        private Material _material;

        [SetUp]
        public void SetUp()
        {
            _controllerGO = new GameObject("Controller");
            _rendererGO = GameObject.CreatePrimitive(PrimitiveType.Cube);

            _material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _rendererGO.GetComponent<Renderer>().sharedMaterial = _material;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_controllerGO);
            Object.DestroyImmediate(_rendererGO);
            Object.DestroyImmediate(_material);
        }

        [Test]
        public void SetSystemAlpha_BelowOne_AssignsSkeletonQueue()
        {
            var controller = _controllerGO.AddComponent<LayeredTransparencyController>();
            controller.RegisterRenderer(AnatomySystemLayer.Skeleton, _rendererGO.GetComponent<Renderer>());

            controller.SetSystemAlpha(AnatomySystemLayer.Skeleton, 0.5f);

            Assert.AreEqual(3000, _material.renderQueue);
        }

        [Test]
        public void SetSystemAlpha_SkinLayer_HasHigherQueueThanSkeleton()
        {
            var skinMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var skinGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            skinGO.GetComponent<Renderer>().sharedMaterial = skinMaterial;

            var controller = _controllerGO.AddComponent<LayeredTransparencyController>();
            controller.RegisterRenderer(AnatomySystemLayer.Skeleton, _rendererGO.GetComponent<Renderer>());
            controller.RegisterRenderer(AnatomySystemLayer.Skin, skinGO.GetComponent<Renderer>());

            controller.SetSystemAlpha(AnatomySystemLayer.Skeleton, 0.5f);
            controller.SetSystemAlpha(AnatomySystemLayer.Skin, 0.5f);

            Assert.Greater(skinMaterial.renderQueue, _material.renderQueue);

            Object.DestroyImmediate(skinGO);
            Object.DestroyImmediate(skinMaterial);
        }

        [Test]
        public void SetSystemAlpha_FullOpacity_RestoresOpaqueQueue()
        {
            var controller = _controllerGO.AddComponent<LayeredTransparencyController>();
            controller.RegisterRenderer(AnatomySystemLayer.Muscles, _rendererGO.GetComponent<Renderer>());

            controller.SetSystemAlpha(AnatomySystemLayer.Muscles, 0.5f);
            controller.SetSystemAlpha(AnatomySystemLayer.Muscles, 1f);

            Assert.AreEqual(2000, _material.renderQueue);
        }
    }
}
