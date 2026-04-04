using AICharacterModule.NPC.StateMachine.Data;
using AICharacterModule.NPC.StateMachine.Managers;
using AICharacterModule.NPC.StateMachine.States;
using AICharacterModule.NPC.StateMachine.SubMachines;
using System;
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

        public event Action chaseAnimationCycleEndingEvent;

        private StateMachineManager<NPCGlobalData> _masterStateMachine;

        private void Awake()
        {
            var navAgent = GetComponent<NavMeshAgent>();

            var globalData = new NPCGlobalData
            {
                BehaviourController = this,
                NpcTransform = transform,
                NavAgent = navAgent,
                CurrentTarget = target,
                Anim = GetComponent<Animator>(),
                DetectionRange = navAgent.stoppingDistance + 60f,
                AttackRange = navAgent.stoppingDistance
            };

            _masterStateMachine = new StateMachineManager<NPCGlobalData>(globalData);

            // Navigation State machine
            var navigationStateManager = new StateManager<NavigationData, NPCGlobalData>(new NavigationData(globalData), _masterStateMachine);
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
            combatStateManager.RegisterState("CombatCircle", new CombatCircleState());

            var combatSubMachine = new SubStateMachine<CombatData, NPCGlobalData>("Combat", "CombatCircle", combatStateManager);
            
            // Master state machine
            _masterStateMachine.RegisterSubMachine(navigationSubMachine);
            _masterStateMachine.RegisterSubMachine(combatSubMachine);

            _masterStateMachine.RegisterTransition(
                "Navigation",
                "Combat",
                data => navigationStateManager.CurrentStateName == "Chase" && ShouldEnterCombatCircleFromChase(data));
            _masterStateMachine.RegisterTransition(
                "Combat",
                "Navigation",
                data => !HasTargetWithinRange(data, data.DetectionRange));

            _masterStateMachine.SwitchTo("Navigation");
        }

        private void Update()
        {
            _masterStateMachine.GlobalData.NavAgent.speed = _masterStateMachine.GlobalData.Anim.GetFloat("Speed");
            _masterStateMachine.GlobalData.CurrentTarget = target;
            _masterStateMachine.Tick(Time.deltaTime);
            _masterStateMachine.GlobalData.Tick(Time.deltaTime);
        }


        public void AttackAnimationCompleted()
        {
            if (_masterStateMachine == null)
            {
                Debug.Log("AttackAnimationCompleted failed");
                return;
            }
            Debug.Log("AttackAnimationCompleted");
            _masterStateMachine.GlobalData.IsAttacking = false;
        }

        public void ChaseAnimationCycleEnding()
        {
            chaseAnimationCycleEndingEvent?.Invoke();
        }

        private static bool HasTargetWithinRange(NPCGlobalData data, float range)
        {
            if (data.CurrentTarget == null)
            {
                return false;
            }

            return Vector3.Distance(data.NpcTransform.position, data.CurrentTarget.position) <= range;
        }


        private static bool ShouldEnterCombatCircleFromChase(NPCGlobalData data)
        {
            if (data.CurrentTarget == null)
            {
                return false;
            }

            float distance = Vector3.Distance(data.NpcTransform.position, data.CurrentTarget.position);
            float targetSpeed = data.GetTargetVelocity().magnitude;

            return distance >= 10f && distance <= 20f && targetSpeed < 0.05f;
        }

        private void OnAnimatorMove()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(_masterStateMachine.GlobalData.Anim.velocity);
            _masterStateMachine.GlobalData.Anim.SetFloat("Speed", localVelocity.z);
            _masterStateMachine.GlobalData.NpcLastVelocity = _masterStateMachine.GlobalData.Anim.velocity;
        }
    }
}
