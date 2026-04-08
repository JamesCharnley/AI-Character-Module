using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Managers;

namespace AICharacterModule.NPC.StateMachine.SubMachines
{
    public class SubStateMachine<TLocalData, TGlobalData> : ISubStateMachine<TGlobalData>
    {
        private readonly string _entryState;

        public SubStateMachine(string name, string entryState, StateManager<TLocalData, TGlobalData> stateManager)
        {
            Name = name;
            _entryState = entryState;
            StateManager = stateManager;
        }

        public string Name { get; }

        public StateManager<TLocalData, TGlobalData> StateManager { get; }

        public TGlobalData GlobalData => StateManager.GlobalData;
        public bool IsLocked => StateManager.IsCurrentStateLocked;

        public void Enter()
        {
            StateManager.SwitchTo(_entryState);
        }

        public void Tick(float deltaTime)
        {
            StateManager.Tick(deltaTime);
        }

        public void Exit()
        {
            StateManager.ExitCurrent();
        }
    }
}
