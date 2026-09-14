using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 29: instantiates a URP Decal Projector at a raycast hit point (e.g. a
    /// virtual scalpel's tip) and aligns its forward vector to the surface normal
    /// so an "incision" decal texture wraps correctly over curved organic geometry.
    /// Requires the URP Renderer's Decal Renderer Feature to actually composite the
    /// decal on screen - that Renderer Feature asset isn't wired up in this
    /// environment (no incision texture asset exists to preview), so this manager
    /// is compile-verified but not visually verified.
    /// </summary>
    public class DecalProjectorManager : MonoBehaviour
    {
        [SerializeField] private GameObject decalProjectorPrefab;
        [SerializeField] private Vector3 decalSize = new Vector3(0.1f, 0.1f, 0.05f);

        public DecalProjector SpawnIncisionDecal(RaycastHit hit)
        {
            GameObject instance = decalProjectorPrefab != null
                ? Instantiate(decalProjectorPrefab)
                : new GameObject("IncisionDecal", typeof(DecalProjector));

            instance.transform.position = hit.point;
            instance.transform.rotation = Quaternion.LookRotation(-hit.normal);

            var projector = instance.GetComponent<DecalProjector>();
            if (projector == null) projector = instance.AddComponent<DecalProjector>();

            projector.size = decalSize;
            return projector;
        }
    }
}
