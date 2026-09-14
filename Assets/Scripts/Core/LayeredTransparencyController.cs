using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    public enum AnatomySystemLayer
    {
        Skeleton,
        Muscles,
        Skin
    }

    /// <summary>
    /// Phase 26: avoids Z-fighting between simultaneously-transparent anatomy
    /// systems by giving each system category a distinct render queue once alpha
    /// drops below 1, guaranteeing inner structures (skeleton) render before outer
    /// ones (skin).
    /// </summary>
    public class LayeredTransparencyController : MonoBehaviour
    {
        private const int SkeletonQueue = 3000;
        private const int MuscleQueue = 3010;
        private const int SkinQueue = 3020;
        private const int OpaqueQueue = 2000;

        private readonly Dictionary<AnatomySystemLayer, List<Renderer>> _renderersByLayer =
            new Dictionary<AnatomySystemLayer, List<Renderer>>();

        public void RegisterRenderer(AnatomySystemLayer layer, Renderer renderer)
        {
            if (!_renderersByLayer.TryGetValue(layer, out var list))
            {
                list = new List<Renderer>();
                _renderersByLayer[layer] = list;
            }
            list.Add(renderer);
        }

        public void SetSystemAlpha(AnatomySystemLayer layer, float alpha)
        {
            if (!_renderersByLayer.TryGetValue(layer, out var renderers)) return;

            int queue = alpha < 1f ? QueueForLayer(layer) : OpaqueQueue;

            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.sharedMaterial == null) continue;

                renderer.sharedMaterial.renderQueue = queue;

                if (renderer.sharedMaterial.HasProperty("_Alpha"))
                {
                    renderer.sharedMaterial.SetFloat("_Alpha", alpha);
                }
            }
        }

        private static int QueueForLayer(AnatomySystemLayer layer) => layer switch
        {
            AnatomySystemLayer.Skeleton => SkeletonQueue,
            AnatomySystemLayer.Muscles => MuscleQueue,
            AnatomySystemLayer.Skin => SkinQueue,
            _ => OpaqueQueue
        };
    }
}
