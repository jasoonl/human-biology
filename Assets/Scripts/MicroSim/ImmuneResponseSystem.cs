using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.MicroSim
{
    /// <summary>
    /// Phases 42+43 (reduced scope - per-Transform Update loop instead of ECS/DOTS
    /// Physics + Burst Jobs, see Planning.md M4). White blood cells flock toward a
    /// detected pathogen using classic Cohesion/Alignment/Separation boid rules
    /// plus a pathogen-attraction term, then trigger phagocytosis on contact.
    /// </summary>
    public class ImmuneResponseSystem : MonoBehaviour
    {
        [SerializeField] private List<Transform> whiteBloodCells = new List<Transform>();
        [SerializeField] private Transform pathogen;
        [SerializeField] private ParticleSystem phagocytosisBurstPrefab;

        [SerializeField] private float moveSpeed = 1f;
        [SerializeField] private float neighborRadius = 1f;
        [SerializeField] private float separationRadius = 0.3f;
        [SerializeField] private float pathogenAttractionWeight = 2f;
        [SerializeField] private float contactThreshold = 0.15f;
        [SerializeField] private float engulfDuration = 0.5f;

        private bool _pathogenEngulfed;

        /// <summary>Pure function so the boid math is unit-testable without a live scene.</summary>
        public static Vector3 ComputeBoidForce(Vector3 selfPos, IReadOnlyList<Vector3> neighborPositions,
            Vector3 pathogenPos, float neighborRadius, float separationRadius, float pathogenAttractionWeight)
        {
            Vector3 cohesion = Vector3.zero;
            Vector3 alignmentSum = Vector3.zero;
            Vector3 separation = Vector3.zero;
            int neighborCount = 0;

            foreach (var neighborPos in neighborPositions)
            {
                float dist = Vector3.Distance(selfPos, neighborPos);
                if (dist < 0.0001f || dist > neighborRadius) continue;

                cohesion += neighborPos;
                neighborCount++;

                if (dist < separationRadius)
                {
                    separation += (selfPos - neighborPos) / dist;
                }
            }

            Vector3 force = Vector3.zero;
            if (neighborCount > 0)
            {
                cohesion = (cohesion / neighborCount - selfPos).normalized;
                force += cohesion * 0.5f;
                force += separation.normalized * (separation.sqrMagnitude > 0.0001f ? 1f : 0f);
            }

            Vector3 towardPathogen = (pathogenPos - selfPos).normalized;
            force += towardPathogen * pathogenAttractionWeight;

            return force.normalized;
        }

        private void Update()
        {
            if (pathogen == null || _pathogenEngulfed) return;

            var positions = new List<Vector3>(whiteBloodCells.Count);
            foreach (var wbc in whiteBloodCells) if (wbc != null) positions.Add(wbc.position);

            foreach (var wbc in whiteBloodCells)
            {
                if (wbc == null) continue;

                Vector3 force = ComputeBoidForce(wbc.position, positions, pathogen.position,
                    neighborRadius, separationRadius, pathogenAttractionWeight);

                wbc.position += force * moveSpeed * Time.deltaTime;

                if (Vector3.Distance(wbc.position, pathogen.position) < contactThreshold)
                {
                    TriggerPhagocytosis(wbc);
                    return;
                }
            }
        }

        private void TriggerPhagocytosis(Transform wbc)
        {
            _pathogenEngulfed = true;
            StartCoroutine(EngulfCoroutine(wbc));
        }

        private System.Collections.IEnumerator EngulfCoroutine(Transform wbc)
        {
            Vector3 startScale = pathogen.localScale;
            Vector3 startPos = pathogen.position;
            float elapsed = 0f;

            while (elapsed < engulfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / engulfDuration;
                pathogen.position = Vector3.Lerp(startPos, wbc.position, t);
                pathogen.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            if (phagocytosisBurstPrefab != null)
            {
                var burst = Instantiate(phagocytosisBurstPrefab, wbc.position, Quaternion.identity);
                burst.Play();
            }

            pathogen.gameObject.SetActive(false);
        }
    }
}
