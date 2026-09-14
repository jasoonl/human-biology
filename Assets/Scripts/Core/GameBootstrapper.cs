using System.IO;
using System.Threading.Tasks;
using HumanBodyExplorer.Data;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Entry point placed on a bootstrap scene's root GameObject. Constructs the DI
    /// container, initializes DataController/ViewController/InputController
    /// concurrently via Task.WhenAll so the main thread isn't blocked, then hands
    /// control to GameManager.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private string anatomyJsonRelativePath = "Data/anatomy_dictionary.json";

        private async void Awake()
        {
            var gameManagerGO = new GameObject("GameManager");
            var gameManager = gameManagerGO.AddComponent<GameManager>();
            DontDestroyOnLoad(gameManagerGO);

            gameManager.SetGameState(GameState.Initializing);

            var container = new DiContainer();

            var dataController = new DataController();
            var viewController = new ViewController();
            var inputController = new HumanBodyExplorer.Input.InputManager();
            var databaseManager = new DatabaseManager();
            var assetStreamingManager = new AssetStreamingManager();

            container.Bind<IDataController>().ToInstance(dataController);
            container.Bind<IViewController>().ToInstance(viewController);
            container.Bind<IInputController>().ToInstance(inputController);
            container.Bind<IDatabaseManager>().ToInstance(databaseManager);
            container.Bind<IAssetStreamingManager>().ToInstance(assetStreamingManager);

            databaseManager.Initialize();
            var studyTracker = new StudyTracker(databaseManager);
            container.Bind<IStudyTracker>().ToInstance(studyTracker);

            string jsonPath = Path.Combine(Application.streamingAssetsPath, anatomyJsonRelativePath);

            Task dataTask = dataController.LoadDataAsync(jsonPath);
            Task viewTask = viewController.InitializeAsync();
            Task inputTask = inputController.InitializeAsync();

            await Task.WhenAll(dataTask, viewTask, inputTask);

            gameManager.ConfigureContainer(
                container, dataController, viewController, inputController,
                databaseManager, studyTracker, assetStreamingManager);

            Debug.Log($"[GameBootstrapper] Boot complete. {dataController.AllNodes.Count} anatomy nodes loaded.");

            gameManager.SetGameState(GameState.FreeRoam);
        }
    }
}
