using System.Collections.Generic;
using AICharacterModule.NPC.StateMachine.Core;

namespace AICharacterModule.NPC.StateMachine.Managers
{
    /// <summary>
    /// Master state machine that treats sub-state machines as states.
    /// </summary>
    public class StateMachineManager<TGlobalData>
    {
        private readonly Dictionary<string, ISubStateMachine<TGlobalData>> _subMachines = new();
        private ISubStateMachine<TGlobalData> _current;

        public StateMachineManager(TGlobalData globalData)
        {
            GlobalData = globalData;
        }

        public TGlobalData GlobalData { get; }

        public string CurrentSubMachineName => _current?.Name;

        public void RegisterSubMachine(ISubStateMachine<TGlobalData> subMachine)
        {
            _subMachines[subMachine.Name] = subMachine;
        }

        public bool SwitchTo(string name)
        {
            if (!_subMachines.TryGetValue(name, out var next) || next == _current)
            {
                return false;
            }

            _current?.Exit();
            _current = next;
            _current.Enter();
            return true;
        }

        public void Tick(float deltaTime)
        {
            _current?.Tick(deltaTime);
        }
    }
}
