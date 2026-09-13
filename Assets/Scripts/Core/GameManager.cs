using HumanBodyExplorer.Data;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Application-wide singleton exposing the composed subsystems once
    /// GameBootstrapper has finished wiring the DI container.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentGameState { get; private set; } = GameState.Initializing;
        public AppStateMachine StateMachine { get; private set; }
        public DiContainer Container { get; private set; }

        public IDataController DataController { get; private set; }
        public IViewController ViewController { get; private set; }
        public IInputController InputController { get; private set; }
        public IDatabaseManager DatabaseManager { get; private set; }
        public IStudyTracker StudyTracker { get; private set; }
        public IAssetStreamingManager AssetStreamingManager { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            StateMachine = new AppStateMachine();
        }

        public void ConfigureContainer(
            DiContainer container,
            IDataController dataController,
            IViewController viewController,
            IInputController inputController,
            IDatabaseManager databaseManager,
            IStudyTracker studyTracker,
            IAssetStreamingManager assetStreamingManager)
        {
            Container = container;
            DataController = dataController;
            ViewController = viewController;
            InputController = inputController;
            DatabaseManager = databaseManager;
            StudyTracker = studyTracker;
            AssetStreamingManager = assetStreamingManager;
        }

        public void SetGameState(GameState newState)
        {
            CurrentGameState = newState;
            Debug.Log($"[GameManager] GameState -> {newState}");

            switch (newState)
            {
                case GameState.FreeRoam:
                    StateMachine.ChangeState(new MacroExplorationState());
                    break;
                case GameState.MicroDive:
                    StateMachine.ChangeState(new MicroSimulationState());
                    break;
                case GameState.QuizMode:
                    StateMachine.ChangeState(new ClinicalQuizState());
                    break;
                case GameState.AssemblyMode:
                    StateMachine.ChangeState(new MacroExplorationState());
                    break;
                case GameState.Initializing:
                    StateMachine.ChangeState(new MainMenuState());
                    break;
            }
        }

        private void Update()
        {
            StateMachine?.Tick();
        }

        private void OnDestroy()
        {
            DatabaseManager?.Shutdown();
        }
    }
}
