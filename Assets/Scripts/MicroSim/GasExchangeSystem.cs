using UnityEngine;

namespace HumanBodyExplorer.MicroSim
{
    /// <summary>
    /// Phase 45 (deviates from spec): the spec asks for Unity VFX Graph. The
    /// com.unity.visualeffectgraph package isn't installed and hand-authoring a
    /// .vfx graph asset's serialized format blind isn't reliable, so this uses two
    /// standard ParticleSystems instead - functionally equivalent (blue CO2
    /// particles emit from the vessel toward the alveoli cavity, red O2 particles
    /// emit the opposite direction) at a smaller particle-count ceiling than VFX
    /// Graph's GPU-driven systems would support.
    /// </summary>
    public class GasExchangeSystem : MonoBehaviour
    {
        [SerializeField] private ParticleSystem co2Emitter;
        [SerializeField] private ParticleSystem o2Emitter;
        [SerializeField] private Transform vesselSurface;
        [SerializeField] private Transform alveolarCavity;
        [SerializeField] private float partialPressureGradient = 1f;

        public void SetGradient(float partialPressureDelta)
        {
            partialPressureGradient = partialPressureDelta;

            if (co2Emitter != null)
            {
                var emission = co2Emitter.emission;
                emission.rateOverTime = Mathf.Max(0f, partialPressureGradient) * 10f;
            }

            if (o2Emitter != null)
            {
                var emission = o2Emitter.emission;
                emission.rateOverTime = Mathf.Max(0f, -partialPressureGradient) * 10f;
            }
        }

        private void Update()
        {
            if (vesselSurface == null || alveolarCavity == null) return;

            if (co2Emitter != null)
            {
                co2Emitter.transform.position = vesselSurface.position;
                co2Emitter.transform.rotation = Quaternion.LookRotation(alveolarCavity.position - vesselSurface.position);
            }

            if (o2Emitter != null)
            {
                o2Emitter.transform.position = alveolarCavity.position;
                o2Emitter.transform.rotation = Quaternion.LookRotation(vesselSurface.position - alveolarCavity.position);
            }
        }
    }
}
