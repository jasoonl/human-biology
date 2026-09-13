using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace HumanBodyExplorer.Data
{
    public interface IDataController
    {
        Task<Dictionary<string, AnatomyNode>> LoadDataAsync(string path);
        AnatomyNode GetNode(string entityId);
        IReadOnlyDictionary<string, AnatomyNode> AllNodes { get; }
    }

    /// <summary>
    /// Loads the anatomy dictionary JSON (array of AnatomyNode) into an O(1) lookup
    /// dictionary keyed by EntityID.
    /// </summary>
    public class DataController : IDataController
    {
        private Dictionary<string, AnatomyNode> _cache = new Dictionary<string, AnatomyNode>();

        public IReadOnlyDictionary<string, AnatomyNode> AllNodes => _cache;

        public async Task<Dictionary<string, AnatomyNode>> LoadDataAsync(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[DataController] Anatomy JSON not found at {path}");
                _cache = new Dictionary<string, AnatomyNode>();
                return _cache;
            }

            string json = await File.ReadAllTextAsync(path);
            var nodes = JsonConvert.DeserializeObject<List<AnatomyNode>>(json) ?? new List<AnatomyNode>();

            var result = new Dictionary<string, AnatomyNode>(nodes.Count);
            foreach (var node in nodes)
            {
                if (string.IsNullOrEmpty(node.EntityID))
                {
                    Debug.LogWarning("[DataController] Skipping anatomy node with empty EntityID.");
                    continue;
                }
                result[node.EntityID] = node;
            }

            _cache = result;
            return _cache;
        }

        public AnatomyNode GetNode(string entityId)
        {
            if (_cache.TryGetValue(entityId, out var node))
            {
                return node;
            }

            throw new AnatomyNotFoundException(entityId);
        }
    }
}
