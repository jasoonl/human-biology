using System.Threading.Tasks;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Owns the New Input System action maps. Fully implemented in Module II
    /// (InputManager, Phase 11); stubbed here so GameBootstrapper can initialize it
    /// alongside DataController and ViewController via Task.WhenAll.
    /// </summary>
    public interface IInputController
    {
        Task InitializeAsync();
    }

    public class InputController : IInputController
    {
        public Task InitializeAsync()
        {
            UnityEngine.Debug.Log("[InputController] Initialized.");
            return Task.CompletedTask;
        }
    }
}
