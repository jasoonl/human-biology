using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.MicroSim
{
    public enum Neurotransmitter
    {
        Dopamine,
        Serotonin,
        Glutamate,
        GABA
    }

    /// <summary>
    /// Phase 48: when an action potential reaches the axon terminal, plays vesicle
    /// fusion and emits neurotransmitter-colored particles that drift across the
    /// cleft to the postsynaptic receptors.
    /// </summary>
    public class SynapticCleftManager : MonoBehaviour
    {
        [SerializeField] private ParticleSystem vesicleParticles;
        [SerializeField] private Transform postsynapticReceptors;

        private static readonly Dictionary<Neurotransmitter, Color> ColorMap = new Dictionary<Neurotransmitter, Color>
        {
            { Neurotransmitter.Dopamine, Color.green },
            { Neurotransmitter.Serotonin, Color.blue },
            { Neurotransmitter.Glutamate, Color.yellow },
            { Neurotransmitter.GABA, Color.magenta }
        };

        public static Color GetColorFor(Neurotransmitter type) => ColorMap[type];

        public void ReleaseNeurotransmitter(Neurotransmitter type)
        {
            if (vesicleParticles == null) return;

            var main = vesicleParticles.main;
            main.startColor = GetColorFor(type);

            if (postsynapticReceptors != null)
            {
                vesicleParticles.transform.LookAt(postsynapticReceptors);
            }

            vesicleParticles.Play();
        }
    }
}
