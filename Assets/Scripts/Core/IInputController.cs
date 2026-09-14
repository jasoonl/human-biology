using System.Threading.Tasks;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Owns the New Input System action maps. Implemented by
    /// HumanBodyExplorer.Input.InputManager (Module II, Phase 11).
    /// </summary>
    public interface IInputController
    {
        Task InitializeAsync();
    }
}
