using System;
using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>Body systems that can be peeled away independently.</summary>
    public enum AnatomyLayerGroup
    {
        Skin,
        Muscular,
        Skeletal,
        Organs,
        Circulatory,
        Nervous
    }

    /// <summary>
    /// Shows and hides whole body systems the way a dissection peels back layers:
    /// drop the skin and muscle to study the skeleton, or hide everything except the
    /// nervous system to trace it. Membership is derived from the EntityID prefix, so
    /// any part added later is classified automatically with no extra wiring.
    /// </summary>
    public class AnatomyLayerVisibility : MonoBehaviour
    {
        /// <summary>Ordered outermost-inward, which is the order a dissection removes them.</summary>
        public static readonly AnatomyLayerGroup[] AllGroups =
        {
            AnatomyLayerGroup.Skin,
            AnatomyLayerGroup.Muscular,
            AnatomyLayerGroup.Skeletal,
            AnatomyLayerGroup.Organs,
            AnatomyLayerGroup.Circulatory,
            AnatomyLayerGroup.Nervous
        };

        private readonly Dictionary<AnatomyLayerGroup, List<GameObject>> _members =
            new Dictionary<AnatomyLayerGroup, List<GameObject>>();
        private readonly Dictionary<AnatomyLayerGroup, bool> _visible =
            new Dictionary<AnatomyLayerGroup, bool>();

        public event Action OnVisibilityChanged;

        private void Start() => Rebuild();

        public static AnatomyLayerGroup GroupFor(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return AnatomyLayerGroup.Organs;
            if (entityId.StartsWith("SYS_INTEG_", StringComparison.Ordinal)) return AnatomyLayerGroup.Skin;
            if (entityId.StartsWith("SYS_SK_", StringComparison.Ordinal)) return AnatomyLayerGroup.Skeletal;
            if (entityId.StartsWith("SYS_MUSC_", StringComparison.Ordinal)) return AnatomyLayerGroup.Muscular;
            if (entityId.StartsWith("SYS_NERV_", StringComparison.Ordinal)) return AnatomyLayerGroup.Nervous;
            if (entityId.StartsWith("SYS_CV_", StringComparison.Ordinal)) return AnatomyLayerGroup.Circulatory;
            return AnatomyLayerGroup.Organs;
        }

        public static string DisplayName(AnatomyLayerGroup group)
        {
            switch (group)
            {
                case AnatomyLayerGroup.Skin: return "Skin";
                case AnatomyLayerGroup.Muscular: return "Muscle";
                case AnatomyLayerGroup.Skeletal: return "Skeleton";
                case AnatomyLayerGroup.Organs: return "Organs";
                case AnatomyLayerGroup.Circulatory: return "Vessels";
                default: return "Nerves";
            }
        }

        public void Rebuild()
        {
            foreach (var group in AllGroups)
            {
                if (!_members.TryGetValue(group, out var list))
                {
                    list = new List<GameObject>();
                    _members[group] = list;
                }
                list.Clear();

                if (!_visible.ContainsKey(group)) _visible[group] = true;
            }

            var nodes = FindObjectsByType<AnatomyNodeReference>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var node in nodes)
            {
                _members[GroupFor(node.EntityId)].Add(node.gameObject);
            }

            foreach (var group in AllGroups) Apply(group);
        }

        public bool IsVisible(AnatomyLayerGroup group) =>
            !_visible.TryGetValue(group, out bool visible) || visible;

        public int CountIn(AnatomyLayerGroup group) =>
            _members.TryGetValue(group, out var list) ? list.Count : 0;

        public void SetVisible(AnatomyLayerGroup group, bool visible)
        {
            _visible[group] = visible;
            Apply(group);
            OnVisibilityChanged?.Invoke();
        }

        public void Toggle(AnatomyLayerGroup group) => SetVisible(group, !IsVisible(group));

        private void Apply(AnatomyLayerGroup group)
        {
            if (!_members.TryGetValue(group, out var list)) return;
            bool visible = IsVisible(group);

            foreach (var go in list)
            {
                if (go == null) continue;

                var partRenderer = go.GetComponent<Renderer>();
                if (partRenderer != null) partRenderer.enabled = visible;

                // A hidden layer must not swallow clicks meant for what it was covering.
                var partCollider = go.GetComponent<Collider>();
                if (partCollider != null) partCollider.enabled = visible;
            }
        }
    }
}
