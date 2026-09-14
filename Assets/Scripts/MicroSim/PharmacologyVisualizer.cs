using System.Collections;
using UnityEngine;

namespace HumanBodyExplorer.MicroSim
{
    /// <summary>
    /// Phase 44: eases a drug molecule toward a cell receptor, and on contact
    /// triggers a particle burst plus an emission color change on the receptor to
    /// signify the mechanism of action.
    /// </summary>
    public class PharmacologyVisualizer : MonoBehaviour
    {
        [SerializeField] private Transform receptor;
        [SerializeField] private Transform drugMolecule;
        [SerializeField] private ParticleSystem bindingBurstPrefab;
        [SerializeField] private Renderer receptorRenderer;
        [SerializeField] private Color boundEmissionColor = Color.green;
        [SerializeField] private float approachDuration = 1.5f;
        [SerializeField] private float bindDistance = 0.05f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public void PlayBindingSequence()
        {
            StartCoroutine(ApproachAndBind());
        }

        private IEnumerator ApproachAndBind()
        {
            if (drugMolecule == null || receptor == null) yield break;

            Vector3 startPos = drugMolecule.position;
            float elapsed = 0f;

            while (elapsed < approachDuration)
            {
                elapsed += Time.deltaTime;
                float t = EaseIn(Mathf.Clamp01(elapsed / approachDuration));
                drugMolecule.position = Vector3.Lerp(startPos, receptor.position, t);

                if (Vector3.Distance(drugMolecule.position, receptor.position) < bindDistance)
                {
                    OnBind();
                    yield break;
                }

                yield return null;
            }

            OnBind();
        }

        private void OnBind()
        {
            if (bindingBurstPrefab != null)
            {
                Instantiate(bindingBurstPrefab, receptor.position, Quaternion.identity).Play();
            }

            if (receptorRenderer != null)
            {
                var block = new MaterialPropertyBlock();
                receptorRenderer.GetPropertyBlock(block);
                block.SetColor(EmissionColorId, boundEmissionColor);
                receptorRenderer.SetPropertyBlock(block);
            }
        }

        private static float EaseIn(float t) => t * t;
    }
}
