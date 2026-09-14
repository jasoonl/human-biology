using UnityEngine;

namespace HumanBodyExplorer.MicroSim
{
    /// <summary>
    /// Phase 47: bursts fast-moving glowing particles along a nerve mesh's bounds,
    /// synchronized to a conduction-velocity parameter so myelinated (fast) vs
    /// unmyelinated (slow) propagation is visually distinct.
    /// </summary>
    public class ActionPotentialVisualizer : MonoBehaviour
    {
        [SerializeField] private ParticleSystem impulseParticles;
        [SerializeField] private Renderer nerveRenderer;
        [SerializeField] private float myelinatedConductionVelocity = 100f; // m/s (roughly)
        [SerializeField] private float unmyelinatedConductionVelocity = 1f;

        public bool IsMyelinated { get; set; } = true;

        public float CurrentConductionVelocity => IsMyelinated ? myelinatedConductionVelocity : unmyelinatedConductionVelocity;

        public void FireImpulse()
        {
            if (impulseParticles == null || nerveRenderer == null) return;

            Bounds bounds = nerveRenderer.bounds;
            impulseParticles.transform.position = bounds.min;

            var main = impulseParticles.main;
            main.startSpeed = CurrentConductionVelocity;

            impulseParticles.Play();
        }
    }
}
