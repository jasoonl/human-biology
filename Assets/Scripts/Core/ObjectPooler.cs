using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 59: zero-GC-allocation object pooling, keyed by an arbitrary string
    /// pool ID (typically the prefab name).
    /// </summary>
    public class ObjectPooler : MonoBehaviour
    {
        private readonly Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<GameObject, string> _instanceToPoolId = new Dictionary<GameObject, string>();

        public void InitializePool(string poolId, GameObject prefab, int count)
        {
            if (!_pools.TryGetValue(poolId, out var queue))
            {
                queue = new Queue<GameObject>();
                _pools[poolId] = queue;
            }

            for (int i = 0; i < count; i++)
            {
                var instance = Instantiate(prefab, transform);
                instance.SetActive(false);
                queue.Enqueue(instance);
                _instanceToPoolId[instance] = poolId;
            }
        }

        public GameObject SpawnFromPool(string poolId, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(poolId, out var queue) || queue.Count == 0)
            {
                Debug.LogWarning($"[ObjectPooler] Pool '{poolId}' is empty or does not exist.");
                return null;
            }

            var instance = queue.Dequeue();
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            return instance;
        }

        public void ReturnToPool(GameObject instance)
        {
            if (!_instanceToPoolId.TryGetValue(instance, out string poolId)) return;

            instance.SetActive(false);
            instance.transform.SetParent(transform);
            _pools[poolId].Enqueue(instance);
        }

        public int AvailableCount(string poolId) => _pools.TryGetValue(poolId, out var queue) ? queue.Count : 0;
    }
}
