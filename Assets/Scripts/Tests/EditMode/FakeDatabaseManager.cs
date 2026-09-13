using System.Collections.Generic;
using HumanBodyExplorer.Data;

namespace HumanBodyExplorer.Tests
{
    /// <summary>
    /// In-memory stand-in for IDatabaseManager, used in place of the Moq-mocked
    /// dependency the spec calls for (Moq/NuGet-for-Unity isn't available offline
    /// in this environment).
    /// </summary>
    public class FakeDatabaseManager : IDatabaseManager
    {
        private readonly Dictionary<string, UserProgressRow> _rows = new Dictionary<string, UserProgressRow>();

        public void Initialize() { }
        public void Shutdown() { }

        public UserProgressRow? GetProgress(string nodeId)
        {
            return _rows.TryGetValue(nodeId, out var row) ? row : (UserProgressRow?)null;
        }

        public void UpsertProgress(UserProgressRow row)
        {
            _rows[row.NodeID] = row;
        }

        public List<UserProgressRow> GetAllProgress()
        {
            return new List<UserProgressRow>(_rows.Values);
        }
    }
}
