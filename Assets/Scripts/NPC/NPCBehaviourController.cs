using AICharacterModule.NPC.StateMachine.Data;
using AICharacterModule.NPC.StateMachine.Managers;
using AICharacterModule.NPC.StateMachine.States;
using AICharacterModule.NPC.StateMachine.SubMachines;
using UnityEngine;
using UnityEngine.AI;

namespace AICharacterModule.NPC
{
    /// <summary>
    /// Example wiring for a hierarchical NPC state machine.
    /// Master machine chooses between navigation and combat sub-machines.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCBehaviourController : MonoBehaviour
    {
        [SerializeField] private Transform target;

        private StateMachineManager<NPCGlobalData> _masterStateMachine;
        private StateManager<NavigationData, NPCGlobalData> _navigationStateManager;
        private StateManager<CombatData, NPCGlobalData> _combatStateManager;

        private void Awake()
        {
            var navAgent = GetComponent<NavMeshAgent>();

            var globalData = new NPCGlobalData
            {
                NpcTransform = transform,
                NavAgent = navAgent,
                CurrentTarget = target,
                DetectionRange = navAgent.stoppingDistance + 8f,
                AttackRange = navAgent.stoppingDistance
            };

            _masterStateMachine = new StateMachineManager<NPCGlobalData>(globalData);

            var navigationStateManager = new StateManager<NavigationData, NPCGlobalData>(new NavigationData(), _masterStateMachine);
            navigationStateManager.RegisterState("Patrol", new PatrolState());
            navigationStateManager.RegisterState("Chase", new ChaseState());
            navigationStateManager.RegisterTransition(
                "Patrol",
                "Chase",
                (_, data) => HasTargetWithinRange(data, data.DetectionRange));
            navigationStateManager.RegisterTransition(
                "Chase",
                "Patrol",
                (_, data) => !HasTargetWithinRange(data, data.DetectionRange));

            var combatStateManager = new StateManager<CombatData, NPCGlobalData>(new CombatData(), _masterStateMachine);
            combatStateManager.RegisterState("Attack", new AttackState());

            var navigationSubMachine = new SubStateMachine<NavigationData, NPCGlobalData>("Navigation", "Patrol", navigationStateManager);
            var combatSubMachine = new SubStateMachine<CombatData, NPCGlobalData>("Combat", "Attack", combatStateManager);

            _masterStateMachine.RegisterSubMachine(navigationSubMachine);
            _masterStateMachine.RegisterSubMachine(combatSubMachine);

            _masterStateMachine.RegisterTransition(
                "Navigation",
                "Combat",
                data => HasTargetWithinRange(data, data.AttackRange));
            _masterStateMachine.RegisterTransition(
                "Combat",
                "Navigation",
                data => !HasTargetWithinRange(data, data.AttackRange));

            _navigationStateManager = new StateManager<NavigationData, NPCGlobalData>(new NavigationData(), _masterStateMachine);
            _navigationStateManager.RegisterState("Patrol", new PatrolState());
            _navigationStateManager.RegisterState("Chase", new ChaseState());

            _combatStateManager = new StateManager<CombatData, NPCGlobalData>(new CombatData(), _masterStateMachine);
            _combatStateManager.RegisterState("Attack", new AttackState());

            var navigationSubMachine = new SubStateMachine<NavigationData, NPCGlobalData>("Navigation", "Patrol", _navigationStateManager);
            var combatSubMachine = new SubStateMachine<CombatData, NPCGlobalData>("Combat", "Attack", _combatStateManager);

            _masterStateMachine.RegisterSubMachine(navigationSubMachine);
            _masterStateMachine.RegisterSubMachine(combatSubMachine);
            _masterStateMachine.SwitchTo("Navigation");
        }

        private void Update()
        {
            _masterStateMachine.GlobalData.CurrentTarget = target;
            _masterStateMachine.Tick(Time.deltaTime);
        }

        private static bool HasTargetWithinRange(NPCGlobalData data, float range)
        {
            if (data.CurrentTarget == null)
            {
                return false;
            }

            return Vector3.Distance(data.NpcTransform.position, data.CurrentTarget.position) <= range;
            var globalData = _masterStateMachine.GlobalData;
            globalData.CurrentTarget = target;

            if (target == null)
            {
                _navigationStateManager.SwitchTo("Patrol");
                _masterStateMachine.SwitchTo("Navigation");
                _masterStateMachine.Tick(Time.deltaTime);
                return;
            }

            var distance = Vector3.Distance(transform.position, target.position);
            if (distance <= globalData.AttackRange)
            {
                _masterStateMachine.SwitchTo("Combat");
            }
            else if (distance <= globalData.DetectionRange)
            {
                _navigationStateManager.SwitchTo("Chase");
                _masterStateMachine.SwitchTo("Navigation");
            }
            else
            {
                _navigationStateManager.SwitchTo("Patrol");
                _masterStateMachine.SwitchTo("Navigation");
            }

            _masterStateMachine.Tick(Time.deltaTime);
        }
    }
}
