using System;
using System.Collections.Generic;
using AICharacterModule.NPC.StateMachine.Core;

namespace AICharacterModule.NPC.StateMachine.Managers
{
    /// <summary>
    /// Master state machine that treats sub-state machines as states.
    /// Handles transitions between sub-state machines.
    /// </summary>
    public class StateMachineManager<TGlobalData>
    {
        private readonly struct Transition
        {
            public Transition(string fromSubMachine, string toSubMachine, Func<TGlobalData, bool> condition)
            {
                FromSubMachine = fromSubMachine;
                ToSubMachine = toSubMachine;
                Condition = condition;
            }

            public string FromSubMachine { get; }
            public string ToSubMachine { get; }
            public Func<TGlobalData, bool> Condition { get; }
        }

        private readonly Dictionary<string, ISubStateMachine<TGlobalData>> _subMachines = new();
        private readonly List<Transition> _transitions = new();
        private ISubStateMachine<TGlobalData> _current;

        public StateMachineManager(TGlobalData globalData)
        {
            GlobalData = globalData;
        }

        public TGlobalData GlobalData { get; }

        public string CurrentSubMachineName { get; private set; }

        public void RegisterSubMachine(ISubStateMachine<TGlobalData> subMachine)
        {
            _subMachines[subMachine.Name] = subMachine;
        }

        public void RegisterTransition(string fromSubMachine, string toSubMachine, Func<TGlobalData, bool> condition)
        {
            _transitions.Add(new Transition(fromSubMachine, toSubMachine, condition));
        }

        public bool SwitchTo(string name)
        {
            if (!_subMachines.TryGetValue(name, out var next) || next == _current)
            {
                return false;
            }

            if (_current != null && _current.IsLocked) return false;
            _current?.Exit();
            _current = next;
            CurrentSubMachineName = name;
            _current.Enter();
            return true;
        }

        public void Tick(float deltaTime)
        {
            EvaluateTransitions();
            _current?.Tick(deltaTime);
        }

        private void EvaluateTransitions()
        {
            if (_current == null)
            {
                return;
            }

            for (var i = 0; i < _transitions.Count; i++)
            {
                var transition = _transitions[i];
                if (transition.FromSubMachine != CurrentSubMachineName)
                {
                    continue;
                }

                if (!transition.Condition(GlobalData))
                {
                    continue;
                }

                SwitchTo(transition.ToSubMachine);
                return;
            }
        }
    }
}
