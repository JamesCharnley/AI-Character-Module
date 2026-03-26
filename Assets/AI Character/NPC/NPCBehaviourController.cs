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

        private void Awake()
        {
            var navAgent = GetComponent<NavMeshAgent>();

            var globalData = new NPCGlobalData
            {
                NpcTransform = transform,
                NavAgent = navAgent,
                CurrentTarget = target,
                Anim = GetComponent<Animator>(),
                DetectionRange = navAgent.stoppingDistance + 12f,
                AttackRange = navAgent.stoppingDistance
            };

            _masterStateMachine = new StateMachineManager<NPCGlobalData>(globalData);

            // Navigation State machine
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
            
            var navigationSubMachine = new SubStateMachine<NavigationData, NPCGlobalData>("Navigation", "Patrol", navigationStateManager);
            
            
            // Combat state machine
            var combatStateManager = new StateManager<CombatData, NPCGlobalData>(new CombatData(), _masterStateMachine);
            combatStateManager.RegisterState("Attack", new AttackState());

            
            var combatSubMachine = new SubStateMachine<CombatData, NPCGlobalData>("Combat", "Attack", combatStateManager);
            
            // Master state machine
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
        }
    }
}
