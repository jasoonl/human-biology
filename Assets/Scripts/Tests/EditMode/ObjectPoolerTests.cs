using HumanBodyExplorer.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HumanBodyExplorer.Tests
{
    public class ObjectPoolerTests
    {
        private GameObject _poolerGO;
        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            _poolerGO = new GameObject("Pooler");
            _prefab = new GameObject("Prefab");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_poolerGO);
            Object.DestroyImmediate(_prefab);
        }

        [Test]
        public void InitializePool_ThenSpawn_ReducesAvailableCount()
        {
            var pooler = _poolerGO.AddComponent<ObjectPooler>();
            pooler.InitializePool("test", _prefab, 3);

            Assert.AreEqual(3, pooler.AvailableCount("test"));

            var spawned = pooler.SpawnFromPool("test", Vector3.zero, Quaternion.identity);

            Assert.IsNotNull(spawned);
            Assert.IsTrue(spawned.activeSelf);
            Assert.AreEqual(2, pooler.AvailableCount("test"));
        }

        [Test]
        public void ReturnToPool_MakesInstanceAvailableAgain()
        {
            var pooler = _poolerGO.AddComponent<ObjectPooler>();
            pooler.InitializePool("test", _prefab, 1);

            var spawned = pooler.SpawnFromPool("test", Vector3.zero, Quaternion.identity);
            pooler.ReturnToPool(spawned);

            Assert.IsFalse(spawned.activeSelf);
            Assert.AreEqual(1, pooler.AvailableCount("test"));
        }

        [Test]
        public void SpawnFromPool_EmptyPool_ReturnsNull()
        {
            var pooler = _poolerGO.AddComponent<ObjectPooler>();
            pooler.InitializePool("test", _prefab, 0);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*"));
            var spawned = pooler.SpawnFromPool("test", Vector3.zero, Quaternion.identity);

            Assert.IsNull(spawned);
        }
    }
}
