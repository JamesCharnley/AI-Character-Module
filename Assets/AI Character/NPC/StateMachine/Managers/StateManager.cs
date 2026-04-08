using System;
using System.Collections.Generic;
using AICharacterModule.NPC.StateMachine.Core;

namespace AICharacterModule.NPC.StateMachine.Managers
{
    /// <summary>
    /// Normal state machine used by each sub-state machine.
    /// Handles transitions between states in a sub-state machine.
    /// </summary>
    public class StateManager<TLocalData, TGlobalData>
    {
        private readonly struct Transition
        {
            public Transition(string fromState, string toState, Func<TLocalData, TGlobalData, bool> condition)
            {
                FromState = fromState;
                ToState = toState;
                Condition = condition;
            }

            public string FromState { get; }
            public string ToState { get; }
            public Func<TLocalData, TGlobalData, bool> Condition { get; }
        }

        private readonly Dictionary<string, IState<TLocalData, TGlobalData>> _states = new();
        private readonly List<Transition> _transitions = new();
        private IState<TLocalData, TGlobalData> _current;

        public StateManager(TLocalData localData, StateMachineManager<TGlobalData> machineManager)
        {
            LocalData = localData;
            MachineManager = machineManager;
        }

        public TLocalData LocalData { get; }

        public StateMachineManager<TGlobalData> MachineManager { get; }

        public TGlobalData GlobalData => MachineManager.GlobalData;

        public string CurrentStateName { get; private set; }
        public bool IsCurrentStateLocked => _current?.IsLocked ?? false;

        public void RegisterState(string name, IState<TLocalData, TGlobalData> state)
        {
            _states[name] = state;
        }

        public void RegisterTransition(string fromState, string toState, Func<TLocalData, TGlobalData, bool> condition)
        {
            _transitions.Add(new Transition(fromState, toState, condition));
        }

        public bool SwitchTo(string name)
        {
            if (!_states.TryGetValue(name, out var next) || next == _current)
            {
                return false;
            }
            
            if (_current != null && _current.IsLocked)
            {
                return false;
            }

            _current?.Exit(LocalData, GlobalData);
            _current = next;
            CurrentStateName = name;
            _current.Enter(LocalData, GlobalData);
            return true;
        }

        public void Tick(float deltaTime)
        {
            EvaluateTransitions();
            _current?.Tick(LocalData, GlobalData, deltaTime);
        }

        public void ExitCurrent()
        {
            _current?.Exit(LocalData, GlobalData);
            _current = null;
            CurrentStateName = null;
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
                if (transition.FromState != CurrentStateName)
                {
                    continue;
                }

                if (!transition.Condition(LocalData, GlobalData))
                {
                    continue;
                }

                SwitchTo(transition.ToState);
                return;
            }
        }
    }
}
