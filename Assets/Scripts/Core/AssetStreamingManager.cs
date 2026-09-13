using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HumanBodyExplorer.Core
{
    public interface IAssetStreamingManager
    {
        Task<GameObject> LoadAnatomyGroup(string key);
        void UnloadAnatomyGroup(string key);
    }

    /// <summary>
    /// Streams anatomy system groups via Addressables, caching handles so they can be
    /// released and the underlying memory reclaimed when a system is hidden.
    /// </summary>
    public class AssetStreamingManager : IAssetStreamingManager
    {
        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _handles =
            new Dictionary<string, AsyncOperationHandle<GameObject>>();

        public async Task<GameObject> LoadAnatomyGroup(string key)
        {
            if (_handles.TryGetValue(key, out var existingHandle))
            {
                if (existingHandle.IsValid() && existingHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    return existingHandle.Result;
                }
            }

            var handle = Addressables.InstantiateAsync(key);
            _handles[key] = handle;

            GameObject result = await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AssetStreamingManager] Failed to load Addressable group '{key}'");
                _handles.Remove(key);
                return null;
            }

            return result;
        }

        public void UnloadAnatomyGroup(string key)
        {
            if (_handles.TryGetValue(key, out var handle))
            {
                if (handle.IsValid())
                {
                    Addressables.ReleaseInstance(handle);
                }
                _handles.Remove(key);
            }

            Resources.UnloadUnusedAssets();
        }
    }
}
