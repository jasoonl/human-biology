using System.Threading.Tasks;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Owns top-level scene/camera/UI wiring. Fleshed out further in Module II/V;
    /// stubbed to a trivial async no-op here so GameBootstrapper can initialize it
    /// alongside DataController and InputController via Task.WhenAll.
    /// </summary>
    public interface IViewController
    {
        Task InitializeAsync();
    }

    public class ViewController : IViewController
    {
        public Task InitializeAsync()
        {
            UnityEngine.Debug.Log("[ViewController] Initialized.");
            return Task.CompletedTask;
        }
    }
}
