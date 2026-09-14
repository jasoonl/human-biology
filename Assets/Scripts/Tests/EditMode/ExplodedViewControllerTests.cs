using HumanBodyExplorer.Core;
using NUnit.Framework;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    public class ExplodedViewControllerTests
    {
        private GameObject _parent;
        private GameObject _child;

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject("Parent");
            _child = new GameObject("Child");
            _child.transform.SetParent(_parent.transform);
            _child.transform.localPosition = new Vector3(1, 0, 0);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_parent);
        }

        [Test]
        public void UpdateExplosion_ZeroSlider_KeepsOriginalPosition()
        {
            var controller = _parent.AddComponent<ExplodedViewController>();
            controller.CacheChildren();

            controller.UpdateExplosion(0f);

            Assert.AreEqual(new Vector3(1, 0, 0), _child.transform.localPosition);
        }

        [Test]
        public void UpdateExplosion_FullSlider_MovesOutward()
        {
            var controller = _parent.AddComponent<ExplodedViewController>();
            controller.CacheChildren();

            controller.UpdateExplosion(1f);

            float distanceFromOriginal = Vector3.Distance(_child.transform.localPosition, new Vector3(1, 0, 0));
            Assert.Greater(distanceFromOriginal, 0f);
        }

        [Test]
        public void CacheChildren_CountsAllDirectChildren()
        {
            var secondChild = new GameObject("Child2");
            secondChild.transform.SetParent(_parent.transform);

            var controller = _parent.AddComponent<ExplodedViewController>();
            controller.CacheChildren();

            Assert.AreEqual(2, controller.CachedChildCount);
            Object.DestroyImmediate(secondChild);
        }
    }
}
