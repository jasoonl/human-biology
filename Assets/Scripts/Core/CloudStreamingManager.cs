using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// WebGL/remote-catalog manifest check. Points at a configurable remote host so it
    /// can be pointed at a real CDN/S3 bucket later — no such bucket is provisioned in
    /// this environment, so this path is untested against a live endpoint.
    /// </summary>
    public class CloudStreamingManager : MonoBehaviour
    {
        [SerializeField] private string remoteManifestUrl = "https://example-cdn.invalid/catalog/manifest_hash.txt";
        [SerializeField] private string cachedManifestHashKey = "AnatomyManifestHash";

        public event Action<string> OnNewVersionAvailable;

        public IEnumerator CheckForManifestUpdate()
        {
            using var request = UnityWebRequest.Get(remoteManifestUrl);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[CloudStreamingManager] Manifest check failed: {request.error}");
                yield break;
            }

            string remoteHash = request.downloadHandler.text.Trim();
            string cachedHash = PlayerPrefs.GetString(cachedManifestHashKey, string.Empty);

            if (!string.IsNullOrEmpty(remoteHash) && remoteHash != cachedHash)
            {
                Debug.Log($"[CloudStreamingManager] New asset manifest detected: {remoteHash}");
                OnNewVersionAvailable?.Invoke(remoteHash);
            }
        }

        public void AcceptManifestVersion(string hash)
        {
            PlayerPrefs.SetString(cachedManifestHashKey, hash);
            PlayerPrefs.Save();
        }
    }
}
