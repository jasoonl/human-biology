using System.Collections.Generic;
using HumanBodyExplorer.Core;
using NUnit.Framework;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    public class BVHManagerTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        private GameObject SpawnCube(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = position;
            _spawned.Add(go);
            return go;
        }

        [Test]
        public void Build_ThenQuery_FindsIntersectedLeaf()
        {
            var near = SpawnCube(new Vector3(0, 0, 5));
            var far = SpawnCube(new Vector3(0, 0, 50));
            var offAxis = SpawnCube(new Vector3(20, 0, 5));

            var bvh = new BVHManager();
            bvh.Build(new List<GameObject> { near, far, offAxis });

            Assert.AreEqual(3, bvh.LeafCount());

            var ray = new Ray(new Vector3(0, 0, -10), Vector3.forward);
            var results = bvh.Query(ray, 100f);

            CollectionAssert.Contains(results, near);
            CollectionAssert.Contains(results, far);
            CollectionAssert.DoesNotContain(results, offAxis);
        }

        [Test]
        public void Build_EmptyList_QueryReturnsNoResults()
        {
            var bvh = new BVHManager();
            bvh.Build(new List<GameObject>());

            Assert.AreEqual(0, bvh.LeafCount());

            var results = bvh.Query(new Ray(Vector3.zero, Vector3.forward), 100f);
            Assert.AreEqual(0, results.Count);
        }
    }
}
