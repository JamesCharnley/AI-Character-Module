using System.Collections.Generic;
using AICharacterModule.NPC.StateMachine.Core;

namespace AICharacterModule.NPC.StateMachine.Managers
{
    /// <summary>
    /// Normal state machine used by each sub-state machine.
    /// </summary>
    public class StateManager<TLocalData, TGlobalData>
    {
        private readonly Dictionary<string, IState<TLocalData, TGlobalData>> _states = new();
        private IState<TLocalData, TGlobalData> _current;

        public StateManager(TLocalData localData, StateMachineManager<TGlobalData> machineManager)
        {
            LocalData = localData;
            MachineManager = machineManager;
        }

        public TLocalData LocalData { get; }

        public StateMachineManager<TGlobalData> MachineManager { get; }

        public TGlobalData GlobalData => MachineManager.GlobalData;

        public void RegisterState(string name, IState<TLocalData, TGlobalData> state)
        {
            _states[name] = state;
        }

        public bool SwitchTo(string name)
        {
            if (!_states.TryGetValue(name, out var next) || next == _current)
            {
                return false;
            }

            _current?.Exit(LocalData, GlobalData);
            _current = next;
            _current.Enter(LocalData, GlobalData);
            return true;
        }

        public void Tick(float deltaTime)
        {
            _current?.Tick(LocalData, GlobalData, deltaTime);
        }

        public void ExitCurrent()
        {
            _current?.Exit(LocalData, GlobalData);
            _current = null;
        }
    }
}
