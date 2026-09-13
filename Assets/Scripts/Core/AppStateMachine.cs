using UnityEngine;

namespace HumanBodyExplorer.Core
{
    public interface IState
    {
        void Enter();
        void Execute();
        void Exit();
    }

    public class AppStateMachine
    {
        private IState _currentState;

        public IState CurrentState => _currentState;

        public void ChangeState(IState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }

        public void Tick()
        {
            _currentState?.Execute();
        }
    }

    public class MainMenuState : IState
    {
        public void Enter() => Debug.Log("[AppStateMachine] Entered MainMenuState");
        public void Execute() { }
        public void Exit() => Debug.Log("[AppStateMachine] Exited MainMenuState");
    }

    public class MacroExplorationState : IState
    {
        public void Enter() => Debug.Log("[AppStateMachine] Entered MacroExplorationState");
        public void Execute() { }
        public void Exit() => Debug.Log("[AppStateMachine] Exited MacroExplorationState");
    }

    public class MicroSimulationState : IState
    {
        public void Enter() => Debug.Log("[AppStateMachine] Entered MicroSimulationState");
        public void Execute() { }
        public void Exit() => Debug.Log("[AppStateMachine] Exited MicroSimulationState");
    }

    public class ClinicalQuizState : IState
    {
        public void Enter() => Debug.Log("[AppStateMachine] Entered ClinicalQuizState");
        public void Execute() { }
        public void Exit() => Debug.Log("[AppStateMachine] Exited ClinicalQuizState");
    }
}
