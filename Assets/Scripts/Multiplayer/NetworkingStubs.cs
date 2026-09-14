using System;
using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.Multiplayer
{
    // Module VI (Phases 63-70) - STUB ONLY. See Planning.md M6.
    //
    // None of these classes derive from Unity Netcode for GameObjects'
    // NetworkBehaviour, because com.unity.netcode.gameobjects (and Unity
    // Relay/Vivox/Photon Voice) are not installed in this project. Adding them
    // now would repeat the same package-version-compatibility risk seen with
    // Input System/Addressables/Splines in Module I-III, for code with no way to
    // be tested end-to-end anyway (no relay account, no second client machine).
    // These classes document the intended call shape from the spec so a future
    // session can wire in the real NetworkBehaviour base classes and RPC
    // attributes at these exact call sites without redesigning the API.

    /// <summary>Phase 63: intended to be a NetworkBehaviour with a NetworkVariable&lt;string&gt; FocusedAnatomyID.</summary>
    public class ClassroomNetworkManager : MonoBehaviour
    {
        public event Action<string> OnFocusedAnatomyChanged;
        private string _focusedAnatomyId;

        /// <summary>Stand-in for a [ServerRpc] - locally invokes the same effect with no network hop.</summary>
        public void SetFocusServerRpc(string entityId)
        {
            _focusedAnatomyId = entityId;
            OnFocusedAnatomyChanged?.Invoke(entityId);
        }
    }

    /// <summary>Phase 64: intended to smooth 30Hz network position ticks to 120Hz render via Catmull-Rom.</summary>
    public class NetworkTransformInterpolator
    {
        private readonly Queue<Vector3> _positionRingBuffer = new Queue<Vector3>();
        private const int BufferSize = 4;

        public void PushNetworkPosition(Vector3 position)
        {
            _positionRingBuffer.Enqueue(position);
            while (_positionRingBuffer.Count > BufferSize) _positionRingBuffer.Dequeue();
        }

        /// <summary>Catmull-Rom through the last 4 received points; falls back to the latest point until the buffer fills.</summary>
        public Vector3 GetSmoothedPosition(float t)
        {
            if (_positionRingBuffer.Count < 4)
            {
                return _positionRingBuffer.Count > 0 ? new List<Vector3>(_positionRingBuffer)[^1] : Vector3.zero;
            }

            var points = new List<Vector3>(_positionRingBuffer);
            return CatmullRom(points[0], points[1], points[2], points[3], t);
        }

        public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }
    }

    /// <summary>Phase 65: intended [ServerRpc] batching up to 20 points per call.</summary>
    public class NetworkedAnnotationTool : MonoBehaviour
    {
        private const int MaxBatchSize = 20;

        public void DrawLineSegmentServerRpc(Vector3[] points)
        {
            if (points.Length > MaxBatchSize)
            {
                Debug.LogWarning($"[NetworkedAnnotationTool] Batch of {points.Length} exceeds the {MaxBatchSize}-point limit.");
            }
        }
    }

    /// <summary>Phase 66: intended [ClientRpc]/[ServerRpc] pair for a host-launched, student-answered quiz.</summary>
    public class DistributedQuizManager : MonoBehaviour
    {
        private readonly Dictionary<ulong, bool> _responses = new Dictionary<ulong, bool>();

        public void SubmitAnswerServerRpc(ulong clientId, bool isCorrect)
        {
            _responses[clientId] = isCorrect;
        }

        public int CorrectCount()
        {
            int count = 0;
            foreach (var correct in _responses.Values) if (correct) count++;
            return count;
        }
    }

    /// <summary>Phase 67: intended integration point for Photon Voice/Vivox spatial audio.</summary>
    public class VoiceChatManager : MonoBehaviour
    {
        public bool IsVoiceProviderConnected { get; private set; }

        public void Connect()
        {
            Debug.LogWarning("[VoiceChatManager] No VoIP provider (Photon Voice/Vivox) is installed in this project.");
        }
    }

    /// <summary>Phase 68: intended role gate keyed off NetworkManager.IsServer.</summary>
    public class NetworkAuthorityManager
    {
        public bool IsServer { get; set; }

        public bool CanControlSlicer() => IsServer;
        public bool CanControlDiseaseProgression() => IsServer;

        public void RequestControl(Action<bool> onGranted)
        {
            onGranted?.Invoke(IsServer);
        }
    }

    /// <summary>Phase 69: intended late-join state sync payload shape.</summary>
    [Serializable]
    public struct GameStateSnapshot
    {
        public Vector3 SlicePlanePosition;
        public Vector3 SlicePlaneNormal;
        public float DiseaseSeverity;
        public float ExplodedSliderValue;
    }

    public class GameStateSynchronizer
    {
        public byte[] Serialize(GameStateSnapshot snapshot)
        {
            return JsonUtility.ToJson(snapshot).ToCharArray().Length > 0
                ? System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(snapshot))
                : Array.Empty<byte>();
        }

        public GameStateSnapshot Deserialize(byte[] payload)
        {
            return JsonUtility.FromJson<GameStateSnapshot>(System.Text.Encoding.UTF8.GetString(payload));
        }
    }

    /// <summary>Phase 70: intended Unity Relay join-code flow. No Relay account exists in this environment.</summary>
    public class RelayConnectionManager
    {
        public string GenerateJoinCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new System.Random();
            var codeChars = new char[6];
            for (int i = 0; i < 6; i++) codeChars[i] = chars[random.Next(chars.Length)];
            return new string(codeChars);
        }
    }
}
