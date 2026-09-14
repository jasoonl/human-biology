using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.UI
{
    /// <summary>
    /// Phase 54: randomizes bone positions at the start of Assembly Mode, then
    /// snaps a dragged piece into place once it's close enough (both position and
    /// rotation) to its correct transform.
    /// </summary>
    public class AssemblyModeController : MonoBehaviour
    {
        [SerializeField] private List<Transform> assemblyPieces = new List<Transform>();
        [SerializeField] private float scatterRadius = 2f;
        [SerializeField] private float snapPositionThreshold = 0.1f;
        [SerializeField] private float snapAngleThreshold = 15f;

        private readonly Dictionary<Transform, (Vector3 pos, Quaternion rot)> _correctTransforms
            = new Dictionary<Transform, (Vector3, Quaternion)>();
        private readonly HashSet<Transform> _locked = new HashSet<Transform>();

        public bool IsComplete => _locked.Count == assemblyPieces.Count && assemblyPieces.Count > 0;

        public void BeginAssembly()
        {
            _correctTransforms.Clear();
            _locked.Clear();

            foreach (var piece in assemblyPieces)
            {
                if (piece == null) continue;
                _correctTransforms[piece] = (piece.position, piece.rotation);
                piece.position += UnityEngine.Random.insideUnitSphere * scatterRadius;
                piece.rotation = UnityEngine.Random.rotation;
            }
        }

        /// <summary>Call while dragging a piece; snaps and locks it if within threshold.</summary>
        public bool TrySnap(Transform piece)
        {
            if (_locked.Contains(piece) || !_correctTransforms.TryGetValue(piece, out var correct)) return false;

            if (IsWithinSnapThreshold(piece.position, piece.rotation, correct.pos, correct.rot,
                    snapPositionThreshold, snapAngleThreshold))
            {
                piece.position = correct.pos;
                piece.rotation = correct.rot;
                _locked.Add(piece);
                return true;
            }

            return false;
        }

        /// <summary>Exposed as a pure function so the snap condition is unit-testable.</summary>
        public static bool IsWithinSnapThreshold(Vector3 currentPos, Quaternion currentRot,
            Vector3 targetPos, Quaternion targetRot, float positionThreshold, float angleThreshold)
        {
            return Vector3.Distance(currentPos, targetPos) < positionThreshold &&
                   Quaternion.Angle(currentRot, targetRot) < angleThreshold;
        }
    }
}
