using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Tags a GameObject (typically the one holding the MeshRenderer/Collider) with
    /// the AnatomyNode.EntityID it represents, so raycasts/selection can resolve
    /// back to the data dictionary.
    /// </summary>
    public class AnatomyNodeReference : MonoBehaviour
    {
        [SerializeField] private string entityId;
        public string EntityId => entityId;

        public void SetEntityId(string id) => entityId = id;
    }
}
