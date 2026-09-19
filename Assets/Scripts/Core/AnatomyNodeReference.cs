using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Tags a GameObject (typically the one holding the MeshRenderer/Collider) with
    /// the AnatomyNode.EntityID it represents, so raycasts/selection can resolve
    /// back to the data dictionary. Also maintains a static registry so other
    /// code (e.g. quiz feedback flashing) can look up every GameObject sharing an
    /// EntityID (multiple meshes, like left/right kidney, can share one ID)
    /// without repeated scene-wide scans.
    /// </summary>
    public class AnatomyNodeReference : MonoBehaviour
    {
        [SerializeField] private string entityId;
        public string EntityId => entityId;

        private static readonly Dictionary<string, List<AnatomyNodeReference>> Registry =
            new Dictionary<string, List<AnatomyNodeReference>>();

        public void SetEntityId(string id) => entityId = id;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(entityId)) return;

            if (!Registry.TryGetValue(entityId, out var list))
            {
                list = new List<AnatomyNodeReference>();
                Registry[entityId] = list;
            }
            list.Add(this);
        }

        private void OnDisable()
        {
            if (string.IsNullOrEmpty(entityId)) return;
            if (Registry.TryGetValue(entityId, out var list)) list.Remove(this);
        }

        public static IReadOnlyList<AnatomyNodeReference> GetByEntityId(string id)
        {
            return Registry.TryGetValue(id, out var list) ? list : (IReadOnlyList<AnatomyNodeReference>)System.Array.Empty<AnatomyNodeReference>();
        }
    }
}
